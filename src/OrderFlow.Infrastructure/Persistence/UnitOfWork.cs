using OrderFlow.Domain.Repositories;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly OrderFlowDbContext _context;

    public UnitOfWork(OrderFlowDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
