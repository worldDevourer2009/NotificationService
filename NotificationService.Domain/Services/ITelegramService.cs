namespace NotificationService.Domain.Services;

public interface ITelegramService
{
    Task SendTelegramMessageAsync(string chatId, string message);
    Task SendTelegramMessageAsync(string chatId, string message, string? attachmentFilePath);
    Task SendTelegramMessageAsync(string chatId, string message, string? attachmentFilePath, string? attachmentFileName);
}