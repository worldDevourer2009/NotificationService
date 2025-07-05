namespace NotificationService.Domain.Exceptions.Notifications;

public class EmailNotificationException : DomainException
{
    public string Reason { get; }
    
    public EmailNotificationException(string reason)
        : base($"Email notification failed because of : {reason}")
    {
        Reason = reason;
    }
}