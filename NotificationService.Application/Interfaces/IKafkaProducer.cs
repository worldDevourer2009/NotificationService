using Confluent.Kafka;
using NotificationService.Domain.DomainEvents;

namespace NotificationService.Application.Interfaces;

public interface IKafkaProducer
{
    Task ProduceAsync(IDomainEvent @event, string topic, CancellationToken cancellationToken = default);
    Task ProduceAsync(string topic, string key, string value, CancellationToken cancellationToken = default);
    Task ProduceAsync(string topic, string key, string value, Headers? headers, CancellationToken cancellationToken = default);
    Task ProduceAsync(string topic, string key, string value, Headers? headers, int delayMs, CancellationToken cancellationToken = default);
}