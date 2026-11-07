using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Doctors;
using ClinIQ.Domain.PracticeLocations;

namespace ClinIQ.Domain.Appointments;

public enum AppointmentStatus { Confirmed = 1, CheckedIn = 2, Completed = 3, Cancelled = 4, Rescheduled = 5, NoShow = 6, AttendedNotSeen = 7 }
public sealed record AppointmentSelection(Guid SessionId, Guid LocationId, Guid DoctorProfileId, Guid? ClinicId, Guid? BranchId,
    string DoctorName, ProfessionalGrade Grade, Guid SpecialtyId, string SpecialtyAr, string SpecialtyEn,
    string PlaceName, string Address, DateOnly LocalDate, TimeOnly StartsLocal, TimeOnly EndsLocal, string TimeZoneId,
    DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, DateTimeOffset BookingClosesAtUtc,
    decimal Fee, string Currency, string ContactName, string ContactPhone);

public sealed partial class Appointment : AuditableEntity
{
    public Guid PatientId { get; private set; }
    public Guid PatientUserId { get; private set; }
    public Guid DoctorUserId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid LocationId { get; private set; }
    public Guid DoctorProfileId { get; private set; }
    public Guid? ClinicId { get; private set; }
    public Guid? BranchId { get; private set; }
    public string DoctorName { get; private set; } = null!;
    public ProfessionalGrade Grade { get; private set; }
    public Guid SpecialtyId { get; private set; }
    public string SpecialtyAr { get; private set; } = null!;
    public string SpecialtyEn { get; private set; } = null!;
    public string PlaceName { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public DateOnly LocalDate { get; private set; }
    public TimeOnly StartsLocal { get; private set; }
    public TimeOnly EndsLocal { get; private set; }
    public string TimeZoneId { get; private set; } = null!;
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset EndsAtUtc { get; private set; }
    public DateTimeOffset BookingClosesAtUtc { get; private set; }
    public decimal Fee { get; private set; }
    public string Currency { get; private set; } = null!;
    public string ContactName { get; private set; } = null!;
    public string ContactPhone { get; private set; } = null!;
    public AppointmentStatus Status { get; private set; } = AppointmentStatus.Confirmed;
    public Guid? ReplacesAppointmentId { get; private set; }
    public AppointmentStatus? StatusBeforeCancellation { get; private set; }
    public Guid? CancelledByUserId { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? CancellationCategory { get; private set; }
    public byte[] Version { get; private set; } = [];
    public bool ConsumesCapacity => Status is not (AppointmentStatus.Cancelled or AppointmentStatus.Rescheduled);
    public AppointmentSelection Selection => new(SessionId, LocationId, DoctorProfileId, ClinicId, BranchId, DoctorName, Grade, SpecialtyId,
        SpecialtyAr, SpecialtyEn, PlaceName, Address, LocalDate, StartsLocal, EndsLocal, TimeZoneId, StartsAtUtc, EndsAtUtc, BookingClosesAtUtc, Fee, Currency, ContactName, ContactPhone);

    private Appointment() { }
    private Appointment(Guid id) : base(id) { }
    public static Result<Appointment> Create(Guid patient, Guid patientUser, Guid doctorUser, AppointmentSelection s, Guid? replaces = null)
    {
        if (patient == Guid.Empty || patientUser == Guid.Empty || doctorUser == Guid.Empty || s.SessionId == Guid.Empty || s.LocationId == Guid.Empty || s.DoctorProfileId == Guid.Empty || replaces == Guid.Empty)
            throw new ArgumentException("A booking requires authoritative patient, doctor and session identities.");
        var price = ConsultationPrice.Create(s.Fee, s.Currency); if (price.IsError) return Result<Appointment>.Failure(price.Errors);
        var contact = Patients.PatientProfile.Create(patientUser, s.ContactName, s.ContactPhone); if (contact.IsError) return Result<Appointment>.Failure(contact.Errors);
        if (s.EndsAtUtc <= s.StartsAtUtc || s.EndsLocal <= s.StartsLocal || s.TimeZoneId != "Africa/Cairo" ||
            s.StartsAtUtc.Offset != TimeSpan.Zero || s.EndsAtUtc.Offset != TimeSpan.Zero || s.BookingClosesAtUtc > s.EndsAtUtc)
            throw new ArgumentException("The accepted session window must be valid.");
        return new Appointment(Guid.NewGuid())
        {
            PatientId = patient,
            PatientUserId = patientUser,
            DoctorUserId = doctorUser,
            SessionId = s.SessionId,
            LocationId = s.LocationId,
            DoctorProfileId = s.DoctorProfileId,
            ClinicId = s.ClinicId,
            BranchId = s.BranchId,
            DoctorName = s.DoctorName,
            Grade = s.Grade,
            SpecialtyId = s.SpecialtyId,
            SpecialtyAr = s.SpecialtyAr,
            SpecialtyEn = s.SpecialtyEn,
            PlaceName = s.PlaceName,
            Address = s.Address,
            LocalDate = s.LocalDate,
            StartsLocal = s.StartsLocal,
            EndsLocal = s.EndsLocal,
            TimeZoneId = s.TimeZoneId,
            StartsAtUtc = s.StartsAtUtc,
            EndsAtUtc = s.EndsAtUtc,
            BookingClosesAtUtc = s.BookingClosesAtUtc,
            Fee = price.Value.Amount,
            Currency = price.Value.Currency,
            ContactName = contact.Value.FullName,
            ContactPhone = contact.Value.PhoneNumber,
            ReplacesAppointmentId = replaces
        };
    }
    public Result<Success> CanChangeByPatient(Guid actor, DateTimeOffset now)
    {
        if (actor != PatientUserId) return AppointmentErrors.NotFound;
        return Status == AppointmentStatus.Confirmed && now < EndsAtUtc ? Result.Success : AppointmentErrors.CannotChange;
    }
    public Result<Success> CancelByPatient(Guid actor, string reason, DateTimeOffset now)
    {
        if (actor != PatientUserId) return AppointmentErrors.NotFound;
        if (Status == AppointmentStatus.Cancelled) return Result.Success;
        var allowed = CanChangeByPatient(actor, now); if (allowed.IsError) return allowed;
        return Cancel(actor, reason, "PatientRequest", now);
    }
    public Result<Success> CancelByDoctor(Guid actor, string reason, DateTimeOffset now)
    {
        if (actor != DoctorUserId) return AppointmentErrors.Forbidden;
        if (Status is not (AppointmentStatus.Confirmed or AppointmentStatus.CheckedIn)) return AppointmentErrors.CannotChange;
        return Cancel(actor, reason, "DoctorSessionDisruption", now);
    }
    private Result<Success> Cancel(Guid actor, string reason, string category, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000) return AppointmentErrors.Reason;
        StatusBeforeCancellation = Status; Status = AppointmentStatus.Cancelled; CancelledByUserId = actor; CancelledAtUtc = now;
        CancellationReason = reason.Trim(); CancellationCategory = category; return Result.Success;
    }
    public Result<Success> CancelForEmergency(PracticeLifecycle.OperationalEmergency request, Guid? currentBranchId, DateTimeOffset now)
    {
        if (request.Status != PracticeLifecycle.EmergencyRequestStatus.Approved || request.ReviewerUserId is not Guid reviewer ||
            !(request.LocationId == LocationId || (request.BranchId.HasValue && (request.BranchId == BranchId || request.BranchId == currentBranchId))))
            return PracticeLifecycle.LifecycleErrors.Forbidden;
        if (Status is not (AppointmentStatus.Confirmed or AppointmentStatus.CheckedIn)) return PracticeLifecycle.LifecycleErrors.State;
        // Preserve factual arrival, queue and the previous status even for a disruption recorded after session end.
        return Cancel(reviewer, request.ReviewReason!, "AdminEmergencyClosure", now);
    }
    public Result<Success> Reschedule(Guid actor, DateTimeOffset now)
    {
        var allowed = CanChangeByPatient(actor, now); if (allowed.IsError) return allowed;
        Status = AppointmentStatus.Rescheduled; return Result.Success;
    }
}

public static class AppointmentErrors
{
    public static readonly Error Unauthorized = Error.Unauthorized("Appointment.Unauthorized", "Please log in to continue.");
    public static readonly Error Forbidden = Error.Forbidden("Appointment.Forbidden", "The current account cannot perform this operation.");
    public static readonly Error NotFound = Error.NotFound("Appointment.NotFound", "The appointment or session was not found in your scope.");
    public static readonly Error Busy = Error.Conflict("Appointment.Busy", "The booking is being updated. Retry with the same request ID.");
    public static readonly Error Closed = Error.Conflict("Appointment.Closed", "The selected session is no longer accepting reservations.");
    public static readonly Error Duplicate = Error.Conflict("Appointment.Duplicate", "You already hold a place in this session.");
    public static readonly Error CannotChange = Error.Conflict("Appointment.CannotChange", "Only an unchecked-in confirmed booking before its session end can be changed by its patient.");
    public static readonly Error SelectionChanged = Error.Conflict("Appointment.SelectionChanged", "The selection, contact or overlapping bookings changed. Preview and explicitly accept the current details.");
    public static readonly Error Quote = Error.Conflict("Appointment.QuoteExpiredOrInvalid", "Preview this selection again before confirming it.");
    public static readonly Error Acceptance = Error.Validation("Appointment.AcceptanceRequired", "Explicitly accept the price and confirm your contact details.");
    public static readonly Error Overlap = Error.Conflict("Appointment.OverlapAcknowledgementRequired", "The session overlaps another doctor's booking. Explicitly acknowledge that attendance is not guaranteed.");
    public static readonly Error RequestReuse = Error.Conflict("Appointment.RequestIdReused", "This request ID was already used with different input.");
    public static readonly Error SameDoctor = Error.Validation("Appointment.SameDoctorRequired", "Reschedule to a different session with the same doctor; changing doctors requires a new booking.");
    public static readonly Error Reason = Error.Validation("Appointment.Reason", "Provide a cancellation reason of 1 to 2000 characters.", "Reason");
    public static readonly Error Stale = Error.Conflict("Appointment.StaleVersion", "Reload the current version before retrying.");
}
