using ClinIQ.Application.Features.Accounts;
using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Doctors;

namespace ClinIQ.Application.Features.DoctorProfessionalChanges;

public sealed record VerifiedDoctorProfileDto(Guid Id, Guid UserId, string ProfessionalName, Guid SpecialtyId, ProfessionalGrade ProfessionalGrade, string Version);
public sealed record ProfessionalChangeSummary(Guid Id, Guid DoctorProfileId, Guid UserId, string ProfessionalName,
    Guid SpecialtyId, ProfessionalGrade ProfessionalGrade, string Status, DateTimeOffset SubmittedAtUtc, string Version);
public sealed record ProfessionalChangePage(ProfessionalChangeSummary[] Items, int TotalCount, int Page, int PageSize);
public sealed record ChangeEvidenceDto(Guid Id, DoctorEvidenceKind Kind, string FileName, string MediaType, long Length);
public sealed record ProfessionalChangeDetail(ProfessionalChangeSummary Request, VerifiedDoctorProfileDto Current,
    string OriginalProfessionalName, Guid OriginalSpecialtyId, ProfessionalGrade OriginalProfessionalGrade, string OriginalProfileVersion,
    ChangeEvidenceDto[] Documents, Guid? ReviewerUserId, string? ReviewReason, DateTimeOffset? ReviewedAtUtc);
public sealed record ChangeEvidenceDownload(Stream Content, string MediaType, string FileName);

public interface IProfessionalChangeService
{
    Task<Result<VerifiedDoctorProfileDto>> GetOwnProfileAsync(CancellationToken ct);
    Task<Result<ProfessionalChangePage>> ListAsync(bool admin, int page, int pageSize, ProfessionalChangeStatus? status, CancellationToken ct);
    Task<Result<ProfessionalChangeDetail>> GetAsync(Guid id, bool admin, CancellationToken ct);
    Task<Result<ProfessionalChangeSummary>> SubmitAsync(string name, Guid specialtyId, ProfessionalGrade grade, string profileVersion,
        IReadOnlyList<RegistrationUpload> documents, CancellationToken ct);
    Task<Result<ProfessionalChangeSummary>> ReviewAsync(Guid id, bool approve, string reason, string version, CancellationToken ct);
    Task<Result<ChangeEvidenceDownload>> DownloadAsync(Guid id, Guid documentId, bool admin, CancellationToken ct);
}

public static class ProfessionalVersion
{
    public static bool IsValid(string? value) => value is { Length: 12 } &&
        Convert.TryFromBase64String(value, new byte[8], out var written) && written == 8;
}
