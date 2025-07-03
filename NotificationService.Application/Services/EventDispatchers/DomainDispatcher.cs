using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationService.Application.DomainEventHandlers;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.DomainEvents;

namespace NotificationService.Application.Services.EventDispatchers;

public class DomainDispatcher : IDomainDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainDispatcher> _logger;

    public DomainDispatcher(IServiceProvider serviceProvider, ILogger<DomainDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var domainEventHandlerType = typeof(IDomainEventHandler<>)
                .MakeGenericType(domainEvent.GetType());

            var handlers = (IEnumerable<object?>)_serviceProvider.GetServices(domainEventHandlerType);

            foreach (var handler in handlers)
            {
                if (handler == null)
                {
                    continue;
                }

                try
                {
                    dynamic domainEventHandler = handler;
                    dynamic domainEventTyped = domainEvent;

                    await domainEventHandler.HandleAsync(domainEventTyped, cancellationToken);
                    _logger.LogInformation("Domain event handled successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error occured while handling domain event: {domainEvent.GetType()}");
                }
            }
        }
    }
}