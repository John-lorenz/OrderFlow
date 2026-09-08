using FluentValidation;
using MediatR;
using OrderFlow.Application.Abstractions;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Domain.Repositories;

namespace OrderFlow.Application.Orders.Commands;

public sealed record ConfirmOrderCommand(Guid OrderId) : IRequest<OrderDto>;

public sealed class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmOrderCommandHandler(
        IOrderRepository orders,
        IProductRepository products,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _products = products;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Order", request.OrderId);

        var products = await _products.GetByIdsAsync(order.Items.Select(i => i.ProductId), cancellationToken);

        foreach (var item in order.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId)
                ?? throw new EntityNotFoundException("Product", item.ProductId);
            product.Reserve(item.Quantity);
        }

        order.Confirm(_currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderDto.From(order);
    }
}

public sealed record PayOrderCommand(Guid OrderId, string PaymentReference) : IRequest<OrderDto>;

public sealed class PayOrderCommandValidator : AbstractValidator<PayOrderCommand>
{
    public PayOrderCommandValidator()
    {
        RuleFor(x => x.PaymentReference).NotEmpty().MaximumLength(80);
    }
}

public sealed class PayOrderCommandHandler : IRequestHandler<PayOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public PayOrderCommandHandler(IOrderRepository orders, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(PayOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Order", request.OrderId);

        order.MarkAsPaid(_currentUser.UserId, request.PaymentReference);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderDto.From(order);
    }
}

public sealed record ShipOrderCommand(Guid OrderId, string TrackingNumber) : IRequest<OrderDto>;

public sealed class ShipOrderCommandValidator : AbstractValidator<ShipOrderCommand>
{
    public ShipOrderCommandValidator()
    {
        RuleFor(x => x.TrackingNumber).NotEmpty().MaximumLength(80);
    }
}

public sealed class ShipOrderCommandHandler : IRequestHandler<ShipOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ShipOrderCommandHandler(IOrderRepository orders, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Order", request.OrderId);

        order.Ship(_currentUser.UserId, request.TrackingNumber);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderDto.From(order);
    }
}

public sealed record CompleteOrderCommand(Guid OrderId) : IRequest<OrderDto>;

public sealed class CompleteOrderCommandHandler : IRequestHandler<CompleteOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteOrderCommandHandler(IOrderRepository orders, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(CompleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Order", request.OrderId);

        order.Complete(_currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderDto.From(order);
    }
}

public sealed record CancelOrderCommand(Guid OrderId, string Reason) : IRequest<OrderDto>;

public sealed class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(
        IOrderRepository orders,
        IProductRepository products,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _products = products;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new EntityNotFoundException("Order", request.OrderId);

        var shouldReleaseStock = order.ConfirmedAtUtc is not null || order.PaidAtUtc is not null;
        order.Cancel(_currentUser.UserId, request.Reason);

        if (shouldReleaseStock)
        {
            var products = await _products.GetByIdsAsync(order.Items.Select(i => i.ProductId), cancellationToken);
            foreach (var item in order.Items)
            {
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                product?.Release(item.Quantity);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderDto.From(order);
    }
}
