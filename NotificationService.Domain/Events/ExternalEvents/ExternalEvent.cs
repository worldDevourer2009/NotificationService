namespace NotificationService.Domain.Events.ExternalEvents;

public abstract class ExternalEvent : IExternalEvent
{
    public string EventType { get; }
    public string EventData { get; }
    public DateTime ReceivedAt { get; }
    public string Source { get; }

    protected ExternalEvent(string eventType, string eventData, string source)
    {
        EventType = eventType;
        EventData = eventData;
        ReceivedAt = DateTime.UtcNow;
        Source = source;
    }
}