using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Accounts.Commands.ResetPassword;

public sealed record ResetPasswordCommand(Guid UserId, string Token, string NewPassword) : IRequest<Result<Success>>;
public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty().MaximumLength(4096);
        RuleFor(x => x.NewPassword).NotEmpty().MaximumLength(128);
    }
}
public sealed class ResetPasswordCommandHandler(IPasswordRecoveryService recovery)
    : IRequestHandler<ResetPasswordCommand, Result<Success>>
{
    public Task<Result<Success>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken) =>
        recovery.ResetAsync(request.UserId, request.Token, request.NewPassword, cancellationToken);
}
