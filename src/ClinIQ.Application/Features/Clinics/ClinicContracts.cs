using ClinIQ.Domain.Clinics;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Application.Features.Clinics;

public sealed record ClinicEvidenceUpload(ClinicEvidenceKind Kind, string FileName, string ContentType, long Length, Stream Content);
public sealed record ClinicApplicationDto(Guid Id, Guid ApplicantUserId, Guid? TargetClinicId, ClinicApplicationData Details, string Status,
    int AttemptNumber, Guid? CurrentSubmissionId, Guid? ApprovedClinicId, Guid? ApprovedBranchId, DateTimeOffset CreatedAtUtc, string Version);
public sealed record ClinicApplicationPage(ClinicApplicationDto[] Items, int TotalCount, int Page, int PageSize);
public sealed record ClinicEvidenceDto(Guid Id, ClinicEvidenceKind Kind, string FileName, string MediaType, long Length);
public sealed record ClinicSubmissionDto(Guid Id, Guid RequestId, int AttemptNumber, ClinicApplicationData Details, string Status,
    DateTimeOffset SubmittedAtUtc, Guid? ReviewerUserId, string? ReviewReason, DateTimeOffset? ReviewedAtUtc,
    bool LicenseDetailsVerified, bool IdentityVerified, bool AuthorityVerified, bool IndependentlyCorroborated, ClinicEvidenceDto[] Documents);
public sealed record ClinicApplicationDetail(ClinicApplicationDto Application, ClinicSubmissionDto? CurrentSubmission);
public sealed record ClinicSubmissionPage(ClinicSubmissionDto[] Items, int TotalCount, int Page, int PageSize);
public sealed record ClinicEvidenceDownload(Stream Content, string MediaType, string FileName);
public sealed record ManagedClinicDto(Guid Id, string Name, string Description, bool IsActive, string Version);
public sealed record ManagedClinicPage(ManagedClinicDto[] Items, int TotalCount, int Page, int PageSize);
public sealed record ManagedBranchDto(Guid Id, Guid ClinicId, string Name, string Address, decimal? Latitude, decimal? Longitude,
    string PublicPhone, string TimeZoneId, bool IsActive, string Version);
public sealed record ManagedBranchPage(ManagedBranchDto[] Items, int TotalCount, int Page, int PageSize);

public interface IClinicService
{
    Task<Result<ClinicApplicationDto>> CreateAsync(ClinicApplicationData input, Guid? targetClinicId, CancellationToken ct);
    Task<Result<ClinicApplicationDto>> UpdateDraftAsync(Guid id, ClinicApplicationData input, string version, CancellationToken ct);
    Task<Result<ClinicApplicationDto>> SubmitAsync(Guid id, string version, IReadOnlyList<ClinicEvidenceUpload> documents, CancellationToken ct);
    Task<Result<ClinicApplicationDto>> ReviewAsync(Guid id, Guid submissionId, ClinicApplicationStatus outcome, string reason, string version,
        bool licenseDetailsVerified, bool identityVerified, bool authorityVerified, bool independentlyCorroborated, CancellationToken ct);
    Task<Result<ClinicApplicationPage>> ListAsync(bool admin, int page, int pageSize, ClinicApplicationStatus? status, CancellationToken ct);
    Task<Result<ClinicApplicationDetail>> GetAsync(Guid id, bool admin, CancellationToken ct);
    Task<Result<ClinicSubmissionPage>> ListSubmissionsAsync(Guid id, bool admin, int page, int pageSize, CancellationToken ct);
    Task<Result<ClinicSubmissionDto>> GetSubmissionAsync(Guid id, Guid submissionId, bool admin, CancellationToken ct);
    Task<Result<ClinicEvidenceDownload>> DownloadAsync(Guid id, Guid submissionId, Guid documentId, bool admin, CancellationToken ct);
    Task<Result<ManagedClinicPage>> ListManagedAsync(int page, int pageSize, CancellationToken ct);
    Task<Result<ManagedClinicDto>> GetManagedAsync(Guid id, CancellationToken ct);
    Task<Result<ManagedBranchPage>> ListBranchesAsync(Guid id, int page, int pageSize, CancellationToken ct);
    Task<Result<ManagedBranchDto>> GetBranchAsync(Guid id, Guid branchId, CancellationToken ct);
}
