using System.Globalization;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.DomainEvents;

namespace NotificationService.Infrastructure.Kafka.Producers;

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IProducer<string, string> producer, ILogger<KafkaProducer> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task ProduceAsync(IDomainEvent @event, string topic, CancellationToken cancellationToken = default)
    {
        var key = @event.Id.ToString();
        var value = JsonSerializer.Serialize(@event);

        var kafkaMessage = new Message<string, string>
        {
            Key = key,
            Value = value,
            Headers = new Headers()
            {
                {"event-type", Encoding.UTF8.GetBytes(@event.EventType!)},
                {"event-id", Encoding.UTF8.GetBytes(@event.Id.ToString()!)},
                {"occured-on", Encoding.UTF8.GetBytes(@event.CreatedAt.ToString(CultureInfo.InvariantCulture))}
            }
        };

        try
        {
            await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
            _logger.LogInformation($"Message with key {key} produced to topic {topic}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while producing message to Kafka");
        }
    }
}