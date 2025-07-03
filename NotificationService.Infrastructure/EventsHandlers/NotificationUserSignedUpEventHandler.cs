using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Configurations;
using NotificationService.Infrastructure.Kafka.Consumers;

namespace NotificationService.Infrastructure.EventsHandlers;

public class NotificationUserSignedUpEventHandler : KafkaConsumerBase
{
    public NotificationUserSignedUpEventHandler(
        ILogger<NotificationUserSignedUpEventHandler> logger, 
        IServiceProvider serviceProvider, 
        IOptions<KafkaSettings> kafkaSettings, 
        IEnumerable<string> topics) : 
        base(logger, serviceProvider, kafkaSettings, topics)
    {
        
    }

    protected override async Task HandleEventAsync(string eventType, string value, IServiceProvider scopeServiceProvider)
    {
        throw new NotImplementedException();
    }
}