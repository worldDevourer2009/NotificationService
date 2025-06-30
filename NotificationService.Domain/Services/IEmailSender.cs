namespace TaskHandler.Domain.Services;

public interface IEmailSender
{
    Task<bool> SendEmailAsync(string email, string subject, string message);
    Task<bool> SendEmailAsync(string email, string subject, string message, string? htmlMessage); 
    Task<bool> SendEmailAsync(string email, string subject, string message, string? htmlMessage, string[]? attachments);
}