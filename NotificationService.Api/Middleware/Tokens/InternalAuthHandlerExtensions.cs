namespace NotificationService.Api.Middleware.Tokens;

public static class InternalAuthHandlerExtensions
{
    public static IServiceCollection AddInternalAuthHandler(this IServiceCollection services)
    {
        services.AddTransient<InternalAuthHandler>();
        return services;
    }

    public static IHttpClientBuilder AddInternalAuthHandler(this IHttpClientBuilder builder)
    {
        return builder.AddHttpMessageHandler<InternalAuthHandler>();
    }
}