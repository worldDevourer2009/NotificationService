using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;
using NotificationService.Application.Services.Repos;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.SignalR;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;

namespace NotificationService.Tests.Notifications;

public class NotificationHubIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;
    private AppDbContext _context;
    private readonly PostgreSqlContainer _postgreSqlContainer;

    public NotificationHubIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _output = output;

        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("notification_service_test")
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .WithCleanUp(true)
            .Build();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => { logging.ClearProviders(); });
        });

        _factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((ctx, conf) =>
            {
                var settings = new Dictionary<string, string>
                {
                    ["ConnectionStrings:DefaultConnection"] = _postgreSqlContainer.GetConnectionString(),
                    ["JwtSettings:Issuer"] = "test-issuer",
                    ["JwtSettings:Audience"] = "test-audience",
                    ["JwtSettings:Key"] = "Nj8s@Z%~4vH8*91@"
                };
                conf.AddInMemoryCollection(settings);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();

                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(_postgreSqlContainer.GetConnectionString(),
                        npgsql => npgsql.MigrationsAssembly("AuthService.Infrastructure")));

                services.AddLogging(log => log.AddConsole().SetMinimumLevel(LogLevel.Debug));
                services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();

        var connectionString = _postgreSqlContainer.GetConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly("AuthService.Infrastructure"))
            .Options;

        _context = new AppDbContext(optionsBuilder);
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgreSqlContainer.StopAsync();
    }

    [Fact]
    public async Task JoinUserAsync_ShouldAddToGroup()
    {
        // Arrange
        var mockRepo = new Mock<INotificationGroupRepository>();
        var hub = new NotificationHub(mockRepo.Object);
        
        var mockContext = new Mock<HubCallerContext>();
        var mockGroups = new Mock<IGroupManager>();
        var userId = "test-user-123";
        var connectionId = "connection-123";

        mockContext.Setup(x => x.ConnectionId).Returns(connectionId);
        mockGroups.Setup(x => x.AddToGroupAsync(connectionId, $"user_{userId}", default))
            .Returns(Task.CompletedTask);
        
        SetHubProperties(hub, mockContext.Object, mockGroups.Object);

        // Act
        await hub.JoinUserAsync(userId);

        // Assert
        mockGroups.Verify(x => x.AddToGroupAsync(connectionId, $"user_{userId}", default), Times.Once);
        _output.WriteLine($"User {userId} joined group successfully");
    }

    [Fact]
    public async Task LeaveUserAsync_ShouldRemoveFromGroup()
    {
        // Arrange
        var mockRepo = new Mock<INotificationGroupRepository>();
        var hub = new NotificationHub(mockRepo.Object);
        
        var mockContext = new Mock<HubCallerContext>();
        var mockGroups = new Mock<IGroupManager>();
        var userId = "test-user-123";
        var connectionId = "connection-123";

        mockContext.Setup(x => x.ConnectionId).Returns(connectionId);
        mockGroups.Setup(x => x.RemoveFromGroupAsync(connectionId, $"user_{userId}", default))
            .Returns(Task.CompletedTask);

        // Устанавливаем контекст и группы через рефлексию
        SetHubProperties(hub, mockContext.Object, mockGroups.Object);

        // Act
        await hub.LeaveUserAsync(userId);

        // Assert
        mockGroups.Verify(x => x.RemoveFromGroupAsync(connectionId, $"user_{userId}", default), Times.Once);
        _output.WriteLine($"User {userId} left group successfully");
    }

    [Fact]
    public async Task JoinNotificationGroupAsync_ShouldAddToGroup()
    {
        // Arrange
        var mockRepo = new Mock<INotificationGroupRepository>();
        var hub = new NotificationHub(mockRepo.Object);
        
        var mockContext = new Mock<HubCallerContext>();
        var mockGroups = new Mock<IGroupManager>();
        var groupId = "group-123";
        var connectionId = "connection-123";

        mockContext.Setup(x => x.ConnectionId).Returns(connectionId);
        mockGroups.Setup(x => x.AddToGroupAsync(connectionId, $"notifGroup_{groupId}", default))
            .Returns(Task.CompletedTask);

        SetHubProperties(hub, mockContext.Object, mockGroups.Object);

        // Act
        await hub.JoinNotificationGroupAsync(groupId);

        // Assert
        mockGroups.Verify(x => x.AddToGroupAsync(connectionId, $"notifGroup_{groupId}", default), Times.Once);
        _output.WriteLine($"Successfully joined notification group {groupId}");
    }

    [Fact]
    public async Task LeaveNotificationGroupAsync_ShouldRemoveFromGroup()
    {
        // Arrange
        var mockRepo = new Mock<INotificationGroupRepository>();
        var hub = new NotificationHub(mockRepo.Object);
        
        var mockContext = new Mock<HubCallerContext>();
        var mockGroups = new Mock<IGroupManager>();
        var groupId = "group-123";
        var connectionId = "connection-123";

        mockContext.Setup(x => x.ConnectionId).Returns(connectionId);
        mockGroups.Setup(x => x.RemoveFromGroupAsync(connectionId, $"notifGroup_{groupId}", default))
            .Returns(Task.CompletedTask);

        SetHubProperties(hub, mockContext.Object, mockGroups.Object);

        // Act
        await hub.LeaveNotificationGroupAsync(groupId);

        // Assert
        mockGroups.Verify(x => x.RemoveFromGroupAsync(connectionId, $"notifGroup_{groupId}", default), Times.Once);
        _output.WriteLine($"Successfully left notification group {groupId}");
    }

    [Fact]
    public async Task OnConnectedAsync_WithValidUserId_ShouldJoinUserAndNotificationGroups()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var connectionId = "connection-123";
        var notificationGroups = new List<NotificationGroupEntity>
        {
            NotificationGroupEntity.Create("Group 1", "", userId, new List<string> { userId }),
            NotificationGroupEntity.Create("Group 2", "", userId, new List<string> { userId }),
        };

        var mockRepo = new Mock<INotificationGroupRepository>();
        mockRepo.Setup(r => r.GetAllNotificationGroupsForUserAsync(Guid.Parse(userId), default))
            .ReturnsAsync(notificationGroups);

        var hub = new NotificationHub(mockRepo.Object);
        
        var mockContext = new Mock<HubCallerContext>();
        var mockGroups = new Mock<IGroupManager>();

        mockContext.Setup(x => x.ConnectionId).Returns(connectionId);
        mockContext.Setup(x => x.UserIdentifier).Returns(userId);
        
        mockGroups.Setup(x => x.AddToGroupAsync(connectionId, $"user_{userId}", default))
            .Returns(Task.CompletedTask);
        
        foreach (var group in notificationGroups)
        {
            mockGroups.Setup(x => x.AddToGroupAsync(connectionId, $"notifGroup_{group.Id}", default))
                .Returns(Task.CompletedTask);
        }

        SetHubProperties(hub, mockContext.Object, mockGroups.Object);

        // Act
        await hub.OnConnectedAsync();

        // Assert
        mockGroups.Verify(x => x.AddToGroupAsync(connectionId, $"user_{userId}", default), Times.Once);
        
        foreach (var group in notificationGroups)
        {
            mockGroups.Verify(x => x.AddToGroupAsync(connectionId, $"notifGroup_{group.Id}", default), Times.Once);
        }
        
        mockRepo.Verify(r => r.GetAllNotificationGroupsForUserAsync(Guid.Parse(userId), default), Times.Once);
        _output.WriteLine($"User {userId} connected and joined {notificationGroups.Count} notification groups");
    }

    [Fact]
    public async Task OnConnectedAsync_WithNullUserId_ShouldNotJoinAnyGroups()
    {
        // Arrange
        var mockRepo = new Mock<INotificationGroupRepository>();
        var hub = new NotificationHub(mockRepo.Object);
        
        var mockContext = new Mock<HubCallerContext>();
        var mockGroups = new Mock<IGroupManager>();

        mockContext.Setup(x => x.ConnectionId).Returns("connection-123");
        mockContext.Setup(x => x.UserIdentifier).Returns((string)null);

        SetHubProperties(hub, mockContext.Object, mockGroups.Object);

        // Act
        await hub.OnConnectedAsync();

        // Assert
        mockGroups.Verify(x => x.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
        mockRepo.Verify(r => r.GetAllNotificationGroupsForUserAsync(It.IsAny<Guid>(), default), Times.Never);
        _output.WriteLine("Connection with null user ID handled correctly");
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithValidUserId_ShouldLeaveUserGroup()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var connectionId = "connection-123";
        var mockRepo = new Mock<INotificationGroupRepository>();
        var hub = new NotificationHub(mockRepo.Object);
        
        var mockContext = new Mock<HubCallerContext>();
        var mockGroups = new Mock<IGroupManager>();

        mockContext.Setup(x => x.ConnectionId).Returns(connectionId);
        mockContext.Setup(x => x.UserIdentifier).Returns(userId);
        
        mockGroups.Setup(x => x.RemoveFromGroupAsync(connectionId, $"user_{userId}", default))
            .Returns(Task.CompletedTask);

        SetHubProperties(hub, mockContext.Object, mockGroups.Object);

        // Act
        await hub.OnDisconnectedAsync(null);

        // Assert
        mockGroups.Verify(x => x.RemoveFromGroupAsync(connectionId, $"user_{userId}", default), Times.Once);
        _output.WriteLine($"User {userId} disconnected and left user group");
    }

    [Fact]
    public async Task JoinSubgroupAsync_ShouldAddToSubgroup()
    {
        // Arrange
        var mockRepo = new Mock<INotificationGroupRepository>();
        var hub = new NotificationHub(mockRepo.Object);
        
        var mockContext = new Mock<HubCallerContext>();
        var mockGroups = new Mock<IGroupManager>();
        var groupId = "group-123";
        var subgroupId = "subgroup-456";
        var connectionId = "connection-123";

        mockContext.Setup(x => x.ConnectionId).Returns(connectionId);
        mockGroups.Setup(x => x.AddToGroupAsync(connectionId, $"notifGroup_{groupId}_sub_{subgroupId}", default))
            .Returns(Task.CompletedTask);

        SetHubProperties(hub, mockContext.Object, mockGroups.Object);

        // Act
        await hub.JoinSubgroupAsync(groupId, subgroupId);

        // Assert
        mockGroups.Verify(x => x.AddToGroupAsync(connectionId, $"notifGroup_{groupId}_sub_{subgroupId}", default), Times.Once);
        _output.WriteLine($"Successfully joined subgroup {subgroupId} in group {groupId}");
    }

    [Fact]
    public async Task LeaveSubgroupAsync_ShouldRemoveFromSubgroup()
    {
        // Arrange
        var mockRepo = new Mock<INotificationGroupRepository>();
        var hub = new NotificationHub(mockRepo.Object);
        
        var mockContext = new Mock<HubCallerContext>();
        var mockGroups = new Mock<IGroupManager>();
        var groupId = "group-123";
        var subgroupId = "subgroup-456";
        var connectionId = "connection-123";

        mockContext.Setup(x => x.ConnectionId).Returns(connectionId);
        mockGroups.Setup(x => x.RemoveFromGroupAsync(connectionId, $"notifGroup_{groupId}_sub_{subgroupId}", default))
            .Returns(Task.CompletedTask);

        SetHubProperties(hub, mockContext.Object, mockGroups.Object);

        // Act
        await hub.LeaveSubgroupAsync(groupId, subgroupId);

        // Assert
        mockGroups.Verify(x => x.RemoveFromGroupAsync(connectionId, $"notifGroup_{groupId}_sub_{subgroupId}", default), Times.Once);
        _output.WriteLine($"Successfully left subgroup {subgroupId} in group {groupId}");
    }
    
    private static void SetHubProperties(Hub hub, HubCallerContext context, IGroupManager groups)
    {
        var contextProperty = typeof(Hub).GetProperty("Context", 
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        var groupsProperty = typeof(Hub).GetProperty("Groups", 
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        contextProperty?.SetValue(hub, context);
        groupsProperty?.SetValue(hub, groups);
    }
}