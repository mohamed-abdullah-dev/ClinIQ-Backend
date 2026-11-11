using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Accounts.Commands.ResendConfirmation;

public sealed record ResendConfirmationCommand(string Email) : IRequest<Result<Success>>;

public sealed class ResendConfirmationCommandValidator : AbstractValidator<ResendConfirmationCommand>
{
    public ResendConfirmationCommandValidator() => RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(256);
}

public sealed class ResendConfirmationCommandHandler(IEmailConfirmationService confirmation)
    : IRequestHandler<ResendConfirmationCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ResendConfirmationCommand request, CancellationToken cancellationToken)
    {
        await confirmation.ResendAsync(request.Email, cancellationToken);
        return Result.Success;
    }
}
