using ClinIQ.Domain.Accounts;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Application.Features.Accounts;

public sealed record SessionUser(Guid Id, string FullName, string Email, string[] Roles);
public sealed record SessionGrant(Guid SessionId, SessionClient Client, SessionUser User, string AccessToken,
    string RefreshToken, DateTimeOffset AccessExpiresAtUtc, DateTimeOffset ExpiresAtUtc, bool RememberMe);
public sealed record AuthenticatedSession(Guid SessionId, SessionClient Client, SessionUser User);

public interface ISessionService
{
    Task<Result<SessionGrant>> LoginAsync(string email, string password, bool rememberMe, SessionClient client, CancellationToken ct);
    Task<Result<SessionGrant>> RefreshAsync(string token, SessionClient client, CancellationToken ct);
    Task<Result<Success>> LogoutAsync(string token, SessionClient client, CancellationToken ct);
    Task<AuthenticatedSession?> AuthenticateAsync(string token, SessionClient client, CancellationToken ct);
}

public static class SessionErrors
{
    public static readonly Error InvalidCredentials = Error.Unauthorized("Auth.InvalidCredentials", "Email or password is incorrect, or login is temporarily unavailable.");
    public static readonly Error InvalidSession = Error.Unauthorized("Auth.InvalidSession", "The session is invalid or expired. Please log in again.");
    public static readonly Error PendingReview = Error.Forbidden("Auth.DoctorPendingReview", "حسابك لسه تحت المراجعة.");
    public static readonly Error DoctorRejected = Error.Forbidden("Auth.DoctorRejected", "طلب تسجيلك كدكتور اترفض. اطلب رابط إعادة التقديم على بريدك لتصحيح الطلب.");
    public static readonly Error ClinicPendingReview = Error.Forbidden("Auth.ClinicPendingReview", "طلب تسجيل المنشأة لسه تحت المراجعة. الدخول متاح بعد الاعتماد.");
    public static readonly Error ClinicRejected = Error.Forbidden("Auth.ClinicRejected", "طلب تسجيل المنشأة اترفض. تقدر تعيد التقديم من نفس فورم التسجيل ببيانات حسابك.");
    public static readonly Error ClinicNeedsInformation = Error.Forbidden("Auth.ClinicNeedsInformation", "طلب تسجيل المنشأة محتاج استكمال. ابعت البيانات والمستندات المصححة من نفس فورم التسجيل ببيانات حسابك.");
    public static readonly Error EmailUnconfirmed = Error.Forbidden("Auth.EmailUnconfirmed", "Confirm your email before logging in.");
    public static readonly Error ClientNotAllowed = Error.Forbidden("Auth.ClientNotAllowed", "This account has no approved role for this client.");
    public static readonly Error Busy = Error.Conflict("Auth.SessionBusy", "The session is busy. Retry the request.");
}
