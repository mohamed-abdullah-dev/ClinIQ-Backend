using ClinIQ.Domain.Accounts;
using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Accounts.Commands.Sessions;

public sealed record LoginCommand(string Email, string Password, bool RememberMe, SessionClient Client) : IRequest<Result<SessionGrant>>;
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(1024);
        RuleFor(x => x.Client).IsInEnum();
    }
}
public sealed class LoginCommandHandler(ISessionService sessions) : IRequestHandler<LoginCommand, Result<SessionGrant>>
{
    public Task<Result<SessionGrant>> Handle(LoginCommand request, CancellationToken cancellationToken) =>
        sessions.LoginAsync(request.Email, request.Password, request.RememberMe, request.Client, cancellationToken);
}

public sealed record RefreshSessionCommand(string Token, SessionClient Client) : IRequest<Result<SessionGrant>>;
public sealed record LogoutSessionCommand(string Token, SessionClient Client) : IRequest<Result<Success>>;

public sealed class RefreshSessionCommandValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Client).IsInEnum();
    }
}
public sealed class LogoutSessionCommandValidator : AbstractValidator<LogoutSessionCommand>
{
    public LogoutSessionCommandValidator()
    {
        RuleFor(x => x.Token).NotNull().MaximumLength(128);
        RuleFor(x => x.Client).IsInEnum();
    }
}
public sealed class RefreshSessionCommandHandler(ISessionService sessions) : IRequestHandler<RefreshSessionCommand, Result<SessionGrant>>
{
    public Task<Result<SessionGrant>> Handle(RefreshSessionCommand request, CancellationToken cancellationToken) =>
        sessions.RefreshAsync(request.Token, request.Client, cancellationToken);
}
public sealed class LogoutSessionCommandHandler(ISessionService sessions) : IRequestHandler<LogoutSessionCommand, Result<Success>>
{
    public Task<Result<Success>> Handle(LogoutSessionCommand request, CancellationToken cancellationToken) =>
        sessions.LogoutAsync(request.Token, request.Client, cancellationToken);
}
