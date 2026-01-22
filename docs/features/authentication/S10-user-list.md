# S10: View User List

## Story

**As an** Administrator,
**I want** to see all users in the system,
**So that** I can manage the user base and verify roles.

## Context

Administrators need visibility into who has access to Cadence. The user list provides an overview of all accounts, their roles, and status. This is essential for access control and compliance.

## Acceptance Criteria

- [ ] **Given** I am an Administrator, **when** I navigate to Settings > Users, **then** I see a list of all users
- [ ] **Given** I view the user list, **when** I look at each row, **then** I see: Display Name, Email, Role, Status, Last Login
- [ ] **Given** the list has many users, **when** I scroll, **then** the list is paginated (20 per page)
- [ ] **Given** I am looking for a user, **when** I type in the search box, **then** users are filtered by name or email
- [ ] **Given** I want to filter by role, **when** I select a role filter, **then** only users with that role are shown
- [ ] **Given** I am not an Administrator, **when** I try to access this page, **then** I am redirected or shown "Access Denied"

## Out of Scope

- Bulk user operations
- Export user list
- User activity logs

## Dependencies

- Authentication complete
- Role-based access control

## Domain Terms

| Term | Definition |
|------|------------|
| User Status | Active or Deactivated |
| Last Login | Most recent successful authentication timestamp |

## API Contract

**Endpoint:** `GET /api/users`

**Query Parameters:**
- `page` (int, default 1)
- `pageSize` (int, default 20, max 100)
- `search` (string, optional)
- `role` (string, optional)

**Success Response (200 OK):**
```json
{
  "users": [
    {
      "id": "guid",
      "displayName": "Jane Smith",
      "email": "jane@example.com",
      "role": "Controller",
      "status": "Active",
      "lastLogin": "2025-01-20T14:30:00Z",
      "createdAt": "2025-01-01T09:00:00Z"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 45,
    "totalPages": 3
  }
}
```

## UI/UX Notes

- Table layout with sortable columns
- Search with debounce (300ms)
- Role filter as dropdown
- Status indicator: green dot for Active, gray for Deactivated
- Click row to view/edit user details

---

*Story created: 2025-01-21*
