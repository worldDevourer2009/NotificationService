using NotificationService.Domain.Services;

namespace NotificationService.Application.Commands.Groups;

public record DeleteGroupCommand(string? GroupId) : ICommand<DeleteGroupCommandResponse>;
public record DeleteGroupCommandResponse(bool Success, string? Message = null);

public class DeleteGroupCommandHandler : ICommandHandler<DeleteGroupCommand, DeleteGroupCommandResponse>
{
    private readonly INotificationGroupService _notificationGroupService;
    private readonly ILogger<DeleteGroupCommandHandler> _logger;

    public DeleteGroupCommandHandler(INotificationGroupService notificationGroupService, ILogger<DeleteGroupCommandHandler> logger)
    {
        _notificationGroupService = notificationGroupService;
        _logger = logger;
    }

    public async Task<DeleteGroupCommandResponse> Handle(DeleteGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting notification group");

        try
        {
            var result = await _notificationGroupService.DeleteGroupAsync(request.GroupId!, cancellationToken);
            
            if (!result)
            {
                _logger.LogError("Error while deleting notification group : {GroupId}", request.GroupId);
                return new DeleteGroupCommandResponse(false, "Error while deleting notification group");
            }
            
            _logger.LogInformation("Notification group deleted with Id : {GroupId}", request.GroupId);
            return new DeleteGroupCommandResponse(true, "Notification group deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting notification group");
            return new DeleteGroupCommandResponse(false, "Error while deleting notification group");
        }
    }
}