namespace NotificationService.Domain.Entities;

public class InternalNotification : Notification
{
    private InternalNotification() : base()
    {
    }

    public static InternalNotification Create(string title, string body, string sender, string receiver, bool isRead)
    {
        return new InternalNotification()
        {
            Title = title,
            Body = body,
            Sender = sender,
            Receiver = receiver,
            IsRead = isRead,
            CreatedAt = DateTime.UtcNow
        };
    }
}