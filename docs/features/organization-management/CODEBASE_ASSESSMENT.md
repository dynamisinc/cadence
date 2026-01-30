# Codebase Assessment Findings - Organization Management Feature

**Date:** 2025-01-29
**Feature:** Organization Management (Multi-Tenancy)

---

## 1. Current Entity Structure

### User Entity
**Location:** `src/Cadence.Core/Models/Entities/ApplicationUser.cs`

**Existing Fields:**
- `Id` (string - from IdentityUser)
- `Email` (unique, login credential)
- `DisplayName`
- `SystemRole` (enum: User | Manager | Admin) - **SYSTEM-LEVEL roles**
- `Status` (enum: Active | Deactivated)
- `LastLoginAt`
- `CreatedAt`, `CreatedById`
- `OrganizationId` (FK to Organization - **single primary org**)

**Required Changes:**
- Add `CurrentOrganizationId` (Guid?, FK) - user's active working org
- Update `Status` enum: Pending | Active | Disabled (was Active | Deactivated)
- Remove/deprecate single `OrganizationId` in favor of OrganizationMembership

### Organization Entity
**Location:** `src/Cadence.Core/Models/Entities/Organization.cs`

**Existing Fields:**
- `Id` (Guid)
- `Name` (required, max 200)
- `Description` (max 4000)
- Navigation: Users, Exercises

**Required Additions:**
- `Slug` (string, unique, URL-safe, max 50)
- `ContactEmail` (string?, optional)
- `Status` (enum: Active | Archived | Inactive)

### Existing Role Enums
**Location:** `src/Cadence.Core/Models/Entities/Enums.cs`

```csharp
// SYSTEM-LEVEL (exists)
public enum SystemRole { User = 0, Manager = 1, Admin = 2 }

// EXERCISE-LEVEL (exists - HSEEP roles)
public enum ExerciseRole { Administrator = 1, ExerciseDirector = 2, Controller = 3, Evaluator = 4, Observer = 5 }

// ORGANIZATION-LEVEL (NEEDS TO BE ADDED)
// OrgRole: OrgAdmin, OrgManager, OrgUser
```

---

## 2. Authentication System

**Type:** JWT Bearer + Refresh Token (HttpOnly cookies)

### Token Configuration
**Location:** `src/Cadence.Core/Features/Authentication/Models/JwtOptions.cs`
- Access tokens: 15 minutes
- Refresh tokens: 4 hours (30 days with RememberMe)
- Algorithm: HS256

### Current JWT Claims
**Location:** `src/Cadence.Core/Features/Authentication/Services/JwtTokenService.cs`
```csharp
- sub (User ID)
- email
- name (DisplayName)
- role (SystemRole)
- SystemRole (custom claim)
- jti (unique token ID)
- iat (issued at)
```

**Required Additions:**
- `org_id` (current organization ID)
- `org_role` (role in current organization)

### Token Services
- `ITokenService` / `JwtTokenService` - token generation
- `IRefreshTokenStore` / `RefreshTokenStore` - refresh token management
- `IAuthenticationService` / `AuthenticationService` - login, register, refresh

---

## 3. Authorization Patterns

### Existing Policies
**Location:** `src/Cadence.WebApi/Authorization/`

| Policy | Description |
|--------|-------------|
| `RequireAdmin` | SystemRole == Admin |
| `RequireManager` | SystemRole >= Manager |
| `ExerciseAccess` | Participant in exercise OR SysAdmin |
| `ExerciseController` | Controller role or higher |
| `ExerciseDirector` | Director role or higher |
| `ExerciseEvaluator` | Evaluator role or higher |

### Role Resolver
**Location:** `src/Cadence.Core/Features/Authorization/Services/RoleResolver.cs`
- `GetExerciseRoleAsync()` - get user's role in specific exercise
- `CanAccessExerciseAsync()` - SysAdmins can access all, others need participation
- `HasExerciseRoleAsync()` - check minimum role requirement

**Required Additions:**
- `SysAdminOnly` policy (for admin/organizations endpoints)
- `OrgAdminOnly` policy (for org-scoped admin endpoints)
- Organization-level role resolution

---

## 4. Database Context

### DbContext
**Location:** `src/Cadence.Core/Core/Data/AppDbContext.cs`

**Features:**
- Extends `IdentityDbContext<ApplicationUser>`
- Global `datetime2` column type
- Global soft delete query filters via ISoftDeletable
- Automatic timestamp updates in SaveChanges

**Existing DbSets (26):**
Organizations, ApplicationUsers, Exercises, Msels, Injects, ExerciseParticipants, Observations, etc.

**Required Additions:**
- `DbSet<OrganizationMembership>`
- `DbSet<OrganizationInvite>`
- `DbSet<Agency>`
- Organization-scoped query filters

### Seeded Data
- Default Organization: `00000000-0000-0000-0000-000000000001`
- System User: `00000000-0000-0000-0000-000000000001`
- 5 HseepRoles, 7 DeliveryMethods

---

## 5. Frontend Architecture

### State Management
- **React Query** (TanStack Query v5) - server state
- **Context Providers** - AuthContext, ExerciseNavigationContext
- **Component State** - local state for UI

### Authentication Flow
**Location:** `src/frontend/src/contexts/AuthContext.tsx`
- Access token in memory (React state)
- Refresh tokens in HttpOnly cookies
- Automatic refresh 2 minutes before expiry
- Cross-tab logout synchronization

### Routing
**Location:** `src/frontend/src/App.tsx`
- React Router v7 data mode
- Protected routes via RootLayout
- Exercise context via ExerciseContextWrapper

**Current Routes:**
- `/exercises`, `/exercises/new`, `/exercises/:id/*`
- `/admin` (admin dashboard - requires SysAdmin)
- `/admin/users`, `/admin/capabilities`

**Required Additions:**
- `/admin/organizations`, `/admin/organizations/new`, `/admin/organizations/:id`
- `/pending` (pending user page)
- `/join/:code` (organization code redemption)
- OrganizationContext provider

### API Client
**Location:** `src/frontend/src/core/services/api.ts`
- Axios with credentials (for cookies)
- Authorization header interceptor
- 401 handler with token refresh

**Required Changes:**
- Add org_id header/context
- Handle 403 OrganizationAccessRevoked error

### COBRA Styling
**Location:** `src/frontend/src/theme/`
- `cobraTheme.ts` - MUI theme
- `styledComponents/` - CobraPrimaryButton, CobraTextField, etc.
- FontAwesome icons only (no @mui/icons-material)

---

## 6. Integration Points for Organization Management

### Entity Changes Required

| Entity | Changes |
|--------|---------|
| `Organization` | Add Slug, ContactEmail, Status |
| `ApplicationUser` | Add CurrentOrganizationId, update Status enum |
| NEW `OrganizationMembership` | UserId, OrganizationId, OrgRole, JoinedAt, Status |
| NEW `OrganizationInvite` | OrganizationId, Email, Code, OrgRole, Expires, Uses |
| NEW `Agency` | OrganizationId, Name, Abbreviation, IsActive, SortOrder |

### Enum Additions

```csharp
public enum OrgRole { OrgAdmin = 1, OrgManager = 2, OrgUser = 3 }
public enum OrgStatus { Active = 1, Archived = 2, Inactive = 3 }
public enum UserStatus { Pending = 0, Active = 1, Disabled = 2 }  // Updated
```

### API Endpoints Required

**Admin (SysAdmin only):**
- `GET /api/admin/organizations` - list all orgs
- `POST /api/admin/organizations` - create org
- `GET/PUT /api/admin/organizations/:id` - get/update org
- `POST /api/admin/organizations/:id/archive|restore|deactivate`
- `GET /api/admin/organizations/check-slug` - slug availability
- `GET /api/admin/users` - list all users
- `POST/PUT/DELETE /api/admin/users/:id/memberships/:id` - membership management

**User (Authenticated):**
- `GET /api/users/me/organizations` - user's org memberships
- `POST /api/users/current-organization` - switch org (returns new JWT)
- `POST /api/join/:code` - redeem org code

**Org Admin:**
- `GET/PUT /api/organizations/current` - current org details
- `GET/POST /api/organizations/current/invites` - manage invites
- `GET/POST/PUT/DELETE /api/organizations/current/agencies` - manage agencies

### JWT Claim Updates

```json
{
  "sub": "user-guid",
  "email": "user@example.com",
  "org_id": "org-guid",
  "org_role": "OrgAdmin",
  "system_role": "None"
}
```

### Frontend Context Required

```typescript
interface OrganizationContextValue {
  currentOrg: Organization | null;
  memberships: OrganizationMembership[];
  isLoading: boolean;
  isPending: boolean;
  switchOrganization: (orgId: string) => Promise<void>;
}
```

---

## 7. Risks & Considerations

### Breaking Changes
1. **User.OrganizationId deprecation** - Existing users have single OrganizationId; need migration to create OrganizationMembership records
2. **JWT claim changes** - Existing tokens won't have org_id; need graceful handling during rollout
3. **Global query filters** - Adding org-scoped filters will affect all existing queries

### Migration Strategy
1. Add new columns/tables as nullable first
2. Migrate existing data (create memberships from User.OrganizationId)
3. Update application code to use new structure
4. Make columns required and add constraints
5. Deprecate old columns (soft deprecation, keep for backward compat)

### Data Isolation Verification
- All Exercise queries must filter by OrganizationId
- All Inject, Observation, etc. queries inherit org context from Exercise
- SysAdmins bypass org filters (use `.IgnoreQueryFilters()` carefully)

---

## 8. Summary

The codebase is well-structured with clear patterns:
- **Feature-based architecture** in both backend and frontend
- **TDD workflow** with test projects
- **JWT authentication** with refresh tokens
- **Role-based authorization** at system and exercise levels

The Organization Management feature adds a **third role level** (organization-level) between system and exercise roles, requiring:
1. New entities and enums
2. JWT claim updates
3. New authorization policies
4. Frontend context provider
5. Migration of existing user data

The existing patterns provide clear templates for implementation.
