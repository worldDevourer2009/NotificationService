using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Configurations;
using NotificationService.Domain.Services;
using NotificationService.Infrastructure.Services;
using StackExchange.Redis;
using TaskHandler.Domain.Services;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        BindRedis(services);
        BindEmail(services);
        
        services.AddScoped<ITelegramService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<TelegramSettings>>();
            var logger  = sp.GetRequiredService<ILogger<TelegramService>>();
            var redisService = sp.GetRequiredService<IRedisService>();
            return new TelegramService(options.Value, redisService, logger );
        });

        return services;
    }

    private static void BindEmail(IServiceCollection services)
    {
        services.AddSingleton<IEmailSender>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EmailSettings>>();
            return new SmtpEmailSender(options);
        });
    }

    private static void BindRedis(IServiceCollection services)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<DbSettings>>();
            var config = ConfigurationOptions.Parse(settings.Value.DefaultConnection!);
            config.AbortOnConnectFail = false;
            config.AllowAdmin = true;
            config.ConnectTimeout = 10000;
            config.ConnectRetry = 3;
            config.AsyncTimeout = 5000;
            return ConnectionMultiplexer.Connect(settings.Value.DefaultConnection!);
        });

        services.AddSingleton(sp => 
            sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        
        services.AddScoped<IRedisService, RedisService>();
    }
}