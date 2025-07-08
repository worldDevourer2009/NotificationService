using NotificationService.Domain.Entities;
using NotificationService.Domain.Services;

namespace NotificationService.Application.Commands.InternalNotificationsCommandHandlers;

public record InternalNotificationCommand(string Id, string Title, string Message) : ICommand<InternalNotificationResponse>;
public record InternalNotificationResponse(bool Success, string? Message);

public class InternalNotificationCommandHandler : ICommandHandler<InternalNotificationCommand, InternalNotificationResponse>
{
    private readonly INotificationService _notificationService;

    public InternalNotificationCommandHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<InternalNotificationResponse> Handle(InternalNotificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.SendInternalNotificationAsync(
                request.Id,
                InternalNotification.Create(
                    request.Title,
                    request.Message,
                    "Internal",
                    request.Id,
                    false),
                cancellationToken);
            return new InternalNotificationResponse(true, "Notification sent");
        }
        catch (Exception ex)
        {
            return new InternalNotificationResponse(false, ex.Message);
        }
    }
}