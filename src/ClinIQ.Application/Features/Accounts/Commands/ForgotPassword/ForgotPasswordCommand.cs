using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Accounts.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result<Success>>;
public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
}
public sealed class ForgotPasswordCommandHandler(IPasswordRecoveryService recovery)
    : IRequestHandler<ForgotPasswordCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        await recovery.RequestAsync(request.Email, cancellationToken);
        return Result.Success;
    }
}
