using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.Appointments;

public enum AttendanceAction { CheckIn = 1, Complete = 2, NoShow = 3, NotSeen = 4, Correct = 5, LateCheckIn = 6 }
public sealed record AttendanceInput(Guid ConfirmedPatientId, AttendanceAction Action, string Version,
    DateTimeOffset? ArrivedAtUtc = null, DateTimeOffset? CompletedAtUtc = null, AppointmentStatus? CorrectedStatus = null, string? Reason = null);
public sealed record AttendanceFacts(AppointmentStatus Status, int? QueueNumber, DateTimeOffset? ArrivedAtUtc,
    DateTimeOffset? CheckInRecordedAtUtc, DateTimeOffset? CompletedAtUtc, DateTimeOffset? CompletionRecordedAtUtc,
    Guid? LateAuthorizedByUserId, DateTimeOffset? LateAuthorizedAtUtc);
public sealed record AttendanceChange(bool Changed, AttendanceFacts Before, AttendanceFacts After);

public sealed partial class Appointment
{
    public int? QueueNumber { get; private set; }
    public int AttendanceRevision { get; private set; }
    public DateTimeOffset? ArrivedAtUtc { get; private set; }
    public DateTimeOffset? CheckInRecordedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? CompletionRecordedAtUtc { get; private set; }
    public Guid? LateAuthorizedByUserId { get; private set; }
    public DateTimeOffset? LateAuthorizedAtUtc { get; private set; }
    public AttendanceFacts Attendance => new(Status, QueueNumber, ArrivedAtUtc, CheckInRecordedAtUtc, CompletedAtUtc, CompletionRecordedAtUtc, LateAuthorizedByUserId, LateAuthorizedAtUtc);

    public bool IsAttendanceReplay(AttendanceInput input) => input.Action switch
    {
        AttendanceAction.CheckIn => Status == AppointmentStatus.CheckedIn,
        AttendanceAction.Complete => Status == AppointmentStatus.Completed && input.CompletedAtUtc == CompletedAtUtc,
        AttendanceAction.NoShow => Status == AppointmentStatus.NoShow,
        AttendanceAction.NotSeen => Status == AppointmentStatus.AttendedNotSeen,
        AttendanceAction.LateCheckIn => Status == AppointmentStatus.CheckedIn && LateAuthorizedByUserId.HasValue && input.ArrivedAtUtc == ArrivedAtUtc,
        _ => false
    };

    public Result<AttendanceChange> RecordAttendance(AttendanceInput input, Guid actor, bool responsibleDoctor, bool cancelledSession, DateTimeOffset now, int? proposedQueueNumber)
    {
        if (actor == Guid.Empty) throw new ArgumentException("An attendance actor is required.");
        if (input.ConfirmedPatientId != PatientId) return AttendanceErrors.PatientConfirmation;
        if (!Enum.IsDefined(input.Action)) return AttendanceErrors.Facts;
        if (input.Action == AttendanceAction.LateCheckIn && (!responsibleDoctor || actor != DoctorUserId)) return AttendanceErrors.DoctorRequired;
        var before = Attendance;
        if (IsAttendanceReplay(input)) return new AttendanceChange(false, before, before);
        if (Status is AppointmentStatus.Cancelled or AppointmentStatus.Rescheduled) return AttendanceErrors.State;
        var reasonRequired = input.Action is AttendanceAction.NotSeen or AttendanceAction.Correct or AttendanceAction.LateCheckIn;
        if (reasonRequired && (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length > 2000)) return AttendanceErrors.Reason;
        var after = before;
        switch (input.Action)
        {
            case AttendanceAction.CheckIn:
                if (Status != AppointmentStatus.Confirmed) return AttendanceErrors.State;
                if (now < StartsAtUtc) return AttendanceErrors.NotStarted;
                if (now >= EndsAtUtc) return AttendanceErrors.Late;
                after = WithArrival(before, now, now, proposedQueueNumber); break;
            case AttendanceAction.LateCheckIn:
                if (now < EndsAtUtc || Status is not (AppointmentStatus.Confirmed or AppointmentStatus.NoShow)) return AttendanceErrors.State;
                if (!ValidArrival(input.ArrivedAtUtc, now)) return AttendanceErrors.Facts;
                after = WithArrival(before, input.ArrivedAtUtc!.Value, now, proposedQueueNumber) with { LateAuthorizedByUserId = actor, LateAuthorizedAtUtc = now }; break;
            case AttendanceAction.Complete:
                if (Status != AppointmentStatus.CheckedIn || ArrivedAtUtc is null) return AttendanceErrors.State;
                if (!ValidCompletion(input.CompletedAtUtc, ArrivedAtUtc.Value, now)) return AttendanceErrors.Facts;
                after = before with { Status = AppointmentStatus.Completed, CompletedAtUtc = input.CompletedAtUtc, CompletionRecordedAtUtc = now }; break;
            case AttendanceAction.NoShow:
                if (now < EndsAtUtc) return AttendanceErrors.NotEnded;
                if (Status != AppointmentStatus.Confirmed || ArrivedAtUtc.HasValue) return AttendanceErrors.State;
                after = before with { Status = AppointmentStatus.NoShow }; break;
            case AttendanceAction.NotSeen:
                if (Status != AppointmentStatus.CheckedIn || ArrivedAtUtc is null) return AttendanceErrors.State;
                after = before with { Status = AppointmentStatus.AttendedNotSeen }; break;
            case AttendanceAction.Correct:
                if (input.CorrectedStatus is not (AppointmentStatus.Confirmed or AppointmentStatus.CheckedIn or AppointmentStatus.Completed or AppointmentStatus.NoShow or AppointmentStatus.AttendedNotSeen)) return AttendanceErrors.State;
                var target = input.CorrectedStatus.Value;
                if (target == AppointmentStatus.NoShow && now < EndsAtUtc) return AttendanceErrors.NotEnded;
                if (target is AppointmentStatus.CheckedIn or AppointmentStatus.Completed or AppointmentStatus.AttendedNotSeen)
                {
                    if (ArrivedAtUtc.HasValue)
                    {
                        if (input.ArrivedAtUtc.HasValue && input.ArrivedAtUtc != ArrivedAtUtc) return AttendanceErrors.PreserveArrival;
                    }
                    else
                    {
                        if (!ValidArrival(input.ArrivedAtUtc, now)) return AttendanceErrors.Facts;
                        // A missed historical in-window fact can be corrected; a genuine late arrival needs its doctor's action.
                        if (input.ArrivedAtUtc >= EndsAtUtc) return AttendanceErrors.Late;
                        after = WithArrival(before, input.ArrivedAtUtc!.Value, now, proposedQueueNumber);
                    }
                    if (target == AppointmentStatus.Completed)
                    {
                        if (!ValidCompletion(input.CompletedAtUtc, after.ArrivedAtUtc!.Value, now)) return AttendanceErrors.Facts;
                        after = after with { CompletedAtUtc = input.CompletedAtUtc, CompletionRecordedAtUtc = before.CompletedAtUtc == input.CompletedAtUtc ? before.CompletionRecordedAtUtc : now };
                    }
                    else after = after with { CompletedAtUtc = null, CompletionRecordedAtUtc = null };
                    after = after with { Status = target };
                }
                else after = before with { Status = target, ArrivedAtUtc = null, CheckInRecordedAtUtc = null, CompletedAtUtc = null, CompletionRecordedAtUtc = null, LateAuthorizedByUserId = null, LateAuthorizedAtUtc = null };
                break;
            default: return AttendanceErrors.Facts;
        }
        if (cancelledSession && (input.Action != AttendanceAction.Correct || after.Status is AppointmentStatus.Confirmed or AppointmentStatus.CheckedIn)) return AttendanceErrors.Closed;
        if (after.ArrivedAtUtc.HasValue && (!after.QueueNumber.HasValue || after.QueueNumber <= 0)) throw new ArgumentException("An arrival requires an authoritative queue number.");
        if (after == before) return new AttendanceChange(false, before, before);
        Status = after.Status; QueueNumber = after.QueueNumber; ArrivedAtUtc = after.ArrivedAtUtc; CheckInRecordedAtUtc = after.CheckInRecordedAtUtc;
        AttendanceRevision = checked(AttendanceRevision + 1);
        CompletedAtUtc = after.CompletedAtUtc; CompletionRecordedAtUtc = after.CompletionRecordedAtUtc; LateAuthorizedByUserId = after.LateAuthorizedByUserId; LateAuthorizedAtUtc = after.LateAuthorizedAtUtc;
        return new AttendanceChange(true, before, Attendance);
    }
    private AttendanceFacts WithArrival(AttendanceFacts before, DateTimeOffset arrived, DateTimeOffset now, int? queue) => before with
    { Status = AppointmentStatus.CheckedIn, QueueNumber = before.QueueNumber ?? queue, ArrivedAtUtc = arrived, CheckInRecordedAtUtc = now, CompletedAtUtc = null, CompletionRecordedAtUtc = null };
    private bool ValidArrival(DateTimeOffset? arrived, DateTimeOffset now) => arrived.HasValue && arrived >= StartsAtUtc && arrived <= now && arrived.Value.Offset == TimeSpan.Zero;
    private static bool ValidCompletion(DateTimeOffset? completed, DateTimeOffset arrived, DateTimeOffset now) => completed.HasValue && completed >= arrived && completed <= now && completed.Value.Offset == TimeSpan.Zero;
}

public static class AttendanceErrors
{
    public static readonly Error Unauthorized = Error.Unauthorized("Attendance.Unauthorized", "Please log in to continue.");
    public static readonly Error Forbidden = Error.Forbidden("Attendance.Forbidden", "A confirmed scoped Doctor or Receptionist is required.");
    public static readonly Error NotFound = Error.NotFound("Attendance.NotFound", "The selected doctor/session/appointment is outside your operational scope.");
    public static readonly Error DoctorRequired = Error.Forbidden("Attendance.ResponsibleDoctorRequired", "Only the responsible doctor can authorize a new late check-in.");
    public static readonly Error PatientConfirmation = Error.Conflict("Attendance.PatientConfirmation", "Confirm the selected appointment's patient before recording this action.");
    public static readonly Error State = Error.Conflict("Attendance.State", "This transition is not allowed for the appointment's current state.");
    public static readonly Error Facts = Error.Validation("Attendance.Facts", "Provide valid factual UTC timestamps without future events or completion before arrival.");
    public static readonly Error PreserveArrival = Error.Conflict("Attendance.PreserveArrival", "Keep the genuine recorded arrival and its existing queue position.");
    public static readonly Error Late = Error.Conflict("Attendance.LateAuthorizationRequired", "A new late check-in requires the responsible doctor's explicit action and reason.");
    public static readonly Error NotStarted = Error.Conflict("Attendance.NotStarted", "Check-in opens at the advertised session start.");
    public static readonly Error NotEnded = Error.Conflict("Attendance.NotEnded", "NoShow cannot be recorded before the advertised session end.");
    public static readonly Error Closed = Error.Conflict("Attendance.Closed", "This action would reopen outstanding work in a cancelled session.");
    public static readonly Error Stale = Error.Conflict("Attendance.StaleVersion", "Reload the appointment and retry with its current version.");
    public static readonly Error Busy = Error.Conflict("Attendance.Busy", "The session or assignment is being updated. Retry the request.");
    public static readonly Error Reason = Error.Validation("Attendance.Reason", "Provide a reason of 1 to 2000 characters.", "Reason");
}
