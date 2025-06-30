using System.Runtime.Serialization;

namespace NotificationService.Domain.Exceptions.Redis;

[Serializable]
public class InvalidRedisKeyException : Exception
{
    public string InvalidKey { get; }
    
    public InvalidRedisKeyException(string invalidKey)
        : base($"Invalid Redis key: {invalidKey}")
    {
        InvalidKey = invalidKey;
    }
    
    public InvalidRedisKeyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
    
    public InvalidRedisKeyException()
        : base("Invalid Redis key.")
    {
    }
    
    [Obsolete("Obsolete")]
    protected InvalidRedisKeyException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        InvalidKey = info.GetString(nameof(InvalidKey))!;
    }
    
    [Obsolete("Obsolete")]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info is null)
        {
            throw new ArgumentNullException(nameof(info));
        }
        
        base.GetObjectData(info, context);
        info.AddValue(nameof(InvalidKey), InvalidKey);
    }
}