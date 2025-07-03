using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Configurations;

namespace NotificationService.Infrastructure.Kafka.Consumers;

public abstract class KafkaConsumerBase : BackgroundService, IDisposable
{
    protected readonly IConsumer<string, string> _consumer;
    protected readonly ILogger<KafkaConsumerBase> _logger;
    protected readonly IServiceProvider _serviceProvider;
    private readonly KafkaSettings _kafkaSettings;
    
    protected KafkaConsumerBase(
        ILogger<KafkaConsumerBase> logger, 
        IServiceProvider serviceProvider,
        IOptions<KafkaSettings> kafkaSettings,
        IEnumerable<string> topics)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _kafkaSettings = kafkaSettings.Value;

        var config = new ConsumerConfig()
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            GroupId = _kafkaSettings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnablePartitionEof = true
        };
        
        _consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError(e.Reason))
            .SetStatisticsHandler((_, json) => 
                _logger.LogInformation("Statistics: {0}", json))
            .Build();
        
        _consumer.Subscribe(topics);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka consumer started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);

                if (consumeResult == null)
                {
                    continue;
                }

                if (consumeResult?.Message != null)
                {
                    await ProcessMessageAsync(consumeResult);
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError($"Error occured: {ex.Error.Reason}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured");
            }
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string>? consumeResultMessage)
    {
        try
        {
            if (consumeResultMessage == null || consumeResultMessage.Message == null || consumeResultMessage.Message.Value == null)
            {
                return;
            }
            
            _logger.LogInformation($"Received message: {consumeResultMessage.Message.Value}");
            var eventType = GetEventType(consumeResultMessage.Message);
            using var scope = _serviceProvider.CreateScope();
            await HandleEventAsync(eventType, consumeResultMessage.Message.Value, scope.ServiceProvider);
            _consumer.Commit(consumeResultMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
        }
    }

    protected abstract Task HandleEventAsync(string eventType, string value, IServiceProvider scopeServiceProvider);

    private string GetEventType(Message<string, string> consumeResultMessage)
    {
        if (consumeResultMessage.Headers.TryGetLastBytes("event-type", out var eventType))
        {
            return Encoding.UTF8.GetString(eventType);
        }

        throw new InvalidOperationException("Event type header not found");
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        _consumer.Close();
        await base.StopAsync(cancellationToken);
    }
    
    public override void Dispose()
    {
        base.Dispose();
        _consumer?.Dispose();
        base.Dispose();
    }
}