namespace NotificationService.Domain.Entities;

public abstract class Notification : ValueObject
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? ImageUrl { get; set; }
    public string? TargetUrl { get; set; }
    public string? TargetType { get; set; }
    public string? Sender { get; set; }
    public string? Receiver { get; set; }
    public DateTime? CreatedAt { get; set; }
    
    public override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
        yield return TargetUrl;
        yield return Sender;
        yield return Receiver;
        yield return CreatedAt;
    }

    public Notification()
    {
        Id = Guid.NewGuid();
    }
}