using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using NotificationService.Application.Configurations;

namespace NotificationService.Api.Middleware.Tokens;

public class PublicKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PublicKeyMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly JwtSettings _jwtSettings;
    private readonly AuthSettings _authSettings;
    
    private static RSA? _cachedRsa;
    private static DateTime _lastCacheTime;
    private static readonly TimeSpan KeyCacheTime = TimeSpan.FromMinutes(30);
    private static readonly object _lock = new();

    public PublicKeyMiddleware(RequestDelegate next, ILogger<PublicKeyMiddleware> logger, IServiceProvider serviceProvider, IOptions<JwtSettings> jwtSettings, IOptions<AuthSettings> authSettings)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _jwtSettings = jwtSettings.Value;
        _authSettings = authSettings.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldUpdateKey())
        {
            await _next(context);
        }

        await UpdatePublicKeyAsync();
    }

    private async Task UpdatePublicKeyAsync()
    {
        lock (_lock)
        {
            if (!ShouldUpdateKey())
            {
                return;
            }

            try
            {
                var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                
                var publicKeyPem =  httpClient.GetStringAsync($"{_authSettings.BaseUrl}/.well-known/public-key.pem")
                    .GetAwaiter()
                    .GetResult();
                
                var newRsa = RSA.Create();
                newRsa.ImportFromPem(publicKeyPem);
                
                _cachedRsa?.Dispose();
                _cachedRsa = newRsa;
                _lastCacheTime = DateTime.UtcNow;
                
                _logger.LogInformation("Public key for auth received successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting public key for auth");
                try
                {
                    if (_cachedRsa == null)
                    {
                        var fallbackRsa = RSA.Create();
                        var fallbackKey = _jwtSettings.FallbackPublicKey 
                                          ?? throw new InvalidOperationException("Fallback public key is not set");
                        
                        fallbackRsa.ImportFromPem(fallbackKey);
                        _cachedRsa = fallbackRsa;
                        _lastCacheTime = DateTime.UtcNow;
                    }
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "Error while getting fallback public key");
                    throw;
                }
            }
        }
    }

    private bool ShouldUpdateKey()
    {
        return _cachedRsa == null || DateTime.UtcNow - _lastCacheTime > KeyCacheTime;
    }
    
    public static RSA GetCurrenPublicKey()
    {
        return _cachedRsa ?? throw new InvalidOperationException("Public key is not set");
    }
}