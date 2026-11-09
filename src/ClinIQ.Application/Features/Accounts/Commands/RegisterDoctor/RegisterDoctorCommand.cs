using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Doctors;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Accounts.Commands.RegisterDoctor;

public sealed record RegisterDoctorCommand(string FullName, string Email, string Password, string PhoneNumber,
    string RegistrationNumber, Guid SpecialtyId, ProfessionalGrade ProfessionalGrade,
    IReadOnlyList<RegistrationUpload> Documents) : IRequest<Result<DoctorRegistrationReceipt>>;

public sealed class RegisterDoctorCommandValidator : AbstractValidator<RegisterDoctorCommand>
{
    public RegisterDoctorCommandValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(command => command.Password).NotEmpty().MaximumLength(128);
        RuleFor(command => command.PhoneNumber).NotEmpty().Matches(@"^\+?[0-9]{7,15}$");
        RuleFor(command => command.RegistrationNumber).NotEmpty().MaximumLength(100);
        RuleFor(command => command.SpecialtyId).NotEmpty();
        RuleFor(command => command.ProfessionalGrade).IsInEnum();
        RuleFor(command => command.Documents).NotNull().Must(documents => documents is { Count: >= 3 and <= 4 })
            .WithMessage("Provide three required documents and grade evidence when applicable.");
    }
}

public sealed class RegisterDoctorCommandHandler(IDoctorRegistrationService registration)
    : IRequestHandler<RegisterDoctorCommand, Result<DoctorRegistrationReceipt>>
{
    public Task<Result<DoctorRegistrationReceipt>> Handle(RegisterDoctorCommand request, CancellationToken cancellationToken) =>
        registration.RegisterAsync(request, cancellationToken);
}
