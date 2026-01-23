# S03: First User Becomes Administrator

## Story

**As the** first person to deploy Cadence,
**I want** my account to automatically become Administrator,
**So that** I can configure the system without manual database intervention.

## Context

When Cadence is deployed fresh, someone needs to be able to manage users and configure the system. Rather than requiring database seeding or manual SQL, the first user to register automatically receives Administrator privileges. This eliminates deployment friction and follows the "first user wins" bootstrap pattern.

## Acceptance Criteria

- [ ] **Given** no users exist in the database, **when** I register, **then** I am assigned the Administrator role
- [ ] **Given** one or more users exist, **when** I register, **then** I am assigned the Observer role (default)
- [ ] **Given** I am the first user, **when** my registration completes, **then** I see a welcome message indicating I am the Administrator
- [ ] **Given** two users register simultaneously on empty database, **when** both complete, **then** exactly one is Administrator (race condition handled)
- [ ] **Given** I am the Administrator, **when** I view my profile, **then** I see my role as "Administrator"

## Out of Scope

- Transferring Administrator role (handled in User Management)
- Multiple Administrators (future consideration)
- Configuration wizard for first Administrator

## Dependencies

- S02 (Validate and Save User)

## Domain Terms

| Term | Definition |
|------|------------|
| Administrator | Highest privilege role; can manage users, all exercises, system settings |
| Bootstrap | Initial system setup process without manual configuration |
| First User | The first account created on a fresh Cadence deployment |

## Technical Notes

```csharp
// Pseudo-code for first-user check
public async Task<string> DetermineRoleForNewUser()
{
    var userCount = await _userManager.Users.CountAsync();
    return userCount == 0 ? Roles.Administrator : Roles.Observer;
}
```

- Use database transaction to prevent race condition
- Consider `SELECT COUNT(*) ... FOR UPDATE` or equivalent locking
- Log first-admin assignment as security event

## Open Questions

- [x] Should first user see a special onboarding flow? **Decision: No, keep simple for MVP**
- [x] Can Administrator role be demoted? **Decision: Yes, but must always have at least one Admin**

---

*Story created: 2025-01-21*
