using NotificationService.Domain.Entities;
using NotificationService.Domain.Services;

namespace NotificationService.Application.Commands.Groups;

public record UpdateGroupCommand(string? GroupId, string? Creator, string? Name, string? Description, List<string> Members) : ICommand<UpdateGroupCommandResponse>;
public record UpdateGroupCommandResponse(bool Success, string? Message = null);

public class UpdateGroupCommandHandler : ICommandHandler<UpdateGroupCommand, UpdateGroupCommandResponse>
{
    private readonly INotificationGroupService _notificationGroupService;
    private readonly ILogger<UpdateGroupCommandHandler> _logger;

    public UpdateGroupCommandHandler(INotificationGroupService notificationGroupService, ILogger<UpdateGroupCommandHandler> logger)
    {
        _notificationGroupService = notificationGroupService;
        _logger = logger;
    }

    public async Task<UpdateGroupCommandResponse> Handle(UpdateGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating notification group");
        
        try
        {
            if (string.IsNullOrWhiteSpace(request.Creator))
            {
                _logger.LogError("Creator is required");
                return new UpdateGroupCommandResponse(false, "Creator is required");
            }
            
            var group = NotificationGroupEntity.Create(request.Name, request.Description, request.Creator, request.Members);

            var result = await _notificationGroupService.UpdateGroupAsync(group, cancellationToken);
            
            if (!result)
            {
                _logger.LogError("Error while updating notification group");
                return new UpdateGroupCommandResponse(false, "Error while updating notification group");
            }
            
            _logger.LogInformation("Notification group updated");
            return new UpdateGroupCommandResponse(true, "Notification group updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating notification group");
            return new UpdateGroupCommandResponse(false, "Error while updating notification group");
        }
    }
}