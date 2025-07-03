using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.DomainEventHandlers;
using NotificationService.Application.DomainEventHandlers.Notifications;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Services.EventDispatchers;
using NotificationService.Application.Services.Factories;
using NotificationService.Domain.DomainEvents.Notifications;

namespace NotificationService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
        
        BindDispatchers(services);
        
        services.AddScoped<IExternalEventFactory, ExternalEventFactory>();
        services.AddScoped<IDomainEventHandler<NotificationDomainEvent>, NotificationSentDomainEventHandler>();
        
        return services;
    }

    private static void BindDispatchers(IServiceCollection services)
    {
        services.AddScoped<IDomainDispatcher, DomainDispatcher>();
        services.AddScoped<IExternalEventDispatcher, ExternalEventDispatcher>();
    }
}