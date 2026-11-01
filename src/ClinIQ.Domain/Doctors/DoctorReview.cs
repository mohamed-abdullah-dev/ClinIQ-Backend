using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.Doctors;

public static class DoctorReviewErrors
{
    public static readonly Error AlreadyReviewed = Error.Conflict("DoctorReview.AlreadyReviewed", "This registration already has a final decision.");
    public static readonly Error SelfReview = Error.Forbidden("DoctorReview.SelfReview", "An admin cannot review their own professional registration.");
    public static readonly Error ReasonRequired = Error.Validation("DoctorReview.Reason", "A review reason containing 1 to 2000 characters is required.", "Reason");
    public static readonly Error NotFound = Error.NotFound("DoctorReview.NotFound", "The registration or document was not found.");
    public static readonly Error Stale = Error.Conflict("DoctorReview.Stale", "The registration changed. Reload it before reviewing.");
    public static readonly Error Forbidden = Error.Forbidden("DoctorReview.Forbidden", "A confirmed Admin account is required.");
    public static readonly Error Busy = Error.Conflict("DoctorReview.Busy", "Review is busy. Please retry.");
    public static readonly Error EvidenceUnavailable = Error.NotFound("DoctorReview.EvidenceUnavailable", "The document is unavailable.");
}

public sealed class DoctorRegistrationReview : Entity
{
    public Guid DoctorRegistrationId { get; private set; }
    public Guid ReviewerUserId { get; private set; }
    public DoctorRegistrationStatus Outcome { get; private set; }
    public string Reason { get; private set; } = null!;
    public DateTimeOffset ReviewedAtUtc { get; private set; }
    private DoctorRegistrationReview() { }
    private DoctorRegistrationReview(Guid id) : base(id) { }
    internal static DoctorRegistrationReview Create(Guid registrationId, Guid reviewerId,
        DoctorRegistrationStatus outcome, string reason, DateTimeOffset now) => new(Guid.NewGuid())
        { DoctorRegistrationId = registrationId, ReviewerUserId = reviewerId, Outcome = outcome, Reason = reason, ReviewedAtUtc = now };
}

// An access grant audit: it does not claim the client received the complete response.
public sealed class DoctorEvidenceAccess : Entity
{
    public Guid DocumentId { get; private set; }
    public Guid ViewerUserId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }
    private DoctorEvidenceAccess() { }
    private DoctorEvidenceAccess(Guid id) : base(id) { }
    public static DoctorEvidenceAccess Grant(Guid documentId, Guid viewerId, DateTimeOffset now)
    {
        if (documentId == Guid.Empty || viewerId == Guid.Empty) throw new ArgumentException("Document and viewer IDs are required.");
        return new(Guid.NewGuid()) { DocumentId = documentId, ViewerUserId = viewerId, GrantedAtUtc = now };
    }
}
