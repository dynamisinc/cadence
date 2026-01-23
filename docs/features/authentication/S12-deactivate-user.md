# S12: Deactivate User Account

## Story

**As an** Administrator,
**I want** to deactivate user accounts,
**So that** former team members can no longer access Cadence.

## Context

When someone leaves the team or no longer needs access, their account should be deactivated rather than deleted. This preserves audit trails and allows reactivation if needed. Deactivated users cannot login.

## Acceptance Criteria

- [ ] **Given** I am viewing a user, **when** I click "Deactivate", **then** I see a confirmation dialog
- [ ] **Given** I confirm deactivation, **when** the action completes, **then** the user's status changes to "Deactivated"
- [ ] **Given** a user is deactivated, **when** they try to login, **then** they see "Account deactivated. Contact administrator."
- [ ] **Given** a user is deactivated, **when** I view them in the list, **then** they show with a "Deactivated" badge
- [ ] **Given** I am viewing a deactivated user, **when** I click "Reactivate", **then** their account is restored
- [ ] **Given** I try to deactivate myself, **when** I click Deactivate, **then** the action is blocked with explanation
- [ ] **Given** I try to deactivate the last Administrator, **when** I click Deactivate, **then** the action is blocked

## Out of Scope

- Hard delete (permanent removal)
- Deactivation reason tracking
- Automatic deactivation after inactivity

## Dependencies

- S11 (Edit User)
- S04 (Login - check status)

## Domain Terms

| Term | Definition |
|------|------------|
| Deactivate | Soft disable of account - user cannot login but data is preserved |
| Reactivate | Restore a deactivated account to active status |

## API Contract

**Endpoint:** `POST /api/users/{userId}/deactivate`

**Success Response (200 OK):**
```json
{
  "id": "guid",
  "status": "Deactivated",
  "deactivatedAt": "2025-01-21T12:00:00Z",
  "deactivatedBy": "admin-guid"
}
```

**Endpoint:** `POST /api/users/{userId}/reactivate`

**Success Response (200 OK):**
```json
{
  "id": "guid",
  "status": "Active",
  "reactivatedAt": "2025-01-21T12:00:00Z",
  "reactivatedBy": "admin-guid"
}
```

## Technical Notes

```csharp
// Login check for deactivated status
public async Task<AuthResult> AuthenticateAsync(LoginRequest request)
{
    var user = await _userManager.FindByEmailAsync(request.Email);
    
    if (user?.Status == UserStatus.Deactivated)
    {
        return AuthResult.Failure(new AuthError
        {
            Code = "account_deactivated",
            Message = "Account deactivated. Contact administrator."
        });
    }
    
    // Continue with normal auth flow...
}
```

## UI/UX Notes

- Deactivate button in red (destructive action)
- Confirmation dialog: "Are you sure you want to deactivate [Name]? They will no longer be able to login."
- Reactivate button is less prominent
- Filter option to show/hide deactivated users

---

*Story created: 2025-01-21*
