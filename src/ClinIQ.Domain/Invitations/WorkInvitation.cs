using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.PracticeLocations;

namespace ClinIQ.Domain.Invitations;

public enum WorkInvitationKind { Doctor = 1, Receptionist = 2 }
public enum WorkInvitationStatus { Pending = 1, Accepted = 2, Declined = 3, Revoked = 4, Expired = 5, Superseded = 6 }

public sealed class WorkInvitation : AuditableEntity
{
    public WorkInvitationKind Kind { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? LocationId { get; private set; }
    public Guid IssuerUserId { get; private set; }
    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;
    public Guid? TargetUserId { get; private set; }
    public string SecretHash { get; private set; } = null!;
    public string Terms { get; private set; } = null!;
    public decimal? ConsultationFee { get; private set; }
    public string? Currency { get; private set; }
    public WorkInvitationStatus Status { get; private set; } = WorkInvitationStatus.Pending;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public Guid? ClosedByUserId { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public string? ClosureReason { get; private set; }
    public Guid? ReplacesInvitationId { get; private set; }
    public string DeliveryStatus { get; private set; } = "Pending";
    public byte[] Version { get; private set; } = [];
    private WorkInvitation() { }
    private WorkInvitation(Guid id) : base(id) { }

    public static Result<WorkInvitation> Create(Guid id, WorkInvitationKind kind, Guid? branchId, Guid? locationId,
        Guid issuer, string email, string normalizedEmail, Guid? targetUserId, string hash, string terms,
        ConsultationPrice? price, DateTimeOffset now, Guid? replaces = null)
    {
        if (id == Guid.Empty || issuer == Guid.Empty || branchId == Guid.Empty || locationId == Guid.Empty ||
            branchId.HasValue == locationId.HasValue || !Enum.IsDefined(kind) || (kind == WorkInvitationKind.Doctor && !branchId.HasValue))
            throw new ArgumentException("A valid invitation kind, issuer and exactly one permitted scope are required.");
        if (hash.Length != 64 || !hash.All(Uri.IsHexDigit)) throw new ArgumentException("A secret hash is required.");
        if (string.IsNullOrWhiteSpace(email) || email.Length > 256 || string.IsNullOrWhiteSpace(normalizedEmail) || normalizedEmail.Length > 256 ||
            string.IsNullOrWhiteSpace(terms) || terms.Trim().Length > 2000 || (kind == WorkInvitationKind.Doctor) != (price is not null)) return InvitationErrors.InvalidDetails;
        return new WorkInvitation(id)
        {
            Kind = kind,
            BranchId = branchId,
            LocationId = locationId,
            IssuerUserId = issuer,
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            TargetUserId = targetUserId,
            SecretHash = hash,
            Terms = terms.Trim(),
            ConsultationFee = price?.Amount,
            Currency = price?.Currency,
            ExpiresAtUtc = now.AddDays(7),
            ReplacesInvitationId = replaces
        };
    }
    public Result<Success> Respond(Guid actor, string normalizedEmail, bool accept, DateTimeOffset now)
    {
        if (actor == Guid.Empty) throw new ArgumentException("A recipient is required.");
        if (NormalizedEmail != normalizedEmail || (TargetUserId.HasValue && TargetUserId != actor)) return InvitationErrors.NotFound;
        if (Status != WorkInvitationStatus.Pending) return InvitationErrors.Closed;
        if (now >= ExpiresAtUtc) return InvitationErrors.Expired;
        Status = accept ? WorkInvitationStatus.Accepted : WorkInvitationStatus.Declined;
        ClosedByUserId = actor; ClosedAtUtc = now; return Result.Success;
    }
    public Result<Success> Close(Guid actor, WorkInvitationStatus status, string reason, DateTimeOffset now)
    {
        if (actor == Guid.Empty || status is not (WorkInvitationStatus.Revoked or WorkInvitationStatus.Expired or WorkInvitationStatus.Superseded))
            throw new ArgumentException("A valid closing actor and status are required.");
        if (Status != WorkInvitationStatus.Pending) return InvitationErrors.Closed;
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000) return InvitationErrors.Reason;
        Status = status; ClosedByUserId = actor; ClosedAtUtc = now; ClosureReason = reason.Trim(); return Result.Success;
    }
}

public static class InvitationErrors
{
    public static readonly Error Unauthorized = Error.Unauthorized("Invitation.Unauthorized", "Sign in before continuing.");
    public static readonly Error Forbidden = Error.Forbidden("Invitation.Forbidden", "A confirmed eligible account and current scope permission are required.");
    public static readonly Error NotFound = Error.NotFound("Invitation.NotFound", "The invitation or authorized scope was not found.");
    public static readonly Error InvalidDetails = Error.Validation("Invitation.Details", "Provide a valid recipient email, terms and the appropriate EGP doctor fee.");
    public static readonly Error Reason = Error.Validation("Invitation.Reason", "Provide a reason of 1 to 2000 characters.", "Reason");
    public static readonly Error PendingExists = Error.Conflict("Invitation.PendingExists", "An invitation is already pending for this recipient and scope. Revoke or resend it.");
    public static readonly Error Closed = Error.Conflict("Invitation.Closed", "The invitation already has a final outcome.");
    public static readonly Error Expired = Error.Conflict("Invitation.Expired", "The invitation expired. Ask the issuer for a new invitation.");
    public static readonly Error AlreadyAssigned = Error.Conflict("Invitation.AlreadyAssigned", "This account already has an active association in that scope.");
    public static readonly Error Stale = Error.Conflict("Invitation.Stale", "The invitation or association changed. Reload before continuing.");
    public static readonly Error Busy = Error.Conflict("Invitation.Busy", "Another operation is in progress. Try again.");
    public static readonly Error Cooldown = Error.Conflict("Invitation.Cooldown", "Wait one minute before resending this invitation.");
    public static readonly Error DoctorRequired = Error.Forbidden("Invitation.DoctorRequired", "Doctor acceptance requires confirmed email and an approved professional profile.");
}
