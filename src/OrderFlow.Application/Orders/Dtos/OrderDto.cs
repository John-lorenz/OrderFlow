using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Orders.Dtos;

public sealed record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record OrderHistoryDto(
    Guid Id,
    OrderStatus FromStatus,
    OrderStatus ToStatus,
    string? Reason,
    Guid ChangedByUserId,
    DateTime ChangedAtUtc);

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    OrderStatus Status,
    decimal TotalAmount,
    string? Notes,
    string? PaymentReference,
    string? TrackingNumber,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ConfirmedAtUtc,
    DateTime? PaidAtUtc,
    DateTime? ShippedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? CancelledAtUtc,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderHistoryDto> History)
{
    public static OrderDto From(Order order) => new(
        order.Id,
        order.OrderNumber,
        order.CustomerId,
        order.Status,
        order.TotalAmount,
        order.Notes,
        order.PaymentReference,
        order.TrackingNumber,
        order.CancellationReason,
        order.CreatedAtUtc,
        order.UpdatedAtUtc,
        order.ConfirmedAtUtc,
        order.PaidAtUtc,
        order.ShippedAtUtc,
        order.CompletedAtUtc,
        order.CancelledAtUtc,
        order.Items.Select(item => new OrderItemDto(
            item.Id,
            item.ProductId,
            item.Sku,
            item.ProductName,
            item.UnitPrice,
            item.Quantity,
            item.LineTotal)).ToList(),
        order.History.Select(history => new OrderHistoryDto(
            history.Id,
            history.FromStatus,
            history.ToStatus,
            history.Reason,
            history.ChangedByUserId,
            history.ChangedAtUtc)).ToList());
}
