using NotificationService.Domain.Events.ExternalEvents;

namespace NotificationService.Application.ExternalEvents.EventHandlers;

public interface IExternalEventHandler<in TExternalEvent> 
    where TExternalEvent : IExternalEvent
{
    Task HandleAsync(TExternalEvent value, CancellationToken cancellationToken = default);
}