using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Behaviors;
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
        BindValidators(services);
        
        services.AddScoped<IExternalEventFactory, ExternalEventFactory>();
        services.AddScoped<IDomainEventHandler<NotificationDomainEvent>, NotificationSentDomainEventHandler>();
        
        return services;
    }

    private static void BindValidators(IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    }

    private static void BindDispatchers(IServiceCollection services)
    {
        services.AddScoped<IDomainDispatcher, DomainDispatcher>();
        services.AddScoped<IExternalEventDispatcher, ExternalEventDispatcher>();
    }
}