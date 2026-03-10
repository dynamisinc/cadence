using Cadence.Core.Constants;
using Cadence.Core.Features.BulkParticipantImport.Models.Entities;
using Cadence.Core.Features.Feedback.Models.Entities;
using Cadence.Core.Features.Email.Models;
using Cadence.Core.Features.SystemSettings.Models.Entities;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Cadence.Core.Data;

/// <summary>
/// Entity Framework Core database context for the application.
/// Extends IdentityDbContext to support ASP.NET Core Identity for authentication.
///
/// Implements automatic organization-scoped query filters for data isolation:
/// - Entities implementing IOrganizationScoped are automatically filtered by the current organization
/// - SysAdmins bypass organization filters and can see all data
/// - Use IgnoreQueryFilters() for explicit cross-organization access
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentOrganizationContext? _orgContext;

    /// <summary>
    /// Creates a new DbContext with organization context for automatic data filtering.
    /// Use this constructor for normal application operations.
    /// </summary>
    /// <param name="options">Database context options</param>
    /// <param name="orgContext">Current organization context for filtering</param>
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentOrganizationContext orgContext)
        : base(options)
    {
        _orgContext = orgContext;
    }

    /// <summary>
    /// Creates a new DbContext without organization context.
    /// Use this constructor for migrations, design-time operations, and testing.
    /// WARNING: No organization filtering will be applied!
    /// </summary>
    /// <param name="options">Database context options</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // =========================================================================
    // Organization Context Properties (for parameterized query filters)
    // =========================================================================

    /// <summary>
    /// When true, the OrganizationValidationInterceptor skips write-side org validation.
    /// Used for legitimate cross-org operations like accepting an invitation.
    /// Callers MUST reset this to false after SaveChangesAsync.
    /// </summary>
    public bool BypassOrgValidation { get; set; }

    /// <summary>
    /// Gets whether the current user is a SysAdmin (bypasses org filters).
    /// Used by parameterized query filters at query execution time.
    /// </summary>
    private bool IsSysAdmin => _orgContext?.IsSysAdmin ?? false;

    /// <summary>
    /// Gets the current organization ID for filtering.
    /// Used by parameterized query filters at query execution time.
    /// </summary>
    private Guid? CurrentOrganizationId => _orgContext?.CurrentOrganizationId;

    /// <summary>
    /// Determines if org filters should be bypassed entirely.
    /// Returns true when: no org context service (tests/design-time/migrations), no HTTP context (seeding), or SysAdmin.
    /// When false, filters by OrgIdForFilter - users without org context see nothing.
    /// </summary>
    private bool BypassOrgFilter =>
        _orgContext == null ||     // No service (tests, design-time, migrations)
        !_orgContext.HasContext || // No HTTP context (seeding, background jobs)
        _orgContext.IsSysAdmin;    // SysAdmin sees all

    /// <summary>
    /// Gets the organization ID to filter by, or Guid.Empty if user has no org.
    /// When OrgIdForFilter is Guid.Empty and BypassOrgFilter is false,
    /// the filter matches nothing (pending users see no data), which is secure.
    /// </summary>
    private Guid OrgIdForFilter => _orgContext?.CurrentOrganizationId ?? Guid.Empty;

    // =========================================================================
    // DbSets
    // =========================================================================

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();
    public DbSet<OrganizationInvite> OrganizationInvites => Set<OrganizationInvite>();
    public DbSet<Agency> Agencies => Set<Agency>();
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
    public DbSet<Capability> Capabilities => Set<Capability>();
    public DbSet<ObservationCapability> ObservationCapabilities => Set<ObservationCapability>();
    public DbSet<ExerciseTargetCapability> ExerciseTargetCapabilities => Set<ExerciseTargetCapability>();
    public DbSet<InjectStatusHistory> InjectStatusHistories => Set<InjectStatusHistory>();
    public DbSet<ApprovalNotification> ApprovalNotifications => Set<ApprovalNotification>();

    // EEG (Exercise Evaluation Guide) entities
    public DbSet<CapabilityTarget> CapabilityTargets => Set<CapabilityTarget>();
    public DbSet<CriticalTask> CriticalTasks => Set<CriticalTask>();
    public DbSet<InjectCriticalTask> InjectCriticalTasks => Set<InjectCriticalTask>();
    public DbSet<EegEntry> EegEntries => Set<EegEntry>();

    // Email entities
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<UserEmailPreference> UserEmailPreferences => Set<UserEmailPreference>();

    // System configuration
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();
    public DbSet<EulaAcceptance> EulaAcceptances => Set<EulaAcceptance>();

    // Photo entities
    public DbSet<ExercisePhoto> ExercisePhotos => Set<ExercisePhoto>();

    // Bulk Participant Import entities
    public DbSet<PendingExerciseAssignment> PendingExerciseAssignments => Set<PendingExerciseAssignment>();
    public DbSet<BulkImportRecord> BulkImportRecords => Set<BulkImportRecord>();
    public DbSet<BulkImportRowResult> BulkImportRowResults => Set<BulkImportRowResult>();

    // Autocomplete suggestion management
    public DbSet<OrganizationSuggestion> OrganizationSuggestions => Set<OrganizationSuggestion>();

    // Feedback
    public DbSet<FeedbackReport> FeedbackReports => Set<FeedbackReport>();

    // =========================================================================
    // Model Configuration
    // =========================================================================

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure global settings for all entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Configure global datetime2 for all DateTime properties
            // Configure audit columns (CreatedBy, ModifiedBy, DeletedBy) to nvarchar(450)
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("datetime2");
                }

                // Set audit columns to nvarchar(450) to match ASP.NET Identity and enable indexing
                if (property.Name is "CreatedBy" or "ModifiedBy" or "DeletedBy" &&
                    property.ClrType == typeof(string))
                {
                    property.SetMaxLength(450);
                }
            }

            // Determine which interfaces this entity implements
            var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType);
            var isOrgScoped = typeof(IOrganizationScoped).IsAssignableFrom(entityType.ClrType);

            // Apply combined query filter based on implemented interfaces
            if (isSoftDeletable && isOrgScoped)
            {
                // Entity has both soft delete AND organization scoping
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ConfigureCombinedFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .MakeGenericMethod(entityType.ClrType);
                method?.Invoke(this, new object[] { modelBuilder });
            }
            else if (isSoftDeletable)
            {
                // Entity only has soft delete (no org scoping)
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ConfigureSoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?
                    .MakeGenericMethod(entityType.ClrType);
                method?.Invoke(null, new object[] { modelBuilder });
            }
            else if (isOrgScoped)
            {
                // Entity only has organization scoping (no soft delete)
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ConfigureOrganizationFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .MakeGenericMethod(entityType.ClrType);
                method?.Invoke(this, new object[] { modelBuilder });
            }
        }

        // Apply all IEntityTypeConfiguration<T> classes from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Instance-dependent configurations that require access to DbContext filter properties
        // (BypassOrgFilter / OrgIdForFilter) for custom query filters on join entities.
        // These cannot use IEntityTypeConfiguration because they reference instance state.
        ConfigureClockEvent(modelBuilder);
        ConfigureObservationCapability(modelBuilder);
        ConfigureExerciseTargetCapability(modelBuilder);
        ConfigureInjectCriticalTask(modelBuilder);
    }

    /// <summary>
    /// Configures a global query filter to exclude soft-deleted entities.
    /// Used for entities that implement ISoftDeletable but NOT IOrganizationScoped.
    /// </summary>
    private static void ConfigureSoftDeleteFilter<T>(ModelBuilder modelBuilder) where T : class, ISoftDeletable
    {
        modelBuilder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    /// <summary>
    /// Configures a global query filter for organization scoping only.
    /// Used for entities that implement IOrganizationScoped but NOT ISoftDeletable.
    ///
    /// Filter logic:
    /// - If no org context service (tests/design-time): no filtering (see all)
    /// - SysAdmins: see all organizations
    /// - Users with org context: see only their organization's data
    /// - Users without org context (pending): see nothing (OrgIdForFilter = Guid.Empty)
    /// </summary>
    private void ConfigureOrganizationFilter<T>(ModelBuilder modelBuilder)
        where T : class, IOrganizationScoped
    {
        // BypassOrgFilter is true for tests/design-time or SysAdmin
        // When bypassed: show all data
        // When not bypassed: filter by org (pending users have OrgIdForFilter = Guid.Empty, so see nothing)
        modelBuilder.Entity<T>().HasQueryFilter(e =>
            BypassOrgFilter || e.OrganizationId == OrgIdForFilter
        );
    }

    /// <summary>
    /// Configures a combined query filter for both soft delete AND organization scoping.
    /// Used for entities that implement both ISoftDeletable AND IOrganizationScoped.
    ///
    /// Combined filter logic:
    /// - Entity must NOT be soft-deleted
    /// - AND organization filter applies (see ConfigureOrganizationFilter)
    /// </summary>
    private void ConfigureCombinedFilter<T>(ModelBuilder modelBuilder)
        where T : class, ISoftDeletable, IOrganizationScoped
    {
        // Combine soft delete and org filters
        // BypassOrgFilter is true for tests/design-time or SysAdmin
        modelBuilder.Entity<T>().HasQueryFilter(e =>
            !e.IsDeleted &&
            (BypassOrgFilter || e.OrganizationId == OrgIdForFilter)
        );
    }

    // =========================================================================
    // Instance-Dependent Entity Configurations
    // (Require access to BypassOrgFilter / OrgIdForFilter for custom query filters)
    // All other entity configurations are in Data/Configurations/ as IEntityTypeConfiguration<T>.
    // =========================================================================

    private void ConfigureClockEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClockEvent>(entity =>
        {
            entity.Property(e => e.EventType).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.UserId).HasMaxLength(450);

            // Store ElapsedTimeAtEvent as bigint (ticks) to support durations > 24 hours
            entity.Property(e => e.ElapsedTimeAtEvent)
                .HasConversion(
                    v => v.Ticks,
                    v => TimeSpan.FromTicks(v));

            // Matching query filter for required Exercise navigation (Exercise has soft-delete + org filters)
            entity.HasQueryFilter(e =>
                !e.Exercise.IsDeleted &&
                (BypassOrgFilter || e.Exercise.OrganizationId == OrgIdForFilter));

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

    private void ConfigureObservationCapability(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ObservationCapability>(entity =>
        {
            // Composite primary key
            entity.HasKey(e => new { e.ObservationId, e.CapabilityId });

            // Matching query filter for required Capability navigation (Capability has org filter)
            entity.HasQueryFilter(e =>
                BypassOrgFilter || e.Capability.OrganizationId == OrgIdForFilter);

            // Index on CapabilityId for reverse lookups (finding observations by capability)
            entity.HasIndex(e => e.CapabilityId);

            // Relationships
            entity.HasOne(e => e.Observation)
                .WithMany(o => o.ObservationCapabilities)
                .HasForeignKey(e => e.ObservationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Capability)
                .WithMany(c => c.ObservationCapabilities)
                .HasForeignKey(e => e.CapabilityId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureExerciseTargetCapability(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExerciseTargetCapability>(entity =>
        {
            // Composite primary key
            entity.HasKey(e => new { e.ExerciseId, e.CapabilityId });

            // Matching query filter for required Capability navigation (Capability has org filter)
            entity.HasQueryFilter(e =>
                BypassOrgFilter || e.Capability.OrganizationId == OrgIdForFilter);

            // Index on CapabilityId for reverse lookups (finding exercises by capability)
            entity.HasIndex(e => e.CapabilityId);

            // Relationships
            entity.HasOne(e => e.Exercise)
                .WithMany(ex => ex.TargetCapabilities)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Capability)
                .WithMany(c => c.ExerciseTargetCapabilities)
                .HasForeignKey(e => e.CapabilityId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureInjectCriticalTask(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InjectCriticalTask>(entity =>
        {
            // Composite primary key
            entity.HasKey(e => new { e.InjectId, e.CriticalTaskId });

            // Matching query filter for required CriticalTask navigation (CriticalTask has soft-delete + org filters)
            entity.HasQueryFilter(e =>
                !e.CriticalTask.IsDeleted &&
                (BypassOrgFilter || e.CriticalTask.OrganizationId == OrgIdForFilter));

            // Audit fields for HSEEP compliance
            entity.Property(e => e.CreatedBy).HasMaxLength(450).IsRequired();

            // Indexes for efficient queries
            entity.HasIndex(e => e.InjectId);
            entity.HasIndex(e => e.CriticalTaskId);

            // Relationship to Inject (cascade - when inject is deleted, remove links)
            entity.HasOne(e => e.Inject)
                .WithMany(i => i.LinkedCriticalTasks)
                .HasForeignKey(e => e.InjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship to CriticalTask (restrict to avoid cascade cycle in SQL Server)
            entity.HasOne(e => e.CriticalTask)
                .WithMany(ct => ct.LinkedInjects)
                .HasForeignKey(e => e.CriticalTaskId)
                .OnDelete(DeleteBehavior.Restrict);
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
