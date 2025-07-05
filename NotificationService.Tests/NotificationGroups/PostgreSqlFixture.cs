using Testcontainers.PostgreSql;

namespace NotificationService.Tests.NotificationGroups;

public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    public string ConnectionString { get; private set; } = string.Empty;

    public PostgreSqlFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithDatabase("notification_test_db")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}