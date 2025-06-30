namespace NotificationService.Domain.Entities;

public class TelegramNotification : Notification
{
    public TelegramNotification() : base()
    {
    }
    
    public static TelegramNotification Create(string title, string body, string imageUrl, string targetUrl,
        string targetType, string sender, string receiver)
    {
        return new TelegramNotification
        {
            Title = title,
            Body = body,
            ImageUrl = imageUrl,
            TargetUrl = targetUrl,
            TargetType = targetType,
            Sender = sender,
            Receiver = receiver
        };
    }
}