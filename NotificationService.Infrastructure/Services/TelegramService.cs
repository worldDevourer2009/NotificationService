using System.Text.Json;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Configurations;
using NotificationService.Domain.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace NotificationService.Infrastructure.Services;

public class TelegramService : ITelegramService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IRedisService _redisService;
    private readonly ILogger<TelegramService> _logger;

    public TelegramService(TelegramSettings telegramSettings, IRedisService redisService,
        ILogger<TelegramService> logger)
    {
        _redisService = redisService;
        _logger = logger;

        _botClient = new TelegramBotClient(telegramSettings.Token!);
        _logger.LogInformation("Telegram bot client created");
    }

    public async Task StartBotAsync(CancellationToken cancellationToken = default)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
            DropPendingUpdates = true
        };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken);

        var me = await _botClient.GetMe(cancellationToken);
        _logger.LogInformation($"Bot started as @{me.Username}");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        try
        {
            if (update.Message is not { } message || message.Text is not { } messageText)
            {
                return;
            }

            var chatId = message.Chat.Id;
            var user = message.From;

            _logger.LogInformation($"Received message from {user.FirstName} {user.LastName} ({user.Id})");

            await ProcessMessageAsync(chatId, user, messageText, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while handling update");
        }
    }

    private async Task ProcessMessageAsync(long chatId, User user, string messageText,
        CancellationToken cancellationToken)
    {
        var command = messageText.Trim().ToLower();

        switch (command)
        {
            case "/start":
                await HandleStartCommand(chatId, user, cancellationToken);;
                break;
            case "/connect":
                await HandleConnectCommand(chatId, user, cancellationToken);
                break;
        }
    }

    private async Task HandleStartCommand(long chatId, User? user, CancellationToken cancellationToken)
    {
        var welcomeMessage = $"Welcome to Task Handler!\n\n";
        welcomeMessage += $"Your data:\n";
        welcomeMessage += $"Chat ID: `{chatId}`\n";
        welcomeMessage += $"User ID: `{user?.Id}`\n";
        welcomeMessage += $"Username: @{user?.Username ?? "can't find user data"}\n";
        welcomeMessage += $"Name: {user?.FirstName} {user?.LastName}\n\n";
        welcomeMessage += $"Available commands:\n";
        //welcomeMessage += $"/status - Check connection status\n";
        //welcomeMessage += $"/disconnect - Disable notifications\n";
        welcomeMessage += $"To recieve notification in telegram type -> /connect";

        await _botClient.SendMessage(
            chatId: chatId,
            text: welcomeMessage,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
        
        await SaveUserChatInfo(chatId, user);
    }

    private async Task HandleConnectCommand(long chatId, User user, CancellationToken cancellationToken)
    {
        try
        {
            var connectionToken = Guid.NewGuid().ToString("N")[..8].ToUpper();
            
            var connectionKey = $"telegram:connect:{connectionToken}";
            var connectionData = new
            {
                ChatId = chatId,
                TelegramUserId = user?.Id,
                Username = user?.Username,
                FirstName = user?.FirstName,
                LastName = user?.LastName,
                CreatedAt = DateTime.UtcNow
            };
            
            await _redisService.SetAsync(connectionKey, JsonSerializer.Serialize(connectionData), cancellationToken);
            await _redisService.SetExpireAsync(connectionKey, TimeSpan.FromMinutes(10), cancellationToken);

            var connectMessage = $"In order to connect notifications:\n\n";
            connectMessage += $"Copy code: `{connectionToken}`\n";
            connectMessage += $"Go to your profile\n";
            connectMessage += $"Paste code in \"Telegram notifications\"\n\n";
            connectMessage += $"Code is available for 10 minutes \n";
            connectMessage += $"For renew token type /connect";

            await _botClient.SendMessage(
                chatId: chatId,
                text: connectMessage,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Generated connection token {Token} for ChatId: {ChatId}", 
                connectionToken, chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating connection token for ChatId: {ChatId}", chatId);
            
            await _botClient.SendMessage(
                chatId: chatId,
                text: "Canceled. Something went wrong.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task SaveUserChatInfo(long chatId, User? user)
    {
        try
        {
            var chatInfoKey = $"telegram:chat:{chatId}";
            
            var chatInfo = new
            {
                ChatId = chatId,
                TelegramUserId = user?.Id,
                Username = user?.Username,
                FirstName = user?.FirstName,
                LastName = user?.LastName,
                LastSeen = DateTime.UtcNow
            };

            await _redisService.SetAsync(chatInfoKey, JsonSerializer.Serialize(chatInfo));
            _logger.LogInformation("Saved chat info for ChatId: {ChatId}", chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving chat info for ChatId: {ChatId}", chatId);
        }
    }


    private async Task HandleErrorAsync(ITelegramBotClient botClient, Exception update, HandleErrorSource errorSource,
        CancellationToken cancellationToken)
    {
    }

    public async Task SendTelegramMessageAsync(string chatId, string message, CancellationToken cancellationToken = default)
    {
        var messageToSend = await _botClient.SendMessage(1, message, cancellationToken: cancellationToken);
    }

    public async Task SendTelegramMessageAsync(string chatId, string message, string? attachmentFilePath,
        CancellationToken cancellationToken = default)
    {
        var messageToSend = await _botClient.SendMessage(1, message, cancellationToken: cancellationToken);
    }

    public async Task SendTelegramMessageAsync(string chatId, string message, string? attachmentFilePath, string? attachmentFileName,
        CancellationToken cancellationToken = default)
    {
        var messageToSend = await _botClient.SendMessage(1, message, cancellationToken: cancellationToken);
    }
}