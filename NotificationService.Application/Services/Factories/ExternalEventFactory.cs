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
        _eventFactories[AuthEventTypes.UserSignedUp] = UserSignedUpExternalEvent.Create;
        _eventFactories[AuthEventTypes.UserLoggedIn] = UserLoggedInExternalEvent.Create;
        _eventFactories[AuthEventTypes.UserLoggedOut] = UserLoggedOutExternalEvent.Create;
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