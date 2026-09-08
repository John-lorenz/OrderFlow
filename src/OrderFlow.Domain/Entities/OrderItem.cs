using OrderFlow.Domain.Abstractions;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Domain.Entities;

public sealed class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal => decimal.Round(UnitPrice * Quantity, 2);

    private OrderItem()
    {
    }

    internal OrderItem(Guid orderId, Product product, int quantity)
        : base(Guid.NewGuid())
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("invalid_quantity", "Item quantity must be greater than zero.");
        }

        OrderId = orderId;
        ProductId = product.Id;
        ProductName = product.Name;
        Sku = product.Sku;
        UnitPrice = product.UnitPrice;
        Quantity = quantity;
    }

    internal void Increase(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("invalid_quantity", "Item quantity must be greater than zero.");
        }

        Quantity += quantity;
    }

    internal void SetQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("invalid_quantity", "Item quantity must be greater than zero.");
        }

        Quantity = quantity;
    }
}
