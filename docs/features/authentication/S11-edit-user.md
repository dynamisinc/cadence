# S11: Edit User Details

## Story

**As an** Administrator,
**I want** to edit user information,
**So that** I can correct mistakes or update roles.

## Context

User details may need updating - display names change, roles need adjustment. Administrators should be able to make these changes without requiring users to do it themselves or requiring database access.

## Acceptance Criteria

- [ ] **Given** I click a user in the list, **when** the detail view opens, **then** I see editable fields
- [ ] **Given** I am editing a user, **when** I view the form, **then** I can change: Display Name, Role
- [ ] **Given** I change the display name, **when** I save, **then** the new name is persisted
- [ ] **Given** I change the role, **when** I save, **then** the new role takes effect immediately
- [ ] **Given** I am editing my own account, **when** I try to demote from Admin, **then** I see a warning
- [ ] **Given** I make changes, **when** I click Cancel, **then** changes are discarded
- [ ] **Given** I save changes, **when** the save completes, **then** I see a success toast

## Out of Scope

- Changing email address (identity)
- Resetting password (separate story if needed)
- Viewing user's exercise participation

## Dependencies

- S10 (User List)
- S13 (Role Assignment)

## Domain Terms

| Term | Definition |
|------|------------|
| Display Name | User's preferred name shown in UI |
| Email | User's login identifier (not editable) |

## API Contract

**Endpoint:** `PATCH /api/users/{userId}`

**Request:**
```json
{
  "displayName": "Jane Doe",
  "role": "Exercise Director"
}
```

**Success Response (200 OK):**
```json
{
  "id": "guid",
  "displayName": "Jane Doe",
  "email": "jane@example.com",
  "role": "Exercise Director",
  "status": "Active",
  "updatedAt": "2025-01-21T12:00:00Z"
}
```

## UI/UX Notes

- Slide-over panel or modal for edit form
- Email shown but disabled/readonly
- Role as dropdown
- Save/Cancel buttons
- Unsaved changes warning on close

---

*Story created: 2025-01-21*
