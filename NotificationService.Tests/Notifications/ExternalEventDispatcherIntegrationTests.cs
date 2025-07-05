using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Application.ExternalEvents.AuthService;
using NotificationService.Application.ExternalEvents.EventHandlers;
using NotificationService.Application.ExternalEvents.ExternalEventHandlers;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Services.EventDispatchers;
using NotificationService.Domain.Services;
using Xunit.Abstractions;

namespace NotificationService.Tests.Notifications;

public class ExternalEventDispatcherIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<ILogger<ExternalEventDispatcher>> _loggerMock;

    public ExternalEventDispatcherIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerMock = new Mock<ILogger<ExternalEventDispatcher>>();

        var services = new ServiceCollection();
        services.AddSingleton(_loggerMock.Object);
        var motificationService = new Mock<INotificationService>();
        services.AddSingleton(motificationService.Object);
        var handlerLoggerMock = new Mock<ILogger<UserSignedUpExternalEventHandler>>();
        services.AddSingleton(handlerLoggerMock.Object);
        services.AddTransient<UserSignedUpExternalEventHandler>();
        services.AddTransient<IExternalEventHandler<UserSignedUpExternalEvent>, UserSignedUpExternalEventHandler>();
        services.AddTransient<IExternalEventDispatcher, ExternalEventDispatcher>();
            
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task DispatchAsync_WithValidEvent_ShouldCallHandler()
    {
        // Arrange
        var dispatcher = _serviceProvider.GetRequiredService<IExternalEventDispatcher>();
        var testEvent = UserSignedUpExternalEvent.Create("user.signed.up", "test data", "test source");

        // Act
        await dispatcher.DispatchAsync(testEvent);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("External event handled successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
            
        _output.WriteLine("External event dispatched successfully");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}