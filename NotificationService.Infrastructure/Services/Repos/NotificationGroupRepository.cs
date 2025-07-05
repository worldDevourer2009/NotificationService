using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services.Repos;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Interfaces;

namespace NotificationService.Infrastructure.Services.Repos;

public class NotificationGroupRepository : INotificationGroupRepository
{
    private readonly IApplicationDbContext _applicationDbContext;
    private readonly ILogger<NotificationGroupRepository> _logger;

    public NotificationGroupRepository(IApplicationDbContext applicationDbContext,
        ILogger<NotificationGroupRepository> logger)
    {
        _applicationDbContext = applicationDbContext;
        _logger = logger;
    }

    public async Task<List<NotificationGroupEntity>> GetAllNotificationGroupsForUserAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all notification groups for user {Id}", userId.ToString());

        try
        {
            var allGroups = await _applicationDbContext.NotificationGroups
                .Where(x => x.Members.Contains(userId.ToString()))
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Got {Count} notification groups for user {Id}", allGroups.Count, userId.ToString());
            return allGroups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting all notification groups for user {Id}", userId.ToString());
            return new List<NotificationGroupEntity>();
        }
    }

    public async Task<NotificationGroupEntity?> GetNotificationGroupForUser(Guid userId, Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting notification group {GroupId} for {User}", notificationId.ToString(),
            userId.ToString());

        try
        {
            var group = await _applicationDbContext.NotificationGroups.FirstOrDefaultAsync(x => x.Id == notificationId,
                cancellationToken);

            if (group == null)
            {
                _logger.LogInformation("Notification group for user {Id} and notification {NotificationId} not found",
                    userId.ToString(), notificationId.ToString());
                return null;
            }

            if (!group.Members.Contains(userId.ToString()))
            {
                _logger.LogInformation("User {Id} is not a member of notification group {GroupId}", userId.ToString(),
                    notificationId.ToString());
                return null;
            }

            _logger.LogInformation("Group which was gotten {GroupId} for {User}", group.Id.ToString(),
                userId.ToString());
            return group;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error while getting notification group for user {Id} and notification {NotificationId}",
                userId.ToString(), notificationId.ToString());
            return null;
        }
    }

    public async Task<bool> AddNotificationGroupForUserAsync(NotificationGroupEntity notificationGroup,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding notification group for user {Id}", notificationGroup.Creator);

        try
        {
            if (notificationGroup.Members.Count == 0)
            {
                _logger.LogError("Notification group for user {Id} has no members", notificationGroup.Creator);
                return false;
            }

            if (await _applicationDbContext.NotificationGroups.ContainsAsync(notificationGroup, cancellationToken))
            {
                _logger.LogError("Notification group for user {Id} already exists", notificationGroup.Creator);
                return false;
            }

            await _applicationDbContext.NotificationGroups.AddAsync(notificationGroup, cancellationToken);
            await _applicationDbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Added notification group for user {Id}", notificationGroup.Creator);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while adding notification group for user {Id}", notificationGroup.Creator);
            return false;
        }
    }

    public async Task<bool> CreateGroupForUserAsync(NotificationGroupEntity group, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating notification group for user {Id}", group.Creator);

        try
        {
            if (group.Members.Count == 0)
            {
                _logger.LogError("Notification group for user {Id} has no members", group.Creator);
                return false;
            }

            if (await _applicationDbContext.NotificationGroups.ContainsAsync(group, cancellationToken))
            {
                _logger.LogError("Notification group for user {Id} already exists", group.Creator);
                return false;
            }
            
            await _applicationDbContext.NotificationGroups.AddAsync(group, cancellationToken);
            await _applicationDbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created notification group for user {Id}", group.Creator);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating notification group for user {Id}", group.Creator);
            return false;
        }
    }

    public async Task<bool> DeleteNotificationGroupForUserAsync(Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting notification group for user {Id}", notificationId.ToString());

        try
        {
            var group = await _applicationDbContext.NotificationGroups.FirstOrDefaultAsync(x => x.Id == notificationId,
                cancellationToken);

            if (group == null)
            {
                _logger.LogInformation("Notification group for user {Id} not found", notificationId.ToString());
                return false;
            }

            _applicationDbContext.NotificationGroups.Remove(group);
            await _applicationDbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Deleted notification group for user {Id}", notificationId.ToString());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting notification group for user {Id}", notificationId.ToString());
            return false;
        }
    }

    public async Task<bool> UpdateNotificationGroupForUserAsync(Guid notificationId,
        NotificationGroupEntity notificationGroup,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating notification group for user {Id}", notificationId.ToString());

        try
        {
            var dbGroup =
                await _applicationDbContext.NotificationGroups.Where(x => x.Id == notificationId)
                    .FirstOrDefaultAsync(cancellationToken);

            if (dbGroup is null)
            {
                _logger.LogInformation("Notification group for user {Id} not found", notificationId.ToString());
                return false;
            }
            
            var allMembers = dbGroup.Members.ToList();

            foreach (var member in notificationGroup.Members)
            {
                if (!allMembers.Contains(member))
                {
                    allMembers.Add(member);
                }
            }

            if (dbGroup.Name != notificationGroup.Name)
            {
                dbGroup.UpdateName(notificationGroup.Name);
            }

            if (dbGroup.Description != notificationGroup.Description)
            {
                dbGroup.UpdateDescription(notificationGroup.Description);
            }
            
            _logger.LogInformation("Updating notification group for user {Id}", notificationId.ToString());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating notification group for user {Id}", notificationId.ToString());
            return false;
        }
    }
}