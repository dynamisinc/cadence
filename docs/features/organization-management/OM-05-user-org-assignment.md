# Story OM-05: User-Organization Assignment

**Priority:** P0 (MVP)  
**Feature:** Organization Management  
**Created:** 2025-01-29

---

## User Story

**As a** System Administrator,  
**I want** to assign pending users to organizations and manage organization memberships,  
**So that** new users can access the platform and existing users can be moved between organizations as needed.

---

## Context

When users self-register without an organization code, they enter a "Pending" state. SysAdmins need to review these users and assign them to appropriate organizations. Additionally, SysAdmins may need to:

- Add existing users to additional organizations
- Remove users from organizations
- Change a user's role within an organization

OrgAdmins have limited membership management (covered in OM-07 Invite User).

---

## Acceptance Criteria

### View Pending Users

- [ ] **Given** I am a SysAdmin, **when** I navigate to User Management, **then** I see a list of all users with their status and organization memberships
- [ ] **Given** there are pending users, **when** viewing the user list, **then** I can filter to show only "Pending" status users
- [ ] **Given** I view a pending user, **when** looking at their details, **then** I see their registration date and email

### Assign User to Organization

- [ ] **Given** I am viewing a pending user, **when** I click "Assign to Organization", **then** I see a dialog to select an organization and role
- [ ] **Given** I am assigning a user, **when** I select an organization from the dropdown, **then** I see only Active organizations
- [ ] **Given** I am assigning a user, **when** I select a role, **then** I can choose from: OrgAdmin, OrgManager, OrgUser
- [ ] **Given** I complete the assignment, **when** I confirm, **then** the user is added to that organization with the selected role
- [ ] **Given** a pending user is assigned their first organization, **when** assignment completes, **then** their status changes from "Pending" to "Active"

### Add User to Additional Organization

- [ ] **Given** I am viewing an active user with existing org memberships, **when** I click "Add to Organization", **then** I can assign them to another organization
- [ ] **Given** I am adding to an org, **when** viewing the org dropdown, **then** organizations the user already belongs to are disabled/excluded
- [ ] **Given** I complete adding to an org, **when** assignment completes, **then** the user now has memberships in multiple organizations

### Remove User from Organization

- [ ] **Given** I am viewing a user's organization memberships, **when** I click "Remove" next to an org, **then** I see a confirmation dialog
- [ ] **Given** I confirm removal, **when** the action completes, **then** the user is removed from that organization
- [ ] **Given** I remove a user's last organization, **when** removal completes, **then** their status changes to "Pending"
- [ ] **Given** I am removing an OrgAdmin, **when** they are the only OrgAdmin, **then** I see a warning "This is the only administrator for this organization"

### Change User's Role in Organization

- [ ] **Given** I am viewing a user's organization membership, **when** I click on their role, **then** I can change it to a different role
- [ ] **Given** I change a role, **when** the change is saved, **then** the new role takes effect immediately
- [ ] **Given** I am demoting the only OrgAdmin, **when** I try to save, **then** I see an error "Organization must have at least one administrator"

### Bulk Assignment (Optional Enhancement)

- [ ] **Given** I have selected multiple pending users, **when** I click "Assign Selected", **then** I can assign all of them to the same organization with the same role

### User Search and Filter

- [ ] **Given** I am on the user list, **when** I type in the search box, **then** users are filtered by name or email
- [ ] **Given** I am on the user list, **when** I filter by organization, **then** I see only users belonging to that organization
- [ ] **Given** I am on the user list, **when** I filter by status, **then** I see only users with that status (Pending, Active, Disabled)

### Notifications

- [ ] **Given** a user is assigned to an organization, **when** assignment completes, **then** the user receives an email notification (if configured)
- [ ] **Given** assignment succeeds, **when** the action completes, **then** I see a success toast message

---

## Out of Scope

- User self-service organization requests
- Organization transfer (moving all users from one org to another)
- User account creation by SysAdmin (use invite flow instead)
- Disabling/deleting user accounts (separate story)
- Role hierarchy enforcement beyond OrgAdmin minimum

---

## Dependencies

- OM-02: Create Organization (organizations must exist)
- User registration system
- Email notification service (optional)

---

## Domain Terms

| Term | Definition |
|------|------------|
| Pending User | Registered user not yet assigned to any organization |
| Active User | User with at least one organization membership |
| Organization Membership | Association between user and organization with a specific role |
| OrgRole | Role within an organization: OrgAdmin, OrgManager, OrgUser |

---

## UI/UX Notes

### User List View
```
┌─────────────────────────────────────────────────────────────────────────┐
│ User Management                                       [+ Invite User]   │
├─────────────────────────────────────────────────────────────────────────┤
│ 🔍 Search users...    Status: [All ▼]    Organization: [All ▼]         │
├─────────────────────────────────────────────────────────────────────────┤
│ ☐ │ Name           │ Email                │ Status  │ Organizations     │
├───┼────────────────┼──────────────────────┼─────────┼───────────────────┤
│ ☐ │ John Smith     │ john@cisa.gov        │ 🟢 Active│ CISA (Admin)      │
│ ☐ │ Jane Doe       │ jane@state.gov       │ 🟢 Active│ State EMA (User)  │
│   │                │                      │         │ FEMA (Manager)    │
│ ☐ │ New Person     │ new@example.com      │ 🟡 Pending│ [Assign →]        │
│ ☐ │ Bob Wilson     │ bob@company.com      │ 🔴 Disabled│ Acme (User)      │
└───┴────────────────┴──────────────────────┴─────────┴───────────────────┘

Selected: 1                                    [Assign Selected to Org...]
```

### Assign to Organization Dialog
```
┌─────────────────────────────────────────────────┐
│ Assign User to Organization               [X]   │
├─────────────────────────────────────────────────┤
│                                                 │
│ User: new@example.com                          │
│                                                 │
│ Organization *                                  │
│ ┌─────────────────────────────────────────┐    │
│ │ Select organization...              ▼   │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Role *                                          │
│ ○ Organization Administrator                    │
│   Full access to manage organization settings,  │
│   users, and all exercises                     │
│                                                 │
│ ○ Organization Manager                          │
│   Can create and manage exercises              │
│                                                 │
│ ● Organization User                             │
│   Can participate in assigned exercises         │
│                                                 │
│                    [Cancel]  [Assign User]      │
│                                                 │
└─────────────────────────────────────────────────┘
```

### User Detail / Memberships Panel
```
┌─────────────────────────────────────────────────────────────────┐
│ User: John Smith                                                │
│ Email: john@cisa.gov                                           │
│ Status: 🟢 Active                                               │
│ Registered: January 10, 2025                                    │
├─────────────────────────────────────────────────────────────────┤
│ Organization Memberships                   [+ Add to Org]       │
├─────────────────────────────────────────────────────────────────┤
│ Organization      │ Role          │ Joined      │ Actions       │
├───────────────────┼───────────────┼─────────────┼───────────────┤
│ CISA Region 4     │ [Admin ▼]     │ Jan 15      │ [Remove]      │
│ State EMA         │ [User ▼]      │ Jan 20      │ [Remove]      │
└───────────────────┴───────────────┴─────────────┴───────────────┘
```

### Role Dropdown (Inline Edit)
```
┌──────────────────┐
│ [Admin ▼]        │
├──────────────────┤
│ ✓ Admin          │
│   Manager        │
│   User           │
└──────────────────┘
```

### Warning States

**Only OrgAdmin Warning:**
```
┌─────────────────────────────────────────────────┐
│ ⚠️ Warning                                      │
│                                                 │
│ John Smith is the only administrator for       │
│ CISA Region 4. Removing them or changing their │
│ role will leave the organization without an    │
│ admin.                                          │
│                                                 │
│ • Assign another admin first, or               │
│ • Archive the organization                     │
│                                                 │
│                              [Cancel] [Proceed] │
└─────────────────────────────────────────────────┘
```

---

## Technical Notes

### API Endpoints

**List Users (SysAdmin):**
```
GET /api/admin/users
Authorization: Bearer {token} (SysAdmin only)

Query Parameters:
  - search: string (name or email)
  - status: Pending|Active|Disabled
  - organizationId: guid (filter by org)
  - page: int
  - pageSize: int

Response:
{
  "items": [
    {
      "id": "guid",
      "email": "john@cisa.gov",
      "firstName": "John",
      "lastName": "Smith",
      "status": "Active",
      "registeredAt": "2025-01-10T10:00:00Z",
      "memberships": [
        {
          "organizationId": "guid",
          "organizationName": "CISA Region 4",
          "role": "OrgAdmin",
          "joinedAt": "2025-01-15T10:00:00Z"
        }
      ]
    }
  ],
  "totalCount": 50,
  "page": 1,
  "pageSize": 20
}
```

**Assign User to Organization:**
```
POST /api/admin/users/{userId}/memberships
Authorization: Bearer {token} (SysAdmin only)

Request:
{
  "organizationId": "guid",
  "role": "OrgUser"
}

Response (201 Created):
{
  "id": "guid",
  "userId": "guid",
  "organizationId": "guid",
  "organizationName": "CISA Region 4",
  "role": "OrgUser",
  "joinedAt": "2025-01-29T15:30:00Z"
}
```

**Update Membership Role:**
```
PUT /api/admin/users/{userId}/memberships/{membershipId}
Authorization: Bearer {token} (SysAdmin only)

Request:
{
  "role": "OrgManager"
}

Response (200 OK):
{
  "id": "guid",
  "role": "OrgManager"
}
```

**Remove Membership:**
```
DELETE /api/admin/users/{userId}/memberships/{membershipId}
Authorization: Bearer {token} (SysAdmin only)

Response (200 OK):
{
  "removed": true,
  "userStatusChanged": true,  // if now pending
  "newUserStatus": "Pending"
}
```

### Business Rules

```csharp
public class MembershipService
{
    public async Task ValidateMembershipChange(OrganizationMembership membership, OrgRole newRole)
    {
        // Rule: Organization must have at least one OrgAdmin
        if (membership.Role == OrgRole.OrgAdmin && newRole != OrgRole.OrgAdmin)
        {
            var adminCount = await _db.Memberships
                .CountAsync(m => m.OrganizationId == membership.OrganizationId 
                              && m.Role == OrgRole.OrgAdmin);
            
            if (adminCount <= 1)
                throw new BusinessRuleException("Organization must have at least one administrator");
        }
    }
    
    public async Task UpdateUserStatusAfterMembershipChange(User user)
    {
        var hasMemberships = await _db.Memberships.AnyAsync(m => m.UserId == user.Id);
        user.Status = hasMemberships ? UserStatus.Active : UserStatus.Pending;
    }
}
```

---

## Test Scenarios

| Scenario | Test Type | Priority |
|----------|-----------|----------|
| Assign pending user to org | Integration | P0 |
| Pending user becomes Active after assignment | Integration | P0 |
| Assign active user to additional org | Integration | P0 |
| Remove user from org | Integration | P0 |
| User becomes Pending when last org removed | Integration | P0 |
| Change user role in org | Integration | P0 |
| Cannot remove only OrgAdmin | Integration | P0 |
| Cannot demote only OrgAdmin | Integration | P0 |
| Search users by email | Integration | P1 |
| Filter users by status | Integration | P1 |
| Filter users by organization | Integration | P1 |
| Non-SysAdmin cannot access | Integration | P0 |

---

## Implementation Checklist

### Backend
- [ ] Create `GET /api/admin/users` endpoint with filters
- [ ] Create `POST /api/admin/users/{userId}/memberships` endpoint
- [ ] Create `PUT /api/admin/users/{userId}/memberships/{membershipId}` endpoint
- [ ] Create `DELETE /api/admin/users/{userId}/memberships/{membershipId}` endpoint
- [ ] Implement OrgAdmin minimum validation
- [ ] Implement user status auto-update logic
- [ ] Add email notification for assignment (optional)
- [ ] Unit tests for business rules
- [ ] Integration tests for endpoints

### Frontend
- [ ] Create `UserListPage` component
- [ ] Create `UserTable` component with expandable rows
- [ ] Create `AssignToOrgDialog` component
- [ ] Create `UserDetailPanel` component (sidebar or modal)
- [ ] Create `MembershipTable` component
- [ ] Add inline role editing
- [ ] Add membership removal with confirmation
- [ ] Add bulk selection and assignment
- [ ] Add search and filter controls
- [ ] Add route `/admin/users`
- [ ] Component tests

---

## Changelog

| Date | Change |
|------|--------|
| 2025-01-29 | Initial story creation |
