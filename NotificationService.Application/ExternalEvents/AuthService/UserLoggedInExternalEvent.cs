using NotificationService.Domain.Events.ExternalEvents;

namespace NotificationService.Application.ExternalEvents.AuthService;

public class UserLoggedInExternalEvent : ExternalEvent
{
    private UserLoggedInExternalEvent(
        string eventType,
        string eventData,
        string source)
        : base(eventType, eventData, source)
    {
    }
    
    public static UserLoggedInExternalEvent Create(string eventType, string eventData, string source)
    {
        return new UserLoggedInExternalEvent(eventType, eventData, source);
    }
}