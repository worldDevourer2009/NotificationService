using NotificationService.Domain.Entities;
using NotificationService.Domain.VO;

namespace NotificationService.Domain.Services;

public interface INotificationSender
{
    Task SendNotificationAsync(Guid userId, Notification notification);
    Task SendNotificationAsync(string groupId, Notification notification, CancellationToken cancellationToken);
}