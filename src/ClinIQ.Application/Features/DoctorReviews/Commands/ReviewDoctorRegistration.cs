using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.DoctorReviews.Commands;

public sealed record ReviewDoctorRegistrationCommand(Guid Id, bool Approve, string Reason, string Version)
    : IRequest<Result<DoctorReviewReceipt>>;
public sealed class ReviewDoctorRegistrationCommandValidator : AbstractValidator<ReviewDoctorRegistrationCommand>
{
    public ReviewDoctorRegistrationCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(command => command.Version).Must(IsVersion).WithMessage("Provide the 8-byte Base64 version returned by the registration details.");
    }
    private static bool IsVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 24) return false;
        return Convert.TryFromBase64String(value, new byte[8], out var length) && length == 8;
    }
}
public sealed class ReviewDoctorRegistrationCommandHandler(IDoctorReviewService service)
    : IRequestHandler<ReviewDoctorRegistrationCommand, Result<DoctorReviewReceipt>>
{
    public Task<Result<DoctorReviewReceipt>> Handle(ReviewDoctorRegistrationCommand request, CancellationToken ct) =>
        service.ReviewAsync(request.Id, request.Approve, request.Reason, request.Version, ct);
}
