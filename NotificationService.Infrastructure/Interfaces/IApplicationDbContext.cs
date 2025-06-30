using Microsoft.EntityFrameworkCore;

namespace NotificationService.Infrastructure.Services;

public interface IApplicationDbContext
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}