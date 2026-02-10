using Cadence.Core.Constants;
using Cadence.Core.Features.BulkParticipantImport.Models.Entities;
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

    // Bulk Participant Import entities
    public DbSet<PendingExerciseAssignment> PendingExerciseAssignments => Set<PendingExerciseAssignment>();
    public DbSet<BulkImportRecord> BulkImportRecords => Set<BulkImportRecord>();
    public DbSet<BulkImportRowResult> BulkImportRowResults => Set<BulkImportRowResult>();

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

        // Configure entities
        ConfigureOrganization(modelBuilder);
        ConfigureOrganizationMembership(modelBuilder);
        ConfigureOrganizationInvite(modelBuilder);
        ConfigureAgency(modelBuilder);
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
        ConfigureCapability(modelBuilder);
        ConfigureObservationCapability(modelBuilder);
        ConfigureExerciseTargetCapability(modelBuilder);
        ConfigureInjectStatusHistory(modelBuilder);
        ConfigureApprovalNotification(modelBuilder);

        // EEG (Exercise Evaluation Guide) entities
        ConfigureCapabilityTarget(modelBuilder);
        ConfigureCriticalTask(modelBuilder);
        ConfigureInjectCriticalTask(modelBuilder);
        ConfigureEegEntry(modelBuilder);

        // Email entities
        ConfigureEmailLog(modelBuilder);
        ConfigureUserEmailPreference(modelBuilder);

        // System configuration
        ConfigureSystemSettings(modelBuilder);

        // Bulk Participant Import entities
        ConfigurePendingExerciseAssignment(modelBuilder);
        ConfigureBulkImportRecord(modelBuilder);
        ConfigureBulkImportRowResult(modelBuilder);
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
    // Entity Configurations
    // =========================================================================

    private static void ConfigureOrganization(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.ContactEmail).HasMaxLength(200);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.InjectApprovalPolicy).HasConversion<string>().HasMaxLength(20);

            // Unique index on Slug
            entity.HasIndex(e => e.Slug).IsUnique();

            // Index for status queries
            entity.HasIndex(e => e.Status);

            // Seed default organization
            entity.HasData(new Organization
            {
                Id = SystemConstants.DefaultOrganizationId,
                Name = "Default Organization",
                Slug = "default",
                Description = "Default organization for the Cadence system",
                Status = OrgStatus.Active,
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString,
                CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });
    }

    private static void ConfigureOrganizationMembership(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrganizationMembership>(entity =>
        {
            entity.Property(e => e.UserId).HasMaxLength(450).IsRequired(); // Match AspNetUsers.Id length
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.InvitedById).HasMaxLength(450);

            // Unique constraint: one membership per user per organization
            entity.HasIndex(e => new { e.UserId, e.OrganizationId }).IsUnique();

            // Indexes for common queries
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => new { e.OrganizationId, e.Status });

            // Relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.Memberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Memberships)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InvitedBy)
                .WithMany()
                .HasForeignKey(e => e.InvitedById)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureOrganizationInvite(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrganizationInvite>(entity =>
        {
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Code).HasMaxLength(8).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.UsedById).HasMaxLength(450);
            entity.Property(e => e.CreatedByUserId).HasMaxLength(450).IsRequired();

            // Unique index on Code
            entity.HasIndex(e => e.Code).IsUnique();

            // Indexes for common queries
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => new { e.OrganizationId, e.ExpiresAt });
            entity.HasIndex(e => e.Email);

            // Relationships
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Invites)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.UsedBy)
                .WithMany()
                .HasForeignKey(e => e.UsedById)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureAgency(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agency>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Abbreviation).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);

            // Unique constraint: one agency name per organization
            entity.HasIndex(e => new { e.OrganizationId, e.Name }).IsUnique();

            // Indexes for common queries
            entity.HasIndex(e => new { e.OrganizationId, e.IsActive, e.SortOrder });

            // Relationship
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Agencies)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.HasIndex(e => e.CurrentOrganizationId);

            // Relationship to Organization (nullable for pending users)
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship to CurrentOrganization
            entity.HasOne(e => e.CurrentOrganization)
                .WithMany()
                .HasForeignKey(e => e.CurrentOrganizationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

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

            // Store ClockElapsedBeforePause as bigint (ticks) to support durations > 24 hours
            entity.Property(e => e.ClockElapsedBeforePause)
                .HasConversion(
                    v => v.HasValue ? v.Value.Ticks : (long?)null,
                    v => v.HasValue ? TimeSpan.FromTicks(v.Value) : null);

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

            // Approval workflow override fields
            entity.Property(e => e.ApprovalOverrideReason).HasMaxLength(500);
            entity.Property(e => e.ApprovalOverriddenById).HasMaxLength(450); // Match AspNetUsers.Id length

            // Navigation property for approval override user
            entity.HasOne(e => e.ApprovalOverriddenByUser)
                .WithMany()
                .HasForeignKey(e => e.ApprovalOverriddenById)
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

            // Approval workflow properties
            entity.Property(e => e.ApproverNotes).HasMaxLength(1000);
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);
            entity.Property(e => e.RevertReason).HasMaxLength(1000);

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
            entity.HasIndex(e => e.SubmittedByUserId);
            entity.HasIndex(e => e.ApprovedByUserId);
            entity.HasIndex(e => e.RejectedByUserId);
            entity.HasIndex(e => e.RevertedByUserId);

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

            // Approval workflow user references (ApplicationUser) are optional to handle deactivated users gracefully.
            entity.HasOne(e => e.SubmittedByUser)
                .WithMany()
                .HasForeignKey(e => e.SubmittedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ApprovedByUser)
                .WithMany()
                .HasForeignKey(e => e.ApprovedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.RejectedByUser)
                .WithMany()
                .HasForeignKey(e => e.RejectedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.RevertedByUser)
                .WithMany()
                .HasForeignKey(e => e.RevertedByUserId)
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
                    CreatedBy = SystemConstants.SystemUserIdString,
                    ModifiedBy = SystemConstants.SystemUserIdString,
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
                    CreatedBy = SystemConstants.SystemUserIdString,
                    ModifiedBy = SystemConstants.SystemUserIdString,
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
                    CreatedBy = SystemConstants.SystemUserIdString,
                    ModifiedBy = SystemConstants.SystemUserIdString,
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
                    CreatedBy = SystemConstants.SystemUserIdString,
                    ModifiedBy = SystemConstants.SystemUserIdString,
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
                    CreatedBy = SystemConstants.SystemUserIdString,
                    ModifiedBy = SystemConstants.SystemUserIdString,
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
                    CreatedBy = SystemConstants.SystemUserIdString,
                    ModifiedBy = SystemConstants.SystemUserIdString,
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
                    CreatedBy = SystemConstants.SystemUserIdString,
                    ModifiedBy = SystemConstants.SystemUserIdString,
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

            // Store ElapsedTimeAtEvent as bigint (ticks) to support durations > 24 hours
            entity.Property(e => e.ElapsedTimeAtEvent)
                .HasConversion(
                    v => v.Ticks,
                    v => TimeSpan.FromTicks(v));

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

    private static void ConfigureCapability(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Capability>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.SourceLibrary).HasMaxLength(50);

            // Unique index on (OrganizationId, Name)
            entity.HasIndex(e => new { e.OrganizationId, e.Name }).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsActive);

            // Covering index for common query: WHERE OrganizationId = @id AND IsActive = true ORDER BY Category, SortOrder, Name
            entity.HasIndex(e => new { e.OrganizationId, e.IsActive, e.Category, e.SortOrder, e.Name });

            // Relationship to Organization
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureObservationCapability(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ObservationCapability>(entity =>
        {
            // Composite primary key
            entity.HasKey(e => new { e.ObservationId, e.CapabilityId });

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

    private static void ConfigureExerciseTargetCapability(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExerciseTargetCapability>(entity =>
        {
            // Composite primary key
            entity.HasKey(e => new { e.ExerciseId, e.CapabilityId });

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

    private static void ConfigureInjectStatusHistory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InjectStatusHistory>(entity =>
        {
            // Enums stored as strings for readability
            entity.Property(e => e.FromStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.ToStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.ChangedByUserId).HasMaxLength(450).IsRequired(); // Match AspNetUsers.Id length

            // Indexes for efficient queries
            entity.HasIndex(e => e.InjectId);
            entity.HasIndex(e => new { e.InjectId, e.ChangedAt });
            entity.HasIndex(e => e.ChangedByUserId);

            // Relationship to Inject
            entity.HasOne(e => e.Inject)
                .WithMany(i => i.StatusHistory)
                .HasForeignKey(e => e.InjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // User who made the change - references ApplicationUser (ASP.NET Core Identity)
            // Uses string FK to match IdentityUser.Id type
            entity.HasOne(e => e.ChangedByUser)
                .WithMany()
                .HasForeignKey(e => e.ChangedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureApprovalNotification(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApprovalNotification>(entity =>
        {
            // String properties
            entity.Property(e => e.UserId).HasMaxLength(450).IsRequired(); // Match AspNetUsers.Id length
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Metadata).HasColumnType("nvarchar(max)");
            entity.Property(e => e.TriggeredByUserId).HasMaxLength(450);

            // Enum stored as string for readability
            entity.Property(e => e.Type).HasConversion<string>().HasMaxLength(30);

            // Indexes for efficient queries
            entity.HasIndex(e => new { e.UserId, e.IsRead }); // Unread count query
            entity.HasIndex(e => new { e.UserId, e.CreatedAt }); // Notification list
            entity.HasIndex(e => e.ExerciseId);
            entity.HasIndex(e => e.OrganizationId); // Organization filter

            // Relationships
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Exercise)
                .WithMany()
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Inject)
                .WithMany()
                .HasForeignKey(e => e.InjectId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction); // NoAction to avoid cascade cycle with Exercise

            entity.HasOne(e => e.TriggeredByUser)
                .WithMany()
                .HasForeignKey(e => e.TriggeredByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    // =========================================================================
    // EEG (Exercise Evaluation Guide) Entity Configurations
    // =========================================================================

    private void ConfigureCapabilityTarget(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CapabilityTarget>(entity =>
        {
            entity.Property(e => e.TargetDescription).HasMaxLength(500).IsRequired();

            // Indexes for efficient queries
            entity.HasIndex(e => e.ExerciseId);
            entity.HasIndex(e => e.CapabilityId);
            entity.HasIndex(e => new { e.ExerciseId, e.SortOrder });

            // Relationship to Exercise (cascade delete when exercise is deleted)
            entity.HasOne(e => e.Exercise)
                .WithMany(ex => ex.CapabilityTargets)
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship to Capability (no cascade - capability can be deactivated without deleting targets)
            entity.HasOne(e => e.Capability)
                .WithMany(c => c.CapabilityTargets)
                .HasForeignKey(e => e.CapabilityId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship to Organization (for data isolation)
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCriticalTask(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CriticalTask>(entity =>
        {
            entity.Property(e => e.TaskDescription).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Standard).HasMaxLength(1000);

            // Indexes for efficient queries
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.CapabilityTargetId);
            entity.HasIndex(e => new { e.CapabilityTargetId, e.SortOrder });

            // Relationship to Organization (required for multi-tenancy data isolation)
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship to CapabilityTarget (cascade delete when target is deleted)
            entity.HasOne(e => e.CapabilityTarget)
                .WithMany(ct => ct.CriticalTasks)
                .HasForeignKey(e => e.CapabilityTargetId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureInjectCriticalTask(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InjectCriticalTask>(entity =>
        {
            // Composite primary key
            entity.HasKey(e => new { e.InjectId, e.CriticalTaskId });

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

    private void ConfigureEegEntry(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EegEntry>(entity =>
        {
            entity.Property(e => e.ObservationText).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.Rating).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.EvaluatorId).HasMaxLength(450).IsRequired();

            // Indexes for efficient queries
            entity.HasIndex(e => e.CriticalTaskId);
            entity.HasIndex(e => e.EvaluatorId);
            entity.HasIndex(e => e.TriggeringInjectId);
            entity.HasIndex(e => e.ObservedAt);
            entity.HasIndex(e => new { e.CriticalTaskId, e.ObservedAt });

            // Relationship to CriticalTask (cascade delete when task is deleted)
            entity.HasOne(e => e.CriticalTask)
                .WithMany(ct => ct.EegEntries)
                .HasForeignKey(e => e.CriticalTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship to ApplicationUser (evaluator)
            entity.HasOne(e => e.Evaluator)
                .WithMany()
                .HasForeignKey(e => e.EvaluatorId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationship to Inject (triggering inject, optional)
            entity.HasOne(e => e.TriggeringInject)
                .WithMany(i => i.TriggeredEegEntries)
                .HasForeignKey(e => e.TriggeringInjectId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationship to Organization (for data isolation)
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // =========================================================================
    // Email Entity Configurations
    // =========================================================================

    private static void ConfigureEmailLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.Property(e => e.RecipientEmail).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Subject).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TemplateId).HasMaxLength(100);
            entity.Property(e => e.AcsMessageId).HasMaxLength(200);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.StatusDetail).HasMaxLength(1000);
            entity.Property(e => e.RelatedEntityType).HasMaxLength(50);
            entity.Property(e => e.UserId).HasMaxLength(450);

            // Indexes for efficient queries
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.AcsMessageId);
            entity.HasIndex(e => new { e.OrganizationId, e.SentAt });
            entity.HasIndex(e => new { e.OrganizationId, e.Status });

            // Relationships
            entity.HasOne(e => e.Organization)
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureUserEmailPreference(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEmailPreference>(entity =>
        {
            entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
            entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(20);

            // Unique constraint: one preference per user per category
            entity.HasIndex(e => new { e.UserId, e.Category }).IsUnique();

            // Index for user lookups
            entity.HasIndex(e => e.UserId);

            // Relationship to ApplicationUser
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureSystemSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemSettings>(entity =>
        {
            entity.ToTable("SystemSettings");
            entity.Property(e => e.SupportAddress).HasMaxLength(200);
            entity.Property(e => e.DefaultSenderAddress).HasMaxLength(200);
            entity.Property(e => e.DefaultSenderName).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(450);
        });
    }

    // =========================================================================
    // Bulk Participant Import Entity Configurations
    // =========================================================================

    private static void ConfigurePendingExerciseAssignment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PendingExerciseAssignment>(entity =>
        {
            entity.Property(e => e.ExerciseRole).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

            // Indexes for efficient queries
            entity.HasIndex(e => e.OrganizationInviteId);
            entity.HasIndex(e => e.ExerciseId);
            entity.HasIndex(e => new { e.ExerciseId, e.Status });

            // Relationship to OrganizationInvite
            entity.HasOne(e => e.OrganizationInvite)
                .WithMany(i => i.PendingExerciseAssignments)
                .HasForeignKey(e => e.OrganizationInviteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship to Exercise
            entity.HasOne(e => e.Exercise)
                .WithMany()
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationship to BulkImportRecord (optional)
            entity.HasOne(e => e.BulkImportRecord)
                .WithMany(r => r.PendingAssignments)
                .HasForeignKey(e => e.BulkImportRecordId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureBulkImportRecord(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BulkImportRecord>(entity =>
        {
            entity.Property(e => e.ImportedById).HasMaxLength(450).IsRequired();
            entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();

            // Indexes for efficient queries
            entity.HasIndex(e => e.ExerciseId);
            entity.HasIndex(e => new { e.ExerciseId, e.ImportedAt });

            // Relationship to Exercise
            entity.HasOne(e => e.Exercise)
                .WithMany()
                .HasForeignKey(e => e.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship to ImportedBy user
            entity.HasOne(e => e.ImportedBy)
                .WithMany()
                .HasForeignKey(e => e.ImportedById)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureBulkImportRowResult(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BulkImportRowResult>(entity =>
        {
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ExerciseRole).HasMaxLength(50);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.Classification).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            entity.Property(e => e.PreviousExerciseRole).HasMaxLength(50);

            // Indexes for efficient queries
            entity.HasIndex(e => e.BulkImportRecordId);
            entity.HasIndex(e => new { e.BulkImportRecordId, e.Classification });

            // Relationship to BulkImportRecord
            entity.HasOne(e => e.BulkImportRecord)
                .WithMany(r => r.RowResults)
                .HasForeignKey(e => e.BulkImportRecordId)
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
