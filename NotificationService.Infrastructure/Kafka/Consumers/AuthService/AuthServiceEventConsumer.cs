using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Configurations;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Services.Factories;

namespace NotificationService.Infrastructure.Kafka.Consumers.AuthService;

public class AuthServiceEventConsumer : KafkaConsumerBase
{
    public AuthServiceEventConsumer(
        ILogger<KafkaConsumerBase> logger,
        IServiceProvider serviceProvider, 
        IOptions<KafkaSettings> kafkaSettings, 
        IEnumerable<string> topics) 
        : base(logger, serviceProvider, kafkaSettings, topics)
    {
    }

    protected override async Task HandleEventAsync(string eventType, string value, IServiceProvider scopeServiceProvider)
    {
        var eventFactory = scopeServiceProvider.GetRequiredService<IExternalEventFactory>();
        var dispatcher = scopeServiceProvider.GetRequiredService<IExternalEventDispatcher>();

        try
        {
            var externalEvent = await eventFactory.CreateEventAsync(eventType, value, "AuthService");

            try
            {
                _logger.LogInformation("Dispatching event {EventType}", eventType);
                await dispatcher.DispatchAsync(externalEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching event");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
        }
    }
}