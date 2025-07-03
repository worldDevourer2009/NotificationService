using NotificationService.Domain.Events.ExternalEvents;

namespace NotificationService.Application.Services.Factories;

public interface IExternalEventFactory
{
    Task<IExternalEvent> CreateEventAsync(string eventType, string eventData, string source); 
}