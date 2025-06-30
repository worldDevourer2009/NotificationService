namespace NotificationService.Domain.Entities;

public class EmailNotification : Notification
{
    public string? Subject { get; set; }
    public string? HtmlBody { get; set; }
    
    public EmailNotification() : base()
    {
    }
    
    public static EmailNotification Create(string title, string body, string imageUrl, string targetUrl,
        string targetType, string sender, string receiver)
    {
        return new EmailNotification
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