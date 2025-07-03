using NotificationService.Domain.DomainEvents;

namespace NotificationService.Application.Interfaces;

public interface IDomainDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}