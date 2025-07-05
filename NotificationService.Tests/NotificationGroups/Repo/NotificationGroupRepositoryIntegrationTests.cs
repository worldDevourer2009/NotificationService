using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Services.Repos;
using NotificationService.Domain.Entities;

namespace NotificationService.Tests.NotificationGroups.Repo;

public class NotificationGroupRepositoryIntegrationTests : BaseIntegrationTest
{
    private readonly INotificationGroupRepository _repository;

    public NotificationGroupRepositoryIntegrationTests(PostgreSqlFixture fixture) : base(fixture)
    {
        _repository = _serviceProvider.GetRequiredService<INotificationGroupRepository>();
    }

    [Fact]
    public async Task CreateGroupForUserAsync_ShouldCreateGroup_WhenValidGroup()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var group = CreateTestGroup();

        // Act
        var result = await _repository.CreateGroupForUserAsync(group);

        // Assert
        Assert.True(result);

        var createdGroup = await _dbContext.NotificationGroups
            .FirstOrDefaultAsync(x => x.Id == group.Id);

        Assert.NotNull(createdGroup);
        Assert.Equal(group.Name, createdGroup.Name);
        Assert.Equal(group.Creator, createdGroup.Creator);
        Assert.Equal(group.Members.Count, createdGroup.Members.Count);

        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task CreateGroupForUserAsync_ShouldReturnFalse_WhenGroupAlreadyExists()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var group = CreateTestGroup();

        await _repository.CreateGroupForUserAsync(group);

        // Act
        var result = await _repository.CreateGroupForUserAsync(group);

        // Assert
        Assert.False(result);

        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task GetNotificationGroupForUser_ShouldReturnGroup_WhenUserIsMember()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var group = CreateTestGroup();
        var userId = Guid.Parse(group.Members.First());

        await _repository.CreateGroupForUserAsync(group);

        // Act
        var result = await _repository.GetNotificationGroupForUser(userId, group.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.Id);
        Assert.Equal(group.Name, result.Name);

        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task GetNotificationGroupForUser_ShouldReturnNull_WhenUserNotMember()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var group = CreateTestGroup();
        var nonMemberUserId = Guid.NewGuid();

        await _repository.CreateGroupForUserAsync(group);

        // Act
        var result = await _repository.GetNotificationGroupForUser(nonMemberUserId, group.Id);

        // Assert
        Assert.Null(result);

        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task GetAllNotificationGroupsForUserAsync_ShouldReturnUserGroups()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var userId = Guid.NewGuid();
        var group1 = CreateTestGroup();
        var group2 = CreateTestGroup();

        group1.Members.Add(userId.ToString());
        group2.Members.Add(userId.ToString());

        await _repository.CreateGroupForUserAsync(group1);
        await _repository.CreateGroupForUserAsync(group2);

        // Act
        var result = await _repository.GetAllNotificationGroupsForUserAsync(userId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, g => g.Id == group1.Id);
        Assert.Contains(result, g => g.Id == group2.Id);

        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task DeleteNotificationGroupForUserAsync_ShouldDeleteGroup_WhenExists()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var group = CreateTestGroup();

        await _repository.CreateGroupForUserAsync(group);

        // Act
        var result = await _repository.DeleteNotificationGroupForUserAsync(group.Id);

        // Assert
        Assert.True(result);

        var deletedGroup = await _dbContext.NotificationGroups
            .FirstOrDefaultAsync(x => x.Id == group.Id);

        Assert.Null(deletedGroup);

        await CleanupDatabaseAsync();
    }

    [Fact]
    public async Task UpdateNotificationGroupForUserAsync_ShouldUpdateGroup_WhenExists()
    {
        // Arrange
        await InitializeDatabaseAsync();
        var group = CreateTestGroup();

        await _repository.CreateGroupForUserAsync(group);

        var updatedGroup = CreateTestGroup();
        updatedGroup.UpdateName("Updated Name");
        updatedGroup.UpdateDescription("Updated Description");

        // Act
        var result = await _repository.UpdateNotificationGroupForUserAsync(group.Id, updatedGroup);

        // Assert
        Assert.True(result);

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