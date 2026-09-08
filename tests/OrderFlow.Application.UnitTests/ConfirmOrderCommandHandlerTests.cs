using FluentAssertions;
using Moq;
using OrderFlow.Application.Abstractions;
using OrderFlow.Application.Orders.Commands;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Domain.Repositories;
using OrderFlow.Domain.ValueObjects;

namespace OrderFlow.Application.UnitTests;

public class ConfirmOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_ReservesStockAndConfirmsOrder()
    {
        var customer = Customer.Create("Acme", "acme@test.dev", "123", null, Address.Create("Rua 1", "Navegantes", "SC", "000", "BR"));
        var product = Product.Create("CNT-20", "Container", null, 100m, 5);
        var order = Order.Create("OF-1", customer, Guid.NewGuid());
        order.AddItem(product, 2);

        var orders = new Mock<IOrderRepository>();
        orders.Setup(x => x.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        var products = new Mock<IProductRepository>();
        products.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { product });

        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(x => x.UserId).Returns(Guid.NewGuid());

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new ConfirmOrderCommandHandler(orders.Object, products.Object, currentUser.Object, uow.Object);

        var result = await handler.Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None);

        result.Status.Should().Be(Domain.Enums.OrderStatus.Confirmed);
        product.StockQuantity.Should().Be(3);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrderDoesNotExist_ThrowsNotFound()
    {
        var orders = new Mock<IOrderRepository>();
        orders.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var handler = new ConfirmOrderCommandHandler(
            orders.Object,
            new Mock<IProductRepository>().Object,
            new Mock<ICurrentUser>().Object,
            new Mock<IUnitOfWork>().Object);

        var act = async () => await handler.Handle(new ConfirmOrderCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }
}
