using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Accounts.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(Guid UserId, string Token) : IRequest<Result<Success>>;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Token).NotEmpty().MaximumLength(4096);
    }
}

public sealed class ConfirmEmailCommandHandler(IEmailConfirmationService confirmation)
    : IRequestHandler<ConfirmEmailCommand, Result<Success>>
{
    public Task<Result<Success>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken) =>
        confirmation.ConfirmAsync(request.UserId, request.Token, cancellationToken);
}
