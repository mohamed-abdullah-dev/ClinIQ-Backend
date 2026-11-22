using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Application.Features.Specialties;

public sealed record AdminSpecialtyDto(Guid Id, string NameAr, string NameEn, string Version);

public interface ISpecialtyAdministrationService
{
    Task<Result<IReadOnlyList<AdminSpecialtyDto>>> ListAsync(CancellationToken ct);
    Task<Result<AdminSpecialtyDto>> GetAsync(Guid id, CancellationToken ct);
    Task<Result<AdminSpecialtyDto>> CreateAsync(string nameAr, string nameEn, CancellationToken ct);
    Task<Result<AdminSpecialtyDto>> RenameAsync(Guid id, string nameAr, string nameEn, string version, CancellationToken ct);
    Task<Result<Success>> DeleteAsync(Guid id, string version, CancellationToken ct);
}

public static class SpecialtyVersion
{
    public static bool IsValid(string? value) => value is { Length: 12 } &&
        Convert.TryFromBase64String(value, new byte[8], out var written) && written == 8;
}
