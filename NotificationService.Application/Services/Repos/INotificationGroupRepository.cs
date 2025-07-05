using NotificationService.Domain.Entities;

namespace NotificationService.Application.Services.Repos;

public interface INotificationGroupRepository
{
    Task<List<NotificationGroupEntity>> GetAllNotificationGroupsForUserAsync(Guid userId,
        CancellationToken cancellationToken = default);

    Task<NotificationGroupEntity?> GetNotificationGroupForUser(Guid userId, Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<bool> AddNotificationGroupForUserAsync(NotificationGroupEntity notificationGroup,
        CancellationToken cancellationToken = default);
    
    Task<bool> CreateGroupForUserAsync(NotificationGroupEntity group, CancellationToken cancellationToken = default);

    Task<bool> DeleteNotificationGroupForUserAsync(Guid notificationId, CancellationToken cancellationToken = default);

    Task<bool> UpdateNotificationGroupForUserAsync(Guid notificationId, NotificationGroupEntity notificationGroup,
        CancellationToken cancellationToken = default);
}