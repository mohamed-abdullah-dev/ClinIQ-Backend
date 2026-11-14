using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Application.Features.Accounts;

public interface IPasswordRecoveryService
{
    Task RequestAsync(string email, CancellationToken cancellationToken);
    Task<Result<Success>> ResetAsync(Guid userId, string token, string newPassword, CancellationToken cancellationToken);
}

public static class PasswordRecoveryErrors
{
    public static readonly Error InvalidToken = Error.Validation("Account.InvalidPasswordReset",
        "The password reset link is invalid or expired.", "Token");
    public static readonly Error Busy = Error.Conflict("Account.PasswordRecoveryBusy", "Password recovery is busy. Please retry.");
}
