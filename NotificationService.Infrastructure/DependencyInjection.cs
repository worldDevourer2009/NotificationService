using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Configurations;
using NotificationService.Domain.Services;
using NotificationService.Infrastructure.Services;
using StackExchange.Redis;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ITelegramService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<TelegramSettings>>();
            var logger  = sp.GetRequiredService<ILogger<TelegramService>>();
            return new TelegramService(options.Value, logger);
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<DbSettings>>();
            var config = ConfigurationOptions.Parse(settings.Value.ConnectionString!);
            config.AbortOnConnectFail = false;
            config.AllowAdmin = true;
            config.ConnectTimeout = 10000;
            config.ConnectRetry = 3;
            config.AsyncTimeout = 5000;
            return ConnectionMultiplexer.Connect(settings.Value.ConnectionString!);
        });

        services.AddSingleton(sp => 
            sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        
        services.AddScoped<IRedisService, RedisService>();
        
        return services;
    }
}