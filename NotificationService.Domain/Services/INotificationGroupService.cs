using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Services;

public interface INotificationGroupService
{
    Task<NotificationGroupEntity?> GetGroupAsync(string groupId, CancellationToken cancellationToken = default);
    Task<List<NotificationGroupEntity>?> GetGroupsForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> CreateGroupAsync(NotificationGroupEntity group, CancellationToken cancellationToken = default);
    Task<bool> AddUserToGroupAsync(string groupId, string userId, CancellationToken cancellationToken = default);
    Task<bool> RemoveUserFromGroupAsync(string groupId, string userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default);
    Task<bool> UpdateGroupAsync(NotificationGroupEntity group, CancellationToken cancellationToken = default);
}