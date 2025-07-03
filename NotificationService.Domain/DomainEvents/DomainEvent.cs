namespace NotificationService.Domain.DomainEvents;

public abstract class DomainEvent : IDomainEvent
{
    public Guid? Id { get; }
    public DateTime CreatedAt { get; }
    public string? EventType { get; }
    
    protected DomainEvent(string eventType)
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        EventType = eventType;
    }
}