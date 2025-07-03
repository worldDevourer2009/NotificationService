namespace NotificationService.Application.Interfaces;

public interface IEventHandler<in T>
{
    Task HandleAsync(T @event, CancellationToken cancellationToken = default);
}