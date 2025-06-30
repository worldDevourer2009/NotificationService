namespace NotificationService.Domain.Services;

public interface IRedisService
{
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task SetExpireAsync(string key, TimeSpan timeSpan, CancellationToken cancellationToken = default);
}