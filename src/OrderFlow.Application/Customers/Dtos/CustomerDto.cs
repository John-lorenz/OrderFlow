using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Customers.Dtos;

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string Email,
    string Document,
    string? Phone,
    string Street,
    string City,
    string State,
    string PostalCode,
    string Country,
    bool IsActive,
    DateTime CreatedAtUtc)
{
    public static CustomerDto From(Customer customer) => new(
        customer.Id,
        customer.Name,
        customer.Email.Value,
        customer.Document,
        customer.Phone,
        customer.Address.Street,
        customer.Address.City,
        customer.Address.State,
        customer.Address.PostalCode,
        customer.Address.Country,
        customer.IsActive,
        customer.CreatedAtUtc);
}
