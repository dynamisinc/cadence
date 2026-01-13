using Cadence.Core.Constants;
using Cadence.Core.Models.Entities;

namespace Cadence.Core.Data;

/// <summary>
/// Entity Framework Core database context for the application.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // =========================================================================
    // DbSets
    // =========================================================================

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<Msel> Msels => Set<Msel>();
    public DbSet<Phase> Phases => Set<Phase>();
    public DbSet<Inject> Injects => Set<Inject>();
    public DbSet<ExerciseParticipant> ExerciseParticipants => Set<ExerciseParticipant>();
    public DbSet<Objective> Objectives => Set<Objective>();
    public DbSet<HseepRole> HseepRoles => Set<HseepRole>();
    public DbSet<Observation> Observations => Set<Observation>();

    // =========================================================================
    // Model Configuration
    // =========================================================================

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Configure global settings for all entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Configure global datetime2 for all DateTime properties
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("datetime2");
                }
            }

            // Configure global soft delete query filter for ISoftDeletable entities
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ConfigureSoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?
                    .MakeGenericMethod(entityType.ClrType);

                method?.Invoke(null, new object[] { modelBuilder });
            }
        }

        // Configure entities
        ConfigureOrganization(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureExercise(modelBuilder);
        ConfigureMsel(modelBuilder);
        ConfigurePhase(modelBuilder);
        ConfigureInject(modelBuilder);
        ConfigureExerciseParticipant(modelBuilder);
        ConfigureObjective(modelBuilder);
        ConfigureHseepRole(modelBuilder);
        ConfigureObservation(modelBuilder);
    }

    /// <summary>
    /// Configures a global query filter to exclude soft-deleted entities.
    /// </summary>
    private static void ConfigureSoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : class, ISoftDeletable
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    // =========================================================================
    // Entity Configurations
    // =========================================================================

    private static void ConfigureOrganization(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000);

            // Seed default organization
            entity.HasData(new Organization
            {
                Id = SystemConstants.DefaultOrganizationId,
                Name = "Default Organization",
                Description = "Default organization for the Cadence system",
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();

            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed system user
            entity.HasData(new User
            {
                Id = SystemConstants.SystemUserId,
                Email = "system@cadence.local",
                DisplayName = "System",
                OrganizationId = SystemConstants.DefaultOrganizationId,
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedBy = SystemConstants.SystemUserId,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });
    }

    private static void ConfigureExercise(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.ExerciseType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.TimeZoneId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(500);

            // Clock state configuration
            entity.Property(e => e.ClockState).HasConversion<string>().HasMaxLength(20);

            entity.HasIndex(e => new { e.OrganizationId, e.Status });
            entity.HasIndex(e => e.ScheduledDate);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Exercises)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ActiveMsel)
                .WithMany()
                .HasForeignKey(e => e.ActiveMselId)
                .OnDelete(DeleteBehavior.NoAction);

            // Clock started by user reference (optional)
            entity.HasOne(e => e.ClockStartedByUser)
                .WithMany()
                .HasForeignKey(e => e.ClockStartedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureMsel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Msel>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000);

            entity.HasIndex(e => new { e.ExerciseId, e.Version });

            entity.HasOne(e => e.Exercise)
                .WithMany(ex => ex.Msels)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePhase(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Phase>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000);

            entity.HasIndex(e => new { e.ExerciseId, e.Sequence });

            entity.HasOne(e => e.Exercise)
                .WithMany(ex => ex.Phases)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureInject(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inject>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.Target).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(200);
            entity.Property(e => e.DeliveryMethod).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.InjectType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.FireCondition).HasMaxLength(500);
            entity.Property(e => e.ExpectedAction).HasMaxLength(2000);
            entity.Property(e => e.ControllerNotes).HasMaxLength(2000);
            entity.Property(e => e.SkipReason).HasMaxLength(500);

            entity.HasIndex(e => new { e.MselId, e.InjectNumber }).IsUnique();
            entity.HasIndex(e => new { e.MselId, e.Sequence });
            entity.HasIndex(e => new { e.MselId, e.Status });
            entity.HasIndex(e => e.PhaseId);
            entity.HasIndex(e => e.ParentInjectId);

            entity.HasOne(e => e.Msel)
                .WithMany(m => m.Injects)
                .HasForeignKey(e => e.MselId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Phase)
                .WithMany(p => p.Injects)
                .HasForeignKey(e => e.PhaseId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ParentInject)
                .WithMany(i => i.ChildInjects)
                .HasForeignKey(e => e.ParentInjectId)
                .OnDelete(DeleteBehavior.NoAction);

            // User references are optional to handle soft-deleted users gracefully.
            // For historical reports, use IgnoreQueryFilters() to include deleted users.
            entity.HasOne(e => e.FiredByUser)
                .WithMany()
                .HasForeignKey(e => e.FiredBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.SkippedByUser)
                .WithMany()
                .HasForeignKey(e => e.SkippedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureExerciseParticipant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExerciseParticipant>(entity =>
        {
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);

            entity.HasIndex(e => new { e.ExerciseId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.Exercise)
                .WithMany(ex => ex.Participants)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            // User navigation is optional to handle soft-deleted users gracefully.
            // For historical reports, use IgnoreQueryFilters() to include deleted users.
            entity.HasOne(e => e.User)
                .WithMany(u => u.ExerciseParticipations)
                .HasForeignKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

        });
    }

    private static void ConfigureObjective(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Objective>(entity =>
        {
            entity.Property(e => e.ObjectiveNumber).HasMaxLength(10).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);

            entity.HasIndex(e => new { e.ExerciseId, e.ObjectiveNumber });

            entity.HasOne(e => e.Exercise)
                .WithMany(ex => ex.Objectives)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureHseepRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HseepRole>(entity =>
        {
            entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => e.Code).IsUnique();

            // Seed HSEEP-defined roles
            entity.HasData(
                new HseepRole
                {
                    Id = (int)ExerciseRole.Administrator,
                    Code = nameof(ExerciseRole.Administrator),
                    Name = "Administrator",
                    Description = "System-wide configuration and user management. Has full access to all exercises within their organization.",
                    SortOrder = 1,
                    IsSystemWide = true,
                    CanFireInjects = true,
                    CanRecordObservations = true,
                    CanManageExercise = true,
                    IsActive = true
                },
                new HseepRole
                {
                    Id = (int)ExerciseRole.ExerciseDirector,
                    Code = nameof(ExerciseRole.ExerciseDirector),
                    Name = "Exercise Director",
                    Description = "Full exercise management authority. Responsible for Go/No-Go decisions and overall exercise conduct.",
                    SortOrder = 2,
                    IsSystemWide = false,
                    CanFireInjects = true,
                    CanRecordObservations = true,
                    CanManageExercise = true,
                    IsActive = true
                },
                new HseepRole
                {
                    Id = (int)ExerciseRole.Controller,
                    Code = nameof(ExerciseRole.Controller),
                    Name = "Controller",
                    Description = "Delivers injects to players and manages scenario flow during exercise conduct.",
                    SortOrder = 3,
                    IsSystemWide = false,
                    CanFireInjects = true,
                    CanRecordObservations = false,
                    CanManageExercise = false,
                    IsActive = true
                },
                new HseepRole
                {
                    Id = (int)ExerciseRole.Evaluator,
                    Code = nameof(ExerciseRole.Evaluator),
                    Name = "Evaluator",
                    Description = "Observes and documents player performance for the After-Action Report (AAR).",
                    SortOrder = 4,
                    IsSystemWide = false,
                    CanFireInjects = false,
                    CanRecordObservations = true,
                    CanManageExercise = false,
                    IsActive = true
                },
                new HseepRole
                {
                    Id = (int)ExerciseRole.Observer,
                    Code = nameof(ExerciseRole.Observer),
                    Name = "Observer",
                    Description = "Watches exercise conduct without interfering. Read-only access to exercise data.",
                    SortOrder = 5,
                    IsSystemWide = false,
                    CanFireInjects = false,
                    CanRecordObservations = false,
                    CanManageExercise = false,
                    IsActive = true
                }
            );
        });
    }

    private static void ConfigureObservation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Observation>(entity =>
        {
            entity.Property(e => e.Content).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.Recommendation).HasMaxLength(2000);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Rating).HasConversion<string>().HasMaxLength(20);

            entity.HasIndex(e => e.ExerciseId);
            entity.HasIndex(e => e.InjectId);
            entity.HasIndex(e => e.ObjectiveId);
            entity.HasIndex(e => e.ObservedAt);

            entity.HasOne(e => e.Exercise)
                .WithMany(ex => ex.Observations)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Inject)
                .WithMany(i => i.Observations)
                .HasForeignKey(e => e.InjectId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Objective)
                .WithMany()
                .HasForeignKey(e => e.ObjectiveId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            // User who created the observation (optional to handle soft-deleted users)
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
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
