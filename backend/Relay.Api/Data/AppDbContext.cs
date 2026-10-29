using Microsoft.EntityFrameworkCore;

namespace Relay.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("accounts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
            entity.Property(x => x.Industry).HasColumnName("industry").HasMaxLength(60).IsRequired();
            entity.Property(x => x.Timezone).HasColumnName("timezone").HasMaxLength(60).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        });

        modelBuilder.Entity<ActivityEvent>(entity =>
        {
            entity.ToTable("activity_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(x => x.AccountId).HasColumnName("account_id").IsRequired();
            entity.Property(x => x.Location).HasColumnName("location").HasMaxLength(80).IsRequired();
            entity.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(40).IsRequired();
            entity.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamp with time zone").IsRequired();
            entity.Property(x => x.DurationSeconds).HasColumnName("duration_seconds");
            entity.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(40);
            entity.HasOne(x => x.Account).WithMany(x => x.ActivityEvents).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(x => new { x.AccountId, x.OccurredAt }).HasDatabaseName("ix_activity_events_account_id_occurred_at");
        });
    }
}
