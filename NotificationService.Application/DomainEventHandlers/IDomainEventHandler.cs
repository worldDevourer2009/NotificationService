using NotificationService.Domain.DomainEvents;

namespace NotificationService.Application.DomainEventHandlers;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}