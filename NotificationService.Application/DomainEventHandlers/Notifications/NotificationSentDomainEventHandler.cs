using NotificationService.Application.Interfaces;
using NotificationService.Domain.DomainEvents.Notifications;

namespace NotificationService.Application.DomainEventHandlers.Notifications;

public class NotificationSentDomainEventHandler : IDomainEventHandler<NotificationDomainEvent>
{
    private readonly IKafkaProducer _kafkaProducer;
    private const string Topic = "notification-sent";
    
    public async Task HandleAsync(NotificationDomainEvent @event, CancellationToken cancellationToken = default)
    {
        await _kafkaProducer.ProduceAsync(@event, Topic, cancellationToken);
    }
}