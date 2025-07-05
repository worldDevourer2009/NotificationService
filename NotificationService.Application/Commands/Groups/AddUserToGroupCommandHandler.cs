using NotificationService.Domain.Services;

namespace NotificationService.Application.Commands.Groups;

public record AddUserToGroupCommand(string? GroupId, string? UserId) : ICommand<AddUserToGroupResponse>;
public record AddUserToGroupResponse(bool Success, string? Message = null);

public class AddUserToGroupCommandHandler : ICommandHandler<AddUserToGroupCommand, AddUserToGroupResponse>
{
    private readonly INotificationGroupService _notificationGroupService;
    private readonly ILogger<AddUserToGroupCommandHandler> _logger;

    public AddUserToGroupCommandHandler(INotificationGroupService notificationGroupService, ILogger<AddUserToGroupCommandHandler> logger)
    {
        _notificationGroupService = notificationGroupService;
        _logger = logger;
    }

    public async Task<AddUserToGroupResponse> Handle(AddUserToGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding user {Id} to notification group {Id}", request.UserId, request.GroupId);

        try
        {
            var result = await _notificationGroupService.AddUserToGroupAsync(request.GroupId, request.UserId, cancellationToken);
            
            if (!result)
            {
                _logger.LogWarning("User {Id} not found in notification group {Id}", request.UserId, request.GroupId);
                return new AddUserToGroupResponse(result, "User not found");
            }
            
            _logger.LogInformation("User {Id} added to notification group {Id}", request.UserId, request.GroupId);
            return new AddUserToGroupResponse(result, "User added successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while adding user {Id} to notification group {Id}", request.UserId, request.GroupId);
            return new AddUserToGroupResponse(false, ex.Message);
        }
    }
}