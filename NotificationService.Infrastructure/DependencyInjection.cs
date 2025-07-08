using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Configurations;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Services.Repos;
using NotificationService.Domain.Services;
using NotificationService.Infrastructure.Interfaces;
using NotificationService.Infrastructure.Kafka.Consumers.AuthService;
using NotificationService.Infrastructure.Kafka.Producers;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Services;
using NotificationService.Infrastructure.Services.Repos;
using NotificationService.Infrastructure.SignalR;
using StackExchange.Redis;
using TaskHandler.Shared.InternalAuth.Interfaces;
using TaskHandler.Shared.InternalAuth.Services;
using TaskHandler.Shared.Kafka.Topics.AuthServiceTopics;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        BindRedis(services);
        BindEmail(services);
        BindTelegram(services);
        BindKafkaProducer(services);
        BindSignalR(services);
        BindNotificationService(services);
        BindKafkaConsumers(services);
        BindInternalAuth(services);
        BindDB(services);
        return services;
    }

    private static void BindInternalAuth(IServiceCollection services)
    {
        services.AddSingleton<IInternalTokenProvider, InternalTokenProvider>();
    }

    private static void BindDB(IServiceCollection services)
    {
        services.AddScoped<IApplicationDbContext, AppDbContext>();
        services.AddScoped<IInternalNotificationRepository, InternalNotificationRepository>();
        services.AddScoped<INotificationGroupRepository, NotificationGroupRepository>();
    }

    private static void BindKafkaConsumers(IServiceCollection services)
    {
        var authTopics = new string[]
        {
            AuthServiceTopics.AuthServiceUserLoggedIn,
            AuthServiceTopics.AuthServiceUserLoggedOut,
            AuthServiceTopics.AuthServiceUserSignedUp
        };

        services.AddSingleton<IEnumerable<string>>(provider => authTopics);
        services.AddHostedService<AuthServiceEventConsumer>();
    }
    
    private static void BindNotificationService(IServiceCollection services)
    {
        services.AddScoped<INotificationService, Services.NotificationService>();
        services.AddScoped<INotificationGroupService, NotificationGroupService>();
    }

    private static void BindSignalR(IServiceCollection services)
    {
        services.AddSignalR();
        services.AddScoped<Hub, NotificationHub>();
    }

    private static void BindTelegram(IServiceCollection services)
    {
        services.AddScoped<ITelegramService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<TelegramSettings>>();
            var logger  = sp.GetRequiredService<ILogger<TelegramService>>();
            var redisService = sp.GetRequiredService<IRedisService>();
            return new TelegramService(options.Value, redisService, logger );
        });
    }

    private static void BindKafkaProducer(IServiceCollection services)
    {
        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<KafkaSettings>>().Value;

            if (string.IsNullOrEmpty(options.BootstrapServers))
            {
                throw new InvalidOperationException("KafkaSettings:BootstrapServers is not configured.");
            }
            
            var kafkaConfig = new ProducerConfig
            {
                BootstrapServers = options.BootstrapServers,
                ClientId = options.ClientId,
                MessageTimeoutMs = options.MessageTimeoutMs,
                LingerMs = 10,
                RequestTimeoutMs = 30000,
                DeliveryReportFields = "all"
            };

            var producerBuilder = new ProducerBuilder<string, string>(kafkaConfig);
            return producerBuilder.Build();
        });
        
        services.AddSingleton<IKafkaProducer>(sp =>
        {
            var producer = sp.GetRequiredService<IProducer<string, string>>();
            var logger = sp.GetRequiredService<ILogger<KafkaProducer>>();
            
            return new KafkaProducer(producer, logger);
        });
    }

    private static void BindEmail(IServiceCollection services)
    {
        services.AddSingleton<IEmailSender>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EmailSettings>>();
            var logger = sp.GetRequiredService<ILogger<MailKitEmailSender>>();
            return new MailKitEmailSender(options, logger);
        });
    }

    private static void BindRedis(IServiceCollection services)
    {
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<DbSettings>>();
            var config = ConfigurationOptions.Parse(settings.Value.RedisConnection!);
            config.AbortOnConnectFail = false;
            config.AllowAdmin = true;
            config.ConnectTimeout = 10000;
            config.ConnectRetry = 3;
            config.AsyncTimeout = 5000;
            return ConnectionMultiplexer.Connect(settings.Value.RedisConnection!);
        });

        services.AddSingleton(sp => 
            sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        
        services.AddScoped<IRedisService, RedisService>();
    }
}