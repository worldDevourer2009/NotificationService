using FluentAssertions;
using NotificationService.Application.ExternalEvents.AuthService;
using NotificationService.Application.Services.Factories;
using Xunit.Abstractions;
using TaskHandler.Shared.Kafka.EventTypes.AuthService;

namespace NotificationService.Tests.Notifications;

public class ExternalEventFactoryIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly ExternalEventFactory _factory;

    public ExternalEventFactoryIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _factory = new ExternalEventFactory();
    }

    [Fact]
    public async Task CreateEventAsync_WithUserSignedUpEvent_ShouldReturnCorrectEvent()
    {
        // Arrange
        var eventType = AuthEventTypes.UserSignedUp;
        var eventData = "test data";
        var source = "test source";

        // Act
        var result = await _factory.CreateEventAsync(eventType, eventData, source);

        // Assert
        result.Should().BeOfType<UserSignedUpExternalEvent>();
        result.EventType.Should().Be(eventType);
        result.EventData.Should().Be(eventData);
        result.Source.Should().Be(source);
        _output.WriteLine($"Created event: {result.GetType().Name}");
    }

    [Fact]
    public async Task CreateEventAsync_WithUserLoggedInEvent_ShouldReturnCorrectEvent()
    {
        // Arrange
        var eventType = AuthEventTypes.UserLoggedIn;
        var eventData = "test data";
        var source = "test source";

        // Act
        var result = await _factory.CreateEventAsync(eventType, eventData, source);

        // Assert
        result.Should().BeOfType<UserLoggedInExternalEvent>();
        result.EventType.Should().Be(eventType);
        result.EventData.Should().Be(eventData);
        result.Source.Should().Be(source);
        _output.WriteLine($"Created event: {result.GetType().Name}");
    }

    [Fact]
    public async Task CreateEventAsync_WithUnsupportedEvent_ShouldThrowException()
    {
        // Arrange
        var eventType = "unsupported.event";
        var eventData = "test data";
        var source = "test source";

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _factory.CreateEventAsync(eventType, eventData, source));
        _output.WriteLine("Unsupported event correctly threw exception");
    }
}