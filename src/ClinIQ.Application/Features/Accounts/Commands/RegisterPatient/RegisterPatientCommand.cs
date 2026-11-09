using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Accounts.Commands.RegisterPatient;

public sealed record RegisterPatientCommand(string FullName, string Email, string Password)
    : IRequest<Result<PatientRegistrationReceipt>>;

public sealed class RegisterPatientCommandValidator : AbstractValidator<RegisterPatientCommand>
{
    public RegisterPatientCommandValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class RegisterPatientCommandHandler(IPatientRegistrationService registration)
    : IRequestHandler<RegisterPatientCommand, Result<PatientRegistrationReceipt>>
{
    public Task<Result<PatientRegistrationReceipt>> Handle(RegisterPatientCommand request, CancellationToken cancellationToken) =>
        registration.RegisterAsync(request, cancellationToken);
}
