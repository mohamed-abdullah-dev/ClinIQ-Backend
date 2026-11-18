using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Doctors;

namespace ClinIQ.Application.Features.DoctorReviews;

public sealed record DoctorRegistrationSummary(Guid Id, Guid UserId, string ProfessionalName,
    Guid SpecialtyId, string SpecialtyNameAr, string SpecialtyNameEn, ProfessionalGrade ProfessionalGrade,
    string Status, DateTimeOffset SubmittedAtUtc, bool IsCurrent, int AttemptNumber, Guid? PreviousRegistrationId);
public sealed record DoctorRegistrationPage(DoctorRegistrationSummary[] Items, int TotalCount, int Page, int PageSize);
public sealed record DoctorEvidenceDto(Guid Id, DoctorEvidenceKind Kind, string FileName, string MediaType, long Length);
public sealed record DoctorDecisionDto(Guid ReviewerUserId, string Outcome, string Reason, DateTimeOffset ReviewedAtUtc);
public sealed record DoctorRegistrationDetail(DoctorRegistrationSummary Registration, string Email, bool EmailConfirmed,
    string PhoneNumber, string RegistrationNumber, DoctorEvidenceDto[] Documents,
    DoctorDecisionDto? Decision, Guid? DoctorProfileId, string Version);
public sealed record DoctorEvidenceDownload(Stream Content, string MediaType, string FileName);
public sealed record DoctorReviewReceipt(Guid RegistrationId, string Status, Guid? DoctorProfileId, string Version);

public interface IDoctorReviewService
{
    Task<Result<DoctorRegistrationPage>> ListAsync(int page, int pageSize, DoctorRegistrationStatus? status, CancellationToken ct);
    Task<Result<DoctorRegistrationDetail>> GetAsync(Guid id, CancellationToken ct);
    Task<Result<DoctorEvidenceDownload>> DownloadAsync(Guid id, Guid documentId, CancellationToken ct);
    Task<Result<DoctorReviewReceipt>> ReviewAsync(Guid id, bool approve, string reason, string version, CancellationToken ct);
}
