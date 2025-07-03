namespace NotificationService.Domain.DomainEvents.Notifications;

public class NotificationDomainEvent : DomainEvent
{
    public Guid NotificationId { get; set; }
    public Guid RecipientId { get; set; }
    
    public NotificationDomainEvent(Guid notificationId, Guid recipientId, string eventType) : base(eventType)
    {
        NotificationId = notificationId;
        RecipientId = recipientId;
    }
}