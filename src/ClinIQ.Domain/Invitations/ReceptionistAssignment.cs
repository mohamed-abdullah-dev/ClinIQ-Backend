using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.Invitations;

public sealed class ReceptionistAssignment : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? LocationId { get; private set; }
    public Guid SourceInvitationId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid? RevokedByUserId { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevocationReason { get; private set; }
    public byte[] Version { get; private set; } = [];
    private ReceptionistAssignment() { }
    private ReceptionistAssignment(Guid id) : base(id) { }
    public static Result<ReceptionistAssignment> FromAccepted(WorkInvitation invitation)
    {
        if (invitation.Kind != WorkInvitationKind.Receptionist || invitation.Status != WorkInvitationStatus.Accepted || !invitation.ClosedByUserId.HasValue)
            return InvitationErrors.Closed;
        return new ReceptionistAssignment(Guid.NewGuid())
        {
            UserId = invitation.ClosedByUserId.Value,
            BranchId = invitation.BranchId,
            LocationId = invitation.LocationId,
            SourceInvitationId = invitation.Id
        };
    }
    public Result<Success> Revoke(Guid actor, string reason, DateTimeOffset now)
    {
        if (actor == Guid.Empty) throw new ArgumentException("A revoking actor is required.");
        if (!IsActive) return InvitationErrors.Closed;
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000) return InvitationErrors.Reason;
        IsActive = false; RevokedByUserId = actor; RevokedAtUtc = now; RevocationReason = reason.Trim(); return Result.Success;
    }
}

// Persist a price-change notice with the price write; delivery is attempted after commit.
public sealed class PracticeFeeChange : AuditableEntity
{
    public Guid LocationId { get; private set; }
    public Guid DoctorUserId { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public decimal PreviousFee { get; private set; }
    public decimal NewFee { get; private set; }
    public string Currency { get; private set; } = "EGP";
    public string DeliveryStatus { get; private set; } = "Pending";
    public DateTimeOffset? LastDeliveryAttemptUtc { get; private set; }
    private PracticeFeeChange() { }
    private PracticeFeeChange(Guid id) : base(id) { }
    public static PracticeFeeChange Create(Guid location, Guid doctor, Guid actor, decimal previous, decimal next)
    {
        if (location == Guid.Empty || doctor == Guid.Empty || actor == Guid.Empty || previous == next ||
            PracticeLocations.ConsultationPrice.Create(previous, "EGP").IsError || PracticeLocations.ConsultationPrice.Create(next, "EGP").IsError)
            throw new ArgumentException("A valid changed price and its actors are required.");
        return new PracticeFeeChange(Guid.NewGuid()) { LocationId = location, DoctorUserId = doctor, ChangedByUserId = actor, PreviousFee = previous, NewFee = next };
    }
}
