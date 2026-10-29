using ClinIQ.Domain.Common;

namespace ClinIQ.Domain.Accounts;

public enum SessionClient { Web = 1, Mobile = 2 }

public sealed class AuthSession : Entity
{
    public Guid UserId { get; private set; }
    public SessionClient Client { get; private set; }
    public string SecurityStamp { get; private set; } = null!;
    public string AccessHash { get; private set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset AccessExpiresAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public bool RememberMe { get; private set; }

    private AuthSession() { }
    private AuthSession(Guid id) : base(id) { }

    public static AuthSession Create(Guid userId, SessionClient client, string stamp, string accessHash,
        DateTimeOffset now, bool rememberMe)
    {
        if (userId == Guid.Empty || !Enum.IsDefined(client)) throw new ArgumentException("Invalid session owner/client.");
        ArgumentException.ThrowIfNullOrWhiteSpace(stamp);
        CheckHash(accessHash);
        return new AuthSession(Guid.NewGuid())
        {
            UserId = userId, Client = client, SecurityStamp = stamp, AccessHash = accessHash,
            CreatedAtUtc = now, AccessExpiresAtUtc = now.AddMinutes(10),
            ExpiresAtUtc = now.AddDays(rememberMe ? 7 : 1), RememberMe = rememberMe
        };
    }

    public bool IsActive(DateTimeOffset now) => RevokedAtUtc is null && now < ExpiresAtUtc;

    public void RotateAccess(string hash, DateTimeOffset now)
    {
        if (!IsActive(now)) throw new InvalidOperationException("Cannot renew an inactive session.");
        CheckHash(hash);
        AccessHash = hash;
        AccessExpiresAtUtc = now.AddMinutes(10) < ExpiresAtUtc ? now.AddMinutes(10) : ExpiresAtUtc;
    }

    public void Revoke(DateTimeOffset now) => RevokedAtUtc ??= now;

    internal static void CheckHash(string hash)
    {
        if (hash is null || hash.Length != 64 || !hash.All(Uri.IsHexDigit))
            throw new ArgumentException("Expected a SHA-256 hex digest.", nameof(hash));
    }
}

public sealed class SessionRefreshToken : Entity
{
    public Guid SessionId { get; private set; }
    public string SecretHash { get; private set; } = null!;
    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    private SessionRefreshToken() { }
    private SessionRefreshToken(Guid id) : base(id) { }
    public static SessionRefreshToken Create(Guid sessionId, string hash)
    {
        if (sessionId == Guid.Empty) throw new ArgumentException("Session is required.", nameof(sessionId));
        AuthSession.CheckHash(hash);
        return new SessionRefreshToken(Guid.NewGuid()) { SessionId = sessionId, SecretHash = hash };
    }

    public void Consume(DateTimeOffset now)
    {
        if (ConsumedAtUtc is not null) throw new InvalidOperationException("Refresh token was already consumed.");
        ConsumedAtUtc = now;
    }
}
