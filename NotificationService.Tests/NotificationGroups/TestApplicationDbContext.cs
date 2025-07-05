using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Interfaces;

namespace NotificationService.Tests.NotificationGroups;

public class TestApplicationDbContext : DbContext, IApplicationDbContext
{
    public DbSet<NotificationGroupEntity> NotificationGroups { get; set; }
    public DbSet<InternalNotification> InternalNotifications { get; set; }

    public TestApplicationDbContext(DbContextOptions<TestApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationGroupEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Description);
            entity.Property(e => e.Creator).IsRequired();
            entity.Property(e => e.Members)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
                .HasColumnType("text");
        });

        base.OnModelCreating(modelBuilder);
    }
}