namespace NotificationService.Domain.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
    Task SendEmailAsync(string email, string subject, string htmlMessage, string? attachmentFilePath);
    Task SendEmailAsync(string email, string subject, string htmlMessage, string? attachmentFilePath, string? attachmentFileName);
}