using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.PracticeLocations;

namespace ClinIQ.Domain.PremisesCorrections;

public sealed class PremisesCorrection : AuditableEntity
{
    public Guid? LocationId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid ApplicantUserId { get; private set; }
    public PremisesDetails Original { get; private set; } = null!;
    public PremisesDetails Proposed { get; private set; } = null!;
    public byte[] OriginalVersion { get; private set; } = [];
    public string RequestReason { get; private set; } = null!;
    public PremisesCorrectionStatus Status { get; private set; } = PremisesCorrectionStatus.PendingReview;
    public Guid? ReviewerUserId { get; private set; }
    public string? ReviewReason { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public bool SamePremisesVerified { get; private set; }
    public bool LicenseVerified { get; private set; }
    public bool AuthorityVerified { get; private set; }
    public byte[] Version { get; private set; } = [];
    private readonly List<PremisesCorrectionDocument> _documents = [];
    public IReadOnlyCollection<PremisesCorrectionDocument> Documents => _documents.AsReadOnly();
    private PremisesCorrection() { }
    private PremisesCorrection(Guid id) : base(id) { }

    public static Result<PremisesCorrection> Create(Guid id, Guid? locationId, Guid? branchId, Guid applicant,
        PremisesDetails original, byte[] originalVersion, PremisesDetails proposed, string reason,
        IReadOnlyList<PracticeLocationEvidenceMetadata> evidence)
    {
        if (id == Guid.Empty || applicant == Guid.Empty || (locationId.HasValue == branchId.HasValue) ||
            locationId == Guid.Empty || branchId == Guid.Empty || originalVersion.Length != 8)
            throw new ArgumentException("A correction requires exactly one target, an applicant and a persisted target version.");
        var normalized = proposed.Normalize(branchId.HasValue);
        if (normalized.IsError) return Result<PremisesCorrection>.Failure(normalized.Errors);
        if (original == normalized.Value) return PremisesCorrectionErrors.NoChanges;
        var trimmed = reason?.Trim() ?? "";
        if (trimmed.Length is < 1 or > 2000) return PremisesCorrectionErrors.Reason;
        if (evidence.Count is < 1 or > 2 || evidence.Any(x => !x.IsValid) || evidence.Select(x => x.Kind).Distinct().Count() != evidence.Count ||
            !evidence.Any(x => x.Kind == PracticeLocationEvidenceKind.FacilityLicense)) return PremisesCorrectionErrors.Evidence;
        var row = new PremisesCorrection(id)
        {
            LocationId = locationId,
            BranchId = branchId,
            ApplicantUserId = applicant,
            Original = original,
            Proposed = normalized.Value,
            OriginalVersion = originalVersion.ToArray(),
            RequestReason = trimmed
        };
        row._documents.AddRange(evidence.Select(x => PremisesCorrectionDocument.Create(id, x)));
        return row;
    }

    public Result<Success> Review(PremisesCorrectionStatus outcome, string reason, Guid reviewer, DateTimeOffset now,
        bool samePremises, bool license, bool authority)
    {
        if (reviewer == Guid.Empty) throw new ArgumentException("A reviewer is required.");
        if (reviewer == ApplicantUserId) return PremisesCorrectionErrors.SelfReview;
        if (Status != PremisesCorrectionStatus.PendingReview) return PremisesCorrectionErrors.AlreadyReviewed;
        if (outcome is not (PremisesCorrectionStatus.Approved or PremisesCorrectionStatus.Rejected or PremisesCorrectionStatus.NeedsInformation))
            return PremisesCorrectionErrors.Outcome;
        var trimmed = reason?.Trim() ?? "";
        if (trimmed.Length is < 1 or > 2000) return PremisesCorrectionErrors.Reason;
        if (outcome == PremisesCorrectionStatus.Approved && (!samePremises || !license || !authority)) return PremisesCorrectionErrors.Verification;
        Status = outcome; ReviewReason = trimmed; ReviewerUserId = reviewer; ReviewedAtUtc = now;
        SamePremisesVerified = samePremises; LicenseVerified = license; AuthorityVerified = authority;
        return Result.Success;
    }
}

public sealed class PremisesCorrectionDocument : AuditableEntity
{
    public Guid CorrectionId { get; private set; }
    public PracticeLocationEvidenceKind Kind { get; private set; }
    public string StorageKey { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public string MediaType { get; private set; } = null!;
    public long Length { get; private set; }
    public string Sha256 { get; private set; } = null!;
    private PremisesCorrectionDocument() { }
    private PremisesCorrectionDocument(Guid id) : base(id) { }
    internal static PremisesCorrectionDocument Create(Guid id, PracticeLocationEvidenceMetadata data) => new(Guid.NewGuid())
    { CorrectionId = id, Kind = data.Kind, StorageKey = data.StorageKey, OriginalFileName = data.OriginalFileName, MediaType = data.MediaType, Length = data.Length, Sha256 = data.Sha256 };
}

public sealed class PremisesCorrectionEvidenceAccess : Entity
{
    public Guid DocumentId { get; private set; }
    public Guid ViewerUserId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }
    private PremisesCorrectionEvidenceAccess() { }
    private PremisesCorrectionEvidenceAccess(Guid id) : base(id) { }
    public static PremisesCorrectionEvidenceAccess Grant(Guid document, Guid viewer, DateTimeOffset now)
    {
        if (document == Guid.Empty || viewer == Guid.Empty) throw new ArgumentException("Document and viewer IDs are required.");
        return new PremisesCorrectionEvidenceAccess(Guid.NewGuid()) { DocumentId = document, ViewerUserId = viewer, GrantedAtUtc = now };
    }
}
