using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Patients.Commands.SaveOwnProfile;

public sealed record SaveOwnPatientProfileCommand(string FullName, string PhoneNumber, string? Version)
    : IRequest<Result<PatientProfileSaveResult>>;

public sealed class SaveOwnPatientProfileCommandValidator : AbstractValidator<SaveOwnPatientProfileCommand>
{
    public SaveOwnPatientProfileCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\+?[0-9]{7,15}$");
        RuleFor(x => x.Version).Must(value => value is null ||
            (value.Length == 12 && Convert.TryFromBase64String(value, new byte[8], out var written) && written == 8))
            .WithMessage("Version must be null for creation or a valid profile version for updates.");
    }
}

public sealed class SaveOwnPatientProfileCommandHandler(IPatientProfileService profiles)
    : IRequestHandler<SaveOwnPatientProfileCommand, Result<PatientProfileSaveResult>>
{
    public Task<Result<PatientProfileSaveResult>> Handle(SaveOwnPatientProfileCommand request, CancellationToken ct) =>
        profiles.SaveOwnAsync(request.FullName, request.PhoneNumber, request.Version, ct);
}
