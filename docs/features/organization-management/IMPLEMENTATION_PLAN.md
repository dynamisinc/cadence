# Organization Management - Implementation Plan

**Date:** 2025-01-29
**Status:** Ready for Implementation

---

## Overview

This plan coordinates parallel implementation of the Organization Management feature using specialized agents. Work is organized into sequential phases and parallel streams to maximize efficiency while minimizing conflicts.

---

## Phase 1: Foundation (Sequential - Must Complete First)

**Duration:** Foundation work that all other streams depend on

### 1.1 Entity & Enum Definitions

**Files to Create/Modify:**

```
src/Cadence.Core/Models/Entities/
├── Enums.cs                    # Add OrgRole, OrgStatus, update UserStatus
├── Organization.cs             # Add Slug, ContactEmail, Status
├── OrganizationMembership.cs   # NEW entity
├── OrganizationInvite.cs       # NEW entity (for P1 stories)
├── Agency.cs                   # NEW entity (for P1 stories)
└── ApplicationUser.cs          # Add CurrentOrganizationId
```

**Enum Definitions:**
```csharp
public enum OrgRole
{
    OrgAdmin = 1,    // Full org access
    OrgManager = 2,  // Create/manage exercises
    OrgUser = 3      // Participate in assigned exercises
}

public enum OrgStatus
{
    Active = 1,      // Normal operation
    Archived = 2,    // Read-only, hidden from non-admins
    Inactive = 3     // Completely hidden, data preserved
}

// Update existing
public enum UserStatus
{
    Pending = 0,     // No org assigned
    Active = 1,      // Has org membership
    Disabled = 2     // Account disabled
}
```

### 1.2 Database Migration

**Create migration after entities:**
```bash
dotnet ef migrations add AddOrganizationManagement -p src/Cadence.Core -s src/Cadence.WebApi
```

**Migration should include:**
- Organization table updates (Slug, ContactEmail, Status)
- OrganizationMembership table (new)
- OrganizationInvite table (new, for P1)
- Agency table (new, for P1)
- ApplicationUser.CurrentOrganizationId column
- Indexes and foreign keys
- Data migration: Create OrganizationMembership for each existing User.OrganizationId

### 1.3 DbContext Updates

**Location:** `src/Cadence.Core/Core/Data/AppDbContext.cs`

- Add DbSets for new entities
- Add entity configurations (unique constraints, indexes)
- Organization-scoped query filter for non-SysAdmin users

### 1.4 ICurrentOrganizationContext Interface

**Location:** `src/Cadence.Core/Hubs/ICurrentOrganizationContext.cs`

```csharp
public interface ICurrentOrganizationContext
{
    Guid? CurrentOrganizationId { get; }
    OrgRole? CurrentOrgRole { get; }
    bool IsSysAdmin { get; }
    Task<bool> HasMembershipAsync(Guid organizationId);
}
```

This interface is consumed by services in Core. Implementation lives in WebApi.

---

## Phase 2: Parallel Work Streams

After Phase 1 foundation is complete, these streams can run in parallel:

```
┌─────────────────────────────────────────────────────────────────────┐
│                         PHASE 1: FOUNDATION                          │
│      Entities → Enums → Migration → DbContext → Interface           │
└─────────────────────────────────────────────────────────────────────┘
                                    │
          ┌─────────────────────────┼─────────────────────────┐
          │                         │                         │
          ▼                         ▼                         ▼
    ┌───────────┐           ┌───────────┐           ┌───────────┐
    │ STREAM A  │           │ STREAM B  │           │ STREAM C  │
    │ Org CRUD  │           │ User/Auth │           │ Frontend  │
    │ Backend   │           │ Backend   │           │ Foundation│
    │           │           │           │           │           │
    │ OM-01,02  │           │ OM-05,06  │           │ Shared UI │
    │ OM-03,04  │           │ JWT+Auth  │           │ Components│
    └───────────┘           └───────────┘           └───────────┘
          │                         │                         │
          └─────────────────────────┼─────────────────────────┘
                                    │
                                    ▼
                          ┌───────────────────┐
                          │    STREAM D       │
                          │ Frontend Pages    │
                          │ Integration       │
                          │                   │
                          │ All OM pages      │
                          └───────────────────┘
```

---

## Stream A: Organization CRUD Backend

**Stories:** OM-01, OM-02, OM-03, OM-04
**Agent:** `backend-agent`

### Files to Create

```
src/Cadence.Core/Features/Organizations/
├── Models/
│   └── DTOs/
│       └── OrganizationDtos.cs
├── Services/
│   ├── IOrganizationService.cs
│   └── OrganizationService.cs
├── Mappers/
│   └── OrganizationMapper.cs
└── Validators/
    └── OrganizationValidators.cs

src/Cadence.WebApi/Controllers/
├── AdminOrganizationsController.cs   # SysAdmin only
└── OrganizationsController.cs        # Current org operations
```

### API Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/admin/organizations` | List all orgs (SysAdmin) |
| POST | `/api/admin/organizations` | Create org (SysAdmin) |
| GET | `/api/admin/organizations/{id}` | Get org details (SysAdmin) |
| PUT | `/api/admin/organizations/{id}` | Update org (SysAdmin) |
| GET | `/api/admin/organizations/check-slug` | Check slug availability |
| POST | `/api/admin/organizations/{id}/archive` | Archive org |
| POST | `/api/admin/organizations/{id}/deactivate` | Deactivate org |
| POST | `/api/admin/organizations/{id}/restore` | Restore org |
| GET | `/api/organizations/current` | Get current org (OrgAdmin) |
| PUT | `/api/organizations/current` | Update current org (OrgAdmin) |

### TDD Test Files

```
src/Cadence.Core.Tests/Features/Organizations/
├── OrganizationServiceTests.cs
├── OrganizationValidatorTests.cs
└── SlugGeneratorTests.cs
```

### Key Test Scenarios

1. `GetAllOrganizations_AsSysAdmin_ReturnsAllOrgs`
2. `GetAllOrganizations_AsNonSysAdmin_ThrowsUnauthorized`
3. `CreateOrganization_WithValidData_CreatesOrgAndFirstAdmin`
4. `CreateOrganization_WithDuplicateSlug_ThrowsConflict`
5. `ArchiveOrganization_SetsStatusToArchived`
6. `DeactivateOrganization_HidesFromNonSysAdmin`
7. `RestoreOrganization_FromArchived_SetsStatusToActive`

---

## Stream B: User & Auth Integration Backend

**Stories:** OM-05, OM-06
**Agent:** `backend-agent`

### Files to Create/Modify

```
src/Cadence.Core/Features/Organizations/
├── Services/
│   ├── IMembershipService.cs
│   └── MembershipService.cs

src/Cadence.WebApi/
├── Middleware/
│   └── OrganizationContextMiddleware.cs
├── Services/
│   └── CurrentOrganizationContext.cs
└── Controllers/
    ├── AdminUsersController.cs        # User management (SysAdmin)
    └── UsersController.cs             # User operations (self)

# Modify
src/Cadence.Core/Features/Authentication/Services/JwtTokenService.cs
src/Cadence.WebApi/Authorization/AuthorizationExtensions.cs
```

### API Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/admin/users` | List all users (SysAdmin) |
| POST | `/api/admin/users/{id}/memberships` | Assign user to org |
| PUT | `/api/admin/users/{id}/memberships/{mid}` | Change role |
| DELETE | `/api/admin/users/{id}/memberships/{mid}` | Remove from org |
| GET | `/api/users/me/organizations` | Get user's memberships |
| POST | `/api/users/current-organization` | Switch org (returns new JWT) |

### JWT Claim Updates

Add to token generation:
```csharp
new Claim("org_id", user.CurrentOrganizationId?.ToString() ?? ""),
new Claim("org_role", membership?.Role.ToString() ?? ""),
```

### TDD Test Files

```
src/Cadence.Core.Tests/Features/Organizations/
├── MembershipServiceTests.cs
└── OrganizationContextTests.cs
```

### Key Test Scenarios

1. `AssignUserToOrg_CreatesNewMembership`
2. `AssignUserToOrg_PendingUser_ChangesStatusToActive`
3. `RemoveFromOrg_LastOrg_ChangesStatusToPending`
4. `ChangeRole_OnlyOrgAdmin_ThrowsBusinessRuleException`
5. `SwitchOrganization_ValidMembership_ReturnsNewToken`
6. `SwitchOrganization_NoMembership_ThrowsUnauthorized`

---

## Stream C: Frontend Foundation

**Focus:** Shared components, contexts, routing
**Agent:** `frontend-agent`

### Files to Create

```
src/frontend/src/
├── contexts/
│   └── OrganizationContext.tsx
├── shared/
│   └── components/
│       ├── OrganizationSwitcher.tsx
│       ├── OrganizationDropdown.tsx
│       ├── StatusChip.tsx
│       └── RoleChip.tsx
├── features/
│   └── organizations/
│       ├── types/
│       │   └── index.ts
│       ├── services/
│       │   └── organizationService.ts
│       └── hooks/
│           ├── useOrganizations.ts
│           └── useOrganization.ts
└── pages/
    └── PendingUserPage.tsx
```

### Route Updates

```typescript
// Add to App.tsx routes
'/admin/organizations'          // Org list (SysAdmin)
'/admin/organizations/new'      // Create org (SysAdmin)
'/admin/organizations/:id'      // Edit org (SysAdmin)
'/pending'                      // Pending user page
'/join/:code'                   // Org code redemption (P1)
```

### OrganizationContext Interface

```typescript
interface OrganizationContextValue {
  currentOrg: Organization | null;
  memberships: OrganizationMembership[];
  isLoading: boolean;
  isPending: boolean;  // No org assigned
  switchOrganization: (orgId: string) => Promise<void>;
  refreshMemberships: () => Promise<void>;
}
```

### TDD Test Files

```
src/frontend/src/
├── contexts/OrganizationContext.test.tsx
├── shared/components/OrganizationSwitcher.test.tsx
└── pages/PendingUserPage.test.tsx
```

---

## Stream D: Frontend Pages & Integration

**Stories:** All OM stories (frontend)
**Agent:** `frontend-agent`
**Prerequisites:** Streams A, B, C must be complete

### Files to Create

```
src/frontend/src/features/organizations/
├── pages/
│   ├── OrganizationListPage.tsx
│   ├── OrganizationListPage.test.tsx
│   ├── CreateOrganizationPage.tsx
│   ├── CreateOrganizationPage.test.tsx
│   ├── EditOrganizationPage.tsx
│   └── EditOrganizationPage.test.tsx
├── components/
│   ├── OrganizationTable.tsx
│   ├── OrganizationForm.tsx
│   ├── OrganizationStatusActions.tsx
│   └── SlugInput.tsx

src/frontend/src/features/users/
├── pages/
│   ├── UserListPage.tsx
│   ├── UserListPage.test.tsx
│   └── UserDetailPanel.tsx
├── components/
│   ├── UserTable.tsx
│   ├── AssignToOrgDialog.tsx
│   └── MembershipTable.tsx
```

### Page Components

| Page | Route | Features |
|------|-------|----------|
| OrganizationListPage | `/admin/organizations` | Search, filter, sort, create button |
| CreateOrganizationPage | `/admin/organizations/new` | Form with slug auto-generation |
| EditOrganizationPage | `/admin/organizations/:id` | Form + status actions |
| UserListPage | `/admin/users` | Search, filter, membership management |
| PendingUserPage | `/pending` | Message + org code input |

---

## Phase 3: Integration & Verification

After all streams complete:

### 3.1 Integration Test Suite

```bash
# Backend integration tests
dotnet test src/Cadence.Core.Tests --filter "Category=Integration"

# Frontend tests
cd src/frontend && npm test

# Build verification
dotnet build
cd src/frontend && npm run build:check
```

### 3.2 Manual Verification Checklist

```markdown
## SysAdmin Flow
- [ ] Can view all organizations in admin panel
- [ ] Can create organization with first admin
- [ ] Can edit any organization
- [ ] Can archive/deactivate/restore organizations
- [ ] Can assign pending users to organizations
- [ ] Can manage user memberships

## OrgAdmin Flow
- [ ] Can edit own organization details
- [ ] Cannot see other organizations
- [ ] Can view users in own organization

## User Flow
- [ ] Can switch between organizations (multi-org user)
- [ ] Sees only current org data after switch
- [ ] Pending user sees pending page
- [ ] Can enter org code to join (P1)

## Data Isolation
- [ ] Org A exercises never visible to Org B users
- [ ] Global query filters working correctly
- [ ] JWT contains correct org_id claim
- [ ] Switching org refreshes data correctly
```

---

## File Ownership by Stream

| Stream | Backend Files | Frontend Files |
|--------|---------------|----------------|
| Foundation | Entities, Enums, DbContext, Migration | - |
| Stream A | Organizations/, AdminOrganizationsController | - |
| Stream B | Memberships/, JWT, Middleware, UsersController | - |
| Stream C | - | OrganizationContext, OrganizationSwitcher, shared |
| Stream D | - | organization pages, user pages |

---

## Dependency Graph

```
Foundation
    ├── Stream A (Org CRUD)
    │       └── Stream D (Org Pages)
    ├── Stream B (User/Auth)
    │       └── Stream D (User Pages)
    └── Stream C (Frontend Foundation)
            └── Stream D (All Pages)
```

---

## Commit Convention

```
feat(org): add organization CRUD endpoints
feat(membership): add user-organization assignment service
feat(auth): add org_id claim to JWT
feat(ui): add OrganizationContext provider
feat(ui): add OrganizationSwitcher component
feat(pages): add organization list page
test(org): add organization service unit tests
fix(org): handle concurrent edit conflict
```

---

## Risk Mitigation

### Merge Conflicts
- Each stream owns specific files (see ownership table)
- Stream D waits for A, B, C to complete
- Foundation phase is sequential (no parallel work)

### Breaking Changes
- Add nullable columns first, make required in separate migration
- Feature flag for new org context (optional)
- Backward compatibility for existing JWTs during rollout

### Test Coverage
- TDD mandatory: tests FIRST, then implementation
- Each acceptance criterion maps to at least one test
- Integration tests for authorization policies

---

## Next Steps

1. **Start Phase 1:** Use `database-agent` to create entities and migration
2. **Launch Streams A, B, C:** Use `backend-agent` for A, B; `frontend-agent` for C
3. **Complete Stream D:** After A, B, C are done
4. **Integration Testing:** Run full test suite
5. **Manual Verification:** Walk through all flows
