using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.Clinics;
public enum BranchLifecycleState { Open = 1, PendingClosure = 2, Closed = 3 }

public sealed class Clinic : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public Guid SourceSubmissionId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public byte[] Version { get; private set; } = [];
    private Clinic() { }
    private Clinic(Guid id) : base(id) { }
    public static Result<Clinic> FromApprovedSubmission(Guid id, ClinicRegistrationSubmission submission)
    {
        if (id == Guid.Empty) throw new ArgumentException("A clinic ID is required.");
        if (submission.Status != ClinicApplicationStatus.Approved || submission.TargetClinicId is not null) return ClinicErrors.AlreadyReviewed;
        return new Clinic(id) { Name = submission.Details.ClinicName, Description = submission.Details.Description, SourceSubmissionId = submission.Id };
    }
}

public sealed class ClinicBranch : AuditableEntity
{
    public Guid ClinicId { get; private set; }
    public Guid SourceSubmissionId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public string PublicPhone { get; private set; } = null!;
    public string TimeZoneId { get; private set; } = "Africa/Cairo";
    public string LicensingAuthority { get; private set; } = null!;
    public string LicenseNumber { get; private set; } = null!;
    public string NormalizedLicensingAuthority { get; private set; } = null!;
    public string NormalizedLicenseNumber { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public BranchLifecycleState LifecycleState { get; private set; } = BranchLifecycleState.Open;
    public byte[] Version { get; private set; } = [];
    private ClinicBranch() { }
    private ClinicBranch(Guid id) : base(id) { }
    public Result<bool> BeginClosure()
    {
        if (!IsActive) return PracticeLifecycle.LifecycleErrors.Closed;
        if (LifecycleState == BranchLifecycleState.PendingClosure) return false;
        if (LifecycleState != BranchLifecycleState.Open) return PracticeLifecycle.LifecycleErrors.Closed;
        LifecycleState = BranchLifecycleState.PendingClosure;
        return true;
    }
    public Result<bool> FinalizeClosure(int outstanding)
    {
        if (outstanding < 0) throw new ArgumentOutOfRangeException(nameof(outstanding));
        if (LifecycleState == BranchLifecycleState.Closed) return false;
        if (LifecycleState != BranchLifecycleState.PendingClosure) return PracticeLifecycle.LifecycleErrors.NotPending;
        if (outstanding != 0) return PracticeLifecycle.LifecycleErrors.Outstanding;
        LifecycleState = BranchLifecycleState.Closed;
        IsActive = false;
        return true;
    }
    public static Result<ClinicBranch> FromApprovedSubmission(Guid id, Guid clinicId, ClinicRegistrationSubmission submission)
    {
        if (id == Guid.Empty || clinicId == Guid.Empty) throw new ArgumentException("Branch and clinic IDs are required.");
        if (submission.Status != ClinicApplicationStatus.Approved || (submission.TargetClinicId is Guid target && target != clinicId)) return ClinicErrors.AlreadyReviewed;
        var data = submission.Details;
        return new ClinicBranch(id)
        {
            ClinicId = clinicId,
            SourceSubmissionId = submission.Id,
            Name = data.BranchName,
            Address = data.Address,
            Latitude = data.Latitude,
            Longitude = data.Longitude,
            PublicPhone = data.PublicPhone,
            LicensingAuthority = data.LicensingAuthority,
            LicenseNumber = data.LicenseNumber,
            NormalizedLicensingAuthority = ClinicApplicationData.LicenseKey(data.LicensingAuthority),
            NormalizedLicenseNumber = ClinicApplicationData.LicenseKey(data.LicenseNumber)
        };
    }
    public Result<Success> ApplyCorrection(PremisesCorrections.PremisesCorrection correction)
    {
        if (!IsActive) return PremisesCorrections.PremisesCorrectionErrors.NotApproved;
        if (correction.Status != PremisesCorrections.PremisesCorrectionStatus.Approved || correction.BranchId != Id ||
            !Version.SequenceEqual(correction.OriginalVersion) || PremisesCorrections.PremisesDetails.From(this) != correction.Original)
            return PremisesCorrections.PremisesCorrectionErrors.Stale;
        var details = correction.Proposed;
        Name = details.Name; Address = details.Address; Latitude = details.Latitude; Longitude = details.Longitude;
        PublicPhone = details.PublicPhone!; LicensingAuthority = details.LicensingAuthority!; LicenseNumber = details.LicenseNumber!;
        NormalizedLicensingAuthority = ClinicApplicationData.LicenseKey(LicensingAuthority);
        NormalizedLicenseNumber = ClinicApplicationData.LicenseKey(LicenseNumber);
        return Result.Success;
    }
}

public sealed class ClinicManagementMembership : AuditableEntity
{
    public Guid ClinicId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid SourceSubmissionId { get; private set; }
    public Guid GrantedByUserId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; }
    public bool IsActive { get; private set; } = true;
    private ClinicManagementMembership() { }
    private ClinicManagementMembership(Guid id) : base(id) { }
    public static Result<ClinicManagementMembership> FromApprovedSubmission(Guid clinic, ClinicRegistrationSubmission submission)
    {
        if (clinic == Guid.Empty) throw new ArgumentException("A clinic ID is required.");
        if (submission.Status != ClinicApplicationStatus.Approved || submission.TargetClinicId is not null) return ClinicErrors.AlreadyReviewed;
        return new ClinicManagementMembership(Guid.NewGuid())
        {
            ClinicId = clinic,
            UserId = submission.ApplicantUserId,
            SourceSubmissionId = submission.Id,
            GrantedByUserId = submission.ReviewerUserId!.Value,
            GrantedAtUtc = submission.ReviewedAtUtc!.Value
        };
    }
}
