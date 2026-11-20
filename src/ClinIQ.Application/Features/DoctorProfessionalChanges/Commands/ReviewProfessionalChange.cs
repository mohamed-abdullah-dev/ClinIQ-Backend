using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.DoctorProfessionalChanges.Commands;

public sealed record ReviewProfessionalChangeCommand(Guid Id, bool Approve, string Reason, string Version) : IRequest<Result<ProfessionalChangeSummary>>;
public sealed class ReviewProfessionalChangeCommandValidator : AbstractValidator<ReviewProfessionalChangeCommand>
{
    public ReviewProfessionalChangeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Version).Must(ProfessionalVersion.IsValid).WithMessage("Provide the current request version.");
    }
}
public sealed class ReviewProfessionalChangeCommandHandler(IProfessionalChangeService changes)
    : IRequestHandler<ReviewProfessionalChangeCommand, Result<ProfessionalChangeSummary>>
{
    public Task<Result<ProfessionalChangeSummary>> Handle(ReviewProfessionalChangeCommand request, CancellationToken ct) =>
        changes.ReviewAsync(request.Id, request.Approve, request.Reason, request.Version, ct);
}
