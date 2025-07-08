using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Services;
using NotificationService.Infrastructure.SignalR;
using Xunit.Abstractions;

namespace NotificationService.Tests.Notifications;

public class NotificationServiceIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IHubContext<NotificationHub>> _hubContextMock;
    private readonly Mock<IEmailSender> _emailSenderMock;
    private readonly Mock<ITelegramService> _telegramServiceMock;
    private readonly Mock<ILogger<Infrastructure.Services.NotificationService>> _loggerMock;

    public NotificationServiceIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        _hubContextMock = new Mock<IHubContext<NotificationHub>>();
        _emailSenderMock = new Mock<IEmailSender>();
        _telegramServiceMock = new Mock<ITelegramService>();
        _loggerMock = new Mock<ILogger<Infrastructure.Services.NotificationService>>();

        var services = new ServiceCollection();
        services.AddSingleton(_hubContextMock.Object);
        services.AddSingleton(_emailSenderMock.Object);
        services.AddSingleton(_telegramServiceMock.Object);
        services.AddSingleton(_loggerMock.Object);
        services.AddTransient<Infrastructure.Services.NotificationService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task SendEmailNotificationAsync_ShouldCallEmailSender()
    {
        // Arrange
        var notificationService = _serviceProvider.GetRequiredService<Infrastructure.Services.NotificationService>();
        var notification = EmailNotification.Create("Test", "Body", "sender@test.com", "receiver@test.com", false);
        var email = "test@example.com";

        var cancellationToken = CancellationToken.None;
        _emailSenderMock.Setup(x => x.SendEmailAsync(email, notification.Title!, notification.Body!, cancellationToken))
            .ReturnsAsync(true);

        // Act
        await notificationService.SendEmailNotificationAsync(email, notification, cancellationToken);

        // Assert
        _emailSenderMock.Verify(x => x.SendEmailAsync(email, notification.Title!, notification.Body!, cancellationToken), Times.Once);
        _output.WriteLine("Email notification sent successfully");
    }

    [Fact]
    public async Task SendTelegramNotificationAsync_ShouldCallTelegramService()
    {
        // Arrange
        var notificationService = _serviceProvider.GetRequiredService<Infrastructure.Services.NotificationService>();
        var notification = EmailNotification.Create("Test", "Body", "sender@test.com", "receiver@test.com", false);
        var telegramId = "123456789";

        // Act
        await notificationService.SendTelegramNotificationAsync(telegramId, notification);

        // Assert
        _telegramServiceMock.Verify(
            x => x.SendTelegramMessageAsync(telegramId, "Test\nBody", 
                It.IsAny<CancellationToken>()), Times.Once);
        _output.WriteLine("Telegram notification sent successfully");
    }

    [Fact]
    public async Task SendInternalNotificationAsync_ShouldCallHubContext()
    {
        // Arrange
        var notificationService = _serviceProvider.GetRequiredService<Infrastructure.Services.NotificationService>();
        var notification = EmailNotification.Create("Test", "Body", "sender@test.com", "receiver@test.com", false);
        var userId = Guid.NewGuid();

        var clientProxyMock = new Mock<IClientProxy>();
        var hubClientsMock = new Mock<IHubClients>();
        
        var expectedGroupName = userId.ToString();
        
        hubClientsMock
            .Setup(x => x.Group(expectedGroupName))
            .Returns(clientProxyMock.Object);
        
        _hubContextMock.Setup(x => x.Clients).Returns(hubClientsMock.Object);

        // Act
        await notificationService.SendInternalNotificationAsync(expectedGroupName, notification);

        // Assert
        hubClientsMock.Verify(x => x.Group($"user_{userId}"), Times.Once);
        _output.WriteLine("Internal notification sent successfully");
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}