using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Interfaces;

public interface IApplicationDbContext
{
    DbSet<NotificationGroupEntity> NotificationGroups { get; set; }
    DbSet<InternalNotification> InternalNotifications { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}