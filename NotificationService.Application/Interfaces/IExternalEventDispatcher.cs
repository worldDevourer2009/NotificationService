using NotificationService.Domain.Events.ExternalEvents;

namespace NotificationService.Application.Interfaces;

public interface IExternalEventDispatcher
{
    Task DispatchAsync(IExternalEvent @event, CancellationToken cancellationToken = default);
}