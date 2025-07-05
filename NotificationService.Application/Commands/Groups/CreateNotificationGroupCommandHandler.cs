using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Services;

namespace NotificationService.Application.Commands.Groups;

public record CreateNotificationGroupCommand(string? Name, string? Creator, string? Description, List<string> Members)
    : ICommand<CreateNotificationGroupResponse>;

public record CreateNotificationGroupResponse(bool Success, string? Message = null);

public class
    CreateNotificationGroupCommandHandler : ICommandHandler<CreateNotificationGroupCommand,
    CreateNotificationGroupResponse>
{
    private readonly INotificationGroupService _notificationGroupService;
    private readonly ILogger<CreateNotificationGroupCommandHandler> _logger;

    public CreateNotificationGroupCommandHandler(INotificationGroupService notificationGroupService,
        ILogger<CreateNotificationGroupCommandHandler> logger)
    {
        _notificationGroupService = notificationGroupService;
        _logger = logger;
    }

    public async Task<CreateNotificationGroupResponse> Handle(CreateNotificationGroupCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Creator))
        {
            return new CreateNotificationGroupResponse(false, "Creator is null, can't create group");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new CreateNotificationGroupResponse(false, "Can't create group, name is empty");
        }

        try
        {
            var group = NotificationGroupEntity.Create(request.Name, request.Creator, request.Description,
                request.Members);

            var result = await _notificationGroupService.CreateGroupAsync(group, cancellationToken);

            if (!result)
            {
                return new CreateNotificationGroupResponse(false, "Can't create group");
            }

            return new CreateNotificationGroupResponse(true, "Group created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating notification group for user {Id}", request.Creator);
            return new CreateNotificationGroupResponse(false, "Error while creating notification group");
        }
    }
}