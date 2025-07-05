using NotificationService.Domain.Entities;

namespace NotificationService.Application.Services.Repos;

public interface IInternalNotificationRepository
{
    Task<List<InternalNotification>> GetAllNotificationsForUserAsync(Guid userId,
        CancellationToken cancellationToken = default);

    Task<InternalNotification?> GetNotificationForUser(Guid userId, Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<bool> AddNotificationForUserAsync(InternalNotification notification,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteNotificationForUserAsync(Guid notificationId, CancellationToken cancellationToken = default);

    Task<bool> UpdateNotificationForUserAsync(Guid notificationId, InternalNotification notification,
        CancellationToken cancellationToken = default);
}