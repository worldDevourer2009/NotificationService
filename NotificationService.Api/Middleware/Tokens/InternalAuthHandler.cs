using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using NotificationService.Application.Configurations;
using TaskHandler.Shared.InternalAuth.Interfaces;

namespace NotificationService.Api.Middleware.Tokens;

public class InternalAuthHandler : DelegatingHandler
{
    private readonly IInternalTokenProvider _internalTokenProvider;
    private readonly InternalAuthSettings _internalAuthSettings;
    private readonly ILogger<InternalAuthHandler> _logger;

    public InternalAuthHandler(IInternalTokenProvider internalTokenProvider,
        IOptions<InternalAuthSettings> internalAuthSettings, ILogger<InternalAuthHandler> logger)
    {
        _internalTokenProvider = internalTokenProvider;
        _logger = logger;
        _internalAuthSettings = internalAuthSettings.Value;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating token");
        
        try
        {
            var token = await _internalTokenProvider.GetTokenAsync(
                serviceClientId: _internalAuthSettings.ServiceClientId!,
                serviceClientSecret: _internalAuthSettings.ServiceClientSecret!,
                expiresAt: DateTime.UtcNow.Add(TimeSpan.FromMinutes(_internalAuthSettings.AccessTokenExpirationMinutes)),
                client: _internalAuthSettings.ServiceClientId!,
                endpoint: _internalAuthSettings.Endpoint!, 
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logger.LogInformation("Authorization header added successfully");
            }
            else
            {
                _logger.LogWarning("Token is null or empty, continuing without authorization header");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate token for request");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}