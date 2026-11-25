using ClinIQ.Domain.Clinics;
using ClinIQ.Domain.Common.Results;
using ClinIQ.Application.Features.PracticeLocations.Commands;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Clinics.Commands;

public sealed record CreateClinicApplicationCommand(ClinicApplicationData Input, Guid? TargetClinicId) : IRequest<Result<ClinicApplicationDto>>;
public sealed record UpdateClinicApplicationCommand(Guid Id, ClinicApplicationData Input, string Version) : IRequest<Result<ClinicApplicationDto>>;
public sealed record SubmitClinicApplicationCommand(Guid Id, string Version, IReadOnlyList<ClinicEvidenceUpload> Documents) : IRequest<Result<ClinicApplicationDto>>;
public sealed record ReviewClinicApplicationCommand(Guid Id, Guid SubmissionId, ClinicApplicationStatus Outcome, string Reason, string Version,
    bool LicenseDetailsVerified, bool IdentityVerified, bool AuthorityVerified, bool IndependentlyCorroborated) : IRequest<Result<ClinicApplicationDto>>;
public sealed class ClinicApplicationDataValidator : AbstractValidator<ClinicApplicationData>
{
    public ClinicApplicationDataValidator()
    {
        RuleFor(x => x.ClinicName).NotEmpty().MaximumLength(200); RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.BranchName).NotEmpty().MaximumLength(200); RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PublicPhone).NotEmpty().Matches(@"^\+?[0-9]{7,15}$");
        RuleFor(x => x.LicensingAuthority).NotEmpty().MaximumLength(100); RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LicensedOperator).NotEmpty().MaximumLength(200); RuleFor(x => x.ApplicantCapacity).IsInEnum(); RuleFor(x => x.AuthorityBasis).IsInEnum();
        RuleFor(x => x).Must(x => x.Normalize().IsSuccess).WithMessage("Provide valid clinic, branch and authority details.");
    }
}
public sealed class CreateClinicApplicationCommandValidator : AbstractValidator<CreateClinicApplicationCommand>
{
    public CreateClinicApplicationCommandValidator()
    { RuleFor(x => x.Input).NotNull().SetValidator(new ClinicApplicationDataValidator()); RuleFor(x => x.TargetClinicId).NotEqual(Guid.Empty).When(x => x.TargetClinicId is not null); }
}
public sealed class UpdateClinicApplicationCommandValidator : AbstractValidator<UpdateClinicApplicationCommand>
{
    public UpdateClinicApplicationCommandValidator()
    { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Input).NotNull().SetValidator(new ClinicApplicationDataValidator()); RuleFor(x => x.Version).Must(PracticeLocationValidation.IsVersion); }
}
public sealed class SubmitClinicApplicationCommandValidator : AbstractValidator<SubmitClinicApplicationCommand>
{
    public SubmitClinicApplicationCommandValidator()
    { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).Must(PracticeLocationValidation.IsVersion); RuleFor(x => x.Documents).NotNull().Must(x => x is { Count: >= 2 and <= 3 }); }
}
public sealed class ReviewClinicApplicationCommandValidator : AbstractValidator<ReviewClinicApplicationCommand>
{
    public ReviewClinicApplicationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.SubmissionId).NotEmpty(); RuleFor(x => x.Version).Must(PracticeLocationValidation.IsVersion);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000); RuleFor(x => x.Outcome).Must(x => x is ClinicApplicationStatus.Approved or ClinicApplicationStatus.Rejected or ClinicApplicationStatus.NeedsInformation);
        When(x => x.Outcome == ClinicApplicationStatus.Approved, () =>
        {
            RuleFor(x => x.LicenseDetailsVerified).Equal(true); RuleFor(x => x.IdentityVerified).Equal(true);
            RuleFor(x => x.AuthorityVerified).Equal(true); RuleFor(x => x.IndependentlyCorroborated).Equal(true);
        });
    }
}
public sealed class CreateClinicApplicationCommandHandler(IClinicService service) : IRequestHandler<CreateClinicApplicationCommand, Result<ClinicApplicationDto>>
{ public Task<Result<ClinicApplicationDto>> Handle(CreateClinicApplicationCommand r, CancellationToken ct) => service.CreateAsync(r.Input, r.TargetClinicId, ct); }
public sealed class UpdateClinicApplicationCommandHandler(IClinicService service) : IRequestHandler<UpdateClinicApplicationCommand, Result<ClinicApplicationDto>>
{ public Task<Result<ClinicApplicationDto>> Handle(UpdateClinicApplicationCommand r, CancellationToken ct) => service.UpdateDraftAsync(r.Id, r.Input, r.Version, ct); }
public sealed class SubmitClinicApplicationCommandHandler(IClinicService service) : IRequestHandler<SubmitClinicApplicationCommand, Result<ClinicApplicationDto>>
{ public Task<Result<ClinicApplicationDto>> Handle(SubmitClinicApplicationCommand r, CancellationToken ct) => service.SubmitAsync(r.Id, r.Version, r.Documents, ct); }
public sealed class ReviewClinicApplicationCommandHandler(IClinicService service) : IRequestHandler<ReviewClinicApplicationCommand, Result<ClinicApplicationDto>>
{ public Task<Result<ClinicApplicationDto>> Handle(ReviewClinicApplicationCommand r, CancellationToken ct) => service.ReviewAsync(r.Id, r.SubmissionId, r.Outcome, r.Reason, r.Version, r.LicenseDetailsVerified, r.IdentityVerified, r.AuthorityVerified, r.IndependentlyCorroborated, ct); }
