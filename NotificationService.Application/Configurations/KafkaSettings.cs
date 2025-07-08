namespace NotificationService.Application.Configurations;

public class KafkaSettings
{
    public string? BootstrapServers {get; set;}
    public string? ClientId {get; set;}
    public string? GroupId { get; set; }
    public int? MessageTimeoutMs {get; set;}
    
    public int? MaxRetries { get; set; }
    public int BaseRetryDelayMs { get; set; }
    public int MaxRetryDelayMs { get; set; }
    public bool EnableRetry { get; set; }
    public bool EnableDeadLetterQueue { get; set; }
    
    public string? RetryTopicSuffix { get; set; }
    public string? DeadLetterTopicSuffix { get; set; }
}