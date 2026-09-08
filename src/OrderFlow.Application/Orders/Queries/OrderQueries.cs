using MediatR;
using OrderFlow.Application.Common;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Domain.Repositories;

namespace OrderFlow.Application.Orders.Queries;

public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto>;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orders;

    public GetOrderByIdQueryHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException("Order", request.Id);

        return OrderDto.From(order);
    }
}

public sealed record ListOrdersQuery(OrderStatus? Status, Guid? CustomerId, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<OrderDto>>;

public sealed class ListOrdersQueryHandler : IRequestHandler<ListOrdersQuery, PagedResult<OrderDto>>
{
    private readonly IOrderRepository _orders;

    public ListOrdersQueryHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<PagedResult<OrderDto>> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var (items, total) = await _orders.ListAsync(request.Status, request.CustomerId, page, pageSize, cancellationToken);
        return new PagedResult<OrderDto>(items.Select(OrderDto.From).ToList(), total, page, pageSize);
    }
}
