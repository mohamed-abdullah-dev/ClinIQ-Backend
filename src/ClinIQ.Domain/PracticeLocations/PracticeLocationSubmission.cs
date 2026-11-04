using ClinIQ.Domain.Common;

namespace ClinIQ.Domain.PracticeLocations;

public sealed class PracticeLocationSubmission : AuditableEntity
{
    public Guid LocationId { get; private set; }
    public int AttemptNumber { get; private set; }
    public string Name { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public decimal ConsultationFee { get; private set; }
    public string Currency { get; private set; } = null!;
    public PracticeLocationStatus Status { get; private set; } = PracticeLocationStatus.PendingReview;
    public Guid? ReviewerUserId { get; private set; }
    public string? ReviewReason { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public bool LicenseMatchesAddress { get; private set; }
    public bool DoctorWorkLinkVerified { get; private set; }
    private readonly List<PracticeLocationDocument> _documents = [];
    public IReadOnlyCollection<PracticeLocationDocument> Documents => _documents.AsReadOnly();
    private PracticeLocationSubmission() { }
    private PracticeLocationSubmission(Guid id) : base(id) { }
    internal static PracticeLocationSubmission Create(Guid id, DoctorPracticeLocation location, int attempt, IReadOnlyList<PracticeLocationEvidenceMetadata> documents)
    {
        if (id == Guid.Empty) throw new ArgumentException("A submission ID is required.");
        var row = new PracticeLocationSubmission(id) { LocationId = location.Id, AttemptNumber = attempt, Name = location.Name,
            Address = location.Address, Latitude = location.Latitude, Longitude = location.Longitude, ConsultationFee = location.ConsultationFee, Currency = location.Currency };
        row._documents.AddRange(documents.Select(x => PracticeLocationDocument.Create(id, x))); return row;
    }
    internal void RecordDecision(PracticeLocationStatus outcome, string reason, Guid reviewer, DateTimeOffset now, bool licenseMatches, bool workVerified)
    {
        Status = outcome; ReviewReason = reason; ReviewerUserId = reviewer; ReviewedAtUtc = now;
        LicenseMatchesAddress = licenseMatches; DoctorWorkLinkVerified = workVerified;
    }
}

public sealed class PracticeLocationDocument : AuditableEntity
{
    public Guid SubmissionId { get; private set; }
    public PracticeLocationEvidenceKind Kind { get; private set; }
    public string StorageKey { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public string MediaType { get; private set; } = null!;
    public long Length { get; private set; }
    public string Sha256 { get; private set; } = null!;
    private PracticeLocationDocument() { }
    private PracticeLocationDocument(Guid id) : base(id) { }
    internal static PracticeLocationDocument Create(Guid submissionId, PracticeLocationEvidenceMetadata data) => new(Guid.NewGuid())
    { SubmissionId = submissionId, Kind = data.Kind, StorageKey = data.StorageKey, OriginalFileName = data.OriginalFileName, MediaType = data.MediaType, Length = data.Length, Sha256 = data.Sha256 };
}

// Records granting access, not completion of the client's transfer.
public sealed class PracticeLocationEvidenceAccess : Entity
{
    public Guid DocumentId { get; private set; }
    public Guid ViewerUserId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }
    private PracticeLocationEvidenceAccess() { }
    private PracticeLocationEvidenceAccess(Guid id) : base(id) { }
    public static PracticeLocationEvidenceAccess Grant(Guid documentId, Guid viewer, DateTimeOffset now)
    {
        if (documentId == Guid.Empty || viewer == Guid.Empty) throw new ArgumentException("Document and viewer IDs are required.");
        return new PracticeLocationEvidenceAccess(Guid.NewGuid()) { DocumentId = documentId, ViewerUserId = viewer, GrantedAtUtc = now };
    }
}
