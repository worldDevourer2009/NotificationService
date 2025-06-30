namespace NotificationService.Domain.Services;

public interface ITelegramService
{
    Task SendTelegramMessageAsync(string message);
    Task SendTelegramMessageAsync(string message, string? attachmentFilePath);
    Task SendTelegramMessageAsync(string message, string? attachmentFilePath, string? attachmentFileName);
}