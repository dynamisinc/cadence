# Authorization Implementation - Next Steps

## Progress Summary

### ✅ Completed (Core Schema Changes)

1. **SystemRole enum created** - [Enums.cs:243-262](../../../src/Cadence.Core/Models/Entities/Enums.cs#L243-L262)
   - `Admin` (2) - Full system access
   - `Manager` (1) - Can create exercises
   - `User` (0) - Standard access

2. **ApplicationUser entity updated** - [ApplicationUser.cs](../../../src/Cadence.Core/Models/Entities/ApplicationUser.cs)
   - `GlobalRole` (string) → `SystemRole` (enum)
   - Added `ExerciseParticipations` and `CreatedExercises` navigation properties

3. **ExerciseParticipant entity updated** - [ExerciseParticipant.cs](../../../src/Cadence.Core/Models/Entities/ExerciseParticipant.cs)
   - `UserId`: `Guid` → `string` (ASP.NET Core Identity)
   - `User`: References `ApplicationUser` instead of deprecated `User`
   - Added `AssignedAt`, `AssignedById`, `AssignedBy` for audit trail

4. **DbContext configuration updated** - [AppDbContext.cs](../../../src/Cadence.Core/Core/Data/AppDbContext.cs)
   - `ApplicationUser`: SystemRole as string, added index
   - `ExerciseParticipant`: Updated relationships

5. **DTOs Updated**:
   - ✅ `UserDto` - [UserDtos.cs](../../../src/Cadence.Core/Features/Users/Models/DTOs/UserDtos.cs)
     - `Id`: `Guid` → `string`
     - `Role` → `SystemRole`
   - ✅ `ChangeRoleRequest` - Now uses `SystemRole` property
   - ✅ `ExerciseParticipantDto` - [ParticipantDtos.cs](../../../src/Cadence.Core/Features/Exercises/Models/DTOs/ParticipantDtos.cs)
     - `UserId`: `Guid` → `string`
     - `GlobalRole` → `SystemRole`
     - `ExerciseRole` now required (not nullable)
   - ✅ `AddParticipantRequest` - `UserId`: `Guid` → `string`, Role required
   - ✅ `UpdateParticipantRoleRequest` - Role required (not nullable)

6. **Services Updated**:
   - ✅ `UserService` - [UserService.cs](../../../src/Cadence.Core/Features/Users/Services/UserService.cs)
     - Replaced `GlobalRole` with `SystemRole`
     - Updated validation to use enum
   - ✅ `AuthenticationService` - [AuthenticationService.cs](../../../src/Cadence.Core/Features/Authentication/Services/AuthenticationService.cs)
     - First user gets `SystemRole.Admin`
     - Subsequent users get `SystemRole.User`
     - JWT tokens include SystemRole
   - ✅ `IExerciseParticipantService` - [IExerciseParticipantService.cs](../../../src/Cadence.Core/Features/Exercises/Services/IExerciseParticipantService.cs)
     - All methods now use `string userId` parameter

---

## ⚠️ Remaining Work

### Phase 1: Fix Remaining Compilation Errors

#### 1. ExerciseParticipantService Implementation
**File:** `Features/Exercises/Services/ExerciseParticipantService.cs`

**Required Changes:**
```csharp
// UPDATE method signatures to match interface (Guid userId → string userId)
public async Task<ExerciseParticipantDto?> GetParticipantAsync(
    Guid exerciseId,
    string userId,  // Changed from Guid
    CancellationToken ct = default)

// UPDATE all User references to ApplicationUser
var user = await _userManager.FindByIdAsync(request.UserId);  // Now string
// or from context:
var user = await _context.ApplicationUsers.FindAsync(userId);  // userId is string

// UPDATE mapper to use ApplicationUser
private static ExerciseParticipantDto MapToDto(ExerciseParticipant participant, ApplicationUser user)
{
    return new ExerciseParticipantDto
    {
        UserId = participant.UserId,  // Already string
        Email = user.Email ?? string.Empty,
        DisplayName = user.DisplayName,
        SystemRole = user.SystemRole.ToString(),
        ExerciseRole = participant.Role.ToString()
    };
}
```

#### 2. DevelopmentDataSeeder
**File:** `Data/DevelopmentDataSeeder.cs`

**Required Changes:**
```csharp
// UPDATE all ExerciseParticipant seed data
new ExerciseParticipant
{
    Id = Guid.NewGuid(),
    ExerciseId = exerciseId,
    UserId = userId,  // Now string (from ApplicationUser.Id)
    Role = ExerciseRole.Controller,
    AssignedAt = DateTime.UtcNow,
    AssignedById = null,  // System-seeded
    CreatedBy = SystemConstants.SystemUserId,
    ModifiedBy = SystemConstants.SystemUserId,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
}

// GET ApplicationUser IDs (strings) instead of User IDs (Guids)
var adminUser = await context.ApplicationUsers
    .FirstOrDefaultAsync(u => u.SystemRole == SystemRole.Admin);
var userId = adminUser!.Id;  // string
```

#### 3. Controllers (if they call ExerciseParticipantService)
Any controllers that call participant service methods need their parameters updated:
```csharp
// OLD:
[HttpGet("{userId:guid}")]
public async Task<ActionResult<ExerciseParticipantDto>> GetParticipant(
    Guid exerciseId,
    Guid userId)

// NEW:
[HttpGet("{userId}")]
public async Task<ActionResult<ExerciseParticipantDto>> GetParticipant(
    Guid exerciseId,
    string userId)
```

---

### Phase 2: Create Database Migration

Once all compilation errors are fixed:

```bash
# Remove the empty migration
cd src/Cadence.Core
dotnet ef migrations remove

# Build to verify no errors
dotnet build

# Create new migration
dotnet ef migrations add SeparateSystemAndExerciseRoles

# Review the migration file
# Should include:
# - Rename AspNetUsers.GlobalRole → SystemRole
# - Change ExerciseParticipants.UserId type (uniqueidentifier → nvarchar(450))
# - Add ExerciseParticipants.AssignedAt
# - Add ExerciseParticipants.AssignedById
# - Update indexes
```

#### Data Migration Considerations

The migration will need custom SQL to handle existing data:

```sql
-- 1. Map old GlobalRole values to new SystemRole enum
UPDATE AspNetUsers
SET SystemRole = CASE
    WHEN GlobalRole = 'Administrator' THEN 'Admin'
    WHEN GlobalRole = 'ExerciseDirector' THEN 'Manager'
    ELSE 'User'
END;

-- 2. Update ExerciseParticipants.UserId (Guid → string)
-- This is complex because:
-- - Old UserId column references Users.Id (Guid)
-- - New UserId column must reference AspNetUsers.Id (string)
-- - Need to find matching users between tables

-- Option A: If Users table has Email that matches AspNetUsers.Email
UPDATE ep
SET ep.UserId = au.Id
FROM ExerciseParticipants ep
INNER JOIN Users u ON ep.UserId = CAST(u.Id AS NVARCHAR(450))
INNER JOIN AspNetUsers au ON u.Email = au.Email;

-- Option B: If no data exists yet, just change the column type
-- (safer for MVP if database is still in dev/test)
```

---

### Phase 3: Create Authorization Infrastructure

#### 1. Create RoleResolver Service

**File:** `Core/Features/Authorization/Services/IRoleResolver.cs`

```csharp
namespace Cadence.Core.Features.Authorization.Services;

/// <summary>
/// Resolves effective permissions for a user in a given context.
/// </summary>
public interface IRoleResolver
{
    /// <summary>
    /// Get user's HSEEP role for a specific exercise.
    /// Returns null if user is not a participant (unless Admin).
    /// </summary>
    Task<ExerciseRole?> GetExerciseRoleAsync(string userId, Guid exerciseId);

    /// <summary>
    /// Check if user can access an exercise.
    /// Admins can access all; others need assignment.
    /// </summary>
    Task<bool> CanAccessExerciseAsync(string userId, Guid exerciseId);

    /// <summary>
    /// Check if user has at least the specified role in an exercise.
    /// Admins always have Director-equivalent access.
    /// </summary>
    Task<bool> HasExerciseRoleAsync(
        string userId,
        Guid exerciseId,
        ExerciseRole minimumRole);
}
```

**File:** `Core/Features/Authorization/Services/RoleResolver.cs`

```csharp
public class RoleResolver : IRoleResolver
{
    private readonly AppDbContext _db;

    public RoleResolver(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ExerciseRole?> GetExerciseRoleAsync(string userId, Guid exerciseId)
    {
        var participant = await _db.ExerciseParticipants
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ExerciseId == exerciseId);

        return participant?.Role;
    }

    public async Task<bool> CanAccessExerciseAsync(string userId, Guid exerciseId)
    {
        // Admins can access all exercises
        var user = await _db.ApplicationUsers.FindAsync(userId);
        if (user?.SystemRole == SystemRole.Admin)
            return true;

        // Others need assignment
        return await _db.ExerciseParticipants
            .AnyAsync(p => p.UserId == userId && p.ExerciseId == exerciseId);
    }

    public async Task<bool> HasExerciseRoleAsync(
        string userId,
        Guid exerciseId,
        ExerciseRole minimumRole)
    {
        // Check system role first
        var user = await _db.ApplicationUsers.FindAsync(userId);
        if (user?.SystemRole == SystemRole.Admin)
            return true;  // Admins have Director-equivalent access

        var role = await GetExerciseRoleAsync(userId, exerciseId);
        if (role == null)
            return false;  // Not a participant

        // Compare role levels (higher enum value = more permissions)
        return role >= minimumRole;
    }
}
```

#### 2. Create Authorization Policies

**File:** `Core/Extensions/AuthorizationExtensions.cs`

```csharp
using Microsoft.AspNetCore.Authorization;

namespace Cadence.Core.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddCadenceAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // System-level policies
            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("SystemRole", "Admin")));

            options.AddPolicy("RequireManager", policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("SystemRole", "Admin") ||
                    ctx.User.HasClaim("SystemRole", "Manager")));

            // Exercise-level policies (require exercise ID from route)
            options.AddPolicy("ExerciseAccess", policy =>
                policy.Requirements.Add(new ExerciseAccessRequirement()));

            options.AddPolicy("ExerciseController", policy =>
                policy.Requirements.Add(new ExerciseRoleRequirement(ExerciseRole.Controller)));

            options.AddPolicy("ExerciseDirector", policy =>
                policy.Requirements.Add(new ExerciseRoleRequirement(ExerciseRole.Director)));
        });

        services.AddScoped<IAuthorizationHandler, ExerciseAccessHandler>();
        services.AddScoped<IAuthorizationHandler, ExerciseRoleHandler>();
        services.AddScoped<IRoleResolver, RoleResolver>();

        return services;
    }
}
```

#### 3. Create Authorization Handlers

**File:** `Core/Features/Authorization/Requirements/ExerciseAccessRequirement.cs`

```csharp
public class ExerciseAccessRequirement : IAuthorizationRequirement { }

public class ExerciseAccessHandler : AuthorizationHandler<ExerciseAccessRequirement>
{
    private readonly IRoleResolver _roleResolver;
    private readonly IHttpContextAccessor _httpContextAccessor;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ExerciseAccessRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var exerciseId = GetExerciseIdFromRoute();

        if (userId == null || exerciseId == null)
        {
            context.Fail();
            return;
        }

        if (await _roleResolver.CanAccessExerciseAsync(userId, exerciseId.Value))
        {
            context.Succeed(requirement);
        }
    }

    private Guid? GetExerciseIdFromRoute()
    {
        var routeData = _httpContextAccessor.HttpContext?.GetRouteData();
        if (routeData?.Values.TryGetValue("exerciseId", out var value) == true)
        {
            if (Guid.TryParse(value?.ToString(), out var id))
                return id;
        }
        return null;
    }
}
```

#### 4. Register in Program.cs

```csharp
// Add after other service registrations
builder.Services.AddCadenceAuthorization();
builder.Services.AddScoped<IRoleResolver, RoleResolver>();
```

---

### Phase 4: Update Exercise Creation Logic

When a Manager creates an exercise, automatically assign them as Director:

**File:** `Features/Exercises/Services/ExerciseService.cs`

```csharp
public async Task<ExerciseDto> CreateExerciseAsync(CreateExerciseRequest request, string createdByUserId)
{
    // Create exercise
    var exercise = new Exercise
    {
        // ... other properties ...
        CreatedById = createdByUserId  // Track ownership
    };

    _context.Exercises.Add(exercise);
    await _context.SaveChangesAsync();

    // Auto-assign creator as Director (if Manager or Admin)
    var user = await _context.ApplicationUsers.FindAsync(createdByUserId);
    if (user != null && (user.SystemRole == SystemRole.Manager || user.SystemRole == SystemRole.Admin))
    {
        var participant = new ExerciseParticipant
        {
            ExerciseId = exercise.Id,
            UserId = createdByUserId,
            Role = ExerciseRole.ExerciseDirector,
            AssignedAt = DateTime.UtcNow,
            AssignedById = createdByUserId  // Self-assigned
        };
        _context.ExerciseParticipants.Add(participant);
        await _context.SaveChangesAsync();
    }

    return exercise.ToDto();
}
```

---

### Phase 5: Testing

#### Unit Tests

**File:** `Tests/Features/Authorization/RoleResolverTests.cs`

```csharp
public class RoleResolverTests
{
    [Fact]
    public async Task Admin_CanAccessAnyExercise()
    {
        // Arrange: Admin user not assigned to exercise
        // Act: CanAccessExerciseAsync
        // Assert: Returns true
    }

    [Fact]
    public async Task User_CanOnlyAccessAssignedExercises()
    {
        // Arrange: User assigned to Exercise A but not B
        // Act: CanAccessExerciseAsync for both
        // Assert: True for A, False for B
    }

    [Fact]
    public async Task Manager_BecomesDirectorOnCreate()
    {
        // Arrange: Manager creates exercise
        // Act: Check participant assignment
        // Assert: Has Director role
    }
}
```

---

## Quick Reference

### Key Type Changes

| Entity/DTO | Old Type | New Type |
|------------|----------|----------|
| ApplicationUser.GlobalRole | `string` | `SystemRole` enum |
| ExerciseParticipant.UserId | `Guid` | `string` |
| UserDto.Id | `Guid` | `string` |
| UserDto.Role | `string` | `string` (but from SystemRole enum) |
| ExerciseParticipantDto.UserId | `Guid` | `string` |
| ExerciseParticipantDto.GlobalRole | `string` | Renamed to `SystemRole` |

### Enum Values

**SystemRole:**
- `User` = 0 (standard access)
- `Manager` = 1 (can create exercises)
- `Admin` = 2 (full system access)

**ExerciseRole (unchanged):**
- `Administrator` = 1 (deprecated for exercise context)
- `ExerciseDirector` = 2
- `Controller` = 3
- `Evaluator` = 4
- `Observer` = 5

---

## Estimated Effort

| Phase | Estimated Time | Status |
|-------|----------------|--------|
| Phase 1: Fix Compilation Errors | 1-2 hours | 70% complete |
| Phase 2: Database Migration | 30 min | Not started |
| Phase 3: Authorization Infrastructure | 2-3 hours | Not started |
| Phase 4: Exercise Creation Logic | 30 min | Not started |
| Phase 5: Testing | 2-3 hours | Not started |
| **Total** | **6-9 hours** | **35% complete** |

---

## Files to Track

### ✅ Completed
- [x] Enums.cs
- [x] ApplicationUser.cs
- [x] ExerciseParticipant.cs
- [x] AppDbContext.cs
- [x] UserDtos.cs
- [x] UserService.cs
- [x] AuthenticationService.cs
- [x] IExerciseParticipantService.cs
- [x] ParticipantDtos.cs

### ⚠️ In Progress
- [ ] ExerciseParticipantService.cs (implementation needs updating)
- [ ] DevelopmentDataSeeder.cs (seed data needs updating)

### 📝 Not Started
- [ ] IRoleResolver.cs (new file)
- [ ] RoleResolver.cs (new file)
- [ ] AuthorizationExtensions.cs (new file)
- [ ] ExerciseAccessRequirement.cs (new file)
- [ ] ExerciseRoleRequirement.cs (new file)
- [ ] ExerciseService.cs (auto-assign Director logic)
- [ ] Migration file (after compilation fixes)
- [ ] Unit tests

---

## Quick Start Commands

```bash
# 1. Check current build status
cd src/Cadence.Core
dotnet build 2>&1 | grep "error CS"

# 2. After fixing errors, create migration
dotnet ef migrations remove  # Remove empty migration
dotnet ef migrations add SeparateSystemAndExerciseRoles

# 3. Review migration before applying
# Check: Migrations/[timestamp]_SeparateSystemAndExerciseRoles.cs

# 4. Apply migration
dotnet ef database update

# 5. Run tests
cd ../Cadence.Core.Tests
dotnet test
```

---

## Resources

- [AUTHORIZATION_IMPLEMENTATION_STATUS.md](./AUTHORIZATION_IMPLEMENTATION_STATUS.md) - Detailed implementation progress
- [CLAUDE.md](../../../CLAUDE.md) - Project guidelines
- [ASP.NET Core Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/)
