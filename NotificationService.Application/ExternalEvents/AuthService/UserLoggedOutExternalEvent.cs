using NotificationService.Domain.Events.ExternalEvents;

namespace NotificationService.Application.ExternalEvents.AuthService;

public class UserLoggedOutExternalEvent : ExternalEvent
{
    public UserLoggedOutExternalEvent(
        string eventType, 
        string eventData,
        string source) 
        : base(eventType, eventData, source)
    {
    }
}