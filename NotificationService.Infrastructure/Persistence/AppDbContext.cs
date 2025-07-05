using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;
using NotificationService.Infrastructure.Interfaces;

namespace NotificationService.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public DbSet<NotificationGroupEntity> NotificationGroups { get; set; }
    public DbSet<InternalNotification> InternalNotifications { get; set; }
    
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InternalNotification>(entity =>
        {
            entity.ToTable("internal_notifications");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Receiver)
                .HasColumnName("user_id");
            
            entity.Property(x => x.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(x => x.Title)
                .HasColumnName("title")
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(x => x.Body)
                .HasColumnName("notification_body")
                .HasMaxLength(700);

            entity.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(x => x.IsRead)
                .HasColumnName("is_read")
                .IsRequired();
            
            entity.Property(x => x.IsSent)
                .HasColumnName("is_sent")
                .IsRequired();
        });

        modelBuilder.Entity<NotificationGroupEntity>(entity =>
        {
            entity.ToTable("notification_groups");
            
            entity.HasKey(x => x.Id);
            
            entity.HasIndex(x => x.Creator);
            
            entity.Property(x => x.Id)
                .HasColumnName("id")
                .IsRequired();
            
            entity.Property(x => x.Creator)
                .HasColumnName("creator_id")
                .IsRequired();
            
            entity.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(250)
                .IsRequired();
            
            entity.Property(x => x.Description)
                .HasColumnName("description")
                .HasMaxLength(700);
            
            entity.Property(x => x.Members)
                .HasColumnName("members")
                .HasColumnType("jsonb")
                .HasConversion(
                    x => JsonSerializer.Serialize(x, (JsonSerializerOptions?)null), 
                    x => JsonSerializer.Deserialize<List<string>>(x, (JsonSerializerOptions?)null!))
                .IsRequired();
        });
    }
}