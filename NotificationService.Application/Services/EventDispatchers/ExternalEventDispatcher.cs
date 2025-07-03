using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationService.Application.ExternalEvents.EventHandlers;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Events.ExternalEvents;

namespace NotificationService.Application.Services.EventDispatchers;

public class ExternalEventDispatcher : IExternalEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExternalEventDispatcher> _logger;

    public ExternalEventDispatcher(IServiceProvider serviceProvider, ILogger<ExternalEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IExternalEvent @event, CancellationToken cancellationToken = default)
    {
        var externalEventHandlerType = typeof(IExternalEventHandler<>)
            .MakeGenericType(@event.GetType());

        var handlers = (IEnumerable<object?>)_serviceProvider.GetServices(externalEventHandlerType);

        foreach (var handler in handlers)
        {
            if (handler == null)
            {
                continue;
            }

            try
            {
                dynamic externalHandlerTyped = handler;
                dynamic typedEvent = @event;

                await externalHandlerTyped.HandleAsync(typedEvent, cancellationToken);
                _logger.LogInformation("External event handled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occured while handling external event: {@event.GetType()}");
            }
        }
    }
}