using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Services.Repos;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Interfaces;

namespace NotificationService.Infrastructure.Services.Repos;

public class InternalNotificationRepository : IInternalNotificationRepository
{
    private readonly IApplicationDbContext _applicationDbContext;
    private readonly ILogger<InternalNotificationRepository> _logger;

    public InternalNotificationRepository(IApplicationDbContext applicationDbContext, ILogger<InternalNotificationRepository> logger)
    {
        _applicationDbContext = applicationDbContext;
        _logger = logger;
    }

    public async Task<List<InternalNotification>> GetAllNotificationsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all notifications for user {Id}", userId.ToString());

        try
        {
            var notifications = await _applicationDbContext.InternalNotifications
                .Where(x => x.Receiver == userId.ToString())
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Got {Count} notifications for user {Id}", notifications.Count, userId.ToString());
            return notifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting all notifications for user {Id}", userId.ToString());
            return new List<InternalNotification>();
        }
    }

    public async Task<InternalNotification?> GetNotificationForUser(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting notification for user {Id} with id {NotificationId}", userId.ToString(), notificationId.ToString());

        try
        {
            var notification = await _applicationDbContext.InternalNotifications
                .Where(x => x.Receiver == userId.ToString())
                .Where(x => x.Id == notificationId)
                .FirstOrDefaultAsync(cancellationToken);

            if (notification == null)
            {
                _logger.LogInformation("Notification for user {Id} with id {NotificationId} not found",
                    userId.ToString(), notificationId.ToString());
                return null;
            }

            _logger.LogInformation("Got notification for user {Id} with id {NotificationId}", userId.ToString(),
                notificationId.ToString());
            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting notification for user {Id} with id {NotificationId}",
                userId.ToString(), notificationId.ToString());
            return null;
        }
    }

    public async Task<bool> AddNotificationForUserAsync(InternalNotification notification,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding notification for user {Id}", notification.Receiver);
        
        try
        {
            await _applicationDbContext.InternalNotifications.AddAsync(notification, cancellationToken);
            await _applicationDbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Added notification for user {Id}", notification.Receiver);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while adding notification for user {Id}", notification.Receiver);
            return false;
        }
    }

    public async Task<bool> DeleteNotificationForUserAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting notification for user {Id}", notificationId.ToString());
        
        try
        {
            var notification = await _applicationDbContext.InternalNotifications
                .Where(x => x.Id == notificationId)
                .FirstOrDefaultAsync(cancellationToken);

            if (notification == null)
            {
                _logger.LogInformation("Notification for user {Id} not found", notificationId.ToString());
                return false;
            }
            
            _applicationDbContext.InternalNotifications.Remove(notification);
            await _applicationDbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting notification for user {Id}", notificationId.ToString());
            return false;
        }
    }

    public async Task<bool> UpdateNotificationForUserAsync(Guid notificationId, InternalNotification notification,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating notification for user {Id}", notificationId.ToString());
        
        try
        {
            var notificationDb = await _applicationDbContext.InternalNotifications
                .Where(x => x.Id == notificationId)
                .FirstOrDefaultAsync(cancellationToken);

            if (notificationDb == null)
            {
                _logger.LogInformation("Notification for user {Id} not found", notificationId.ToString());
                return false;
            }

            notificationDb.SetNewBody(notification.Body!);
            notificationDb.SetNewTitle(notification.Title!);
            notificationDb.SetNewSender(notification.Sender!);
            
            _applicationDbContext.InternalNotifications.Update(notificationDb);
            
            await _applicationDbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating notification for user {Id}", notificationId.ToString());
            return false;
        }
    }
}