using OrderFlow.Domain.Abstractions;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Domain.ValueObjects;

namespace OrderFlow.Domain.Entities;

public sealed class Customer : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public EmailAddress Email { get; private set; } = null!;
    public string Document { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public Address Address { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private Customer()
    {
    }

    private Customer(
        Guid id,
        string name,
        EmailAddress email,
        string document,
        string? phone,
        Address address)
        : base(id)
    {
        Name = name;
        Email = email;
        Document = document;
        Phone = phone;
        Address = address;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public static Customer Create(
        string name,
        string email,
        string document,
        string? phone,
        Address address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessRuleException("invalid_customer", "Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(document))
        {
            throw new BusinessRuleException("invalid_customer", "Customer document is required.");
        }

        return new Customer(
            Guid.NewGuid(),
            name.Trim(),
            EmailAddress.Create(email),
            document.Trim(),
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            address);
    }

    public void Update(string name, string email, string document, string? phone, Address address)
    {
        EnsureActive();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessRuleException("invalid_customer", "Customer name is required.");
        }

        Name = name.Trim();
        Email = EmailAddress.Create(email);
        Document = document.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Address = address;
        UpdatedAtUtc = DateTime.UtcNow;
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
            throw new BusinessRuleException("customer_inactive", "Customer is inactive.");
        }
    }
}
