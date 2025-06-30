namespace TaskHandler.Application.DTOs;

public class EmailRequestDTO
{
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public string? HtmlMessage { get; set; }
    public string[]? Attachments { get; set; }
}