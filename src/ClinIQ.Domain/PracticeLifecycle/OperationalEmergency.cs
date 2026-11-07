using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.PracticeLifecycle;

public enum EmergencyReasonCategory { Illness = 1, UnusablePremises = 2 }
public enum EmergencyRequestStatus { Pending = 1, Approved = 2, Rejected = 3 }
public sealed class OperationalEmergency : Entity
{
    public Guid? LocationId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid RequesterUserId { get; private set; }
    public EmergencyReasonCategory Category { get; private set; }
    public string Reason { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public EmergencyRequestStatus Status { get; private set; } = EmergencyRequestStatus.Pending;
    public Guid? ReviewerUserId { get; private set; }
    public string? ReviewReason { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? ReviewedPreviewHash { get; private set; }
    public byte[]? ReviewedScopeVersion { get; private set; }
    public int? CancelledCount { get; private set; }
    public byte[] Version { get; private set; } = [];
    private OperationalEmergency() { }
    private OperationalEmergency(Guid id) : base(id) { }
    public static Result<OperationalEmergency> Create(OperationalScopeKind kind, Guid scope, Guid actor, EmergencyReasonCategory category, string reason, DateTimeOffset now)
    {
        if (scope == Guid.Empty || actor == Guid.Empty || !Enum.IsDefined(kind)) throw new ArgumentException("An authoritative scope and actor are required.");
        if (!Enum.IsDefined(category) || string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000) return LifecycleErrors.Input;
        return new OperationalEmergency(Guid.NewGuid())
        {
            LocationId = kind == OperationalScopeKind.Location ? scope : null,
            BranchId = kind == OperationalScopeKind.Branch ? scope : null,
            RequesterUserId = actor, Category = category, Reason = reason.Trim(), CreatedAtUtc = now
        };
    }
    public Result<bool> Decide(bool approve, Guid actor, string reason, string previewHash, byte[] scopeVersion, int affectedCount, DateTimeOffset now)
    {
        if (actor == Guid.Empty || affectedCount < 0) throw new ArgumentException("A reviewer and authoritative affected count are required.");
        if (actor == RequesterUserId) return LifecycleErrors.SelfReview;
        if (Status != EmergencyRequestStatus.Pending) return LifecycleErrors.State;
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000 || previewHash.Length != 64 || !previewHash.All(Uri.IsHexDigit) || scopeVersion.Length != 8)
            return LifecycleErrors.Input;
        Status = approve ? EmergencyRequestStatus.Approved : EmergencyRequestStatus.Rejected;
        ReviewerUserId = actor; ReviewReason = reason.Trim(); ReviewedAtUtc = now;
        ReviewedPreviewHash = previewHash; ReviewedScopeVersion = scopeVersion.ToArray(); CancelledCount = approve ? affectedCount : 0;
        return true;
    }
}
