using System.Globalization;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.DomainEvents;

namespace NotificationService.Infrastructure.Kafka.Producers;

public class KafkaProducer : IKafkaProducer, IDisposable
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
            var deliveryResult = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
            
            _logger.LogDebug("Message produced successfully to topic {Topic}, partition {Partition}, offset {Offset}",
                deliveryResult.Topic, deliveryResult.Partition, deliveryResult.Offset);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while producing message to Kafka");
        }
    }

    public async Task ProduceAsync(string topic, string key, string value, CancellationToken cancellationToken = default)
    {
        await ProduceAsync(topic, key, value, headers: null, cancellationToken);
    }

    public async Task ProduceAsync(string topic, string key, string value, Headers? headers,
        CancellationToken cancellationToken = default)
    {
        await ProduceAsync(topic, key, value, headers, delayMs: 0, cancellationToken);
    }

    public async Task ProduceAsync(string topic, string key, string value, Headers? headers, int delayMs,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (delayMs > 0)
            {
                _logger.LogDebug("Delaying message production for {DelayMs}ms", delayMs);
                await Task.Delay(delayMs, cancellationToken);
            }

            var message = new Message<string, string>
            {
                Key = key,
                Value = value,
                Headers = headers ?? new Headers()
            };
            
            if (message.Headers.All(h => h.Key != "timestamp"))
            {
                message.Headers.Add("timestamp", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));
            }

            var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken);
            
            _logger.LogDebug("Message produced successfully to topic {Topic}, partition {Partition}, offset {Offset}",
                deliveryResult.Topic, deliveryResult.Partition, deliveryResult.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to produce message to topic {Topic}: {Error}", topic, ex.Error.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error producing message to topic {Topic}", topic);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing Kafka producer");
        }
    }
}