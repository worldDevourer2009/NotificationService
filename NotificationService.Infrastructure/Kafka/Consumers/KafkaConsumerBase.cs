using System.Net.Sockets;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.Configurations;
using NotificationService.Application.Interfaces;

namespace NotificationService.Infrastructure.Kafka.Consumers;

public abstract class KafkaConsumerBase : BackgroundService
{
    protected readonly IConsumer<string, string> _consumer;
    protected readonly ILogger<KafkaConsumerBase> _logger;
    protected readonly IServiceProvider _serviceProvider;
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    private readonly KafkaSettings _kafkaSettings;
    private volatile bool _isConnected = false;

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
            EnablePartitionEof = true,
            SessionTimeoutMs = 10000,
            HeartbeatIntervalMs = 3000,
            MaxPollIntervalMs = 300000,
            SocketTimeoutMs = 5000
        };

        _consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError(e.Reason))
            .SetStatisticsHandler((_, json) =>
                _logger.LogInformation("Kafka Statistics: {0}", json))
            .Build();

        Task.Run(async () => await InitializeConsumerAsync(topics));
    }

    private async Task InitializeConsumerAsync(IEnumerable<string> topics)
    {
        await _connectionSemaphore.WaitAsync();

        try
        {
            _consumer.Subscribe(topics);
            _isConnected = true;
            _logger.LogInformation("Kafka consumer subscribed to topics: {Topics}", string.Join(", ", topics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to Kafka topics");
            _isConnected = false;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        _logger.LogInformation("Kafka consumer background service started");
        
        var connectionTimeout = TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        while (!_isConnected && !stoppingToken.IsCancellationRequested)
        {
            if (DateTime.UtcNow - startTime > connectionTimeout)
            {
                _logger.LogWarning("Kafka consumer connection timeout exceeded. Consumer will retry in background.");
                break;
            }

            await Task.Delay(1000, stoppingToken);
        }

        if (!_isConnected)
        {
            _logger.LogWarning("Starting Kafka consumer without initial connection. Will retry connections.");
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!_isConnected)
                    {
                        await Task.Delay(5000, stoppingToken);
                        continue;
                    }
                    
                    var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(1000));

                    if (consumeResult == null)
                    {
                        continue;
                    }

                    if (consumeResult.Message != null)
                    {
                        _ = ProcessMessageAsync(consumeResult, stoppingToken);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError("Kafka consume error: {Reason}", ex.Error.Reason);

                    if (ex.Error.IsFatal)
                    {
                        _logger.LogWarning("Fatal Kafka error, will attempt to reconnect");
                        _isConnected = false;
                        await Task.Delay(5000, stoppingToken);
                    }
                    else
                    {
                        await Task.Delay(1000, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Kafka consumer operation was cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in Kafka consumer");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer was cancelled");
        }
        finally
        {
            _logger.LogInformation("Kafka consumer stopped");
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string>? consumeResultMessage,
        CancellationToken cancellationToken)
    {
        if (consumeResultMessage == null)
        {
            _logger.LogWarning("Null consume result");
            return;
        }

        var messageId = GetMessageId(consumeResultMessage.Message);
        var retryCount = GetRetryCount(consumeResultMessage.Message);

        try
        {
            if (consumeResultMessage?.Message?.Value == null)
            {
                return;
            }

            _logger.LogDebug("Received message: {MessageId}, RetryCount: {RetryCount}", messageId, retryCount);

            var eventType = GetEventType(consumeResultMessage.Message);

            using var scope = _serviceProvider.CreateScope();
            await HandleEventAsync(eventType, consumeResultMessage.Message.Value, scope.ServiceProvider,
                cancellationToken);

            _consumer.Commit(consumeResultMessage);

            _logger.LogDebug("Message processed successfully: {MessageId}", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message: {MessageId}, RetryCount: {RetryCount}",
                messageId, retryCount);

            await HandleMessageErrorAsync(consumeResultMessage, ex, retryCount, cancellationToken);
        }
    }

    private async Task HandleMessageErrorAsync(ConsumeResult<string, string>? consumeResultMessage, Exception exception,
        int retryCount, CancellationToken cancellationToken)
    {
        if (consumeResultMessage == null)
        {
            _logger.LogError(exception, "Error handling failed message. Message is null");
            return;
        }

        var messageId = GetMessageId(consumeResultMessage.Message);
        var maxRetries = GetMaxRetries();

        try
        {
            var isRetryableError = IsRetryableError(exception);

            if (_kafkaSettings.EnableRetry && isRetryableError && retryCount < maxRetries)
            {
                await SendToRetryTopicAsync(consumeResultMessage, retryCount + 1, exception, cancellationToken);
                _logger.LogWarning("Message {MessageId} sent to retry topic. Attempt {RetryCount}/{MaxRetries}",
                    messageId, retryCount + 1, maxRetries);
            }
            else if (_kafkaSettings.EnableDeadLetterQueue)
            {
                await SendToDeadLetterQueueAsync(consumeResultMessage, exception, retryCount, cancellationToken);
                _logger.LogError("Message {MessageId} sent to dead letter queue after {RetryCount} retries",
                    messageId, retryCount);
            }
            else
            {
                _logger.LogError(
                    "Message {MessageId} skipped due to error after {RetryCount} retries. Retry and DLQ are disabled.",
                    messageId, retryCount);
            }

            _consumer.Commit(consumeResultMessage);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error handling failed message {MessageId}. Skipping message.", messageId);
            _consumer.Commit(consumeResultMessage);
        }
    }

    private async Task SendToRetryTopicAsync(ConsumeResult<string, string> consumeResult, int retryCount,
        Exception exception, CancellationToken cancellationToken)
    {
        if (consumeResult.Message == null)
        {
            _logger.LogWarning("Null message");
            return;
        }

        var retryTopic = GetRetryTopic(consumeResult.Topic);

        var retryMessage = new Message<string, string>
        {
            Key = consumeResult.Message.Key,
            Value = consumeResult.Message.Value,
            Headers = consumeResult.Message.Headers
        };

        SetRetryHeaders(retryMessage.Headers, retryCount, exception);

        var delayMs = CalculateRetryDelay(retryCount);

        using var scope = _serviceProvider.CreateScope();
        var kafkaProducer = scope.ServiceProvider.GetService<IKafkaProducer>();

        if (kafkaProducer != null)
        {
            await kafkaProducer.ProduceAsync(retryTopic, retryMessage.Key, retryMessage.Value,
                retryMessage.Headers, delayMs, cancellationToken);
        }
        else
        {
            _logger.LogWarning("KafkaProducer not available for retry. Message {MessageId} will be skipped.",
                GetMessageId(consumeResult.Message));
        }
    }

    private async Task SendToDeadLetterQueueAsync(ConsumeResult<string, string> consumeResult, Exception exception,
        int retryCount, CancellationToken cancellationToken)
    {
        var dlqTopic = GetDeadLetterTopic(consumeResult.Topic);

        var dlqMessage = new Message<string, string>
        {
            Key = consumeResult.Message.Key,
            Value = consumeResult.Message.Value,
            Headers = consumeResult.Message.Headers
        };

        SetDeadLetterHeaders(dlqMessage.Headers, exception, retryCount);

        using var scope = _serviceProvider.CreateScope();
        var kafkaProducer = scope.ServiceProvider.GetService<IKafkaProducer>();

        if (kafkaProducer != null)
        {
            await kafkaProducer.ProduceAsync(dlqTopic, dlqMessage.Key, dlqMessage.Value,
                dlqMessage.Headers, cancellationToken: cancellationToken);
        }
        else
        {
            _logger.LogWarning("KafkaProducer not available for DLQ. Message {MessageId} will be skipped.",
                GetMessageId(consumeResult.Message));
        }
    }

    private bool IsRetryableError(Exception exception)
    {
        return exception switch
        {
            TimeoutException => true,
            HttpRequestException => true,
            SocketException => true,
            TaskCanceledException when !exception.Message.Contains("timeout") => false,
            TaskCanceledException => true,
            ArgumentException => false,
            InvalidOperationException => false,
            FormatException => false,
            _ => true
        };
    }

    private int GetMaxRetries()
    {
        return _kafkaSettings.MaxRetries ?? 3;
    }

    private string GetRetryTopic(string originalTopic)
    {
        return $"{originalTopic}{_kafkaSettings.RetryTopicSuffix}";
    }

    private string GetDeadLetterTopic(string originalTopic)
    {
        return $"{originalTopic}{_kafkaSettings.DeadLetterTopicSuffix}";
    }

    private string GetMessageId(Message<string, string> message)
    {
        if (message.Headers.TryGetLastBytes("message-id", out var messageId))
        {
            return Encoding.UTF8.GetString(messageId);
        }

        return $"{message.Key ?? "null"}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";
    }

    private int GetRetryCount(Message<string, string> message)
    {
        if (message.Headers.TryGetLastBytes("retry-count", out var retryCountBytes))
        {
            var retryCountStr = Encoding.UTF8.GetString(retryCountBytes);
            if (int.TryParse(retryCountStr, out var retryCount))
            {
                return retryCount;
            }
        }

        return 0;
    }

    private void SetRetryHeaders(Headers headers, int retryCount, Exception exception)
    {
        headers.Remove("retry-count");
        headers.Remove("retry-reason");
        headers.Remove("retry-timestamp");

        headers.Add("retry-count", Encoding.UTF8.GetBytes(retryCount.ToString()));
        headers.Add("retry-reason", Encoding.UTF8.GetBytes(exception.GetType().Name));
        headers.Add("retry-timestamp", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));
    }

    private void SetDeadLetterHeaders(Headers headers, Exception exception, int retryCount)
    {
        headers.Remove("dlq-reason");
        headers.Remove("dlq-timestamp");
        headers.Remove("dlq-retry-count");
        headers.Remove("dlq-exception");

        headers.Add("dlq-reason", Encoding.UTF8.GetBytes("max-retries-exceeded"));
        headers.Add("dlq-timestamp", Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));
        headers.Add("dlq-retry-count", Encoding.UTF8.GetBytes(retryCount.ToString()));
        headers.Add("dlq-exception", Encoding.UTF8.GetBytes(exception.ToString()));
    }

    private int CalculateRetryDelay(int retryCount)
    {
        var baseDelayMs = _kafkaSettings.BaseRetryDelayMs;
        var maxDelayMs = _kafkaSettings.MaxRetryDelayMs;

        var delay = (int)Math.Min(baseDelayMs * Math.Pow(2, retryCount - 1), maxDelayMs);
        var jitter = Random.Shared.Next(0, delay / 4);

        return delay + jitter;
    }

    protected abstract Task HandleEventAsync(string eventType, string value, IServiceProvider scopeServiceProvider,
        CancellationToken cancellationToken);

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
        _logger.LogInformation("Stopping Kafka consumer...");

        try
        {
            await base.StopAsync(cancellationToken);
        }
        finally
        {
            try
            {
                _consumer?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing Kafka consumer");
            }
        }

        _logger.LogInformation("Kafka consumer stopped");
    }

    public override void Dispose()
    {
        try
        {
            _consumer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing Kafka consumer");
        }

        base.Dispose();
    }
}