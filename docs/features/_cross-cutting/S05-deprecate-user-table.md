# Story: S05 Deprecate Legacy User Table

> **Status**: 📋 Ready for Development
> **Priority**: P2 (Technical Debt)
> **Epic**: E2 - Infrastructure
> **Sprint Points**: 8

## User Story

**As a** developer,
**I want** to consolidate user storage to use only `ApplicationUser` (ASP.NET Identity),
**So that** the codebase has a single, consistent user model without type mismatches and confusion.

## Context

During the transition to ASP.NET Core Identity, the codebase ended up with two user entities:

| Entity | Table | ID Type | Current Purpose |
|--------|-------|---------|-----------------|
| `User` | `Users` | `Guid` | Audit fields (`CreatedBy`/`ModifiedBy`) + seeded System user |
| `ApplicationUser` | `AspNetUsers` | `string` | Authentication, user profiles, exercise participation |

This creates several problems:
1. **Type mismatch**: `BaseEntity.CreatedBy`/`ModifiedBy` are `Guid` but `ApplicationUser.Id` is `string`
2. **Confusion**: Two user tables with overlapping fields (Email, DisplayName, OrganizationId)
3. **Broken lookups**: Code like `MselService.cs:103` looks up display names from wrong table
4. **Orphaned relationships**: `Organization.Users` references deprecated `User` entity
5. **No FK constraints**: Guid audit fields have no referential integrity (just scalar values)
6. **Schema mismatch**: `User.ExerciseParticipations` navigation exists but `ExerciseParticipant.UserId` is `string` FK to `ApplicationUser`

### Mixed Authentication Compatibility

**This migration is fully compatible with mixed authentication (local + Entra ID):**

| Auth Method      | User Entity                                        | Impact                                               |
|------------------|----------------------------------------------------|----------------------------------------------------- |
| Local (password) | `ApplicationUser`                                  | No change - already uses correct entity              |
| Entra (SSO)      | `ApplicationUser` + `ExternalLogin`                | No change - `ExternalLogin.UserId` is already string |
| Hybrid (both)    | `ApplicationUser` with password + `ExternalLogins` | Fully supported                                      |

The `ExternalLogin` table correctly uses `string UserId` referencing `ApplicationUser`. When Entra is implemented, it will create `ApplicationUser` records directly - it never touches the legacy `User` table.

**CRITICAL**: This migration should be completed BEFORE enabling Entra authentication to avoid dual user systems.

### Current Architecture

```
BaseEntity
├── CreatedBy: Guid ──────────> User.Id (Guid)
├── ModifiedBy: Guid ─────────> User.Id (Guid)
└── DeletedBy: Guid? ─────────> User.Id (Guid)

ExerciseParticipant
└── UserId: string ───────────> ApplicationUser.Id (string)

Inject
├── FiredBy: string? ─────────> ApplicationUser.Id (string)
└── SkippedBy: string? ───────> ApplicationUser.Id (string)

Observation
└── CreatedByUserId: string? ─> ApplicationUser.Id (string)
```

### Target Architecture

```
BaseEntity
├── CreatedBy: string ────────> ApplicationUser.Id (string)
├── ModifiedBy: string ───────> ApplicationUser.Id (string)
└── DeletedBy: string? ───────> ApplicationUser.Id (string)

(All other relationships remain string-based to ApplicationUser)
```

## Acceptance Criteria

### Phase 1: Add New Columns (Non-Breaking)

- [ ] **Given** the database schema, **when** migration runs, **then** new `string` columns are added: `CreatedByUserId`, `ModifiedByUserId`, `DeletedByUserId` to all `BaseEntity` tables

- [ ] **Given** new audit columns exist, **when** entities are saved, **then** both old (`Guid`) and new (`string`) columns are populated during transition period

### Phase 2: Migrate Data

- [ ] **Given** existing data with `Guid` audit fields, **when** migration script runs, **then** data is mapped to corresponding `ApplicationUser.Id` where possible

- [ ] **Given** the seeded System user (`00000000-0000-0000-0000-000000000001`), **when** migration runs, **then** a corresponding `ApplicationUser` system account exists with a well-known ID

- [ ] **Given** audit records referencing deleted/unknown users, **when** migration runs, **then** they are mapped to the system user (with logging)

### Phase 3: Update Code References

- [ ] **Given** all services using `BaseEntity` audit fields, **when** code is updated, **then** they read/write the new `string` columns

- [ ] **Given** `MselService.GetSummaryAsync`, **when** looking up `ModifiedBy` user, **then** it queries `ApplicationUsers` table with correct ID type

- [ ] **Given** `Organization.Users` navigation property, **when** removed, **then** `Organization.ApplicationUsers` is available (or direct query)

- [ ] **Given** any code referencing `_context.Users` (the old DbSet), **when** updated, **then** it uses `_context.ApplicationUsers` or `UserManager<ApplicationUser>`

### Phase 4: Remove Legacy

- [ ] **Given** all code updated to use `ApplicationUser`, **when** migration runs, **then** old `Guid` audit columns are dropped

- [ ] **Given** the `User` entity and `Users` DbSet, **when** cleanup is complete, **then** they are removed from the codebase

- [ ] **Given** the `Users` table, **when** final migration runs, **then** the table is dropped

- [ ] **Given** `Organization.Users` navigation, **when** removed, **then** the foreign key relationship is also removed

### Validation

- [ ] **Given** the migration is complete, **when** running the application, **then** all CRUD operations work correctly with audit fields

- [ ] **Given** historical data, **when** querying entities, **then** `CreatedByUser` and `ModifiedByUser` navigations resolve to `ApplicationUser`

- [ ] **Given** the system user, **when** no auth context is available, **then** audit fields use the `ApplicationUser` system account ID

## Out of Scope

- Changing `ApplicationUser.Id` to `Guid` (would require more extensive Identity customization)
- Audit logging service (separate feature)
- User activity tracking (separate feature)

## Dependencies

- Authentication system must be fully functional
- All existing data must be backed up before migration
- Coordinate with any pending features that touch user relationships

### Blocking

This story **MUST be completed before**:
- Entra ID authentication implementation (S18-entra-provider)
- User account linking service (S19-user-account-linking)

Completing this migration first ensures Entra users are created in a unified `ApplicationUser` system without dual-table complexity.

## Risks & Mitigations

| Risk                         | Impact | Mitigation                                                              |
|------------------------------|--------|-------------------------------------------------------------------------|
| Data loss during migration   | High   | Backup database, run migration on staging first                         |
| Broken foreign keys          | Low    | **No FK constraints exist** on Guid audit fields (simplifies migration) |
| Performance regression       | Medium | Index new string columns appropriately                                  |
| Missed code references       | Medium | Comprehensive grep/search before removing old code                      |
| Inconsistent historical data | Medium | Audit data before migration; log unmapped records                       |

### Simplifying Factor: No FK Constraints

Analysis revealed that `BaseEntity.CreatedBy`, `ModifiedBy`, and `DeletedBy` are **scalar Guid values with no foreign key constraints** configured in the DbContext. This means:

- ✅ No FK constraints to drop during migration
- ✅ No cascade delete concerns
- ⚠️ Historical data may already contain orphaned Guid references
- ⚠️ Must audit data integrity before assuming all Guids map to valid users

## Migration Strategy

### Recommended Approach: Parallel Columns

1. **Add new columns** alongside old ones (non-breaking)
2. **Dual-write** during transition (write to both old and new)
3. **Migrate historical data** via SQL script
4. **Update read operations** to use new columns
5. **Remove dual-write** once all reads updated
6. **Drop old columns** and `Users` table

### SQL Migration Script (Example)

```sql
-- Step 1: Create system ApplicationUser if not exists
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail,
                         DisplayName, SystemRole, Status, OrganizationId, CreatedAt)
SELECT 'SYSTEM', 'system@cadence.local', 'SYSTEM@CADENCE.LOCAL',
       'system@cadence.local', 'SYSTEM@CADENCE.LOCAL',
       'System', 'Administrator', 'Active',
       '00000000-0000-0000-0000-000000000001', GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE Id = 'SYSTEM');

-- Step 2: Map old User GUIDs to ApplicationUser IDs
-- (Requires mapping table or email-based matching)
UPDATE e SET e.CreatedByUserId = au.Id
FROM Exercises e
JOIN Users u ON e.CreatedBy = u.Id
JOIN AspNetUsers au ON u.Email = au.Email;

-- Step 3: Default unmapped to SYSTEM
UPDATE Exercises SET CreatedByUserId = 'SYSTEM'
WHERE CreatedByUserId IS NULL;
```

## Affected Files

### Entities to Modify

- `Models/Entities/BaseEntity.cs` - Change audit field types from Guid to string
- `Models/Entities/User.cs` - Delete entirely
- `Models/Entities/Organization.cs` - Remove `Users` navigation property

### DbContext Changes

- `Core/Data/AppDbContext.cs`:
  - Remove `DbSet<User> Users` property
  - Remove `ConfigureUser()` method
  - Update `ConfigureOrganization()` to remove User relationship
  - **Add System ApplicationUser seeding** (currently only seeded in Users table)

### Services to Update

- `Features/Msel/Services/MselService.cs` - Fix user lookup (line 103)
- Any service using `_context.Users`

### Constants

- `Constants/SystemConstants.cs` - Add `SystemUserIdString` constant

### Schema Fixes (Pre-existing Issues)

- `Models/Entities/User.cs` line 40: Remove `ExerciseParticipations` navigation (incorrectly configured - `ExerciseParticipant.UserId` is string FK to `ApplicationUser`, not `User`)

## Technical Notes

### System User Strategy

Option A: **Well-known string ID**
```csharp
public static class SystemConstants
{
    public const string SystemUserId = "SYSTEM";
}
```

Option B: **GUID-like string for consistency**
```csharp
public const string SystemUserId = "00000000-0000-0000-0000-000000000001";
```

Recommend Option A for clarity that it's not a real user GUID.

### BaseEntity After Migration

```csharp
public abstract class BaseEntity : IHasTimestamps, ISoftDeletable
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = SystemConstants.SystemUserId;
    public string ModifiedBy { get; set; } = SystemConstants.SystemUserId;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

## Domain Terms

| Term | Definition |
|------|------------|
| ApplicationUser | ASP.NET Core Identity user entity (AspNetUsers table) |
| User (Legacy) | Deprecated custom user entity (Users table) |
| System User | Special account used for audit fields when no auth context |
| Audit Fields | CreatedBy, ModifiedBy, DeletedBy tracking who changed data |

---

## INVEST Checklist

- [x] **I**ndependent - Can be done without blocking other features
- [x] **N**egotiable - Migration phases can be adjusted
- [x] **V**aluable - Eliminates technical debt and confusion
- [x] **E**stimable - Well-defined migration steps, ~8 points
- [ ] **S**mall - Larger story, consider splitting into phases
- [x] **T**estable - Can verify data integrity post-migration

## Test Scenarios

### Unit Tests
- Audit field population with authenticated user
- Audit field population with no auth context (uses system user)
- User lookup returns correct `ApplicationUser`

### Integration Tests
- Create entity → verify `CreatedBy` populated correctly
- Update entity → verify `ModifiedBy` updated
- Query historical data → navigations resolve

### Migration Tests (Staging)
- Run migration on copy of production data
- Verify all records have valid `CreatedByUserId`
- Verify no orphaned foreign keys
- Verify application functionality post-migration

---

*Related Stories*: [S01 Session Management](./S01-session-management.md), [Authentication](../authentication/FEATURE.md)

*Last updated: 2025-01-27*
