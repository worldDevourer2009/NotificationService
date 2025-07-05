namespace NotificationService.Application.DTOToTransfer;

public class Test
{
    
}

public class CreateNotificationGroupDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> Members { get; set; }
}