namespace NotificationService.Domain.Exceptions.Redis;

public class InvalidRedisKeyException : DomainException
{
    public string Reason { get; }
    
    public InvalidRedisKeyException(string reason)
        : base($"Redis failed for because of : {reason}")
    {
        Reason = reason;
    }
}