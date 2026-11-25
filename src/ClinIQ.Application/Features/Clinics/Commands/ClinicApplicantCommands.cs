using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;
using ClinIQ.Domain.Clinics;

namespace ClinIQ.Application.Features.Clinics.Commands;

public sealed record ClinicApplicantReceipt(Guid UserId, Guid ApplicationId, Guid SubmissionId, string Status, bool EmailConfirmationRequired, string EmailDeliveryStatus);
public interface IClinicApplicantService
{
    Task<Result<ClinicApplicantReceipt>> RegisterAsync(string fullName, string email, string password,
        ClinicApplicationData input, IReadOnlyList<ClinicEvidenceUpload> documents, CancellationToken ct);
}
public sealed record RegisterClinicApplicantCommand(string FullName, string Email, string Password,
    ClinicApplicationData Input, IReadOnlyList<ClinicEvidenceUpload> Documents) : IRequest<Result<ClinicApplicantReceipt>>;
public sealed class RegisterClinicApplicantCommandValidator : AbstractValidator<RegisterClinicApplicantCommand>
{
    public RegisterClinicApplicantCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200); RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Input).NotNull();
        RuleFor(x => x).Must(x => x.Input is not null && x.Input.Normalize().IsSuccess).WithMessage("Provide valid clinic and branch details.");
        RuleFor(x => x.Documents).NotNull().Must(x => x is { Count: >= 2 and <= 3 });
    }
}
public sealed class RegisterClinicApplicantCommandHandler(IClinicApplicantService service) : IRequestHandler<RegisterClinicApplicantCommand, Result<ClinicApplicantReceipt>>
{ public Task<Result<ClinicApplicantReceipt>> Handle(RegisterClinicApplicantCommand r, CancellationToken ct) => service.RegisterAsync(r.FullName, r.Email, r.Password, r.Input, r.Documents, ct); }
