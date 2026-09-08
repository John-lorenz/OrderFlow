using OrderFlow.Application.Abstractions;
using OrderFlow.Domain.Repositories;

namespace OrderFlow.Infrastructure.Persistence;

public sealed class OrderNumberGenerator : IOrderNumberGenerator
{
    private readonly IOrderRepository _orders;

    public OrderNumberGenerator(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<string> NextAsync(CancellationToken cancellationToken = default)
    {
        var count = await _orders.CountCreatedTodayAsync(cancellationToken);
        return $"OF-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }
}
