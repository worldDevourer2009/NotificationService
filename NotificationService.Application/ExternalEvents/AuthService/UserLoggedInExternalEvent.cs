using NotificationService.Domain.Events.ExternalEvents;

namespace NotificationService.Application.ExternalEvents.AuthService;

public class UserLoggedInExternalEvent : ExternalEvent
{
    public UserLoggedInExternalEvent(
        string eventType,
        string eventData,
        string source)
        : base(eventType, eventData, source)
    {
    }
}