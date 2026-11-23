using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.PracticeLocations;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.PracticeLocations.Commands;

public sealed record CreatePracticeLocationCommand(PracticeLocationDraft Input) : IRequest<Result<PracticeLocationDto>>;
public sealed record UpdatePracticeLocationDraftCommand(Guid Id, PracticeLocationDraft Input, string Version) : IRequest<Result<PracticeLocationDto>>;
public sealed record ChangePracticeLocationFeeCommand(Guid Id, decimal Amount, string Currency, string Version) : IRequest<Result<PracticeLocationDto>>;
public sealed record SubmitPracticeLocationCommand(Guid Id, string Version, IReadOnlyList<PracticeLocationUpload> Documents) : IRequest<Result<PracticeLocationDto>>;
public sealed record ReviewPracticeLocationCommand(Guid Id, Guid SubmissionId, PracticeLocationStatus Outcome, string Reason, string Version,
    bool LicenseMatchesAddress, bool DoctorWorkLinkVerified) : IRequest<Result<PracticeLocationDto>>;

public static class PracticeLocationValidation
{
    public static bool IsVersion(string? value)
    {
        Span<byte> bytes = stackalloc byte[8];
        return value is { Length: 12 } && Convert.TryFromBase64String(value, bytes, out var count) && count == 8 && Convert.ToBase64String(bytes) == value;
    }
}
public sealed class PracticeLocationDraftValidator : AbstractValidator<PracticeLocationDraft>
{
    public PracticeLocationDraftValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200); RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x).Must(x => PracticeLocationDetails.Create(x.Name, x.Address, x.Latitude, x.Longitude).IsSuccess).WithMessage("Provide valid physical details and coordinate pairs.");
        RuleFor(x => x.ConsultationFee).InclusiveBetween(0, ConsultationPrice.MaxAmount).PrecisionScale(18, 2, true);
        RuleFor(x => x.Currency).Must(x => ConsultationPrice.Create(0, x).IsSuccess).WithMessage("Only EGP is supported.");
    }
}
public sealed class CreatePracticeLocationCommandValidator : AbstractValidator<CreatePracticeLocationCommand>
{
    public CreatePracticeLocationCommandValidator() => RuleFor(x => x.Input).NotNull().SetValidator(new PracticeLocationDraftValidator());
}
public sealed class UpdatePracticeLocationDraftCommandValidator : AbstractValidator<UpdatePracticeLocationDraftCommand>
{
    public UpdatePracticeLocationDraftCommandValidator()
    { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).Must(PracticeLocationValidation.IsVersion); RuleFor(x => x.Input).NotNull().SetValidator(new PracticeLocationDraftValidator()); }
}
public sealed class ChangePracticeLocationFeeCommandValidator : AbstractValidator<ChangePracticeLocationFeeCommand>
{
    public ChangePracticeLocationFeeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).Must(PracticeLocationValidation.IsVersion);
        RuleFor(x => x.Amount).InclusiveBetween(0, ConsultationPrice.MaxAmount).PrecisionScale(18, 2, true);
        RuleFor(x => x.Currency).Must(x => ConsultationPrice.Create(0, x).IsSuccess).WithMessage("Only EGP is supported.");
    }
}
public sealed class SubmitPracticeLocationCommandValidator : AbstractValidator<SubmitPracticeLocationCommand>
{
    public SubmitPracticeLocationCommandValidator()
    { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).Must(PracticeLocationValidation.IsVersion); RuleFor(x => x.Documents).NotNull().Must(x => x is { Count: >= 1 and <= 2 }); }
}
public sealed class ReviewPracticeLocationCommandValidator : AbstractValidator<ReviewPracticeLocationCommand>
{
    public ReviewPracticeLocationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.SubmissionId).NotEmpty(); RuleFor(x => x.Version).Must(PracticeLocationValidation.IsVersion);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Outcome).Must(x => x is PracticeLocationStatus.Approved or PracticeLocationStatus.NeedsInformation or PracticeLocationStatus.Rejected);
        RuleFor(x => x.LicenseMatchesAddress).Equal(true).When(x => x.Outcome == PracticeLocationStatus.Approved);
        RuleFor(x => x.DoctorWorkLinkVerified).Equal(true).When(x => x.Outcome == PracticeLocationStatus.Approved);
    }
}
public sealed class CreatePracticeLocationCommandHandler(IPracticeLocationService service) : IRequestHandler<CreatePracticeLocationCommand, Result<PracticeLocationDto>>
{ public Task<Result<PracticeLocationDto>> Handle(CreatePracticeLocationCommand r, CancellationToken ct) => service.CreateAsync(r.Input, ct); }
public sealed class UpdatePracticeLocationDraftCommandHandler(IPracticeLocationService service) : IRequestHandler<UpdatePracticeLocationDraftCommand, Result<PracticeLocationDto>>
{ public Task<Result<PracticeLocationDto>> Handle(UpdatePracticeLocationDraftCommand r, CancellationToken ct) => service.UpdateDraftAsync(r.Id, r.Input, r.Version, ct); }
public sealed class ChangePracticeLocationFeeCommandHandler(IPracticeLocationService service) : IRequestHandler<ChangePracticeLocationFeeCommand, Result<PracticeLocationDto>>
{ public Task<Result<PracticeLocationDto>> Handle(ChangePracticeLocationFeeCommand r, CancellationToken ct) => service.ChangeFeeAsync(r.Id, r.Amount, r.Currency, r.Version, ct); }
public sealed class SubmitPracticeLocationCommandHandler(IPracticeLocationService service) : IRequestHandler<SubmitPracticeLocationCommand, Result<PracticeLocationDto>>
{ public Task<Result<PracticeLocationDto>> Handle(SubmitPracticeLocationCommand r, CancellationToken ct) => service.SubmitAsync(r.Id, r.Version, r.Documents, ct); }
public sealed class ReviewPracticeLocationCommandHandler(IPracticeLocationService service) : IRequestHandler<ReviewPracticeLocationCommand, Result<PracticeLocationDto>>
{ public Task<Result<PracticeLocationDto>> Handle(ReviewPracticeLocationCommand r, CancellationToken ct) => service.ReviewAsync(r.Id, r.SubmissionId, r.Outcome, r.Reason, r.Version, r.LicenseMatchesAddress, r.DoctorWorkLinkVerified, ct); }
