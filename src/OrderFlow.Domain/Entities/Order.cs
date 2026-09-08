using OrderFlow.Domain.Abstractions;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Events;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Domain.Entities;

public sealed class Order : AggregateRoot
{
    private static readonly HashSet<OrderStatus> CancellableStatuses =
    [
        OrderStatus.Draft,
        OrderStatus.Confirmed,
        OrderStatus.Paid
    ];

    public string OrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public string? PaymentReference { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? CancellationReason { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime? ShippedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public decimal TotalAmount => Items.Sum(item => item.LineTotal);

    public List<OrderItem> Items { get; private set; } = [];
    public List<OrderStatusHistory> History { get; private set; } = [];

    private Order()
    {
    }

    private Order(Guid id, string orderNumber, Guid customerId, Guid createdByUserId, string? notes)
        : base(id)
    {
        OrderNumber = orderNumber;
        CustomerId = customerId;
        CreatedByUserId = createdByUserId;
        Notes = notes;
        Status = OrderStatus.Draft;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;

        AddHistory(OrderStatus.Draft, OrderStatus.Draft, createdByUserId, "Order created.");
        RaiseDomainEvent(new OrderCreatedEvent(Id, OrderNumber, CustomerId));
    }

    public static Order Create(string orderNumber, Customer customer, Guid createdByUserId, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new BusinessRuleException("invalid_order", "Order number is required.");
        }

        if (!customer.IsActive)
        {
            throw new BusinessRuleException("customer_inactive", "Cannot create an order for an inactive customer.");
        }

        return new Order(Guid.NewGuid(), orderNumber.Trim(), customer.Id, createdByUserId, notes?.Trim());
    }

    public OrderItem AddItem(Product product, int quantity)
    {
        EnsureDraft();

        if (!product.IsActive)
        {
            throw new BusinessRuleException("product_inactive", $"Product '{product.Sku}' is inactive.");
        }

        var existing = Items.FirstOrDefault(item => item.ProductId == product.Id);
        if (existing is not null)
        {
            existing.Increase(quantity);
            Touch();
            return existing;
        }

        var item = new OrderItem(Id, product, quantity);
        Items.Add(item);
        Touch();
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        EnsureDraft();

        var item = Items.FirstOrDefault(x => x.Id == itemId)
            ?? throw new EntityNotFoundException("Order item", itemId);

        Items.Remove(item);
        Touch();
    }

    public void Confirm(Guid userId)
    {
        EnsureTransition(OrderStatus.Confirmed);

        if (Items.Count == 0)
        {
            throw new BusinessRuleException("empty_order", "An order must have at least one item before confirmation.");
        }

        Status = OrderStatus.Confirmed;
        ConfirmedAtUtc = DateTime.UtcNow;
        AddHistory(OrderStatus.Draft, OrderStatus.Confirmed, userId, "Order confirmed and stock reserved.");
        Touch();
        RaiseDomainEvent(new OrderConfirmedEvent(Id, OrderNumber, TotalAmount));
    }

    public void MarkAsPaid(Guid userId, string paymentReference)
    {
        EnsureTransition(OrderStatus.Paid);

        if (string.IsNullOrWhiteSpace(paymentReference))
        {
            throw new BusinessRuleException("invalid_payment", "Payment reference is required.");
        }

        Status = OrderStatus.Paid;
        PaymentReference = paymentReference.Trim();
        PaidAtUtc = DateTime.UtcNow;
        AddHistory(OrderStatus.Confirmed, OrderStatus.Paid, userId, $"Payment received: {PaymentReference}.");
        Touch();
        RaiseDomainEvent(new OrderPaidEvent(Id, OrderNumber, PaymentReference));
    }

    public void Ship(Guid userId, string trackingNumber)
    {
        EnsureTransition(OrderStatus.Shipped);

        if (string.IsNullOrWhiteSpace(trackingNumber))
        {
            throw new BusinessRuleException("invalid_shipping", "Tracking number is required.");
        }

        Status = OrderStatus.Shipped;
        TrackingNumber = trackingNumber.Trim();
        ShippedAtUtc = DateTime.UtcNow;
        AddHistory(OrderStatus.Paid, OrderStatus.Shipped, userId, $"Shipped with tracking {TrackingNumber}.");
        Touch();
        RaiseDomainEvent(new OrderShippedEvent(Id, OrderNumber, TrackingNumber));
    }

    public void Complete(Guid userId)
    {
        EnsureTransition(OrderStatus.Completed);

        Status = OrderStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        AddHistory(OrderStatus.Shipped, OrderStatus.Completed, userId, "Order delivered.");
        Touch();
        RaiseDomainEvent(new OrderCompletedEvent(Id, OrderNumber));
    }

    public void Cancel(Guid userId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessRuleException("invalid_cancellation", "Cancellation reason is required.");
        }

        if (!CancellableStatuses.Contains(Status))
        {
            throw new InvalidOrderTransitionException(Status.ToString(), OrderStatus.Cancelled.ToString());
        }

        var from = Status;
        Status = OrderStatus.Cancelled;
        CancellationReason = reason.Trim();
        CancelledAtUtc = DateTime.UtcNow;
        AddHistory(from, OrderStatus.Cancelled, userId, CancellationReason);
        Touch();
        RaiseDomainEvent(new OrderCancelledEvent(Id, OrderNumber, CancellationReason));
    }

    public bool ShouldReleaseStockOnCancel() =>
        Status == OrderStatus.Cancelled && (ConfirmedAtUtc is not null || PaidAtUtc is not null);

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
        {
            throw new BusinessRuleException("order_not_draft", "Items can only be changed while the order is in Draft.");
        }
    }

    private void EnsureTransition(OrderStatus target)
    {
        var allowed = (Status, target) switch
        {
            (OrderStatus.Draft, OrderStatus.Confirmed) => true,
            (OrderStatus.Confirmed, OrderStatus.Paid) => true,
            (OrderStatus.Paid, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Completed) => true,
            _ => false
        };

        if (!allowed)
        {
            throw new InvalidOrderTransitionException(Status.ToString(), target.ToString());
        }
    }

    private void AddHistory(OrderStatus from, OrderStatus to, Guid userId, string? reason)
    {
        History.Add(new OrderStatusHistory(Id, from, to, userId, reason));
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
