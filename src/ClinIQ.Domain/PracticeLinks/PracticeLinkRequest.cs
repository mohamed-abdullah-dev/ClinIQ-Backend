using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Clinics;
using ClinIQ.Domain.PracticeLocations;

namespace ClinIQ.Domain.PracticeLinks;

public enum PracticeLinkStatus { Pending = 1, Accepted = 2, Rejected = 3, Withdrawn = 4 }

public sealed class PracticeLinkRequest : Entity
{
    public const string CurrentTerms = "SamePremises-ManagedFee-NewSessionsOnly-v1";
    public Guid LocationId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid DoctorUserId { get; private set; }
    public string Terms { get; private set; } = CurrentTerms;
    public byte[] LocationVersion { get; private set; } = [];
    public byte[] BranchVersion { get; private set; } = [];
    public string LocationName { get; private set; } = null!;
    public string LocationAddress { get; private set; } = null!;
    public string BranchName { get; private set; } = null!;
    public string BranchAddress { get; private set; } = null!;
    public decimal ConsultationFee { get; private set; }
    public string Currency { get; private set; } = "EGP";
    public DateTimeOffset RequestedAtUtc { get; private set; }
    public PracticeLinkStatus Status { get; private set; } = PracticeLinkStatus.Pending;
    public Guid? DecidedByUserId { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    public string? DecisionReason { get; private set; }
    public byte[] Version { get; private set; } = [];
    private PracticeLinkRequest() { }
    private PracticeLinkRequest(Guid id) : base(id) { }

    public static Result<PracticeLinkRequest> Create(DoctorPracticeLocation place, ClinicBranch branch,
        Guid actor, bool samePremises, bool acceptsTerms, DateTimeOffset now)
    {
        if (actor == Guid.Empty || actor != place.UserId) return PracticeLinkErrors.Forbidden;
        if (!samePremises || !acceptsTerms) return PracticeLinkErrors.Consent;
        if (!Eligible(place, branch)) return PracticeLinkErrors.Unavailable;
        if (place.Version.Length != 8 || branch.Version.Length != 8) throw new ArgumentException("Persisted source versions are required.");
        return new PracticeLinkRequest(Guid.NewGuid())
        {
            LocationId = place.Id, BranchId = branch.Id, DoctorUserId = actor,
            LocationVersion = place.Version.ToArray(), BranchVersion = branch.Version.ToArray(),
            LocationName = place.Name, LocationAddress = place.Address, BranchName = branch.Name, BranchAddress = branch.Address,
            ConsultationFee = place.ConsultationFee, Currency = place.Currency, RequestedAtUtc = now
        };
    }
    public static bool Eligible(DoctorPracticeLocation place, ClinicBranch branch) => place.ClinicBranchId is null &&
        place.IsActive && place.Status == PracticeLocationStatus.Approved && place.LifecycleState == PracticeLifecycleState.Active &&
        branch.IsActive && branch.LifecycleState == BranchLifecycleState.Open;

    // Manager authority is checked against live membership by the application service. One account may hold both roles.
    public Result<Success> Decide(Guid actor, PracticeLinkStatus outcome, string reason, bool samePremises, bool acceptsTerms, DateTimeOffset now)
    {
        if (actor == Guid.Empty) return PracticeLinkErrors.Forbidden;
        if (Status != PracticeLinkStatus.Pending) return PracticeLinkErrors.Closed;
        if (outcome is not (PracticeLinkStatus.Accepted or PracticeLinkStatus.Rejected or PracticeLinkStatus.Withdrawn) ||
            string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000) return PracticeLinkErrors.Input;
        if (outcome == PracticeLinkStatus.Withdrawn && actor != DoctorUserId) return PracticeLinkErrors.Forbidden;
        if (outcome == PracticeLinkStatus.Accepted && (!samePremises || !acceptsTerms)) return PracticeLinkErrors.Consent;
        if (now < RequestedAtUtc) return PracticeLinkErrors.Input;
        Status = outcome; DecidedByUserId = actor; DecidedAtUtc = now; DecisionReason = reason.Trim();
        return Result.Success;
    }
}

public static class PracticeLinkErrors
{
    public static readonly Error Unauthorized = Error.Unauthorized("PracticeLink.Unauthorized", "Sign in first.");
    public static readonly Error Forbidden = Error.Forbidden("PracticeLink.Forbidden", "A confirmed eligible Web account is required.");
    public static readonly Error NotFound = Error.NotFound("PracticeLink.NotFound", "The requested link or scope is not available.");
    public static readonly Error Unavailable = Error.Conflict("PracticeLink.Unavailable", "An active approved independent location and an open approved branch are required.");
    public static readonly Error Duplicate = Error.Conflict("PracticeLink.Duplicate", "This doctor already has an association with the branch. Automatic merging is not supported.");
    public static readonly Error Pending = Error.Conflict("PracticeLink.Pending", "This location already has a pending link request.");
    public static readonly Error Stale = Error.Conflict("PracticeLink.Stale", "The request, place or branch changed. Withdraw and submit a fresh request if its sources changed.");
    public static readonly Error Closed = Error.Conflict("PracticeLink.Closed", "This request already has a final decision.");
    public static readonly Error Busy = Error.Conflict("PracticeLink.Busy", "Another operation is in progress. Retry.");
    public static readonly Error Consent = Error.Validation("PracticeLink.Consent", "Explicitly confirm the same premises and accept managed fee control and new-session-only scope.", "Consent");
    public static readonly Error Input = Error.Validation("PracticeLink.Input", "Provide valid versions, an explicit decision and a reason of 1 to 2000 characters.", "Input");
}
