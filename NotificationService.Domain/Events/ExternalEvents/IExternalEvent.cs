namespace NotificationService.Domain.Events.ExternalEvents;

public interface IExternalEvent
{
    string EventType { get; }
    string EventData { get; }
    DateTime ReceivedAt { get; }
    string Source { get; }
}