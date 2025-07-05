using NotificationService.Domain.Services;

namespace NotificationService.Application.Commands.Groups;

public record RemoveUserFromGroupCommand(string? GroupId, string? UserId) : ICommand<RemoveUserFromGroupCommandResponse>;
public record RemoveUserFromGroupCommandResponse(bool Success, string? Message = null);

public class RemoveUserFromGroupCommandHandler : ICommandHandler<RemoveUserFromGroupCommand, RemoveUserFromGroupCommandResponse>
{
    private readonly INotificationGroupService _notificationGroupService;
    private readonly ILogger<RemoveUserFromGroupCommandHandler> _logger;

    public RemoveUserFromGroupCommandHandler(INotificationGroupService notificationGroupService, ILogger<RemoveUserFromGroupCommandHandler> logger)
    {
        _notificationGroupService = notificationGroupService;
        _logger = logger;
    }

    public async Task<RemoveUserFromGroupCommandResponse> Handle(RemoveUserFromGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Removing user {Id} from notification group {Id}", request.UserId, request.GroupId);

        try
        {
            var result = await _notificationGroupService.RemoveUserFromGroupAsync(request.GroupId!, request.UserId!, cancellationToken);
            
            if (!result)
            {
                _logger.LogWarning("User {Id} not found in notification group {Id}", request.UserId, request.GroupId);
                return new RemoveUserFromGroupCommandResponse(result, "User not found");
            }
            
            _logger.LogInformation("User {Id} removed from notification group {Id}", request.UserId, request.GroupId);
            return new RemoveUserFromGroupCommandResponse(result, "User removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while removing user {Id} from notification group {Id}", request.UserId, request.GroupId);
            return new RemoveUserFromGroupCommandResponse(false, ex.Message);
        }
    }
}