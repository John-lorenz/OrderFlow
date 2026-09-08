using FluentAssertions;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Domain.ValueObjects;

namespace OrderFlow.Domain.UnitTests;

public class OrderLifecycleTests
{
    [Fact]
    public void Create_StartsInDraft()
    {
        var order = CreateDraftOrder();

        order.Status.Should().Be(Domain.Enums.OrderStatus.Draft);
        order.Items.Should().BeEmpty();
        order.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "OrderCreatedEvent");
    }

    [Fact]
    public void Confirm_WithItems_MovesToConfirmed()
    {
        var order = CreateDraftOrder();
        order.AddItem(CreateProduct(), 2);

        order.Confirm(Guid.NewGuid());

        order.Status.Should().Be(Domain.Enums.OrderStatus.Confirmed);
        order.ConfirmedAtUtc.Should().NotBeNull();
        order.TotalAmount.Should().Be(1700m);
    }

    [Fact]
    public void Confirm_WithoutItems_Throws()
    {
        var order = CreateDraftOrder();

        var act = () => order.Confirm(Guid.NewGuid());

        act.Should().Throw<BusinessRuleException>().Where(ex => ex.Code == "empty_order");
    }

    [Fact]
    public void CannotSkipPaymentBeforeShipping()
    {
        var order = CreateDraftOrder();
        order.AddItem(CreateProduct(), 1);
        order.Confirm(Guid.NewGuid());

        var act = () => order.Ship(Guid.NewGuid(), "BR123");

        act.Should().Throw<InvalidOrderTransitionException>();
    }

    [Fact]
    public void FullHappyPath_CompletesOrder()
    {
        var userId = Guid.NewGuid();
        var order = CreateDraftOrder();
        order.AddItem(CreateProduct(), 1);

        order.Confirm(userId);
        order.MarkAsPaid(userId, "PIX-001");
        order.Ship(userId, "TRACK-9");
        order.Complete(userId);

        order.Status.Should().Be(Domain.Enums.OrderStatus.Completed);
        order.History.Should().HaveCount(5);
        order.PaymentReference.Should().Be("PIX-001");
        order.TrackingNumber.Should().Be("TRACK-9");
    }

    [Fact]
    public void Cancel_FromShipped_IsRejected()
    {
        var userId = Guid.NewGuid();
        var order = CreateDraftOrder();
        order.AddItem(CreateProduct(), 1);
        order.Confirm(userId);
        order.MarkAsPaid(userId, "PIX-001");
        order.Ship(userId, "TRACK-9");

        var act = () => order.Cancel(userId, "Too late");

        act.Should().Throw<InvalidOrderTransitionException>();
    }

    [Fact]
    public void ItemsCannotChangeAfterConfirmation()
    {
        var order = CreateDraftOrder();
        order.AddItem(CreateProduct(), 1);
        order.Confirm(Guid.NewGuid());

        var act = () => order.AddItem(CreateProduct("CNT-40"), 1);

        act.Should().Throw<BusinessRuleException>().Where(ex => ex.Code == "order_not_draft");
    }

    [Fact]
    public void Product_Reserve_ThrowsWhenStockIsInsufficient()
    {
        var product = CreateProduct();

        var act = () => product.Reserve(99);

        act.Should().Throw<InsufficientStockException>();
    }

    private static Order CreateDraftOrder()
    {
        var customer = Customer.Create(
            "Atlantic Terminal",
            "ops@atlantic.dev",
            "11222333000181",
            "4730001111",
            Address.Create("Av. Portuaria 100", "Navegantes", "SC", "88370-000", "BR"));

        return Order.Create("OF-20260908-0001", customer, Guid.NewGuid());
    }

    private static Product CreateProduct(string sku = "CNT-20") =>
        Product.Create(sku, "Container 20ft", "Dry", 850m, 10);
}
