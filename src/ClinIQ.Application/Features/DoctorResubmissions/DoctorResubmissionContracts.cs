using ClinIQ.Application.Features.Accounts;
using ClinIQ.Application.Features.DoctorReviews;
using ClinIQ.Application.Features.DoctorResubmissions.Commands;
using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Doctors;

namespace ClinIQ.Application.Features.DoctorResubmissions;

public sealed record DoctorResubmissionPreview(Guid RegistrationId, string ProfessionalName, string PhoneNumber,
    string RegistrationNumber, Guid SpecialtyId, ProfessionalGrade ProfessionalGrade, int AttemptNumber,
    string RejectionReason, DoctorEvidenceDto[] Documents);
public sealed record DoctorResubmissionReceipt(Guid RegistrationId, Guid PreviousRegistrationId, int AttemptNumber, string Status);

public interface IDoctorResubmissionService
{
    Task RequestLinkAsync(string email, CancellationToken ct);
    Task<Result<DoctorResubmissionPreview>> GetAsync(string token, CancellationToken ct);
    Task<Result<DoctorEvidenceDownload>> DownloadAsync(string token, Guid documentId, CancellationToken ct);
    Task<Result<DoctorResubmissionReceipt>> SubmitAsync(ResubmitDoctorCommand command, CancellationToken ct);
}
