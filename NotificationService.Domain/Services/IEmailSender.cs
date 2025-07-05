namespace NotificationService.Domain.Services;

public interface IEmailSender
{
    Task<bool> SendEmailAsync(string email, string subject, string message, CancellationToken cancellationToken = default);
    Task<bool> SendEmailAsync(string email, string subject, string message, string? htmlMessage, CancellationToken cancellationToken = default); 
    Task<bool> SendEmailAsync(string email, string subject, string message, string? htmlMessage, string[]? attachments, CancellationToken cancellationToken = default);
}