using OrderFlow.Domain.Abstractions;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Domain.Entities;

public sealed class OrderStatusHistory : Entity
{
    public Guid OrderId { get; private set; }
    public OrderStatus FromStatus { get; private set; }
    public OrderStatus ToStatus { get; private set; }
    public string? Reason { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }

    private OrderStatusHistory()
    {
    }

    internal OrderStatusHistory(
        Guid orderId,
        OrderStatus fromStatus,
        OrderStatus toStatus,
        Guid changedByUserId,
        string? reason)
        : base(Guid.NewGuid())
    {
        OrderId = orderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedByUserId = changedByUserId;
        Reason = reason;
        ChangedAtUtc = DateTime.UtcNow;
    }
}
