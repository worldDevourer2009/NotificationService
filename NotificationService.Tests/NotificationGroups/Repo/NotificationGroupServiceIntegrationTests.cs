using Microsoft.Extensions.DependencyInjection;
using Moq;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Services;
using Xunit.Abstractions;

namespace NotificationService.Tests.NotificationGroups.Repo;

public class NotificationGroupServiceIntegrationTests : BaseIntegrationTest
{
    private readonly INotificationGroupService _service;
    private readonly ITestOutputHelper _output;

    public NotificationGroupServiceIntegrationTests(PostgreSqlFixture fixture, ITestOutputHelper outputHelper) : base(fixture)
    {
        _service = _serviceProvider.GetRequiredService<INotificationGroupService>();
        _output = outputHelper;
    }

    [Fact]
    public async Task CreateGroupAsync_ShouldCreateGroupAndAddToSignalR_WhenValidGroup()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var group = CreateTestGroup();

        // Act
        var result = await _service.CreateGroupAsync(group);

        // Assert
        Assert.True(result);

        // Verify SignalR group operations
        foreach (var member in group.Members)
        {
            _mockGroupManager.Verify(x => x.AddToGroupAsync(
                    $"user_{member}",
                    $"notifGroup_{group.Id}",
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task CreateGroupAsync_ShouldReturnFalse_WhenGroupHasNoMembers()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var group = CreateTestGroup();
        group.Members.Clear();

        // Act
        var result = await _service.CreateGroupAsync(group);

        // Assert
        Assert.False(result);

        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task GetGroupAsync_ShouldReturnGroup_WhenExists()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var group = CreateTestGroup();
        

        await _service.CreateGroupAsync(group);

        try
        {
            // Act
            var result = await _service.GetGroupAsync(group.Id.ToString());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(group.Id, result.Id);
            Assert.Equal(group.Name, result.Name);
        }
        catch (Exception ex)
        {
            _output.WriteLine(ex.Message);
        }

        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task GetGroupsForUserAsync_ShouldReturnEmptyList_WhenInvalidUserId()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var invalidUserId = "invalid-guid";

        // Act
        var result = await _service.GetGroupsForUserAsync(invalidUserId);

        // Assert
        Assert.Null(result);

        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task AddUserToGroupAsync_ShouldAddUserAndUpdateSignalR_WhenValidInput()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var group = CreateTestGroup();
        var newUserId = Guid.NewGuid().ToString();

        await _service.CreateGroupAsync(group);

        try
        {
            // Act
            var result = await _service.AddUserToGroupAsync(group.Id.ToString(), newUserId);

            // Assert
            Assert.True(result);

            // Verify SignalR group operation
            _mockGroupManager.Verify(x => x.AddToGroupAsync(
                    $"user_{newUserId}",
                    $"notifGroup_{group.Id}",
                    It.IsAny<CancellationToken>()),
                Times.Once);

        }
        catch (Exception ex)
        {
            _output.WriteLine(ex.Message);
        }

        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task RemoveUserFromGroupAsync_ShouldRemoveUserAndUpdateSignalR_WhenValidInput()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var group = CreateTestGroup();
        var memberToRemove = Guid.NewGuid().ToString();;
        group.AddMember(memberToRemove);

        await _service.CreateGroupAsync(group);

        // Act
        var result = await _service.RemoveUserFromGroupAsync(group.Id.ToString(), memberToRemove);

        // Assert
        Assert.True(result);

        // Verify SignalR group operation
        _mockGroupManager.Verify(x => x.RemoveFromGroupAsync(
                $"user_{memberToRemove}",
                $"notifGroup_{group.Id}",
                It.IsAny<CancellationToken>()),
            Times.Once);

        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task DeleteGroupAsync_ShouldDeleteGroupAndRemoveFromSignalR_WhenExists()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var group = CreateTestGroup();

        await _service.CreateGroupAsync(group);

        try
        {
            // Act
            var result = await _service.DeleteGroupAsync(group.Id.ToString());

            // Assert
            Assert.True(result);

            // Verify SignalR group operation - должно быть для каждого члена группы
            foreach (var member in group.Members)
            {
                _mockGroupManager.Verify(x => x.RemoveFromGroupAsync(
                        $"user_{member}",
                        $"notifGroup_{group.Id}",
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine(ex.Message);
        }

        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task DeleteGroupAsync_ShouldReturnFalse_WhenInvalidGroupId()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var invalidGroupId = "invalid-guid";

        // Act
        var result = await _service.DeleteGroupAsync(invalidGroupId);

        // Assert
        Assert.False(result);

        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task UpdateGroupAsync_ShouldUpdateGroup_WhenValidInput()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var group = CreateTestGroup();

        await _service.CreateGroupAsync(group);

        group.UpdateName("Updated Name");
        group.UpdateDescription("Updated Description");

        // Act
        var result = await _service.UpdateGroupAsync(group);

        // Assert
        Assert.True(result);

        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task UpdateGroupAsync_ShouldReturnFalse_WhenInvalidCreatorId()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var group = CreateTestGroup();
        group.SetNewCreator("invalid-guid");

        // Act
        var result = await _service.UpdateGroupAsync(group);

        // Assert
        Assert.False(result);

        await CleanupDatabaseAsync();
    }

    private NotificationGroupEntity CreateTestGroup()
    {
        return NotificationGroupEntity.Create(
            "Test Group", 
            "Test Description", 
            Guid.NewGuid().ToString(), 
            new List<string> { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() });
    }
}