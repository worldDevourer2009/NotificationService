using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Services;

public interface INotificationService
{
    Task SendInternalNotificationAsync(Guid userId, Notification notification);
    Task SendInternalNotificationAsync(string groupId, Notification notification, CancellationToken cancellationToken);
    Task SendEmailNotificationAsync(string email, Notification notification);
    Task SendTelegramNotificationAsync(string telegramId, Notification notification);
}