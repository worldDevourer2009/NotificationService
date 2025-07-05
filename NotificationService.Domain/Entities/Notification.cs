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
    public DateTime? CreatedAt { get; protected set; }

    public void MarkAsRead()
    {
        IsRead = true;
    }
    
    public void MarkAsDeleted()
    {
        IsDeleted = true;
    }

    public void SetNewTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title) || title == Title)
        {
            return;
        }
        
        Title = title;   
    }

    public void SetNewBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body) || body == Body)
        {
            return;
        }
        
        Body = body;
    }

    public void SetNewSender(string sender)
    {
        if (string.IsNullOrWhiteSpace(sender) || sender == Sender)
        {
            return;
        }
        
        Sender = sender;
    }
    
    public void MarkAsSent()
    {
        IsSent = true;
    }
}