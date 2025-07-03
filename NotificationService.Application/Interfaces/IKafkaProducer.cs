using NotificationService.Domain.DomainEvents;

namespace NotificationService.Application.Interfaces;

public interface IKafkaProducer
{
    Task ProduceAsync(IDomainEvent @event, string topic, CancellationToken cancellationToken = default);
}