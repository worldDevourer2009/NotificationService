namespace NotificationService.Domain.Exceptions.Notifications;

public class NotificationGroupEntityException : DomainException
{
    public string Reason { get; }
    
    public NotificationGroupEntityException(string message) : base(message)
    {
        Reason = message;
    }
}