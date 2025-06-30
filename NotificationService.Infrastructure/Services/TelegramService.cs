using Microsoft.Extensions.Logging;
using NotificationService.Application.Configurations;
using NotificationService.Domain.Services;
using Telegram.Bot;

namespace NotificationService.Infrastructure.Services;

public class TelegramService : ITelegramService
{
    private readonly ITelegramBotClient _botClient;
    private readonly TelegramSettings _telegramSettings;
    private readonly ILogger<TelegramService> _logger;

    public TelegramService(TelegramSettings telegramSettings, ILogger<TelegramService> logger)
    {
        _telegramSettings = telegramSettings;
        _logger = logger;
        
        _botClient = new TelegramBotClient(_telegramSettings.Token!);
        _logger.LogInformation("Telegram bot client created");
    }

    public async Task SendTelegramMessageAsync(string message)
    {
        var messageToSend = await _botClient.SendMessage(_telegramSettings.ChatId!, message);
    }

    public async Task SendTelegramMessageAsync(string message, string? attachmentFilePath)
    {
        var messageToSend = await _botClient.SendMessage(_telegramSettings.ChatId!, message);
    }

    public async Task SendTelegramMessageAsync(string message, string? attachmentFilePath, string? attachmentFileName)
    {
        var messageToSend = await _botClient.SendMessage(_telegramSettings.ChatId!, message);
    }
}