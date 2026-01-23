# S15: Role Inheritance & Resolution

## Story

**As a** user with roles at different levels,
**I want** my effective permissions resolved correctly,
**So that** I have the right access in each context.

## Context

When a user accesses an exercise, the system must determine their effective role by checking for an exercise-specific assignment first, then falling back to their global role. This resolution must be fast (cached) and consistent across frontend and backend.

## Acceptance Criteria

- [ ] **Given** I have no exercise role assigned, **when** I access an exercise, **then** my global role is used
- [ ] **Given** I have an exercise role "Controller", **when** I access that exercise, **then** Controller permissions apply
- [ ] **Given** I have global role "Controller", **when** I access an exercise where I'm "Observer", **then** Observer permissions apply (override)
- [ ] **Given** my role changes, **when** I make my next API call, **then** the new role is enforced (no stale cache)
- [ ] **Given** I am offline, **when** I access a cached exercise, **then** my last-known role is used
- [ ] **Given** I view my profile, **when** I look at "My Exercises", **then** I see my role for each exercise

## Out of Scope

- Role inheritance between exercises
- Permission caching strategies (technical detail)
- Custom permission overrides beyond role

## Dependencies

- S13 (Global Role Assignment)
- S14 (Exercise Role Assignment)

## Domain Terms

| Term | Definition |
|------|------------|
| Role Resolution | Process of determining effective role from global + exercise roles |
| Role Precedence | Exercise role > Global role |
| Effective Permissions | The actual permissions a user has after role resolution |

## Technical Notes

```csharp
// Role resolution service
public class RoleResolver : IRoleResolver
{
    public async Task<string> GetEffectiveRole(Guid userId, Guid exerciseId)
    {
        // Check exercise-specific role first
        var participant = await _db.ExerciseParticipants
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ExerciseId == exerciseId);
        
        if (participant != null)
            return participant.Role;
        
        // Fall back to global role
        var user = await _db.Users.FindAsync(userId);
        return user.Role;
    }
}

// Authorization policy example
public class ExerciseRoleRequirement : IAuthorizationRequirement
{
    public string[] AllowedRoles { get; }
    
    public ExerciseRoleRequirement(params string[] roles)
    {
        AllowedRoles = roles;
    }
}

public class ExerciseRoleHandler : AuthorizationHandler<ExerciseRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ExerciseRoleRequirement requirement)
    {
        var exerciseId = GetExerciseIdFromRoute();
        var userId = context.User.GetUserId();
        
        var effectiveRole = await _roleResolver.GetEffectiveRole(userId, exerciseId);
        
        if (requirement.AllowedRoles.Contains(effectiveRole))
        {
            context.Succeed(requirement);
        }
    }
}
```

## Frontend Implementation

```typescript
// Role context hook
function useExerciseRole(exerciseId: string) {
  const { user } = useAuth();
  const [effectiveRole, setEffectiveRole] = useState<string>(user.globalRole);
  
  useEffect(() => {
    const participant = user.exerciseRoles?.[exerciseId];
    setEffectiveRole(participant?.role ?? user.globalRole);
  }, [exerciseId, user]);
  
  const can = useCallback((permission: Permission) => {
    return hasPermission(effectiveRole, permission);
  }, [effectiveRole]);
  
  return { effectiveRole, can };
}

// Usage in component
function InjectActions({ inject }) {
  const { can } = useExerciseRole(inject.exerciseId);
  
  return (
    <>
      {can('fire_inject') && <FireButton inject={inject} />}
      {can('edit_inject') && <EditButton inject={inject} />}
    </>
  );
}
```

## UI/UX Notes

- Show effective role in exercise header
- Tooltip explaining "Your role in this exercise"
- Disabled actions show tooltip "Requires Controller role"
- User profile shows role breakdown per exercise

---

*Story created: 2025-01-21*
