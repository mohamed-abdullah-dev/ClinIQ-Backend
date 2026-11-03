using ClinIQ.Domain.Common;

namespace ClinIQ.Domain.Clinics;

public sealed class ClinicRegistrationSubmission : AuditableEntity
{
    public Guid RequestId { get; private set; }
    public Guid ApplicantUserId { get; private set; }
    public Guid? TargetClinicId { get; private set; }
    public int AttemptNumber { get; private set; }
    public string ClinicName { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string BranchName { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public string PublicPhone { get; private set; } = null!;
    public string LicensingAuthority { get; private set; } = null!;
    public string LicenseNumber { get; private set; } = null!;
    public string LicensedOperator { get; private set; } = null!;
    public ClinicApplicantCapacity ApplicantCapacity { get; private set; }
    public ManagementAuthorityBasis AuthorityBasis { get; private set; }
    public ClinicApplicationData Details => new(ClinicName, Description, BranchName, Address, Latitude, Longitude,
        PublicPhone, LicensingAuthority, LicenseNumber, LicensedOperator, ApplicantCapacity, AuthorityBasis);
    public ClinicApplicationStatus Status { get; private set; } = ClinicApplicationStatus.PendingReview;
    public Guid? ReviewerUserId { get; private set; }
    public string? ReviewReason { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public bool LicenseDetailsVerified { get; private set; }
    public bool IdentityVerified { get; private set; }
    public bool AuthorityVerified { get; private set; }
    public bool IndependentlyCorroborated { get; private set; }
    private readonly List<ClinicRegistrationDocument> _documents = [];
    public IReadOnlyCollection<ClinicRegistrationDocument> Documents => _documents.AsReadOnly();
    private ClinicRegistrationSubmission() { }
    private ClinicRegistrationSubmission(Guid id) : base(id) { }
    internal static ClinicRegistrationSubmission Create(Guid id, ClinicRegistrationRequest request, int attempt, IReadOnlyList<ClinicEvidenceMetadata> documents)
    {
        if (id == Guid.Empty) throw new ArgumentException("A submission ID is required.");
        var data = request.Details;
        var row = new ClinicRegistrationSubmission(id) { RequestId = request.Id, ApplicantUserId = request.ApplicantUserId, TargetClinicId = request.TargetClinicId,
            AttemptNumber = attempt, ClinicName = data.ClinicName,
            Description = data.Description, BranchName = data.BranchName, Address = data.Address, Latitude = data.Latitude, Longitude = data.Longitude,
            PublicPhone = data.PublicPhone, LicensingAuthority = data.LicensingAuthority, LicenseNumber = data.LicenseNumber, LicensedOperator = data.LicensedOperator,
            ApplicantCapacity = data.ApplicantCapacity, AuthorityBasis = data.AuthorityBasis };
        row._documents.AddRange(documents.Select(x => ClinicRegistrationDocument.Create(id, x))); return row;
    }
    internal void RecordDecision(ClinicApplicationStatus outcome, string reason, Guid reviewer, DateTimeOffset now, bool license, bool identity, bool authority, bool corroborated)
    {
        Status = outcome; ReviewerUserId = reviewer; ReviewReason = reason; ReviewedAtUtc = now;
        LicenseDetailsVerified = license; IdentityVerified = identity; AuthorityVerified = authority; IndependentlyCorroborated = corroborated;
    }
}

public sealed class ClinicRegistrationDocument : AuditableEntity
{
    public Guid SubmissionId { get; private set; }
    public ClinicEvidenceKind Kind { get; private set; }
    public string StorageKey { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public string MediaType { get; private set; } = null!;
    public long Length { get; private set; }
    public string Sha256 { get; private set; } = null!;
    private ClinicRegistrationDocument() { }
    private ClinicRegistrationDocument(Guid id) : base(id) { }
    internal static ClinicRegistrationDocument Create(Guid submission, ClinicEvidenceMetadata metadata) => new(Guid.NewGuid())
    { SubmissionId = submission, Kind = metadata.Kind, StorageKey = metadata.StorageKey, OriginalFileName = metadata.OriginalFileName, MediaType = metadata.MediaType, Length = metadata.Length, Sha256 = metadata.Sha256 };
}

public sealed class ClinicEvidenceAccess : Entity
{
    public Guid DocumentId { get; private set; }
    public Guid ViewerUserId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }
    private ClinicEvidenceAccess() { }
    private ClinicEvidenceAccess(Guid id) : base(id) { }
    public static ClinicEvidenceAccess Grant(Guid document, Guid viewer, DateTimeOffset now)
    {
        if (document == Guid.Empty || viewer == Guid.Empty) throw new ArgumentException("Document and viewer IDs are required.");
        return new ClinicEvidenceAccess(Guid.NewGuid()) { DocumentId = document, ViewerUserId = viewer, GrantedAtUtc = now };
    }
}
