using MediatR;
using Microsoft.Extensions.Logging;
using OrderFlow.Domain.Abstractions;

namespace OrderFlow.Application.Events;

public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;

public sealed class DomainEventLoggingHandler : INotificationHandler<DomainEventNotification>
{
    private readonly ILogger<DomainEventLoggingHandler> _logger;

    public DomainEventLoggingHandler(ILogger<DomainEventLoggingHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(DomainEventNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Domain event {EventType} dispatched for {EventId} at {OccurredOnUtc}",
            notification.DomainEvent.GetType().Name,
            notification.DomainEvent.EventId,
            notification.DomainEvent.OccurredOnUtc);

        return Task.CompletedTask;
    }
}
