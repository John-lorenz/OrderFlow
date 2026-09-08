using OrderFlow.Domain.Abstractions;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Domain.Entities;

public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string? CreatedByIp { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken()
    {
    }

    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTime expiresAtUtc, string? createdByIp)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTime.UtcNow;
        CreatedByIp = createdByIp;
    }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAtUtc, string? createdByIp)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new BusinessRuleException("invalid_refresh_token", "Token hash is required.");
        }

        return new RefreshToken(Guid.NewGuid(), userId, tokenHash, expiresAtUtc, createdByIp);
    }

    public void Revoke(string? replacedByTokenHash = null)
    {
        if (IsRevoked)
        {
            return;
        }

        RevokedAtUtc = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
