using NotificationService.Domain.DomainEvents;

namespace NotificationService.Domain.Entities;

public abstract class Entity
{
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public Guid Id { get; private set; }
    private readonly List<IDomainEvent> _domainEvents;
    
    protected Entity()
    {
        Id = Guid.NewGuid();
        _domainEvents = new List<IDomainEvent>();
    }
    
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }
        
        var entity = (Entity) obj;
        
        return entity.Id == Id;
    }

    public static bool operator ==(Entity? left, Entity? right)
    {
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
        {
            return false;
        }
        
        if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
        {
            return true;
        }
        
        return left.Equals(right);
    }

    public static bool operator !=(Entity? left, Entity? right)
    {
        return !(left == right);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}