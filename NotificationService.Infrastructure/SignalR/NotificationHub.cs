using Microsoft.AspNetCore.SignalR;

namespace NotificationService.Infrastructure.SignalR;

public class NotificationHub : Hub
{
    public async Task JoinUserAsync(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId,  $"user_{userId}");
    }
    
    public async Task LeaveUserAsync(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId,  $"user_{userId}");
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
}