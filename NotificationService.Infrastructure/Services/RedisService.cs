using NotificationService.Domain.Exceptions.Redis;
using NotificationService.Domain.Services;
using StackExchange.Redis;

namespace NotificationService.Infrastructure.Services;

public class RedisService : IRedisService
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisService(IDatabase database, IConnectionMultiplexer connectionMultiplexer)
    {
        _database = database;
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidRedisKeyException("Invalid key or value to set");
        }
        
        await _database.StringSetAsync(key, value);
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidRedisKeyException("Invalid key to get");
        }
        
        return await _database.StringGetAsync(key);
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidRedisKeyException("Invalid key to delete");
        }
        
        return await _database.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidRedisKeyException("Invalid key to check");
        }
        
        return await _database.KeyExistsAsync(key);
    }

    public async Task SetExpireAsync(string key, TimeSpan timeSpan, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || timeSpan.TotalSeconds <= 0)
        {
            throw new InvalidRedisKeyException("Invalid time span");
        }
        
        await _database.KeyExpireAsync(key, timeSpan);
    }
}