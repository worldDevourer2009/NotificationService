using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace NotificationService.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = 
            "Host=notification-service-db;Port=5432;Database=notification_service;Username=notification_service_username;Password=notification_service_password;Include Error Detail=true";
        
        optionsBuilder.UseNpgsql(connectionString, b => 
            b.MigrationsAssembly("NotificationService.Infrastructure"));
        
        return new AppDbContext(optionsBuilder.Options);
    }
}