using ClinIQ.Application.Features.Accounts;
using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Doctors;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.DoctorResubmissions.Commands;

public sealed record ResubmitDoctorCommand(string Token, string ProfessionalName, string PhoneNumber,
    string RegistrationNumber, Guid SpecialtyId, ProfessionalGrade ProfessionalGrade,
    IReadOnlyList<RegistrationUpload> Documents) : IRequest<Result<DoctorResubmissionReceipt>>;

public sealed class ResubmitDoctorCommandValidator : AbstractValidator<ResubmitDoctorCommand>
{
    public ResubmitDoctorCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().Length(76);
        RuleFor(x => x.ProfessionalName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).NotEmpty().Matches(@"^\+?[0-9]{7,15}$");
        RuleFor(x => x.RegistrationNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SpecialtyId).NotEmpty();
        RuleFor(x => x.ProfessionalGrade).IsInEnum();
        RuleFor(x => x.Documents).NotNull().Must(x => x is { Count: >= 3 and <= 4 });
    }
}

public sealed class ResubmitDoctorCommandHandler(IDoctorResubmissionService service)
    : IRequestHandler<ResubmitDoctorCommand, Result<DoctorResubmissionReceipt>>
{
    public Task<Result<DoctorResubmissionReceipt>> Handle(ResubmitDoctorCommand request, CancellationToken ct) => service.SubmitAsync(request, ct);
}

public sealed record RequestDoctorResubmissionLinkCommand(string Email) : IRequest<Result<Success>>;
public sealed class RequestDoctorResubmissionLinkCommandValidator : AbstractValidator<RequestDoctorResubmissionLinkCommand>
{
    public RequestDoctorResubmissionLinkCommandValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
}
public sealed class RequestDoctorResubmissionLinkCommandHandler(IDoctorResubmissionService service)
    : IRequestHandler<RequestDoctorResubmissionLinkCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(RequestDoctorResubmissionLinkCommand request, CancellationToken ct)
    {
        await service.RequestLinkAsync(request.Email, ct);
        return Result.Success;
    }
}
