using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Domain.ValueObjects;

public sealed class Address
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }
    public string Country { get; }

    private Address()
    {
        Street = City = State = PostalCode = Country = string.Empty;
    }

    private Address(string street, string city, string state, string postalCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public static Address Create(string street, string city, string state, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
        {
            throw new BusinessRuleException("invalid_address", "Street is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new BusinessRuleException("invalid_address", "City is required.");
        }

        return new Address(
            street.Trim(),
            city.Trim(),
            (state ?? string.Empty).Trim(),
            (postalCode ?? string.Empty).Trim(),
            string.IsNullOrWhiteSpace(country) ? "BR" : country.Trim());
    }

    public override string ToString() => $"{Street}, {City} - {State}, {PostalCode}, {Country}";
}
