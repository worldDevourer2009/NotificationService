using NotificationService.Domain.Events.ExternalEvents;

namespace NotificationService.Application.ExternalEvents.AuthService;

public class UserSignedUpExternalEvent : ExternalEvent
{
    public UserSignedUpExternalEvent(
        string eventType, 
        string eventData, 
        string source) 
        : base(eventType, eventData, source)
    {
    }
}