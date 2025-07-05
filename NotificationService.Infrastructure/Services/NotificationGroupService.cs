using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services.Repos;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Services;
using NotificationService.Infrastructure.SignalR;

namespace NotificationService.Infrastructure.Services;

public class NotificationGroupService : INotificationGroupService
{
    private readonly ILogger<NotificationGroupService> _logger;
    private readonly INotificationGroupRepository _notificationGroupRepository;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationGroupService(ILogger<NotificationGroupService> logger, INotificationGroupRepository notificationGroupRepository, IHubContext<NotificationHub> hubContext)
    {
        _logger = logger;
        _notificationGroupRepository = notificationGroupRepository;
        _hubContext = hubContext;
    }

    public async Task<NotificationGroupEntity?> GetGroupAsync(string groupId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting notification group {GroupId}", groupId);

        try
        {
            if (!Guid.TryParse(groupId, out var groupGuid))
            {
                _logger.LogWarning("Invalid groupId {GroupId}", groupId);
                return null;
            }
            
            var group = await _notificationGroupRepository.GetNotificationGroupByIdAsync(groupGuid, cancellationToken);

            if (group == null)
            {
                _logger.LogInformation("Notification group {GroupId} not found", groupId);
                return null;
            }

            return group;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting notification group {GroupId}", groupId);
            return null;
        }
    }

    public async Task<List<NotificationGroupEntity>> GetGroupsForUserAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all notification groups for user {UserId}", userId);

        try
        {
            if (!Guid.TryParse(userId, out var guid))
            {
                _logger.LogWarning("User id {UserId} is not a valid guid", userId);
                return new List<NotificationGroupEntity>();
            }
            
            var groups = await _notificationGroupRepository.GetAllNotificationGroupsForUserAsync(guid, cancellationToken);
            
            _logger.LogInformation("Got {Count} notification groups for user {UserId}", groups.Count, userId);
            return groups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting notification groups for user {UserId}", userId);
            return new List<NotificationGroupEntity>();
        }
    }
    
    public async Task<bool> CreateGroupAsync(NotificationGroupEntity group,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating notification group for user {Id}", group.Creator);

        try
        {
            if (group.Members.Count == 0)
            {
                _logger.LogError("Notification group for user {Id} has no members", group.Creator);
                return false;
            }

            var result =
                await _notificationGroupRepository.CreateGroupForUserAsync(group,
                    cancellationToken);
            
            if (!result)
            {
                _logger.LogError("Notification group for user {Id} already exists", group.Creator);
                return false;
            }
            
            await AddMembersToSignalRGroupAsync(group.Id.ToString(), group.Members);
            
            _logger.LogInformation("Created notification group for user {Id}", group.Creator);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating notification group for user {Id}", group.Creator);
            return false;
        }
    }

    public async Task<bool> AddUserToGroupAsync(string groupId, string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding user {Id} to notification group {Id}", userId, groupId);

        try
        {
            var group = await _notificationGroupRepository.GetNotificationGroupForUser(
                Guid.Parse(groupId),
                Guid.Parse(groupId),
                cancellationToken);
            
            if (group == null)
            {
                _logger.LogError("Notification group for user {Id} not found", groupId);
                return false;
            }
            
            if (group.Members.Contains(userId))
            {
                _logger.LogInformation("User {UserId} is already a member of group {GroupId}", userId, groupId);
                return true;
            }
            
            group.Members.Add(userId);
            var result = await _notificationGroupRepository.UpdateNotificationGroupForUserAsync(Guid.Parse(groupId), group, cancellationToken);
            
            if (!result)
            {
                _logger.LogError("Notification group for user {Id} not found", groupId);
                return false;
            }
            
            await AddUserToSignalRGroupAsync(groupId, userId);
            return true;
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while adding user {Id} to notification group {Id}", userId, groupId);
            return false;
        }
    }

    public async Task<bool> RemoveUserFromGroupAsync(string groupId, string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing user {Id} from notification group {Id}", userId, groupId);

        try
        {
            var group = await _notificationGroupRepository.GetNotificationGroupByIdAsync(
                Guid.Parse(groupId),
                cancellationToken);
            
            if (group == null)
            {
                _logger.LogError("Notification group for user {Id} not found", groupId);
                return false;
            }
            
            group.Members.Remove(userId);
            var result = await _notificationGroupRepository.UpdateNotificationGroupForUserAsync(Guid.Parse(groupId), group, cancellationToken);
            
            if (!result)
            {
                _logger.LogError("Notification group for user {Id} not found", groupId);
                return false;
            }
            
            await RemoveUserFromSignalRGroupAsync(groupId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while removing user {Id} from notification group {Id}", userId, groupId);
            return false;
        }
    }

    public async Task<bool> DeleteGroupAsync(string groupId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting notification group for user {Id}", groupId);

        try
        {
            if (!Guid.TryParse(groupId, out var groupGuid))
            {
                _logger.LogWarning("Invalid groupId {GroupId}", groupId);
                return false;
            }
            var group = await _notificationGroupRepository.GetNotificationGroupForUser(groupGuid, groupGuid, cancellationToken);

            if (group == null)
            {
                _logger.LogError("Notification group for user {Id} not found", groupId);
                return false;
            }
            
            var result = await _notificationGroupRepository.DeleteNotificationGroupForUserAsync(groupGuid, cancellationToken);
            
            if (!result)
            {
                _logger.LogError("Notification group for user {Id} not found", groupId);
                return false;
            }
            
            await RemoveAllUsersFromSignalRGroupAsync(groupId, group.Members);
            
            _logger.LogInformation("Deleted notification group for user {Id}", groupId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting notification group for user {Id}", groupId);
            return false;
        }
    }

    public async Task<bool> UpdateGroupAsync(NotificationGroupEntity group,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating notification group for user {Id}", group.Creator);

        try
        {
            if (!Guid.TryParse(group.Creator, out var guid))
            {
                _logger.LogWarning("Invalid groupId {GroupId}", group.Creator);
                return false;
            }
            
            var result = await _notificationGroupRepository.UpdateNotificationGroupForUserAsync(group.Id, group, cancellationToken);
            
            if (!result)
            {
                _logger.LogError("Notification group for user {Id} not found", group.Creator);
                return false;
            }
            
            _logger.LogInformation("Updated notification group for user {Id}", group.Creator);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating notification group for user {Id}", group.Creator);
            return false;
        }
    }
    
    private async Task AddUserToSignalRGroupAsync(string groupId, string userId)
    {
        try
        {
            await _hubContext.Groups.AddToGroupAsync($"user_{userId}", $"notifGroup_{groupId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to SignalR group {GroupId}", userId, groupId);
        }
    }

    private async Task RemoveAllUsersFromSignalRGroupAsync(string groupId, List<string> members)
    {
        foreach (var member in members)
        {
            await RemoveUserFromSignalRGroupAsync(groupId, member);
        }
    }
    
    private async Task RemoveUserFromSignalRGroupAsync(string groupId, string userId)
    {
        try
        {
            await _hubContext.Groups.RemoveFromGroupAsync($"user_{userId}", $"notifGroup_{groupId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from SignalR group {GroupId}", userId, groupId);
        }
    }
    
    private async Task AddMembersToSignalRGroupAsync(string groupId, List<string> members)
    {
        foreach (var member in members)
        {
            await AddUserToSignalRGroupAsync(groupId, member);
        }
    }
}