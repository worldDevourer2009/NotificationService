using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Services;

public interface INotificationService
{
    Task SendInternalNotificationAsync(Guid userId, Notification notification, CancellationToken cancellationToken = default);
    Task SendInternalNotificationAsync(string groupId, Notification notification, CancellationToken cancellationToken = default);
    Task SendEmailNotificationAsync(string email, Notification notification, CancellationToken cancellationToken = default);
    Task SendTelegramNotificationAsync(string telegramId, Notification notification, CancellationToken cancellationToken = default);
}