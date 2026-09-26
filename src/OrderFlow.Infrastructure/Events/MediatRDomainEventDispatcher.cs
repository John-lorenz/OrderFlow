using MediatR;
using OrderFlow.Application.Abstractions;
using OrderFlow.Application.Events;
using OrderFlow.Domain.Abstractions;

namespace OrderFlow.Infrastructure.Events;

public sealed class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;

    public MediatRDomainEventDispatcher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(new DomainEventNotification(domainEvent), cancellationToken);
        }
    }
}
