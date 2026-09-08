using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Domain.Repositories;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Order> Items, int TotalCount)> ListAsync(
        OrderStatus? status,
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> CountCreatedTodayAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> ListRecentAsync(int take, CancellationToken cancellationToken = default);
    Task<Dictionary<OrderStatus, int>> CountByStatusAsync(CancellationToken cancellationToken = default);
}
