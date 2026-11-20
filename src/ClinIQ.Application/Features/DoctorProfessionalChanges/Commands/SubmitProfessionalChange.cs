using ClinIQ.Application.Features.Accounts;
using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Doctors;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.DoctorProfessionalChanges.Commands;

public sealed record SubmitProfessionalChangeCommand(string ProfessionalName, Guid SpecialtyId, ProfessionalGrade ProfessionalGrade,
    string ProfileVersion, IReadOnlyList<RegistrationUpload> Documents) : IRequest<Result<ProfessionalChangeSummary>>;
public sealed class SubmitProfessionalChangeCommandValidator : AbstractValidator<SubmitProfessionalChangeCommand>
{
    public SubmitProfessionalChangeCommandValidator()
    {
        RuleFor(x => x.ProfessionalName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SpecialtyId).NotEmpty(); RuleFor(x => x.ProfessionalGrade).IsInEnum();
        RuleFor(x => x.ProfileVersion).Must(ProfessionalVersion.IsValid).WithMessage("Provide the current profile version.");
        RuleFor(x => x.Documents).Must(documents => documents is { Count: >= 1 and <= 4 });
    }
}
public sealed class SubmitProfessionalChangeCommandHandler(IProfessionalChangeService changes)
    : IRequestHandler<SubmitProfessionalChangeCommand, Result<ProfessionalChangeSummary>>
{
    public Task<Result<ProfessionalChangeSummary>> Handle(SubmitProfessionalChangeCommand request, CancellationToken ct) =>
        changes.SubmitAsync(request.ProfessionalName, request.SpecialtyId, request.ProfessionalGrade, request.ProfileVersion, request.Documents, ct);
}
