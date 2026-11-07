using ClinIQ.Domain.Common;

namespace ClinIQ.Domain.Appointments;

public enum AppointmentOperationKind { Book = 1, Cancel = 2, Reschedule = 3 }
public sealed class AppointmentOperation : AuditableEntity
{
    public Guid PatientId { get; private set; }
    public Guid RequestId { get; private set; }
    public AppointmentOperationKind Kind { get; private set; }
    public string RequestHash { get; private set; } = null!;
    public Guid ResultAppointmentId { get; private set; }
    private AppointmentOperation() { }
    private AppointmentOperation(Guid id) : base(id) { }
    public static AppointmentOperation Create(Guid patient, Guid request, AppointmentOperationKind kind, string hash, Guid result)
    {
        if (patient == Guid.Empty || request == Guid.Empty || result == Guid.Empty || !Enum.IsDefined(kind) || hash.Length != 64 || !hash.All(Uri.IsHexDigit)) throw new ArgumentException("Invalid operation identity or digest.");
        return new(Guid.NewGuid()) { PatientId = patient, RequestId = request, Kind = kind, RequestHash = hash, ResultAppointmentId = result };
    }
}
public enum BookingNoticeKind { Booked = 1, Cancelled = 2, Rescheduled = 3 }
public sealed class BookingNotice : AuditableEntity
{
    public Guid AppointmentId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public BookingNoticeKind Kind { get; private set; }
    public string DeliveryStatus { get; private set; } = "Pending";
    public int Attempts { get; private set; }
    public DateTimeOffset? LastAttemptUtc { get; private set; }
    public Guid? ClaimId { get; private set; }
    private BookingNotice() { }
    private BookingNotice(Guid id) : base(id) { }
    public static BookingNotice Create(Guid appointment, Guid recipient, BookingNoticeKind kind)
    {
        if (appointment == Guid.Empty || recipient == Guid.Empty || !Enum.IsDefined(kind)) throw new ArgumentException("Invalid booking notice.");
        return new(Guid.NewGuid()) { AppointmentId = appointment, RecipientUserId = recipient, Kind = kind };
    }
}
