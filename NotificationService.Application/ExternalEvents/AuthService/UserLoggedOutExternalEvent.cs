using NotificationService.Domain.Events.ExternalEvents;

namespace NotificationService.Application.ExternalEvents.AuthService;

public class UserLoggedOutExternalEvent : ExternalEvent
{
    private UserLoggedOutExternalEvent(
        string eventType, 
        string eventData,
        string source) 
        : base(eventType, eventData, source)
    {
    }
    
    public static UserLoggedOutExternalEvent Create(string eventType, string eventData, string source)
    {
        return new UserLoggedOutExternalEvent(eventType, eventData, source);
    }
}