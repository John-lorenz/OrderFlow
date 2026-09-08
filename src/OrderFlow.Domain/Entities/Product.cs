using OrderFlow.Domain.Abstractions;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Domain.Entities;

public sealed class Product : AggregateRoot
{
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private Product()
    {
    }

    private Product(Guid id, string sku, string name, string? description, decimal unitPrice, int stockQuantity)
        : base(id)
    {
        Sku = sku;
        Name = name;
        Description = description;
        UnitPrice = unitPrice;
        StockQuantity = stockQuantity;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public static Product Create(string sku, string name, string? description, decimal unitPrice, int stockQuantity)
    {
        Validate(sku, name, unitPrice, stockQuantity);

        return new Product(
            Guid.NewGuid(),
            sku.Trim().ToUpperInvariant(),
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            decimal.Round(unitPrice, 2),
            stockQuantity);
    }

    public void Update(string name, string? description, decimal unitPrice)
    {
        EnsureActive();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessRuleException("invalid_product", "Product name is required.");
        }

        if (unitPrice <= 0)
        {
            throw new BusinessRuleException("invalid_product", "Unit price must be greater than zero.");
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UnitPrice = decimal.Round(unitPrice, 2);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reserve(int quantity)
    {
        EnsureActive();

        if (quantity <= 0)
        {
            throw new BusinessRuleException("invalid_quantity", "Quantity must be greater than zero.");
        }

        if (StockQuantity < quantity)
        {
            throw new InsufficientStockException(Sku, StockQuantity, quantity);
        }

        StockQuantity -= quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Release(int quantity)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleException("invalid_quantity", "Quantity must be greater than zero.");
        }

        StockQuantity += quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AdjustStock(int quantity)
    {
        EnsureActive();

        if (quantity < 0)
        {
            throw new BusinessRuleException("invalid_stock", "Stock cannot be negative.");
        }

        StockQuantity = quantity;
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
            throw new BusinessRuleException("product_inactive", $"Product '{Sku}' is inactive.");
        }
    }

    private static void Validate(string sku, string name, decimal unitPrice, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new BusinessRuleException("invalid_product", "SKU is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BusinessRuleException("invalid_product", "Product name is required.");
        }

        if (unitPrice <= 0)
        {
            throw new BusinessRuleException("invalid_product", "Unit price must be greater than zero.");
        }

        if (stockQuantity < 0)
        {
            throw new BusinessRuleException("invalid_product", "Stock cannot be negative.");
        }
    }
}
