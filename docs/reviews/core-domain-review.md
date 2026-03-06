# Core Domain & Data Code Review

> **Review Type:** Core Domain & Data
> **Pass:** 1
> **Date:** 2026-03-06
> **Reviewer:** Core Domain & Data Specialist — EF Core 10, domain modeling, service layer patterns, data access
> **Scope:** src/Cadence.Core/ — 45+ entities, 26 feature modules, DbContext, interceptors, seeders, extensions

---

## Reviewer Summary

- **Files reviewed:** 160+
- **Issues found:** 19 total — Critical: 4, Major: 8, Minor: 7
- **Positive patterns identified:** 4
- **Estimated remediation effort:** High
- **Top 3 priorities for this domain:**
  1. Static in-memory session state in import services breaks multi-instance Azure App Service deployments
  2. N+1 `SaveChangesAsync` calls in bulk import loop creates 100+ round-trips for a 100-row import
  3. Double SignalR broadcast for all approval actions — every connected client receives duplicate events

---

## Critical Issues

### CD-C01: Static in-memory session state breaks multi-instance deployments

- **File(s):** `src/Cadence.Core/Features/ExcelImport/Services/ExcelImportService.cs` and `src/Cadence.Core/Features/BulkParticipantImport/Services/BulkParticipantImportService.cs`
- **Category:** Stability
- **Impact:** Both services use `private static readonly ConcurrentDictionary<Guid, ImportSession>` to store import session state. The 3-step import flow (upload → preview → confirm) requires that the confirm request hits the same instance that handled the upload. On Azure App Service with multiple instances (or any horizontal scaling), sessions created on Instance A do not exist on Instance B. The confirm step will fail with a "session not found" error if it routes to a different instance.
- **Description:** The `ConcurrentDictionary` is in-process memory. It does not survive app restarts, is not shared across instances, and has no expiration. Long-running sessions will leak memory. This is a fundamental scalability issue that will surface as soon as the App Service is scaled to 2+ instances.
- **Recommendation:** Replace static `ConcurrentDictionary` with a distributed cache (IDistributedCache backed by Redis or SQL Server). Alternatively, serialize the import session to the database with a session ID and TTL. The session should include: uploaded data, validation results, and mapping configuration. Add an expiration/cleanup mechanism.
- **Affected files:**
  - `src/Cadence.Core/Features/ExcelImport/Services/ExcelImportService.cs`
  - `src/Cadence.Core/Features/BulkParticipantImport/Services/BulkParticipantImportService.cs`

---

### CD-C02: N+1 `SaveChangesAsync` in bulk import per-row loop

- **File(s):** `src/Cadence.Core/Features/BulkParticipantImport/Services/BulkParticipantImportService.cs` (ConfirmImportAsync method)
- **Category:** Stability
- **Impact:** `ConfirmImportAsync` calls `SaveChangesAsync()` inside the per-row loop — 3 separate saves per row (user creation/lookup, membership, exercise assignment). A 100-row import makes 300+ sequential database round-trips. This creates severe latency (potentially 30+ seconds for a large import), holds a long-running transaction, and risks timeout exceptions. Azure SQL has a 30-second default command timeout.
- **Description:** The loop pattern is: for each row → create user if not exists → `SaveChangesAsync` → create membership → `SaveChangesAsync` → create exercise assignment → `SaveChangesAsync`. This should be batched.
- **Recommendation:** Refactor to batch all entity creations: (1) Iterate all rows and build entity lists. (2) Call `AddRange()` for all users, memberships, and assignments. (3) Call `SaveChangesAsync()` once at the end. If individual row validation is needed, collect errors and report them, but still batch the saves. Consider using `context.Database.BeginTransactionAsync()` for all-or-nothing semantics.
- **Affected files:** `src/Cadence.Core/Features/BulkParticipantImport/Services/BulkParticipantImportService.cs`

---

### CD-C03: Double SignalR broadcast for all approval actions

- **File(s):** `src/Cadence.Core/Features/Injects/Services/InjectService.cs`
- **Category:** Stability
- **Impact:** Every inject approval action (submit, approve, reject, revert) broadcasts the same SignalR event twice: once directly via `_hubContext.NotifyInjectSubmitted/Approved/Rejected/Reverted` and once via `ApprovalNotificationService` which calls the same hub method. Every connected client receives 2 identical events, causing double cache invalidation in React Query, double toast notifications (if not deduplicated), and double UI updates.
- **Description:** The `InjectService` calls `_hubContext.NotifyInjectXxx()` directly and then calls `_approvalNotificationService.NotifyXxx()`, which internally calls the same `_hubContext.NotifyInjectXxx()` method again. This is a copy-paste issue where the notification service was added without removing the original direct broadcast.
- **Recommendation:** Remove the direct `_hubContext.NotifyInjectXxx()` calls from `InjectService` and let `ApprovalNotificationService` be the single point of broadcast. Alternatively, remove the `ApprovalNotificationService` calls and keep the direct broadcasts — but pick one pattern, not both.
- **Affected files:** `src/Cadence.Core/Features/Injects/Services/InjectService.cs`

---

### CD-C04: `ExerciseDeleteService` missing EEG and photo cascades

- **File(s):** `src/Cadence.Core/Features/Exercises/Services/ExerciseDeleteService.cs`
- **Category:** Stability
- **Impact:** When an exercise is soft-deleted, the `ExerciseDeleteService` cascades the delete to many related entities (injects, phases, observations, etc.) but misses several entity types: `CapabilityTarget`, `CriticalTask`, `EegEntry`, `InjectCriticalTask`, and `ExercisePhoto`. These entities become permanently orphaned — they reference a deleted exercise but their `IsDeleted` flag remains `false`. EEG data and blob-backed photos consume storage indefinitely. Queries that don't filter by exercise (e.g., admin reports, data cleanup) will return stale data from deleted exercises.
- **Description:** The delete cascade pattern correctly handles the majority of entity types but the newer entities (EEG, photos, critical tasks) were added after the delete service was written and were not included in the cascade.
- **Recommendation:** Add soft-delete cascades for all missing entity types. Query pattern: for each missing type, add a `ExecuteUpdateAsync` or `foreach` + `entity.IsDeleted = true` block following the existing pattern in the service. Also add a unit test that verifies all `IOrganizationScoped` entity types related to `Exercise` are included in the delete cascade — this prevents future regressions when new entity types are added.
- **Affected files:**
  - `src/Cadence.Core/Features/Exercises/Services/ExerciseDeleteService.cs`

---

## Major Issues

### CD-M01: Metrics sub-services bypass DI — registered nowhere

- **File(s):** `src/Cadence.Core/Features/Metrics/Services/ExerciseMetricsService.cs`
- **Category:** Convention
- **Impact:** `IProgressMetricsService`, `ITimelineMetricsService`, `IPhaseMetricsService`, and `IRoleMetricsService` are interfaces with implementations in the Metrics feature, but none are registered in `ServiceCollectionExtensions`. The `ExerciseMetricsService` constructor has a fallback path that `new`s the concrete implementations directly, bypassing dependency injection entirely. This means these services cannot be mocked in tests, cannot have dependencies injected, and won't benefit from the DI container's lifetime management.
- **Description:** The DI bypass via `new ConcreteService()` in the constructor is a workaround for missing registrations. It makes the metrics feature untestable in isolation and violates the DI pattern used everywhere else.
- **Recommendation:** Register all four metrics sub-services in `ServiceCollectionExtensions.AddFeatureServices()` as scoped services. Remove the fallback `new` constructor path from `ExerciseMetricsService`. Add the sub-services as constructor parameters with proper DI injection.
- **Affected files:**
  - `src/Cadence.Core/Features/Metrics/Services/ExerciseMetricsService.cs`
  - `src/Cadence.Core/Extensions/ServiceCollectionExtensions.cs`

---

### CD-M02: Missing `.AsNoTracking()` on read-only queries

- **File(s):** Multiple service files across features
- **Category:** Stability
- **Impact:** Read-only queries (GET endpoints that return DTOs) track entities in the EF Core change tracker unnecessarily. This increases memory usage and slows down queries, especially for list endpoints that return many entities. The overhead is proportional to entity count × tracked properties.
- **Description:** Many service methods that query data for read-only display (list pages, detail views, metrics) do not use `.AsNoTracking()`. The pattern is inconsistent — some services use it correctly, others don't.
- **Recommendation:** Add `.AsNoTracking()` to all read-only query methods (any method that returns DTOs and does not modify entities). Audit all service `Get*` and `List*` methods. As a convention, read-only methods should always include `.AsNoTracking()` in the query chain.
- **Affected files:** Multiple services across `src/Cadence.Core/Features/*/Services/`

---

### CD-M03: Observation DTO projection copy-pasted 5 times

- **File(s):** `src/Cadence.Core/Features/Observations/Services/ObservationService.cs`
- **Category:** DRY
- **Impact:** The same 30-line DTO projection (mapping from `Observation` entity to `ObservationDto`) is copy-pasted in 5 different query methods. If a field is added to `ObservationDto`, all 5 projections must be updated manually — and divergence is likely.
- **Description:** Each query method contains an identical `Select(o => new ObservationDto { ... })` block with 15+ field mappings. This is the same mapping that should be centralized in the `ObservationMapper`.
- **Recommendation:** Extract the projection into a reusable `Expression<Func<Observation, ObservationDto>>` in `ObservationMapper` (or as an `IQueryable<Observation>` extension method that returns `IQueryable<ObservationDto>`). All 5 query methods should reference this single projection. Pattern: `query.Select(ObservationMapper.ToDto)`.
- **Affected files:** `src/Cadence.Core/Features/Observations/Services/ObservationService.cs`

---

### CD-M04: Fetch-then-update/delete instead of `ExecuteUpdateAsync`/`ExecuteDeleteAsync`

- **File(s):** `src/Cadence.Core/Features/Notifications/Services/NotificationService.cs`
- **Category:** Stability
- **Impact:** `MarkAllAsReadAsync` fetches all unread notifications into memory, sets `IsRead = true` on each, and calls `SaveChangesAsync`. `DeleteOldNotificationsAsync` similarly fetches all old notifications then calls `RemoveRange`. For users with many notifications, this loads potentially thousands of entities into memory unnecessarily. EF Core 10 provides `ExecuteUpdateAsync` and `ExecuteDeleteAsync` for efficient server-side bulk operations.
- **Description:** The fetch-then-update pattern was appropriate in older EF Core versions but is unnecessary in EF Core 10. The bulk methods execute a single SQL statement without loading entities.
- **Recommendation:** Replace with:
  ```csharp
  // MarkAllAsReadAsync
  await _context.Notifications
      .Where(n => n.UserId == userId && !n.IsRead)
      .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

  // DeleteOldNotificationsAsync
  await _context.Notifications
      .Where(n => n.CreatedAt < cutoffDate)
      .ExecuteDeleteAsync();
  ```
- **Affected files:** `src/Cadence.Core/Features/Notifications/Services/NotificationService.cs`

---

### CD-M05: "Activate pending user" logic duplicated and missing in invitation flow

- **File(s):** `src/Cadence.Core/Features/Organizations/Services/OrganizationService.cs` and `src/Cadence.Core/Features/Organizations/Services/OrganizationInvitationService.cs`
- **Category:** DRY
- **Impact:** The logic to activate a pending user when they are added to their first organization is duplicated in two methods of `OrganizationService` but missing entirely in `OrganizationInvitationService.AcceptInvitationAsync`. Users who join an organization via an invitation (the primary onboarding flow) may remain in "pending" status indefinitely.
- **Description:** When a user is added to an organization, their account should transition from "Pending" to "Active" status. This logic exists in the direct add-member flow but not in the invitation acceptance flow. This is a functional bug in the invitation onboarding path.
- **Recommendation:** Extract the user activation logic into a shared private method or a dedicated `UserActivationService`. Call it from all three code paths: direct member addition (both methods) and invitation acceptance. Add a test that verifies pending users are activated upon accepting an invitation.
- **Affected files:**
  - `src/Cadence.Core/Features/Organizations/Services/OrganizationService.cs`
  - `src/Cadence.Core/Features/Organizations/Services/OrganizationInvitationService.cs`

---

### CD-M06: Inconsistent org-scoping in service queries

- **File(s):** Multiple service files
- **Category:** Stability
- **Impact:** Some services explicitly filter by `OrganizationId` in every query, while others rely entirely on EF Core's global query filters. The inconsistency makes it unclear which is the intended pattern and creates risk if a global filter is accidentally bypassed or `IgnoreQueryFilters()` is used without re-adding the org filter.
- **Description:** The architecture provides two layers of org-scoping: (1) global query filters in AppDbContext that automatically add `WHERE OrganizationId = @currentOrgId` to all queries on org-scoped entities, and (2) explicit `.Where(e => e.OrganizationId == _orgContext.OrganizationId)` in service methods. Some services do both (belt and suspenders), some do only one.
- **Recommendation:** Standardize on the "belt and suspenders" approach: global filters provide the safety net, but services should also include explicit org-scoping in queries as defense-in-depth. Document this as the required pattern in CODING_STANDARDS.md. Audit all services for consistency.
- **Affected files:** Multiple services across `src/Cadence.Core/Features/*/Services/`

---

### CD-M07: Large service classes exceeding 500 lines

- **File(s):** `src/Cadence.Core/Features/Injects/Services/InjectService.cs`, `src/Cadence.Core/Features/Exercises/Services/ExerciseService.cs`
- **Category:** FileSize
- **Impact:** Large service classes with many methods covering different sub-domains (CRUD, approval workflow, status transitions, import) are harder to navigate, test, and reason about.
- **Description:** `InjectService` handles inject CRUD, firing, approval workflow, batch operations, readiness checks, and more in a single class. `ExerciseService` similarly covers CRUD, cloning, settings, participant management, and status transitions.
- **Recommendation:** Decompose along domain boundaries: extract `InjectApprovalService` from `InjectService` (handles submit/approve/reject/revert), extract `ExerciseCloneService` from `ExerciseService` (handles cloning logic). Each extracted service should have its own interface registered in DI.
- **Affected files:**
  - `src/Cadence.Core/Features/Injects/Services/InjectService.cs`
  - `src/Cadence.Core/Features/Exercises/Services/ExerciseService.cs`

---

### CD-M08: Validation coverage gaps in service layer

- **File(s):** Multiple service files
- **Category:** Stability
- **Impact:** Some service methods accept DTOs and proceed directly to entity creation without validating business rule invariants. For example, inject creation does not verify that the target MSEL belongs to the same exercise, and exercise cloning does not validate that the source exercise is accessible to the current organization.
- **Description:** FluentValidation is registered in the DI container but no `AbstractValidator<T>` implementations exist (see AR-M02). Validation is either in controllers (ad-hoc private methods) or missing entirely at the service layer.
- **Recommendation:** Create FluentValidation validators for all create/update DTOs in `Features/{Feature}/Validators/`. Register them in DI and inject `IValidator<T>` in services. Call `validator.ValidateAndThrowAsync(dto)` at the start of each create/update method.
- **Affected files:** `src/Cadence.Core/Features/*/Validators/` (mostly empty or missing)

---

## Minor Issues

### CD-N01: Inconsistent mapper patterns across features

- **File(s):** Various `Mappers/` directories
- **Category:** Convention
- **Description:** Some mappers use static methods, some use extension methods, and some use instance methods. The most common pattern is static methods, but it's not universal.
- **Recommendation:** Standardize on static mapper classes with `ToDto()` and `ToEntity()` methods, which is the dominant pattern. Convert any deviations.

---

### CD-N02: TODO/FIXME comments that need resolution

- **File(s):** Various files across `src/Cadence.Core/`
- **Category:** Orphaned
- **Description:** Several service files contain TODO comments for deferred work. These should either be resolved or tracked as issues.
- **Recommendation:** Audit all TODO/FIXME comments. Convert actionable ones to GitHub issues. Remove completed or obsolete ones.

---

### CD-N03: Unused entity navigation properties

- **File(s):** Various entity files in `src/Cadence.Core/Models/Entities/`
- **Category:** Orphaned
- **Description:** Some entities define navigation properties (collections) that are never `.Include()`d in any query. These properties add EF Core configuration overhead and can cause accidental lazy loading if a proxy is configured.
- **Recommendation:** Audit navigation properties against actual query usage. Remove unused collection navigation properties unless they're needed for cascade delete configuration.

---

### CD-N04: DbContext `OnModelCreating` is getting large

- **File(s):** `src/Cadence.Core/Data/AppDbContext.cs`
- **Category:** FileSize
- **Description:** The `OnModelCreating` method contains configuration for all 45+ entities inline. While this works, it's getting large and harder to navigate.
- **Recommendation:** Consider extracting entity configurations into separate `IEntityTypeConfiguration<T>` classes for the largest entities, applied via `modelBuilder.ApplyConfigurationsFromAssembly()`.

---

### CD-N05: Seeder ordering dependencies not documented

- **File(s):** `src/Cadence.Core/Data/Seeders/`
- **Category:** Readability
- **Description:** Data seeders have implicit ordering dependencies (e.g., roles must be seeded before users, organizations before exercises). The ordering is correct in the calling code but not documented.
- **Recommendation:** Add a comment block at the top of the seeder orchestration method documenting the required order and why.

---

### CD-N06: Exception swallowing in some service methods

- **File(s):** Various service files
- **Category:** Stability
- **Description:** A few service methods catch exceptions with empty catch blocks or catch-and-log without re-throwing. This can hide bugs during development.
- **Recommendation:** Audit all catch blocks. Either re-throw (preserving the stack trace) or return a Result type that communicates the failure to the caller. Never swallow exceptions silently.

---

### CD-N07: Missing indexes on frequently queried columns

- **File(s):** `src/Cadence.Core/Data/AppDbContext.cs`
- **Category:** Stability
- **Description:** Some columns used frequently in WHERE clauses (e.g., `OrganizationId` on various tables, `ExerciseId` on injects, `Status` on injects) may not have explicit indexes defined. EF Core creates indexes for foreign keys automatically, but composite indexes for common query patterns may be missing.
- **Recommendation:** Review query patterns in services and add composite indexes for frequently used filter combinations. Example: `(ExerciseId, Status, IsDeleted)` on Injects table for the common "get all active injects for exercise" query.

---

## Positive Patterns

### CD-P01: Every `IgnoreQueryFilters()` call has an explanatory comment

- **Where:** Throughout `src/Cadence.Core/Features/*/Services/`
- **What:** Every use of `.IgnoreQueryFilters()` includes a comment explaining why the global filter needs to be bypassed (e.g., "Include soft-deleted items for audit trail", "Admin query across all organizations"). This prevents accidental data leaks and makes code review easier.
- **Replicate in:** Continue enforcing this pattern. Consider adding a Roslyn analyzer that requires a comment on any `IgnoreQueryFilters()` call.

---

### CD-P02: `ExerciseDeleteService` correctly uses `ExecuteDeleteAsync` for large cascades

- **Where:** `src/Cadence.Core/Features/Exercises/Services/ExerciseDeleteService.cs`
- **What:** The delete service uses EF Core 10's `ExecuteDeleteAsync` for bulk cascade deletes instead of loading all entities into memory. This is the correct pattern for efficient bulk operations.
- **Replicate in:** Apply the same pattern in `NotificationService` (see CD-M04) and any other bulk update/delete operations.

---

### CD-P03: `InjectReadinessService` uses two-phase AsNoTracking-then-tracking query

- **Where:** `src/Cadence.Core/Features/Injects/Services/InjectReadinessService.cs`
- **What:** The service first queries with `AsNoTracking()` to check readiness conditions, then only tracks entities that need to be modified. This minimizes change tracker overhead while still allowing efficient updates.
- **Replicate in:** Any service that needs to read data for decision-making before selectively updating entities.

---

### CD-P04: All mapper classes are static pure functions

- **Where:** `src/Cadence.Core/Features/*/Mappers/`
- **What:** Mapper classes contain only static methods with no side effects, no injected dependencies, and no state. They take an entity and return a DTO (or vice versa). This makes them trivially testable, thread-safe, and easy to reason about.
- **Replicate in:** Maintain this pattern. Do not add dependencies to mappers. If complex mapping requires service calls (e.g., resolving user names), do that in the service layer before calling the mapper.
