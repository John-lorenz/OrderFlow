using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Domain.ValueObjects;

public sealed class EmailAddress : IEquatable<EmailAddress>
{
    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleException("invalid_email", "Email is required.");
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (!normalized.Contains('@') || normalized.StartsWith('@') || normalized.EndsWith('@'))
        {
            throw new BusinessRuleException("invalid_email", "Email format is invalid.");
        }

        return new EmailAddress(normalized);
    }

    public bool Equals(EmailAddress? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as EmailAddress);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    public static implicit operator string(EmailAddress email) => email.Value;
}
