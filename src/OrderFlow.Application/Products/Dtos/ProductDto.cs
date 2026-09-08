using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Products.Dtos;

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    int StockQuantity,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static ProductDto From(Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Description,
        product.UnitPrice,
        product.StockQuantity,
        product.IsActive,
        product.CreatedAtUtc,
        product.UpdatedAtUtc);
}
