using NotificationService.Domain.Events.ExternalEvents;

namespace NotificationService.Application.ExternalEvents.AuthService;

public class UserSignedUpExternalEvent : ExternalEvent
{
    private UserSignedUpExternalEvent(
        string eventType, 
        string eventData, 
        string source) 
        : base(eventType, eventData, source)
    {
    }

    public static UserSignedUpExternalEvent Create(string eventType, string eventData, string source)
    {
        return new UserSignedUpExternalEvent(eventType, eventData, source);
    }
}