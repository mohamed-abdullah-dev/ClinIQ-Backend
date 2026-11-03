using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.Clinics;

public sealed class ClinicRegistrationRequest : AuditableEntity
{
    public Guid ApplicantUserId { get; private set; }
    public Guid? TargetClinicId { get; private set; }
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
    public ClinicApplicationStatus Status { get; private set; } = ClinicApplicationStatus.Draft;
    public int AttemptNumber { get; private set; }
    public Guid? CurrentSubmissionId { get; private set; }
    public Guid? ApprovedClinicId { get; private set; }
    public Guid? ApprovedBranchId { get; private set; }
    public byte[] Version { get; private set; } = [];
    private ClinicRegistrationRequest() { }
    private ClinicRegistrationRequest(Guid id) : base(id) { }
    public static Result<ClinicRegistrationRequest> Create(Guid id, Guid applicant, Guid? targetClinicId, ClinicApplicationData input)
    {
        if (id == Guid.Empty || applicant == Guid.Empty || targetClinicId == Guid.Empty) throw new ArgumentException("Valid request, applicant and optional clinic IDs are required.");
        var data = input.Normalize(); if (data.IsError) return Result<ClinicRegistrationRequest>.Failure(data.Errors);
        var row = new ClinicRegistrationRequest(id) { ApplicantUserId = applicant, TargetClinicId = targetClinicId };
        row.Apply(data.Value); return row;
    }
    public Result<Success> UpdateDraft(Guid actor, ClinicApplicationData input)
    {
        if (actor != ApplicantUserId) return ClinicErrors.Forbidden;
        if (Status is not (ClinicApplicationStatus.Draft or ClinicApplicationStatus.Rejected or ClinicApplicationStatus.NeedsInformation)) return ClinicErrors.DraftLocked;
        var data = input.Normalize(); if (data.IsError) return Result<Success>.Failure(data.Errors);
        Apply(data.Value); return Result.Success;
    }
    private void Apply(ClinicApplicationData data)
    {
        ClinicName = data.ClinicName; Description = data.Description; BranchName = data.BranchName; Address = data.Address;
        Latitude = data.Latitude; Longitude = data.Longitude; PublicPhone = data.PublicPhone; LicensingAuthority = data.LicensingAuthority;
        LicenseNumber = data.LicenseNumber; LicensedOperator = data.LicensedOperator; ApplicantCapacity = data.ApplicantCapacity; AuthorityBasis = data.AuthorityBasis;
    }
    public Result<ClinicRegistrationSubmission> Submit(Guid id, Guid actor, IReadOnlyList<ClinicEvidenceMetadata> documents)
    {
        if (actor != ApplicantUserId) return ClinicErrors.Forbidden;
        if (Status is not (ClinicApplicationStatus.Draft or ClinicApplicationStatus.Rejected or ClinicApplicationStatus.NeedsInformation)) return ClinicErrors.DraftLocked;
        if (documents.Count is < 2 or > 3 || documents.Any(x => !x.IsValid) || documents.Select(x => x.Kind).Distinct().Count() != documents.Count ||
            !documents.Any(x => x.Kind == ClinicEvidenceKind.FacilityLicense) || !documents.Any(x => x.Kind == ClinicEvidenceKind.ApplicantIdentity) ||
            (Details.AuthorityBasis == ManagementAuthorityBasis.SignedDelegation && !documents.Any(x => x.Kind == ClinicEvidenceKind.ManagementDelegation))) return ClinicErrors.RequiredEvidence;
        var row = ClinicRegistrationSubmission.Create(id, this, checked(AttemptNumber + 1), documents);
        AttemptNumber = row.AttemptNumber; CurrentSubmissionId = row.Id; Status = ClinicApplicationStatus.PendingReview; return row;
    }
    public Result<Success> Review(ClinicRegistrationSubmission submission, ClinicApplicationStatus outcome, string? reason, Guid reviewer,
        DateTimeOffset now, bool licenseDetailsVerified, bool identityVerified, bool authorityVerified, bool independentlyCorroborated,
        Guid? clinicId = null, Guid? branchId = null)
    {
        if (reviewer == Guid.Empty) throw new ArgumentException("A reviewer ID is required.");
        if (reviewer == ApplicantUserId) return ClinicErrors.SelfReview;
        if (Status != ClinicApplicationStatus.PendingReview || CurrentSubmissionId != submission.Id || submission.RequestId != Id ||
            submission.Status != ClinicApplicationStatus.PendingReview) return ClinicErrors.AlreadyReviewed;
        if (outcome is not (ClinicApplicationStatus.Approved or ClinicApplicationStatus.Rejected or ClinicApplicationStatus.NeedsInformation)) return ClinicErrors.InvalidOutcome;
        var trimmed = reason?.Trim() ?? ""; if (trimmed.Length is < 1 or > 2000) return ClinicErrors.ReasonRequired;
        if (outcome == ClinicApplicationStatus.Approved)
        {
            if (!licenseDetailsVerified || !identityVerified || !authorityVerified || !independentlyCorroborated) return ClinicErrors.UnverifiedEvidence;
            if (clinicId is null || clinicId == Guid.Empty || branchId is null || branchId == Guid.Empty || (TargetClinicId is Guid target && target != clinicId))
                throw new ArgumentException("Approval must identify the matching clinic and new branch.");
            ApprovedClinicId = clinicId; ApprovedBranchId = branchId;
        }
        submission.RecordDecision(outcome, trimmed, reviewer, now, licenseDetailsVerified, identityVerified, authorityVerified, independentlyCorroborated);
        Status = outcome; return Result.Success;
    }
}
