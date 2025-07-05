namespace NotificationService.Domain.Exceptions.Notifications;

public class TelegramNotificationNotFoundException : Exception
{
   public string Reason { get; }
    
   public TelegramNotificationNotFoundException(string reason)
      : base($"Telegram notification failed because of : {reason}")
   {
      Reason = reason;
   }
}