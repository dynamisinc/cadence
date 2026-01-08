using DynamisReferenceApp.Api.Tools.Notes.Models.Entities;

namespace DynamisReferenceApp.Api.Core.Data;

/// <summary>
/// Entity Framework Core database context for the application.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // =========================================================================
    // DbSets - Add new entities here
    // =========================================================================

    /// <summary>
    /// Notes table - sample feature demonstrating CRUD patterns.
    /// </summary>
    public DbSet<Note> Notes => Set<Note>();

    // =========================================================================
    // Model Configuration
    // =========================================================================

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Configure Note entity
        modelBuilder.Entity<Note>(entity =>
        {
            entity.ToTable("Notes");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Content)
                .HasMaxLength(10000);

            entity.Property(e => e.UserId)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime2")
                .HasDefaultValueSql("GETUTCDATE()");

            // Index for user queries
            entity.HasIndex(e => e.UserId);

            // Index for soft delete queries
            entity.HasIndex(e => e.IsDeleted);

            // Composite index for common query pattern
            entity.HasIndex(e => new { e.UserId, e.IsDeleted });
        });
    }

    // =========================================================================
    // Save Changes Override - Automatic Timestamps
    // =========================================================================

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is IHasTimestamps entity)
            {
                entity.UpdatedAt = DateTime.UtcNow;

                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}

/// <summary>
/// Interface for entities that have created/updated timestamps.
/// </summary>
public interface IHasTimestamps
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
