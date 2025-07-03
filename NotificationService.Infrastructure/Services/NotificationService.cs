using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Services;
using NotificationService.Infrastructure.SignalR;
using TaskHandler.Domain.Services;

namespace NotificationService.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IEmailSender _emailSender;
    private readonly ITelegramService _telegramService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IHubContext<NotificationHub> hubContext, IEmailSender emailSender, ITelegramService telegramService, ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _emailSender = emailSender;
        _telegramService = telegramService;
        _logger = logger;
    }

    public async Task SendInternalNotificationAsync(Guid userId, Notification notification)
    {
        try
        {
            await _hubContext.Clients.Group(userId.ToString())
                .SendAsync("ReceiveNotification", notification);
            
            _logger.LogInformation("Notification sent to user {userId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending internal notification");
        }
    }

    public async Task SendInternalNotificationAsync(string groupId, Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            await _hubContext.Clients.Group( $"user_{groupId}")
                .SendAsync("ReceiveNotification", notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while sending internal notification to group {groupId}");
        }
    }

    public async Task SendEmailNotificationAsync(string email, Notification notification)
    {
        try
        {
            await _emailSender.SendEmailAsync(email, notification.Title!, notification.Body!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending email notification");
        }
    }

    public async Task SendTelegramNotificationAsync(string telegramId, Notification notification)
    {
        try
        {
            await _telegramService.SendTelegramMessageAsync(telegramId,notification.Title! + "\n" + notification.Body!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while sending telegram notification");
        }
    }
}