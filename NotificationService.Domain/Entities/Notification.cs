namespace NotificationService.Domain.Entities;

public abstract class Notification : Entity
{
    public string? Title { get; protected set; }
    public string? Body { get; protected set; }
    public string? Sender { get; protected set; }
    public string? Receiver { get; protected set; }
    public bool? IsRead { get; protected set; }
    public bool? IsDeleted { get; protected set; }
    public bool? IsSent { get; protected set; }
    public DateTime? CreatedAt { get; private set; }

    public void MarkAsRead()
    {
        IsRead = true;
    }
    
    public void MarkAsDeleted()
    {
        IsDeleted = true;
    }
    
    public void MarkAsSent()
    {
        IsSent = true;
    }
}