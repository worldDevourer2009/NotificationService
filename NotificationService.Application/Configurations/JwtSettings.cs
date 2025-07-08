namespace NotificationService.Application.Configurations;

public class JwtSettings
{
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public string? FallbackPublicKey { get; set; }
}