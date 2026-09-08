using MediatR;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Repositories;

namespace OrderFlow.Application.Dashboard.Queries;

public sealed record DashboardSummaryDto(
    int TotalOrders,
    decimal TotalRevenue,
    IReadOnlyDictionary<string, int> OrdersByStatus,
    IReadOnlyList<OrderDto> RecentOrders);

public sealed record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public sealed class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private static readonly OrderStatus[] RevenueStatuses =
    [
        OrderStatus.Paid,
        OrderStatus.Shipped,
        OrderStatus.Completed
    ];

    private readonly IOrderRepository _orders;

    public GetDashboardSummaryQueryHandler(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var counts = await _orders.CountByStatusAsync(cancellationToken);
        var recent = await _orders.ListRecentAsync(8, cancellationToken);
        var sample = await _orders.ListAsync(null, null, 1, 200, cancellationToken);
        var totalRevenue = sample.Items
            .Where(order => RevenueStatuses.Contains(order.Status))
            .Sum(order => order.TotalAmount);

        return new DashboardSummaryDto(
            counts.Values.Sum(),
            totalRevenue,
            counts.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value),
            recent.Select(OrderDto.From).ToList());
    }
}
