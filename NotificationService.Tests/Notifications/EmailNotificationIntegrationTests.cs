using FluentAssertions;
using NotificationService.Domain.Entities;
using Xunit.Abstractions;

namespace NotificationService.Tests.Notifications;

public class EmailNotificationIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public EmailNotificationIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void EmailNotification_Create_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var title = "Test Email";
        var body = "Test Body";
        var sender = "sender@example.com";
        var receiver = "receiver@example.com";
        var isRead = false;

        // Act
        var notification = EmailNotification.Create(title, body, sender, receiver, isRead);

        // Assert
        notification.Title.Should().Be(title);
        notification.Body.Should().Be(body);
        notification.Sender.Should().Be(sender);
        notification.Receiver.Should().Be(receiver);
        notification.IsRead.Should().Be(isRead);
        notification.Id.Should().NotBeEmpty();
        _output.WriteLine($"Created notification with ID: {notification.Id}");
    }

    [Fact]
    public void EmailNotification_MarkAsRead_ShouldSetIsReadToTrue()
    {
        // Arrange
        var notification = EmailNotification.Create("Test", "Body", "sender@test.com", "receiver@test.com", false);

        // Act
        notification.MarkAsRead();

        // Assert
        notification.IsRead.Should().BeTrue();
        _output.WriteLine("Notification marked as read");
    }

    [Fact]
    public void EmailNotification_MarkAsDeleted_ShouldSetIsDeletedToTrue()
    {
        // Arrange
        var notification = EmailNotification.Create("Test", "Body", "sender@test.com", "receiver@test.com", false);

        // Act
        notification.MarkAsDeleted();

        // Assert
        notification.IsDeleted.Should().BeTrue();
        _output.WriteLine("Notification marked as deleted");
    }

    [Fact]
    public void EmailNotification_MarkAsSent_ShouldSetIsSentToTrue()
    {
        // Arrange
        var notification = EmailNotification.Create("Test", "Body", "sender@test.com", "receiver@test.com", false);

        // Act
        notification.MarkAsSent();

        // Assert
        notification.IsSent.Should().BeTrue();
        _output.WriteLine("Notification marked as sent");
    }
}