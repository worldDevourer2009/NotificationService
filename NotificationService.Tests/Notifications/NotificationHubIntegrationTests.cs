using Microsoft.AspNetCore.SignalR;
using Moq;
using NotificationService.Infrastructure.SignalR;
using Xunit.Abstractions;

namespace NotificationService.Tests.Notifications;

public class NotificationHubIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public NotificationHubIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task JoinUserAsync_ShouldAddToGroup()
    {
        // Arrange
        var hub = new NotificationHub();
        var mockContext = new Mock<HubCallerContext>();
        var mockGroups = new Mock<IGroupManager>();
        var userId = "test-user-123";

        mockContext.Setup(x => x.ConnectionId).Returns("connection-123");
        mockGroups.Setup(x => x.AddToGroupAsync("connection-123", $"user_{userId}", default))
            .Returns(Task.CompletedTask);

        // Используем reflection для установки Context и Groups
        var contextProperty = typeof(Hub).GetProperty("Context");
        var groupsProperty = typeof(Hub).GetProperty("Groups");

        contextProperty?.SetValue(hub, mockContext.Object);
        groupsProperty?.SetValue(hub, mockGroups.Object);

        // Act
        await hub.JoinUserAsync(userId);

        // Assert
        mockGroups.Verify(x => x.AddToGroupAsync("connection-123", $"user_{userId}", default), Times.Once);
        _output.WriteLine($"User {userId} joined group successfully");
    }

    [Fact]
    public async Task LeaveUserAsync_ShouldRemoveFromGroup()
    {
        // Arrange
        var hub = new NotificationHub();
        var mockContext = new Mock<HubCallerContext>();
        var mockGroups = new Mock<IGroupManager>();
        var userId = "test-user-123";

        mockContext.Setup(x => x.ConnectionId).Returns("connection-123");
        mockGroups.Setup(x => x.RemoveFromGroupAsync("connection-123", $"user_{userId}", default))
            .Returns(Task.CompletedTask);

        var contextProperty = typeof(Hub).GetProperty("Context");
        var groupsProperty = typeof(Hub).GetProperty("Groups");

        contextProperty?.SetValue(hub, mockContext.Object);
        groupsProperty?.SetValue(hub, mockGroups.Object);

        await hub.LeaveUserAsync(userId);

        mockGroups.Verify(x => x.RemoveFromGroupAsync("connection-123", $"user_{userId}", default), Times.Once);
        _output.WriteLine($"User {userId} left group successfully");
    }
}