using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.Doctors;

public enum ProfessionalChangeStatus { PendingReview = 1, Approved = 2, Rejected = 3 }

public sealed class DoctorProfessionalChange : AuditableEntity
{
    public Guid DoctorProfileId { get; private set; }
    public Guid UserId { get; private set; }
    public string OriginalProfessionalName { get; private set; } = null!;
    public Guid OriginalSpecialtyId { get; private set; }
    public ProfessionalGrade OriginalProfessionalGrade { get; private set; }
    public byte[] OriginalProfileVersion { get; private set; } = [];
    public string ProfessionalName { get; private set; } = null!;
    public Guid SpecialtyId { get; private set; }
    public ProfessionalGrade ProfessionalGrade { get; private set; }
    public ProfessionalChangeStatus Status { get; private set; }
    public byte[] Version { get; private set; } = [];
    public Guid? ReviewerUserId { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? ReviewReason { get; private set; }
    private readonly List<ProfessionalChangeDocument> _documents = [];
    public IReadOnlyCollection<ProfessionalChangeDocument> Documents => _documents.AsReadOnly();
    private DoctorProfessionalChange() { }
    private DoctorProfessionalChange(Guid id) : base(id) { }

    public static Result<DoctorProfessionalChange> Create(Guid id, DoctorProfile profile, string? name, Guid specialtyId,
        ProfessionalGrade grade, IReadOnlyList<DoctorEvidenceMetadata> documents)
    {
        if (id == Guid.Empty) throw new ArgumentException("Assign a change-request identity.", nameof(id));
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length is < 1 or > 200 || specialtyId == Guid.Empty || !Enum.IsDefined(grade))
            return ProfessionalChangeErrors.InvalidProposal;
        if (profile.Version.Length != 8) return ProfessionalChangeErrors.StaleProfile;
        var nameChanged = trimmed != profile.ProfessionalName;
        var specialtyChanged = specialtyId != profile.SpecialtyId;
        var gradeChanged = grade != profile.ProfessionalGrade;
        if (!nameChanged && !specialtyChanged && !gradeChanged) return ProfessionalChangeErrors.NoChanges;
        var kinds = documents.Select(x => x.Kind).ToArray();
        if (kinds.Length is < 1 or > 4 || kinds.Distinct().Count() != kinds.Length ||
            kinds.Any(kind => !Enum.IsDefined(kind)) || documents.Any(x => !x.IsValid) ||
            (nameChanged && !kinds.Contains(DoctorEvidenceKind.Identity)) ||
            (specialtyChanged && !kinds.Contains(DoctorEvidenceKind.PracticeLicense)) ||
            (gradeChanged && !kinds.Contains(DoctorEvidenceKind.ProfessionalGrade)))
            return ProfessionalChangeErrors.RequiredEvidence;
        var request = new DoctorProfessionalChange(id)
        {
            DoctorProfileId = profile.Id, UserId = profile.UserId,
            OriginalProfessionalName = profile.ProfessionalName, OriginalSpecialtyId = profile.SpecialtyId,
            OriginalProfessionalGrade = profile.ProfessionalGrade, OriginalProfileVersion = profile.Version.ToArray(),
            ProfessionalName = trimmed, SpecialtyId = specialtyId, ProfessionalGrade = grade,
            Status = ProfessionalChangeStatus.PendingReview
        };
        request._documents.AddRange(documents.Select(x => ProfessionalChangeDocument.Create(id, x)));
        return request;
    }

    public Result<Success> Review(DoctorProfile profile, bool approve, string? reason, Guid reviewerId, DateTimeOffset now)
    {
        if (reviewerId == Guid.Empty) throw new ArgumentException("A reviewer is required.", nameof(reviewerId));
        if (Status != ProfessionalChangeStatus.PendingReview) return ProfessionalChangeErrors.AlreadyReviewed;
        if (reviewerId == UserId) return ProfessionalChangeErrors.SelfReview;
        var trimmed = reason?.Trim() ?? "";
        if (trimmed.Length is < 1 or > 2000) return ProfessionalChangeErrors.ReasonRequired;
        if (profile.Id != DoctorProfileId || profile.UserId != UserId) return ProfessionalChangeErrors.StaleProfile;
        if (approve)
        {
            var applied = profile.ApplyApprovedChange(this);
            if (applied.IsError) return applied;
        }
        Status = approve ? ProfessionalChangeStatus.Approved : ProfessionalChangeStatus.Rejected;
        ReviewerUserId = reviewerId; ReviewedAtUtc = now; ReviewReason = trimmed;
        return Result.Success;
    }
}

public sealed class ProfessionalChangeDocument : AuditableEntity
{
    public Guid RequestId { get; private set; }
    public DoctorEvidenceKind Kind { get; private set; }
    public string StorageKey { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public string MediaType { get; private set; } = null!;
    public long Length { get; private set; }
    public string Sha256 { get; private set; } = null!;
    private ProfessionalChangeDocument() { }
    private ProfessionalChangeDocument(Guid id) : base(id) { }
    internal static ProfessionalChangeDocument Create(Guid requestId, DoctorEvidenceMetadata metadata) => new(Guid.NewGuid())
    {
        RequestId = requestId, Kind = metadata.Kind, StorageKey = metadata.StorageKey,
        OriginalFileName = metadata.OriginalFileName, MediaType = metadata.MediaType, Length = metadata.Length, Sha256 = metadata.Sha256
    };
}

public sealed class ProfessionalChangeEvidenceAccess : Entity
{
    public Guid DocumentId { get; private set; }
    public Guid ViewerUserId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }
    private ProfessionalChangeEvidenceAccess() { }
    private ProfessionalChangeEvidenceAccess(Guid id) : base(id) { }
    public static ProfessionalChangeEvidenceAccess Grant(Guid documentId, Guid viewerId, DateTimeOffset now)
    {
        if (documentId == Guid.Empty || viewerId == Guid.Empty) throw new ArgumentException("Document and viewer are required.");
        return new(Guid.NewGuid()) { DocumentId = documentId, ViewerUserId = viewerId, GrantedAtUtc = now };
    }
}

public static class ProfessionalChangeErrors
{
    public static readonly Error Unauthorized = Error.Unauthorized("ProfessionalChange.Unauthorized", "Please log in to continue.");
    public static readonly Error Forbidden = Error.Forbidden("ProfessionalChange.Forbidden", "This account cannot perform this action.");
    public static readonly Error NotFound = Error.NotFound("ProfessionalChange.NotFound", "The profile, request or document was not found.");
    public static readonly Error StaleProfile = Error.Conflict("ProfessionalChange.StaleProfile", "The approved profile changed. Reload it before continuing.");
    public static readonly Error StaleRequest = Error.Conflict("ProfessionalChange.StaleRequest", "The request changed. Reload it before reviewing.");
    public static readonly Error PendingExists = Error.Conflict("ProfessionalChange.PendingExists", "This doctor already has a pending change request.");
    public static readonly Error AlreadyReviewed = Error.Conflict("ProfessionalChange.AlreadyReviewed", "This request already has a final decision.");
    public static readonly Error SelfReview = Error.Forbidden("ProfessionalChange.SelfReview", "An admin cannot review their own professional change.");
    public static readonly Error Busy = Error.Conflict("ProfessionalChange.Busy", "The profile is being reviewed. Retry the request.");
    public static readonly Error NoChanges = Error.Validation("ProfessionalChange.NoChanges", "Change at least one professional field.");
    public static readonly Error InvalidProposal = Error.Validation("ProfessionalChange.InvalidProposal", "Provide a valid professional name, specialty and grade.");
    public static readonly Error RequiredEvidence = Error.Validation("ProfessionalChange.RequiredEvidence", "Provide identity evidence for a name change, a practice license for a specialty change, and grade evidence for a grade change.", "Documents");
    public static readonly Error ReasonRequired = Error.Validation("ProfessionalChange.Reason", "Provide a review reason of 1 to 2000 characters.", "Reason");
    public static readonly Error UnknownSpecialty = Error.Validation("ProfessionalChange.UnknownSpecialty", "Choose a specialty from the current catalog.", "SpecialtyId");
    public static readonly Error EvidenceUnavailable = Error.NotFound("ProfessionalChange.EvidenceUnavailable", "The document is unavailable.");
}
