# Authorization Implementation Status

## Date: 2026-01-22

## Current Status: **Schema Updated - Breaking Changes to Fix**

---

## What Has Been Completed

### 1. SystemRole Enum Created ✅
- Location: [Enums.cs:243-262](../../../src/Cadence.Core/Models/Entities/Enums.cs#L243-L262)
- Values: `User` (0), `Manager` (1), `Admin` (2)
- Fully documented with clear separation from Exercise roles

### 2. ApplicationUser Entity Updated ✅
- Location: [ApplicationUser.cs](../../../src/Cadence.Core/Models/Entities/ApplicationUser.cs)
- Changed: `GlobalRole` (string) → `SystemRole` (enum)
- Added navigation properties:
  - `ExerciseParticipations` - links to exercises user is assigned to
  - `CreatedExercises` - exercises user created (for ownership tracking)

### 3. ExerciseParticipant Entity Updated ✅
- Location: [ExerciseParticipant.cs](../../../src/Cadence.Core/Models/Entities/ExerciseParticipant.cs)
- Changed: `UserId` from `Guid` → `string` (to match ASP.NET Core Identity)
- Changed: `User` navigation from `User?` → `ApplicationUser?`
- Added: `AssignedAt` (DateTime) - when participant was added
- Added: `AssignedById` (string?) - who added the participant
- Added: `AssignedBy` (ApplicationUser?) - navigation property

### 4. DbContext Configuration Updated ✅
- Location: [AppDbContext.cs:156-175](../../../src/Cadence.Core/Core/Data/AppDbContext.cs#L156-L175)
- Updated `ConfigureApplicationUser`:
  - SystemRole stored as string (convertible enum)
  - Added index on SystemRole for efficient queries
- Updated `ConfigureExerciseParticipant`:
  - References ApplicationUser instead of User
  - Added AssignedBy relationship

### 5. Migration Created ⚠️
- Location: [20260122193456_SeparateSystemAndExerciseRoles.cs](../../../src/Cadence.Core/Migrations/20260122193456_SeparateSystemAndExerciseRoles.cs)
- **Status: Empty** - Won't generate properly until compilation errors are fixed

---

## What Needs to Be Fixed

### Breaking Changes Requiring Updates

#### 1. **GlobalRole → SystemRole** (13 errors)
Files affected:
- `Features/Users/Services/UserService.cs` - Multiple references to `GlobalRole`
- `Features/Authentication/Services/AuthenticationService.cs` - Uses `GlobalRole` for token generation
- `Features/Users/Models/DTOs/UserDtos.cs` - DTO mapping uses `GlobalRole`

**Fix:**
```csharp
// OLD:
user.GlobalRole = nameof(ExerciseRole.Administrator);
if (user.GlobalRole == nameof(ExerciseRole.Administrator))

// NEW:
user.SystemRole = SystemRole.Admin;
if (user.SystemRole == SystemRole.Admin)
```

#### 2. **Guid → string for UserId** (12 errors)
Files affected:
- `Features/Exercises/Services/ExerciseParticipantService.cs` - UserId comparisons and assignments
- `Data/DevelopmentDataSeeder.cs` - Seed data for participants

**Fix:**
```csharp
// OLD:
participant.UserId = Guid.NewGuid();
if (participant.UserId == currentUserId) // currentUserId is Guid

// NEW:
participant.UserId = userId; // userId is string (from ApplicationUser.Id)
if (participant.UserId == currentUserId.ToString())
```

#### 3. **User → ApplicationUser Navigation** (1 error)
File: `Features/Exercises/Services/ExerciseParticipantService.cs:50`

**Fix:**
```csharp
// OLD:
User existingUser

// NEW:
ApplicationUser existingUser
```

---

## Architectural Decisions Implemented

✅ **System Roles (Application-Level)**
- Admin: Full system access, can see ALL exercises
- Manager: Can create exercises, becomes Director automatically
- User: Can only access assigned exercises

✅ **HSEEP Exercise Roles (Per-Exercise)**
- Administrator (system-wide - deprecated for exercise context)
- ExerciseDirector (Director)
- Controller
- Evaluator
- Observer

✅ **ExerciseParticipant as Bridge**
- One user can have different roles in different exercises
- Tracks who assigned them and when (audit trail)
- References ApplicationUser (ASP.NET Core Identity)

✅ **Admin Visibility**
- Admins can access ALL exercises without explicit assignment
- Implemented in authorization logic (not yet created)

---

## Next Steps (Priority Order)

### Phase 1: Fix Compilation Errors
1. **Update UserService.cs** - Replace GlobalRole with SystemRole
2. **Update AuthenticationService.cs** - Replace GlobalRole with SystemRole in JWT claims
3. **Update UserDtos.cs** - Map SystemRole enum to string
4. **Update ExerciseParticipantService.cs** - Handle string UserId instead of Guid
5. **Update DevelopmentDataSeeder.cs** - Fix UserId type in seed data

### Phase 2: Create Authorization Infrastructure
6. **Create IRoleResolver Service**
   - `Task<ExerciseRole?> GetExerciseRoleAsync(string userId, Guid exerciseId)`
   - `Task<bool> CanAccessExerciseAsync(string userId, Guid exerciseId)`
   - `Task<bool> HasExerciseRoleAsync(string userId, Guid exerciseId, ExerciseRole minimumRole)`

7. **Create Authorization Policies**
   - `RequireAdmin` - System-level admin access
   - `RequireManager` - Manager or Admin
   - `ExerciseAccess` - Can view exercise (Admin or assigned participant)
   - `ExerciseController` - Controller or higher in exercise
   - `ExerciseDirector` - Director in exercise (or Admin)

8. **Create Authorization Handlers**
   - `ExerciseAccessHandler` - Checks if user can access exercise
   - `ExerciseRoleHandler` - Checks if user has sufficient role in exercise

### Phase 3: Create Proper Migration
9. **Remove Empty Migration**
   ```bash
   dotnet ef migrations remove
   ```

10. **Create New Migration After Code Fixes**
    ```bash
    dotnet ef migrations add SeparateSystemAndExerciseRoles
    ```

11. **Add Data Migration for Existing Records**
    - Map old GlobalRole values to new SystemRole:
      - "Administrator" → SystemRole.Admin
      - "ExerciseDirector" → SystemRole.Manager
      - Others → SystemRole.User

### Phase 4: Update Exercise Creation Logic
12. **Auto-assign Creator as Director**
    - When Manager creates exercise, automatically create ExerciseParticipant with Role=Director

13. **Track Exercise Ownership**
    - Add CreatedById to Exercise entity (already in ApplicationUser nav property)

### Phase 5: Testing
14. **Write Unit Tests for RoleResolver**
15. **Write Integration Tests for Authorization**
16. **Update Existing Tests** - Fix any tests broken by schema changes

---

## Database Schema Changes Summary

### AspNetUsers Table (ApplicationUser)
| Column | Old Type | New Type | Notes |
|--------|----------|----------|-------|
| `GlobalRole` | `nvarchar(50)` | ❌ Removed | |
| `SystemRole` | - | `nvarchar(20)` ✅ Added | Enum: User/Manager/Admin |

### ExerciseParticipants Table
| Column | Old Type | New Type | Notes |
|--------|----------|----------|-------|
| `UserId` | `uniqueidentifier` | `nvarchar(450)` | References AspNetUsers.Id |
| `AssignedAt` | - | `datetime2` ✅ Added | Timestamp |
| `AssignedById` | - | `nvarchar(450)` ✅ Added | References AspNetUsers.Id |

### Users Table (Deprecated)
- ⚠️ **Still exists** - Will be removed in future migration
- Currently not used (ExerciseParticipant now references ApplicationUser)
- Contains seed data that may need migration

---

## Key Files Modified

1. ✅ [Enums.cs](../../../src/Cadence.Core/Models/Entities/Enums.cs) - Added SystemRole enum
2. ✅ [ApplicationUser.cs](../../../src/Cadence.Core/Models/Entities/ApplicationUser.cs) - GlobalRole → SystemRole
3. ✅ [ExerciseParticipant.cs](../../../src/Cadence.Core/Models/Entities/ExerciseParticipant.cs) - UserId type change, added audit fields
4. ✅ [AppDbContext.cs](../../../src/Cadence.Core/Core/Data/AppDbContext.cs) - Updated configurations

---

## Migration Strategy for Existing Data

### If database has existing users with GlobalRole:

```sql
-- Map GlobalRole to SystemRole
UPDATE AspNetUsers
SET SystemRole =
    CASE GlobalRole
        WHEN 'Administrator' THEN 'Admin'
        WHEN 'ExerciseDirector' THEN 'Manager'
        ELSE 'User'
    END
WHERE SystemRole IS NULL;

-- Update ExerciseParticipants.UserId from Guid to string
-- (This will be handled by EF migration - may require manual data migration)
```

---

## Questions for Product Owner (If Needed)

1. ✅ **RESOLVED:** System roles separate from exercise roles
2. ✅ **RESOLVED:** Admins can see all exercises
3. ✅ **RESOLVED:** Manager becomes Director when creating exercise
4. ⚠️ **PENDING:** What happens to existing Users table data?
   - **Recommendation:** Deprecate Users table, migrate any non-auth data if needed

---

## Risks & Considerations

### 1. **Breaking Change in UserId Type**
- `Guid` → `string` (ASP.NET Core Identity uses string by default)
- May require data migration for existing ExerciseParticipant records
- Any code comparing UserIds needs update

### 2. **Two User Tables Exist**
- `Users` (deprecated, custom)
- `AspNetUsers` (current, Identity)
- Should eventually remove `Users` table after confirming no data loss

### 3. **GlobalRole → SystemRole Migration**
- Need to map existing GlobalRole values to new SystemRole enum
- Old values: "Administrator", "ExerciseDirector", "Controller", "Evaluator", "Observer"
- New values: Admin, Manager, User

---

## Resources

- [CLAUDE.md](../../../CLAUDE.md) - Project guidelines
- [Original Implementation Prompt](./AUTH_TESTING_RECOMMENDATIONS.md)
- [ASP.NET Core Authorization Docs](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/)
