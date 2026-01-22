# S13: Global Role Assignment

## Story

**As an** Administrator,
**I want** to assign a default role to users,
**So that** they have baseline permissions across all exercises they're invited to.

## Context

Global roles define what a user can do by default. When a user joins an exercise without a specific role assignment, their global role determines their permissions. This simplifies onboarding - most Controllers will be Controllers for all exercises.

## Acceptance Criteria

- [ ] **Given** I am an Administrator, **when** I view the user list, **then** I see each user's global role
- [ ] **Given** I am an Administrator, **when** I click to edit a user, **then** I can change their global role
- [ ] **Given** I select a new global role, **when** I save, **then** the user's role is updated
- [ ] **Given** I change a user's global role, **when** the change saves, **then** it takes effect immediately (no re-login)
- [ ] **Given** I am not an Administrator, **when** I view user details, **then** I cannot change their role
- [ ] **Given** I am changing my own role, **when** I try to demote from Administrator, **then** I see a warning (must have at least one Admin)
- [ ] **Given** only one Administrator exists, **when** I try to demote them, **then** the action is blocked with explanation

## Out of Scope

- Bulk role assignment
- Role request workflow
- Role assignment history/audit

## Dependencies

- S10 (User List)

## Domain Terms

| Term | Definition |
|------|------------|
| Global Role | Default role used when no exercise-specific role is assigned |
| Role Demotion | Changing a user to a lower-privilege role |
| Administrator Constraint | System must always have at least one Administrator |

## API Contract

**Endpoint:** `PATCH /api/users/{userId}/role`

**Request:**
```json
{
  "role": "Controller"
}
```

**Success Response (200 OK):**
```json
{
  "userId": "guid-here",
  "displayName": "Jane Smith",
  "email": "jane@example.com",
  "role": "Controller",
  "updatedAt": "2025-01-21T12:00:00Z"
}
```

**Error Response (400 Bad Request - last admin):**
```json
{
  "error": "last_administrator",
  "message": "Cannot remove the last Administrator. Assign another Administrator first."
}
```

## Technical Notes

```csharp
// Role assignment with last-admin protection
public async Task<Result> AssignGlobalRole(Guid userId, string newRole)
{
    if (newRole != Roles.Administrator)
    {
        var adminCount = await _db.Users
            .Where(u => u.Role == Roles.Administrator && u.Id != userId)
            .CountAsync();
            
        if (adminCount == 0)
            return Result.Failure("Cannot remove the last Administrator");
    }
    
    var user = await _db.Users.FindAsync(userId);
    user.Role = newRole;
    await _db.SaveChangesAsync();
    
    // Invalidate cached claims (SignalR broadcast)
    await _hubContext.Clients.User(userId.ToString())
        .SendAsync("RoleChanged", newRole);
    
    return Result.Success();
}
```

## UI/UX Notes

- Role selector is a dropdown with all five roles
- Current role is pre-selected
- Save button only enabled when role changes
- Warning dialog for role demotions
- Toast on successful change

---

*Story created: 2025-01-21*
