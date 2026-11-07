using ClinIQ.Domain.Common;

namespace ClinIQ.Domain.Appointments;

public sealed class AttendanceRecord : Entity
{
    public Guid AppointmentId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid DoctorProfileId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public int Revision { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public AttendanceAction Action { get; private set; }
    public string Reason { get; private set; } = null!;
    public AppointmentStatus PreviousStatus { get; private set; }
    public AppointmentStatus NewStatus { get; private set; }
    public string BeforeFactsJson { get; private set; } = null!;
    public string AfterFactsJson { get; private set; } = null!;
    public byte[] PreviousVersion { get; private set; } = [];
    private AttendanceRecord() { }
    private AttendanceRecord(Guid id) : base(id) { }
    public static AttendanceRecord Create(Appointment a, Guid actor, DateTimeOffset now, AttendanceInput input, AttendanceChange change, string beforeJson, string afterJson, byte[] previousVersion)
    {
        if (!change.Changed || actor == Guid.Empty || previousVersion.Length != 8 || string.IsNullOrWhiteSpace(beforeJson) || string.IsNullOrWhiteSpace(afterJson)) throw new ArgumentException("An immutable record requires a real transition and its source version.");
        return new(Guid.NewGuid())
        {
            AppointmentId = a.Id,
            SessionId = a.SessionId,
            DoctorProfileId = a.DoctorProfileId,
            PatientId = a.PatientId,
            ActorUserId = actor,
            Revision = a.AttendanceRevision,
            RecordedAtUtc = now,
            Action = input.Action,
            Reason = string.IsNullOrWhiteSpace(input.Reason) ? input.Action.ToString() : input.Reason.Trim(),
            PreviousStatus = change.Before.Status,
            NewStatus = change.After.Status,
            BeforeFactsJson = beforeJson,
            AfterFactsJson = afterJson,
            PreviousVersion = previousVersion.ToArray()
        };
    }
}
