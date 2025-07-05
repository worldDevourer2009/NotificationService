using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using NotificationService.Domain.Exceptions;
using NotificationService.Domain.Exceptions.Notifications;
using NotificationService.Domain.Exceptions.Redis;

namespace NotificationService.Api.Middleware.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception");
        var response = httpContext.Response;
        response.ContentType = "application/json";

        var errorsResponse = exception switch
        {
            EmailNotificationException emailNotification => new ErrorResponse
            {
                Title = "Email notification exception",
                Details = emailNotification.Message,
                Status = StatusCodes.Status400BadRequest
            },
            TelegramNotificationNotFoundException telegramNotificationNotFound => new ErrorResponse
            {
                Title = "Telegram notification not found exception",
                Details = telegramNotificationNotFound.Message,
                Status = StatusCodes.Status404NotFound
            },
            InvalidRedisKeyException invalidRedisKeyException => new  ErrorResponse
            {
                Title = "Invalid redis key exception",
                Details = invalidRedisKeyException.Message,
                Status = StatusCodes.Status400BadRequest
            },
            _ => new ErrorResponse
            {
                Title = "Internal server error",
                Details = "Something went wrong",
                Status = StatusCodes.Status500InternalServerError,
                Errors = new List<Exception> { exception }
            }
        };

        await response.WriteAsync(JsonSerializer.Serialize(errorsResponse), cancellationToken);
        
        return true;
    }
}