using FluentValidation;
using MediatR;
using OrderFlow.Application.Abstractions;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Domain.Repositories;

namespace OrderFlow.Application.Orders.Commands;

public sealed record CreateOrderItemInput(Guid ProductId, int Quantity);

public sealed record CreateOrderCommand(Guid CustomerId, string? Notes, IReadOnlyList<CreateOrderItemInput>? Items)
    : IRequest<OrderDto>;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0);
        }).When(x => x.Items is not null);
    }
}

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly ICustomerRepository _customers;
    private readonly IProductRepository _products;
    private readonly IOrderNumberGenerator _orderNumbers;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(
        IOrderRepository orders,
        ICustomerRepository customers,
        IProductRepository products,
        IOrderNumberGenerator orderNumbers,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _customers = customers;
        _products = products;
        _orderNumbers = orderNumbers;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new EntityNotFoundException("Customer", request.CustomerId);

        var orderNumber = await _orderNumbers.NextAsync(cancellationToken);
        var order = Order.Create(orderNumber, customer, _currentUser.UserId, request.Notes);

        if (request.Items is { Count: > 0 })
        {
            var products = await _products.GetByIdsAsync(request.Items.Select(i => i.ProductId), cancellationToken);
            foreach (var item in request.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId)
                    ?? throw new EntityNotFoundException("Product", item.ProductId);
                order.AddItem(product, item.Quantity);
            }
        }

        await _orders.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderDto.From(order);
    }
}

public sealed record AddOrderItemCommand(Guid OrderId, Guid ProductId, int Quantity) : IRequest<OrderDto>;

public sealed class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemCommand>
{
    public AddOrderItemCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class AddOrderItemCommandHandler : IRequestHandler<AddOrderItemCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public AddOrderItemCommandHandler(IOrderRepository orders, IProductRepository products, IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Order", request.OrderId);
        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new EntityNotFoundException("Product", request.ProductId);

        order.AddItem(product, request.Quantity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderDto.From(order);
    }
}

public sealed record RemoveOrderItemCommand(Guid OrderId, Guid ItemId) : IRequest<OrderDto>;

public sealed class RemoveOrderItemCommandHandler : IRequestHandler<RemoveOrderItemCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveOrderItemCommandHandler(IOrderRepository orders, IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(RemoveOrderItemCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Order", request.OrderId);

        order.RemoveItem(request.ItemId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderDto.From(order);
    }
}
