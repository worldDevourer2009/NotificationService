using Microsoft.Extensions.Logging;
using NotificationService.Application.ExternalEvents.AuthService;
using NotificationService.Application.ExternalEvents.EventHandlers;
using NotificationService.Domain.Services;

namespace NotificationService.Application.ExternalEvents.ExternalEventHandlers;

public class UserSignedUpExternalEventHandler : IExternalEventHandler<UserSignedUpExternalEvent>
{
    private readonly ILogger<UserSignedUpExternalEventHandler> _logger;
    private readonly INotificationService _notificationService;

    public UserSignedUpExternalEventHandler(ILogger<UserSignedUpExternalEventHandler> logger, INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task HandleAsync(UserSignedUpExternalEvent value, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User signed up external event processing");
        var notification = UserSignedUpExternalEvent.Create(value.EventType, value.EventData, value.Source);
        
    }
}