using NotificationService.Domain.Exceptions.Notifications;

namespace NotificationService.Domain.Entities;

public class NotificationGroupEntity : Entity
{
    public string? Name { get; private set; }
    public string? Description { get; private set; }
    public string? Creator { get; private set; }
    public List<string> Members { get; private set; }
    
    protected NotificationGroupEntity()
    {
    }

    public static NotificationGroupEntity Create(string? name, string? description, string? creator, List<string>? members)
    {
        if (string.IsNullOrWhiteSpace(creator))
        {
            throw new NotificationGroupEntityException("Creator id is required");
        }

        if (members is null)
        {
            throw new NotificationGroupEntityException("Members is required");
        }
        
        members.Add(creator);

        return new NotificationGroupEntity
        {
            Name = name,
            Description = description,
            Creator = creator,
            Members = members,
        };
    }

    public void UpdateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name == Name)
        {
            throw new NotificationGroupEntityException("Name is required");
        }
        
        Name = name;
    }

    public void UpdateDescription(string? description)
    {
        if (description == null)
        {
            throw new NotificationGroupEntityException("Description can't be null");
        }
        
        Description = description;
    }
    
    public void AddMember(string memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId) || Members.Contains(memberId))
        {
            throw new NotificationGroupEntityException("Member id is required");
        }
        
        Members.Add(memberId);
    }
    
    public void RemoveMember(string memberId)
    {
        if (string.IsNullOrWhiteSpace(memberId) || !Members.Contains(memberId))
        {
            throw new NotificationGroupEntityException("Member id is required");
        }

        if (Creator == memberId)
        {
            throw new NotificationGroupEntityException("Creator can't be removed");
        }
        
        Members.Remove(memberId);
    }
    
    public void SetNewCreator(string creatorId)
    {
        if (string.IsNullOrWhiteSpace(creatorId) || Creator == creatorId)
        {
            return;
        }
        
        Creator = creatorId;
    }
}