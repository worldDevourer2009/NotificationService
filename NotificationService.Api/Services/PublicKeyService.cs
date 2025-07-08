using Microsoft.IdentityModel.Tokens;
using NotificationService.Api.Middleware.Tokens;

namespace NotificationService.Api.Services;

public interface IPublicKeyService
{
    RsaSecurityKey GetPublicKey();
    Task RefreshPublicKey();
}

public class PublicKeyService : IPublicKeyService
{
    public RsaSecurityKey GetPublicKey()
    {
        var rsa = PublicKeyMiddleware.GetCurrenPublicKey();
        return new RsaSecurityKey(rsa);
    }

    public async Task RefreshPublicKey()
    {
        await Task.CompletedTask;
    }
}