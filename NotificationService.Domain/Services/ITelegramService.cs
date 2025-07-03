namespace NotificationService.Domain.Services;

public interface ITelegramService
{
    Task StartBotAsync(CancellationToken cancellationToken = default);
    Task SendTelegramMessageAsync(string chatId, string message, CancellationToken cancellationToken = default);
    Task SendTelegramMessageAsync(string chatId, string message, string? attachmentFilePath, CancellationToken cancellationToken = default);
    Task SendTelegramMessageAsync(string chatId, string message, string? attachmentFilePath, string? attachmentFileName, CancellationToken cancellationToken = default);
}