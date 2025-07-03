namespace NotificationService.Domain.Entities;

public class EmailNotification : Notification
{
    public EmailNotification() : base()
    {
    }
    
    public static EmailNotification Create(string title, string body, string sender, string receiver, bool isRead)
    {
        return new EmailNotification
        {
            Title = title,
            Body = body,
            Sender = sender,
            Receiver = receiver,
            IsRead = isRead
        };
    }
}