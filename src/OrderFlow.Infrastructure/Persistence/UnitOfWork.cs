using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Abstractions;
using OrderFlow.Domain.Abstractions;
using OrderFlow.Domain.Repositories;

namespace OrderFlow.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly OrderFlowDbContext _context;
    private readonly IDomainEventDispatcher _dispatcher;

    public UnitOfWork(OrderFlowDbContext context, IDomainEventDispatcher dispatcher)
    {
        _context = context;
        _dispatcher = dispatcher;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = _context.ChangeTracker
            .Entries<Entity>()
            .SelectMany(entry => entry.Entity.DomainEvents)
            .ToList();

        var result = await _context.SaveChangesAsync(cancellationToken);

        foreach (var entry in _context.ChangeTracker.Entries<Entity>())
        {
            entry.Entity.ClearDomainEvents();
        }

        await _dispatcher.DispatchAsync(domainEvents, cancellationToken);
        return result;
    }
}
