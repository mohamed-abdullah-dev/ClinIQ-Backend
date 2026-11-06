using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.Scheduling;

public sealed class ScheduleException : AuditableEntity
{
    public Guid DoctorProfileId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? LocationId { get; private set; }
    public DateOnly LocalDate { get; private set; }
    public string Reason { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public string? RemovalReason { get; private set; }
    public Guid? RemovedByUserId { get; private set; }
    public DateTimeOffset? RemovedAtUtc { get; private set; }
    public byte[] Version { get; private set; } = [];
    private ScheduleException() { }
    private ScheduleException(Guid id) : base(id) { }
    public static Result<ScheduleException> Create(Guid doctor, Guid user, Guid? location, DateOnly date, string reason)
    {
        if (doctor == Guid.Empty || user == Guid.Empty || location == Guid.Empty) throw new ArgumentException("Valid exception ownership is required.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000) return ScheduleErrors.Reason;
        return new ScheduleException(Guid.NewGuid()) { DoctorProfileId = doctor, UserId = user, LocationId = location, LocalDate = date, Reason = reason.Trim() };
    }
    public Result<Success> Remove(Guid actor, string reason, DateTimeOffset now)
    {
        if (!IsActive) return ScheduleErrors.Stale;
        if (actor != UserId) return ScheduleErrors.Forbidden;
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000) return ScheduleErrors.Reason;
        IsActive = false; RemovalReason = reason.Trim(); RemovedByUserId = actor; RemovedAtUtc = now; return Result.Success;
    }
}
