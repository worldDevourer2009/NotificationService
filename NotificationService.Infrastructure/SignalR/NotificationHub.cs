using Microsoft.AspNetCore.SignalR;
using NotificationService.Application.Services.Repos;

namespace NotificationService.Infrastructure.SignalR;

public class NotificationHub : Hub
{
    private readonly INotificationGroupRepository _notificationGroupRepository;

    public NotificationHub(INotificationGroupRepository notificationGroupRepository)
    {
        _notificationGroupRepository = notificationGroupRepository;
    }

    public async Task JoinUserAsync(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    public async Task LeaveUserAsync(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    public async Task JoinNotificationGroupAsync(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"notifGroup_{groupId}");
    }

    public async Task LeaveNotificationGroupAsync(string groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"notifGroup_{groupId}");
    }

    public async Task JoinSubgroupAsync(string groupId, string subgroupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"notifGroup_{groupId}_sub_{subgroupId}");
    }

    public async Task LeaveSubgroupAsync(string groupId, string subgroupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"notifGroup_{groupId}_sub_{subgroupId}");
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;

        if (string.IsNullOrWhiteSpace(userId))
        {
            await base.OnConnectedAsync();
            return;
        }
        
        await JoinUserAsync(userId);
        
        await JoinUserToAllNotificationGroupsAsync(userId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;

        if (string.IsNullOrWhiteSpace(userId))
        {
            await base.OnDisconnectedAsync(exception);
            return;
        }

        await LeaveUserAsync(userId);
        await base.OnDisconnectedAsync(exception);
    }
    
    private async Task JoinUserToAllNotificationGroupsAsync(string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return;
            }

            var groups = await _notificationGroupRepository.GetAllNotificationGroupsForUserAsync(userGuid);

            foreach (var group in groups)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"notifGroup_{group.Id}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error joining user {userId} to notification groups: {ex.Message}");
        }
    }
}