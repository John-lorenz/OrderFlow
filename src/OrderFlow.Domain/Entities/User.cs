using OrderFlow.Domain.Abstractions;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Domain.ValueObjects;

namespace OrderFlow.Domain.Entities;

public sealed class User : AggregateRoot
{
    public string FullName { get; private set; } = string.Empty;
    public EmailAddress Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }

    private User()
    {
    }

    private User(Guid id, string fullName, EmailAddress email, string passwordHash, UserRole role)
        : base(id)
    {
        FullName = fullName;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public static User Create(string fullName, string email, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new BusinessRuleException("invalid_user", "Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new BusinessRuleException("invalid_user", "Password hash is required.");
        }

        return new User(Guid.NewGuid(), fullName.Trim(), EmailAddress.Create(email), passwordHash, role);
    }

    public void ChangeRole(UserRole role)
    {
        EnsureActive();
        Role = role;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetPasswordHash(string passwordHash)
    {
        EnsureActive();

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new BusinessRuleException("invalid_user", "Password hash is required.");
        }

        PasswordHash = passwordHash;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RegisterLogin()
    {
        LastLoginAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = LastLoginAtUtc.Value;
    }

    public void Deactivate()
    {
        EnsureActive();
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new BusinessRuleException("user_inactive", "User is inactive.");
        }
    }
}
