using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Application.Services.Repos;
using NotificationService.Domain.Services;
using NotificationService.Infrastructure.Interfaces;
using NotificationService.Infrastructure.Services;
using NotificationService.Infrastructure.Services.Repos;
using NotificationService.Infrastructure.SignalR;

namespace NotificationService.Tests.NotificationGroups;

public abstract class BaseIntegrationTest : IClassFixture<PostgreSqlFixture>
{
    protected readonly PostgreSqlFixture _fixture;
    protected readonly ServiceProvider _serviceProvider;
    protected readonly TestApplicationDbContext _dbContext;
    protected readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
    protected readonly Mock<IGroupManager> _mockGroupManager;

    protected BaseIntegrationTest(PostgreSqlFixture fixture)
    {
        _fixture = fixture;

        var services = new ServiceCollection();
        
        // Add DbContext
        services.AddDbContext<TestApplicationDbContext>(options =>
            options.UseNpgsql(_fixture.ConnectionString));

        // Register DbContext as IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<TestApplicationDbContext>());

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add repository
        services.AddScoped<INotificationGroupRepository, NotificationGroupRepository>();

        // Mock SignalR Hub Context
        _mockHubContext = new Mock<IHubContext<NotificationHub>>();
        _mockGroupManager = new Mock<IGroupManager>();
        _mockHubContext.Setup(x => x.Groups).Returns(_mockGroupManager.Object);
        services.AddSingleton(_mockHubContext.Object);

        // Add service
        services.AddScoped<INotificationGroupService, NotificationGroupService>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<TestApplicationDbContext>();
    }

    protected async Task InitializeDatabaseAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();
    }

    protected async Task CleanupDatabaseAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
    }
}