using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.PracticeLocations;

namespace ClinIQ.Application.Features.PracticeLocations;

public sealed record PracticeLocationDraft(string Name, string Address, decimal? Latitude, decimal? Longitude, decimal ConsultationFee, string Currency);
public sealed record PracticeLocationDto(Guid Id, Guid DoctorProfileId, Guid UserId, string Name, string Address,
    decimal? Latitude, decimal? Longitude, decimal ConsultationFee, string Currency, string Status,
    int AttemptNumber, Guid? CurrentSubmissionId, DateTimeOffset CreatedAtUtc, string Version, Guid? ClinicBranchId = null, bool IsActive = true);
public sealed record PracticeLocationPage(PracticeLocationDto[] Items, int TotalCount, int Page, int PageSize);
public sealed record PracticeLocationEvidenceDto(Guid Id, PracticeLocationEvidenceKind Kind, string FileName, string MediaType, long Length);
public sealed record PracticeLocationSubmissionDto(Guid Id, Guid LocationId, int AttemptNumber, string Name, string Address,
    decimal? Latitude, decimal? Longitude, decimal ConsultationFee, string Currency, string Status, DateTimeOffset SubmittedAtUtc,
    Guid? ReviewerUserId, string? ReviewReason, DateTimeOffset? ReviewedAtUtc, bool LicenseMatchesAddress, bool DoctorWorkLinkVerified,
    PracticeLocationEvidenceDto[] Documents);
public sealed record PracticeLocationDetail(PracticeLocationDto Location, PracticeLocationSubmissionDto? CurrentSubmission);
public sealed record PracticeLocationSubmissionPage(PracticeLocationSubmissionDto[] Items, int TotalCount, int Page, int PageSize);
public sealed record PracticeLocationEvidenceDownload(Stream Content, string MediaType, string FileName);

public interface IPracticeLocationService
{
    Task<Result<PracticeLocationDto>> CreateAsync(PracticeLocationDraft input, CancellationToken ct);
    Task<Result<PracticeLocationDto>> UpdateDraftAsync(Guid id, PracticeLocationDraft input, string version, CancellationToken ct);
    Task<Result<PracticeLocationDto>> ChangeFeeAsync(Guid id, decimal amount, string currency, string version, CancellationToken ct);
    Task<Result<PracticeLocationDto>> SubmitAsync(Guid id, string version, IReadOnlyList<PracticeLocationUpload> documents, CancellationToken ct);
    Task<Result<PracticeLocationDto>> ReviewAsync(Guid id, Guid submissionId, PracticeLocationStatus outcome, string reason,
        string version, bool licenseMatchesAddress, bool doctorWorkLinkVerified, CancellationToken ct);
    Task<Result<PracticeLocationPage>> ListAsync(bool admin, int page, int pageSize, PracticeLocationStatus? status, CancellationToken ct);
    Task<Result<PracticeLocationDetail>> GetAsync(Guid id, bool admin, CancellationToken ct);
    Task<Result<PracticeLocationSubmissionPage>> ListSubmissionsAsync(Guid id, bool admin, int page, int pageSize, CancellationToken ct);
    Task<Result<PracticeLocationSubmissionDto>> GetSubmissionAsync(Guid id, Guid submissionId, bool admin, CancellationToken ct);
    Task<Result<PracticeLocationEvidenceDownload>> DownloadAsync(Guid id, Guid submissionId, Guid documentId, bool admin, CancellationToken ct);
}
