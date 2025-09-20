using Microsoft.EntityFrameworkCore;
using TemenosAlertManager.Core.Entities;
using TemenosAlertManager.Core.Entities.Configuration;

namespace TemenosAlertManager.Infrastructure.Data;

public class TemenosAlertContext : DbContext
{
    public TemenosAlertContext(DbContextOptions<TemenosAlertContext> options) : base(options)
    {
    }

    // Core entities
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<AlertOutbox> AlertOutbox { get; set; }
    public DbSet<CheckResult> CheckResults { get; set; }
    public DbSet<AuditEvent> AuditEvents { get; set; }

    // Configuration entities
    public DbSet<ServiceConfig> ServiceConfigs { get; set; }
    public DbSet<QueueConfig> QueueConfigs { get; set; }
    public DbSet<SqlTargetConfig> SqlTargetConfigs { get; set; }
    public DbSet<AuthConfig> AuthConfigs { get; set; }
    public DbSet<AlertConfig> AlertConfigs { get; set; }
    public DbSet<SystemConfig> SystemConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationships
        modelBuilder.Entity<AlertOutbox>()
            .HasOne(ao => ao.Alert)
            .WithMany(a => a.AlertOutboxes)
            .HasForeignKey(ao => ao.AlertId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for performance
        modelBuilder.Entity<Alert>()
            .HasIndex(a => a.Fingerprint)
            .HasDatabaseName("IX_Alerts_Fingerprint");

        modelBuilder.Entity<Alert>()
            .HasIndex(a => new { a.State, a.CreatedAt })
            .HasDatabaseName("IX_Alerts_State_CreatedAt");

        modelBuilder.Entity<AlertOutbox>()
            .HasIndex(ao => new { ao.Status, ao.NextRetryAt })
            .HasDatabaseName("IX_AlertOutbox_Status_NextRetryAt");

        modelBuilder.Entity<CheckResult>()
            .HasIndex(cr => new { cr.Domain, cr.CheckedAt })
            .HasDatabaseName("IX_CheckResults_Domain_CheckedAt");

        modelBuilder.Entity<CheckResult>()
            .HasIndex(cr => cr.RunId)
            .HasDatabaseName("IX_CheckResults_RunId");

        modelBuilder.Entity<AuditEvent>()
            .HasIndex(ae => new { ae.UserId, ae.EventTime })
            .HasDatabaseName("IX_AuditEvents_UserId_EventTime");

        modelBuilder.Entity<AuditEvent>()
            .HasIndex(ae => ae.EventTime)
            .HasDatabaseName("IX_AuditEvents_EventTime");

        // Configure unique constraints
        modelBuilder.Entity<SystemConfig>()
            .HasIndex(sc => sc.Key)
            .IsUnique()
            .HasDatabaseName("UX_SystemConfigs_Key");

        modelBuilder.Entity<AuthConfig>()
            .HasIndex(ac => ac.AdGroupName)
            .IsUnique()
            .HasDatabaseName("UX_AuthConfigs_AdGroupName");

        // Configure soft delete global filter
        modelBuilder.Entity<Alert>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AlertOutbox>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CheckResult>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ServiceConfig>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<QueueConfig>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SqlTargetConfig>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AuthConfig>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AlertConfig>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SystemConfig>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
}