namespace NotificationService.Domain.Entities;

public class TelegramNotification : Notification
{
    public TelegramNotification() : base()
    {
    }
    
    public static TelegramNotification Create(string title, string body, string sender, string receiver, bool isRead)
    {
        return new TelegramNotification
        {
            Title = title,
            Body = body,
            Sender = sender,
            Receiver = receiver,
            IsRead = isRead
        };
    }
}