using Microsoft.Extensions.Logging;
using NotificationService.Application.ExternalEvents.AuthService;
using NotificationService.Application.ExternalEvents.EventHandlers;

namespace NotificationService.Application.ExternalEvents.ExternalEventHandlers;

public class UserSignedUpExternalEventHandler : IExternalEventHandler<UserSignedUpExternalEvent>
{
    private readonly ILogger<UserSignedUpExternalEventHandler> _logger;

    public UserSignedUpExternalEventHandler(ILogger<UserSignedUpExternalEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(UserSignedUpExternalEvent value, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User signed up external event processing");
    }
}