using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Application.Features.Patients;

public sealed record PatientProfileDto(Guid Id, Guid UserId, string FullName, string PhoneNumber, string Version);
public sealed record PatientProfileSaveResult(PatientProfileDto Profile, bool Created);

public interface IPatientProfileService
{
    Task<Result<PatientProfileDto>> GetOwnAsync(CancellationToken ct);
    Task<Result<PatientProfileSaveResult>> SaveOwnAsync(string fullName, string phoneNumber, string? version, CancellationToken ct);
}
