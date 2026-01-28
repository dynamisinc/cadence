using Cadence.Core.Constants;
using Cadence.Core.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Cadence.Core.Data;

/// <summary>
/// Entity Framework Core database context for the application.
/// Extends IdentityDbContext to support ASP.NET Core Identity for authentication.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // =========================================================================
    // DbSets
    // =========================================================================

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<Msel> Msels => Set<Msel>();
    public DbSet<Phase> Phases => Set<Phase>();
    public DbSet<Inject> Injects => Set<Inject>();
    public DbSet<ExerciseParticipant> ExerciseParticipants => Set<ExerciseParticipant>();
    public DbSet<Objective> Objectives => Set<Objective>();
    public DbSet<HseepRole> HseepRoles => Set<HseepRole>();
    public DbSet<Observation> Observations => Set<Observation>();
    public DbSet<InjectObjective> InjectObjectives => Set<InjectObjective>();
    public DbSet<DeliveryMethodLookup> DeliveryMethods => Set<DeliveryMethodLookup>();
    public DbSet<ExpectedOutcome> ExpectedOutcomes => Set<ExpectedOutcome>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ClockEvent> ClockEvents => Set<ClockEvent>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<CoreCapability> CoreCapabilities => Set<CoreCapability>();
    public DbSet<ObservationCapability> ObservationCapabilities => Set<ObservationCapability>();
    public DbSet<ExerciseTargetCapability> ExerciseTargetCapabilities => Set<ExerciseTargetCapability>();

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
        ConfigureApplicationUser(modelBuilder);
        ConfigureRefreshToken(modelBuilder);
        ConfigurePasswordResetToken(modelBuilder);
        ConfigureExternalLogin(modelBuilder);
        ConfigureExercise(modelBuilder);
        ConfigureMsel(modelBuilder);
        ConfigurePhase(modelBuilder);
        ConfigureInject(modelBuilder);
        ConfigureExerciseParticipant(modelBuilder);
        ConfigureObjective(modelBuilder);
        ConfigureHseepRole(modelBuilder);
        ConfigureObservation(modelBuilder);
        ConfigureInjectObjective(modelBuilder);
        ConfigureDeliveryMethodLookup(modelBuilder);
        ConfigureExpectedOutcome(modelBuilder);
        ConfigureNotification(modelBuilder);
        ConfigureClockEvent(modelBuilder);
        ConfigureUserPreferences(modelBuilder);
        ConfigureCoreCapability(modelBuilder);
        ConfigureObservationCapability(modelBuilder);
        ConfigureExerciseTargetCapability(modelBuilder);
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

    private static void ConfigureApplicationUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SystemRole).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

            // Index for common queries
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SystemRole);
            entity.HasIndex(e => e.OrganizationId);

            // Relationship to Organization
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Self-referential relationship for tracking who created the user
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureRefreshToken(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.Property(e => e.TokenHash).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CreatedByIp).HasMaxLength(50);
            entity.Property(e => e.DeviceInfo).HasMaxLength(200);

            // Indexes for efficient token lookup and cleanup
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.UserId, e.IsRevoked });

            // Relationship to ApplicationUser
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurePasswordResetToken(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.Property(e => e.TokenHash).HasMaxLength(256).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(50);

            // Indexes for efficient token lookup and cleanup
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.UserId, e.UsedAt });

            // Relationship to ApplicationUser
            entity.HasOne(e => e.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureExternalLogin(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.Property(e => e.Provider).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProviderUserId).HasMaxLength(200).IsRequired();

            // Unique index to prevent duplicate external logins
            entity.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();
            entity.HasIndex(e => e.UserId);

            // Relationship to ApplicationUser
            entity.HasOne(e => e.User)
                .WithMany(u => u.ExternalLogins)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
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

            // Timing configuration
            entity.Property(e => e.DeliveryMode).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.TimelineMode).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.TimeScale).HasColumnType("decimal(5,2)");

            // Exercise settings (S03-S05)
            entity.Property(e => e.ClockMultiplier).HasColumnType("decimal(4,2)").HasDefaultValue(1.0m);

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

            // Note: ClockStartedBy, ActivatedBy, CompletedBy, ArchivedBy store ApplicationUser IDs
            // as strings, but without FK constraints (for migration simplicity with existing data).
            // Navigation properties are ignored - user lookup should be done manually if needed.
            entity.Ignore(e => e.ClockStartedByUser);
            entity.Ignore(e => e.ActivatedByUser);
            entity.Ignore(e => e.CompletedByUser);
            entity.Ignore(e => e.ArchivedByUser);

            // Archive/delete tracking fields
            entity.Property(e => e.PreviousStatus).HasConversion<string>().HasMaxLength(20);
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
            // Core properties
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.Target).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Source).HasMaxLength(200);

            // Delivery method (legacy enum - will be migrated)
            entity.Property(e => e.DeliveryMethod).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.DeliveryMethodOther).HasMaxLength(100);

            // Enums
            entity.Property(e => e.InjectType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.TriggerType).HasConversion<string>().HasMaxLength(20);

            // Text fields
            entity.Property(e => e.FireCondition).HasMaxLength(500);
            entity.Property(e => e.ExpectedAction).HasMaxLength(2000);
            entity.Property(e => e.ControllerNotes).HasMaxLength(2000);
            entity.Property(e => e.SkipReason).HasMaxLength(500);

            // Import & Excel properties
            entity.Property(e => e.SourceReference).HasMaxLength(50);
            entity.Property(e => e.ResponsibleController).HasMaxLength(200);
            entity.Property(e => e.LocationName).HasMaxLength(200);
            entity.Property(e => e.LocationType).HasMaxLength(100);
            entity.Property(e => e.Track).HasMaxLength(100);

            // Indexes
            entity.HasIndex(e => new { e.MselId, e.InjectNumber }).IsUnique();
            entity.HasIndex(e => new { e.MselId, e.Sequence });
            entity.HasIndex(e => new { e.MselId, e.Status });
            entity.HasIndex(e => e.PhaseId);
            entity.HasIndex(e => e.ParentInjectId);
            entity.HasIndex(e => e.Track);
            entity.HasIndex(e => e.DeliveryMethodId);
            entity.HasIndex(e => e.FiredByUserId);
            entity.HasIndex(e => e.SkippedByUserId);

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

            // User references (ApplicationUser) are optional to handle deactivated users gracefully.
            entity.HasOne(e => e.FiredByUser)
                .WithMany()
                .HasForeignKey(e => e.FiredByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.SkippedByUser)
                .WithMany()
                .HasForeignKey(e => e.SkippedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.DeliveryMethodLookup)
                .WithMany(dm => dm.Injects)
                .HasForeignKey(e => e.DeliveryMethodId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureExerciseParticipant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExerciseParticipant>(entity =>
        {
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.AssignedAt).IsRequired();

            // Unique constraint: one role per user per exercise
            entity.HasIndex(e => new { e.ExerciseId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.Exercise)
                .WithMany(ex => ex.Participants)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            // User navigation references ApplicationUser (ASP.NET Core Identity)
            // Optional to handle deactivated users gracefully
            entity.HasOne(e => e.User)
                .WithMany(u => u.ExerciseParticipations)
                .HasForeignKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Assigned by user (for audit trail)
            entity.HasOne(e => e.AssignedBy)
                .WithMany()
                .HasForeignKey(e => e.AssignedById)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
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
            entity.Property(e => e.CreatedByUserId).HasMaxLength(450); // Match AspNetUsers.Id length

            entity.HasIndex(e => e.ExerciseId);
            entity.HasIndex(e => e.InjectId);
            entity.HasIndex(e => e.ObjectiveId);
            entity.HasIndex(e => e.ObservedAt);
            entity.HasIndex(e => e.CreatedByUserId);

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

            // User who created the observation - references ApplicationUser (ASP.NET Core Identity)
            // Uses string FK to match IdentityUser.Id type
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureInjectObjective(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InjectObjective>(entity =>
        {
            // Composite primary key
            entity.HasKey(e => new { e.InjectId, e.ObjectiveId });

            // Indexes for efficient queries
            entity.HasIndex(e => e.InjectId);
            entity.HasIndex(e => e.ObjectiveId);

            // Relationships
            entity.HasOne(e => e.Inject)
                .WithMany(i => i.InjectObjectives)
                .HasForeignKey(e => e.InjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Objective)
                .WithMany(o => o.InjectObjectives)
                .HasForeignKey(e => e.ObjectiveId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureDeliveryMethodLookup(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeliveryMethodLookup>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            // Unique index on Name since these are system-level reference data
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.SortOrder);

            // Seed system default delivery methods with deterministic GUIDs
            entity.HasData(
                new DeliveryMethodLookup
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    Name = "Verbal",
                    Description = "Spoken directly to player",
                    IsActive = true,
                    SortOrder = 1,
                    IsOther = false,
                    CreatedBy = SystemConstants.SystemUserId,
                    ModifiedBy = SystemConstants.SystemUserId,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new DeliveryMethodLookup
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    Name = "Phone",
                    Description = "Simulated phone call",
                    IsActive = true,
                    SortOrder = 2,
                    IsOther = false,
                    CreatedBy = SystemConstants.SystemUserId,
                    ModifiedBy = SystemConstants.SystemUserId,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new DeliveryMethodLookup
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                    Name = "Email",
                    Description = "Simulated email",
                    IsActive = true,
                    SortOrder = 3,
                    IsOther = false,
                    CreatedBy = SystemConstants.SystemUserId,
                    ModifiedBy = SystemConstants.SystemUserId,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new DeliveryMethodLookup
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                    Name = "Radio",
                    Description = "Radio communication",
                    IsActive = true,
                    SortOrder = 4,
                    IsOther = false,
                    CreatedBy = SystemConstants.SystemUserId,
                    ModifiedBy = SystemConstants.SystemUserId,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new DeliveryMethodLookup
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
                    Name = "Written",
                    Description = "Paper document",
                    IsActive = true,
                    SortOrder = 5,
                    IsOther = false,
                    CreatedBy = SystemConstants.SystemUserId,
                    ModifiedBy = SystemConstants.SystemUserId,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new DeliveryMethodLookup
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000006"),
                    Name = "Simulation",
                    Description = "CAX/simulation input",
                    IsActive = true,
                    SortOrder = 6,
                    IsOther = false,
                    CreatedBy = SystemConstants.SystemUserId,
                    ModifiedBy = SystemConstants.SystemUserId,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new DeliveryMethodLookup
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000007"),
                    Name = "Other",
                    Description = "Custom delivery method (specify in notes)",
                    IsActive = true,
                    SortOrder = 99,
                    IsOther = true,
                    CreatedBy = SystemConstants.SystemUserId,
                    ModifiedBy = SystemConstants.SystemUserId,
                    CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });
    }

    private static void ConfigureExpectedOutcome(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExpectedOutcome>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.EvaluatorNotes).HasMaxLength(2000);

            entity.HasIndex(e => new { e.InjectId, e.SortOrder });

            entity.HasOne(e => e.Inject)
                .WithMany(i => i.ExpectedOutcomes)
                .HasForeignKey(e => e.InjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureNotification(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.ActionUrl).HasMaxLength(500);
            entity.Property(e => e.RelatedEntityType).HasMaxLength(50);
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(20);

            // Indexes for efficient queries
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsRead });
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });

            // Relationship to ApplicationUser
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureClockEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClockEvent>(entity =>
        {
            entity.Property(e => e.EventType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.UserId).HasMaxLength(450); // Match AspNetUsers.Id length

            // Indexes for efficient queries
            entity.HasIndex(e => e.ExerciseId);
            entity.HasIndex(e => new { e.ExerciseId, e.OccurredAt });

            // Relationship to Exercise
            entity.HasOne(e => e.Exercise)
                .WithMany(ex => ex.ClockEvents)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            // User who triggered the event - references ApplicationUser (ASP.NET Core Identity)
            // Uses string FK to match IdentityUser.Id type
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureUserPreferences(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserPreferences>(entity =>
        {
            // Use UserId as primary key (1:1 relationship with ApplicationUser)
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasMaxLength(450); // Match AspNetUsers.Id length

            // Enums stored as strings for readability
            entity.Property(e => e.Theme).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.DisplayDensity).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.TimeFormat).HasConversion<string>().HasMaxLength(20);

            // One-to-one relationship with ApplicationUser
            entity.HasOne(e => e.User)
                .WithOne(u => u.Preferences)
                .HasForeignKey<UserPreferences>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCoreCapability(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CoreCapability>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.MissionArea).HasConversion<string>().HasMaxLength(20);

            // Index for efficient queries
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.MissionArea);
            entity.HasIndex(e => e.IsActive);

            // Seed FEMA Core Capabilities
            // Reference: https://www.fema.gov/emergency-managers/national-preparedness/mission-core-capabilities
            SeedCoreCapabilities(entity);
        });
    }

    private static void SeedCoreCapabilities(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<CoreCapability> entity)
    {
        // All Mission Areas - Common Capabilities
        entity.HasData(
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000001"), Name = "Planning", MissionArea = MissionArea.Response, DisplayOrder = 1, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000002"), Name = "Public Information and Warning", MissionArea = MissionArea.Response, DisplayOrder = 2, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000003"), Name = "Operational Coordination", MissionArea = MissionArea.Response, DisplayOrder = 3, IsActive = true }
        );

        // Prevention
        entity.HasData(
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000101"), Name = "Intelligence and Information Sharing", MissionArea = MissionArea.Prevention, DisplayOrder = 1, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000102"), Name = "Interdiction and Disruption", MissionArea = MissionArea.Prevention, DisplayOrder = 2, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000103"), Name = "Screening, Search, and Detection", MissionArea = MissionArea.Prevention, DisplayOrder = 3, IsActive = true }
        );

        // Protection
        entity.HasData(
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000201"), Name = "Access Control and Identity Verification", MissionArea = MissionArea.Protection, DisplayOrder = 1, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000202"), Name = "Cybersecurity", MissionArea = MissionArea.Protection, DisplayOrder = 2, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000203"), Name = "Physical Protective Measures", MissionArea = MissionArea.Protection, DisplayOrder = 3, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000204"), Name = "Risk Management for Protection Programs and Activities", MissionArea = MissionArea.Protection, DisplayOrder = 4, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000205"), Name = "Supply Chain Integrity and Security", MissionArea = MissionArea.Protection, DisplayOrder = 5, IsActive = true }
        );

        // Mitigation
        entity.HasData(
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000301"), Name = "Community Resilience", MissionArea = MissionArea.Mitigation, DisplayOrder = 1, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000302"), Name = "Long-term Vulnerability Reduction", MissionArea = MissionArea.Mitigation, DisplayOrder = 2, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000303"), Name = "Risk and Disaster Resilience Assessment", MissionArea = MissionArea.Mitigation, DisplayOrder = 3, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000304"), Name = "Threats and Hazard Identification", MissionArea = MissionArea.Mitigation, DisplayOrder = 4, IsActive = true }
        );

        // Response
        entity.HasData(
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000401"), Name = "Critical Transportation", MissionArea = MissionArea.Response, DisplayOrder = 4, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000402"), Name = "Environmental Response/Health and Safety", MissionArea = MissionArea.Response, DisplayOrder = 5, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000403"), Name = "Fatality Management Services", MissionArea = MissionArea.Response, DisplayOrder = 6, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000404"), Name = "Fire Management and Suppression", MissionArea = MissionArea.Response, DisplayOrder = 7, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000405"), Name = "Infrastructure Systems", MissionArea = MissionArea.Response, DisplayOrder = 8, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000406"), Name = "Logistics and Supply Chain Management", MissionArea = MissionArea.Response, DisplayOrder = 9, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000407"), Name = "Mass Care Services", MissionArea = MissionArea.Response, DisplayOrder = 10, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000408"), Name = "Mass Search and Rescue Operations", MissionArea = MissionArea.Response, DisplayOrder = 11, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000409"), Name = "On-scene Security, Protection, and Law Enforcement", MissionArea = MissionArea.Response, DisplayOrder = 12, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000410"), Name = "Operational Communications", MissionArea = MissionArea.Response, DisplayOrder = 13, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000411"), Name = "Public Health, Healthcare, and Emergency Medical Services", MissionArea = MissionArea.Response, DisplayOrder = 14, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000412"), Name = "Situational Assessment", MissionArea = MissionArea.Response, DisplayOrder = 15, IsActive = true }
        );

        // Recovery
        entity.HasData(
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000501"), Name = "Economic Recovery", MissionArea = MissionArea.Recovery, DisplayOrder = 1, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000502"), Name = "Health and Social Services", MissionArea = MissionArea.Recovery, DisplayOrder = 2, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000503"), Name = "Housing", MissionArea = MissionArea.Recovery, DisplayOrder = 3, IsActive = true },
            new CoreCapability { Id = Guid.Parse("00000001-0000-0000-0000-000000000504"), Name = "Natural and Cultural Resources", MissionArea = MissionArea.Recovery, DisplayOrder = 4, IsActive = true }
        );
    }

    private static void ConfigureObservationCapability(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ObservationCapability>(entity =>
        {
            // Composite primary key
            entity.HasKey(e => new { e.ObservationId, e.CoreCapabilityId });

            // Relationships
            entity.HasOne(e => e.Observation)
                .WithMany(o => o.ObservationCapabilities)
                .HasForeignKey(e => e.ObservationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CoreCapability)
                .WithMany(c => c.ObservationCapabilities)
                .HasForeignKey(e => e.CoreCapabilityId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureExerciseTargetCapability(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExerciseTargetCapability>(entity =>
        {
            // Composite primary key
            entity.HasKey(e => new { e.ExerciseId, e.CoreCapabilityId });

            // Relationships
            entity.HasOne(e => e.Exercise)
                .WithMany(ex => ex.TargetCapabilities)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CoreCapability)
                .WithMany(c => c.ExerciseTargetCapabilities)
                .HasForeignKey(e => e.CoreCapabilityId)
                .OnDelete(DeleteBehavior.Cascade);
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
