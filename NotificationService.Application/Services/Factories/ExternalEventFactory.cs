using NotificationService.Application.ExternalEvents.AuthService;
using NotificationService.Domain.Events.ExternalEvents;
using TaskHandler.Shared.Kafka.EventTypes.AuthService;

namespace NotificationService.Application.Services.Factories;

public class ExternalEventFactory : IExternalEventFactory
{
    private readonly Dictionary<string, Func<string, string, string, IExternalEvent>> _eventFactories;

    public ExternalEventFactory()
    {
        _eventFactories = new Dictionary<string, Func<string, string, string, IExternalEvent>>();
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        _eventFactories[AuthEventTypes.UserSignedUp] = (eventType, eventData, source) =>
            new UserSignedUpExternalEvent(eventType, eventData, source);
        
        _eventFactories[AuthEventTypes.UserLoggedIn] = (eventType, eventData, source) =>
            new UserLoggedInExternalEvent(eventType, eventData, source);
        
        _eventFactories[AuthEventTypes.UserLoggedOut] = (eventType, eventData, source) =>
            new UserLoggedOutExternalEvent(eventType, eventData, source);
        
        
    }

    public Task<IExternalEvent> CreateEventAsync(string eventType, string eventData, string source)
    {
        if (!_eventFactories.TryGetValue(eventType, out var factory))
        {
            throw new Exception($"Event type {eventType} is not supported");
        }
        
        return Task.FromResult(factory(eventType, eventData, source));
    }
}