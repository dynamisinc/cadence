# Story OM-07: Invite User to Organization

**Priority:** P1 (Standard)  
**Feature:** Organization Management  
**Created:** 2025-01-29

---

## User Story

**As an** Organization Administrator,  
**I want** to invite users to join my organization,  
**So that** I can onboard team members without requiring SysAdmin involvement.

---

## Context

OrgAdmins need autonomy to manage their organization's users. This story covers direct email invitations where the OrgAdmin invites a specific person by email address.

**Two invitation types:**
1. **Direct invite** (this story) - Email sent to specific person
2. **Organization code** (OM-08) - Shareable code anyone can use

---

## Acceptance Criteria

### Invite Form Access

- [ ] **Given** I am an OrgAdmin, **when** I navigate to Organization Users, **then** I see an "Invite User" button
- [ ] **Given** I am an OrgManager or OrgUser, **when** I navigate to Organization Users, **then** I do not see the "Invite User" button
- [ ] **Given** my organization is Archived, **when** I try to invite users, **then** I see an error "Cannot invite users to an archived organization"

### Invite Creation

- [ ] **Given** I click "Invite User", **when** the form opens, **then** I see fields for Email and Role
- [ ] **Given** I am creating an invite, **when** I enter an email, **then** it must be a valid email format
- [ ] **Given** I am creating an invite, **when** I select a role, **then** I can choose from: OrgAdmin, OrgManager, OrgUser
- [ ] **Given** I am an OrgAdmin, **when** selecting a role, **then** I can invite users as OrgAdmin (can invite admins)
- [ ] **Given** I am creating an invite, **when** I submit with valid data, **then** an invitation is created

### Email Notification

- [ ] **Given** an invitation is created, **when** the email is valid, **then** an invitation email is sent to the recipient
- [ ] **Given** an invitation email is sent, **when** the recipient opens it, **then** they see the organization name, inviter name, and a link to accept
- [ ] **Given** the recipient clicks the link, **when** they don't have an account, **then** they are directed to registration with the invitation pre-applied
- [ ] **Given** the recipient clicks the link, **when** they already have an account, **then** they are directed to login, and the invitation is applied after login

### Duplicate Handling

- [ ] **Given** I am inviting a user, **when** the email already belongs to an organization member, **then** I see "This user is already a member of this organization"
- [ ] **Given** I am inviting a user, **when** there's a pending invitation for this email, **then** I see "An invitation has already been sent to this email"
- [ ] **Given** I see a pending invitation warning, **when** I choose to resend, **then** the existing invitation is resent with a new expiration

### Invitation Management

- [ ] **Given** I am an OrgAdmin, **when** I view Organization Users, **then** I see a "Pending Invitations" section
- [ ] **Given** I view pending invitations, **when** looking at each invite, **then** I see: Email, Role, Sent Date, Expires Date, Sent By
- [ ] **Given** I view a pending invitation, **when** I click "Resend", **then** the invitation email is resent and expiration is extended
- [ ] **Given** I view a pending invitation, **when** I click "Revoke", **then** the invitation is cancelled and cannot be used

### Invitation Expiration

- [ ] **Given** an invitation exists, **when** 7 days pass without acceptance, **then** the invitation expires
- [ ] **Given** an invitation has expired, **when** the recipient clicks the link, **then** they see "This invitation has expired. Please contact the organization administrator."
- [ ] **Given** I view expired invitations, **when** clicking "Resend", **then** a new invitation is created with a fresh expiration

### Acceptance Flow

- [ ] **Given** I receive an invitation, **when** I click the link and complete registration/login, **then** I am added to the organization with the invited role
- [ ] **Given** I accept an invitation, **when** it completes, **then** I see a welcome message for the organization
- [ ] **Given** I accept an invitation, **when** I was a pending user, **then** my status changes to Active

---

## Out of Scope

- Bulk invitations (CSV upload)
- Invitation message customization
- Role-specific invitation permissions (e.g., OrgManager can only invite OrgUser)
- Invitation analytics/reporting
- SSO/SAML-based invitations

---

## Dependencies

- OM-05: User-Organization Assignment (membership model)
- OM-06: Organization Switcher (new member needs to see org)
- Email service configuration
- User registration flow

---

## Domain Terms

| Term | Definition |
|------|------------|
| Direct Invite | Email invitation sent to a specific email address |
| Pending Invitation | Invitation that has been sent but not yet accepted |
| Invitation Token | Secure, single-use code embedded in invitation link |

---

## UI/UX Notes

### Organization Users Page
```
┌─────────────────────────────────────────────────────────────────┐
│ Organization Users                              [+ Invite User] │
├─────────────────────────────────────────────────────────────────┤
│ Members (12)                                                    │
├─────────────────────────────────────────────────────────────────┤
│ Name            │ Email              │ Role      │ Joined       │
├─────────────────┼────────────────────┼───────────┼──────────────┤
│ John Smith      │ john@cisa.gov      │ Admin     │ Jan 15       │
│ Jane Doe        │ jane@cisa.gov      │ Manager   │ Jan 16       │
│ Bob Wilson      │ bob@cisa.gov       │ User      │ Jan 20       │
└─────────────────┴────────────────────┴───────────┴──────────────┘

│ Pending Invitations (2)                                         │
├─────────────────────────────────────────────────────────────────┤
│ Email               │ Role    │ Sent      │ Expires   │ Actions │
├─────────────────────┼─────────┼───────────┼───────────┼─────────┤
│ new@cisa.gov        │ User    │ Jan 28    │ Feb 4     │ [↻] [✕] │
│ contractor@ext.com  │ Manager │ Jan 25    │ Feb 1     │ [↻] [✕] │
└─────────────────────┴─────────┴───────────┴───────────┴─────────┘
```

### Invite User Dialog
```
┌─────────────────────────────────────────────────┐
│ Invite User to CISA Region 4              [X]   │
├─────────────────────────────────────────────────┤
│                                                 │
│ Email Address *                                 │
│ ┌─────────────────────────────────────────┐    │
│ │ colleague@example.com                    │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Role *                                          │
│ ┌─────────────────────────────────────────┐    │
│ │ Organization User                    ▼  │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ ℹ️ An invitation email will be sent. The       │
│ invitation expires in 7 days.                  │
│                                                 │
│                    [Cancel]  [Send Invitation]  │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Invitation Email Template
```
Subject: You've been invited to join [Org Name] on Cadence

─────────────────────────────────────────────────

Hi,

[Inviter Name] has invited you to join [Org Name] 
on Cadence as a [Role].

Cadence is an emergency management exercise 
platform used to conduct HSEEP-compliant exercises.

[    Accept Invitation    ]

This invitation expires on [Expiry Date].

If you didn't expect this invitation, you can 
safely ignore this email.

─────────────────────────────────────────────────
```

### Duplicate User Warning
```
┌─────────────────────────────────────────────────┐
│ ⚠️ Existing Member                              │
│                                                 │
│ john@cisa.gov is already a member of this      │
│ organization as an Administrator.              │
│                                                 │
│                                      [OK]       │
└─────────────────────────────────────────────────┘
```

### Pending Invitation Warning
```
┌─────────────────────────────────────────────────┐
│ ⚠️ Pending Invitation                           │
│                                                 │
│ An invitation was already sent to              │
│ new@cisa.gov on January 25, 2025.             │
│                                                 │
│ Would you like to resend the invitation?       │
│                                                 │
│                    [Cancel]  [Resend Invitation]│
└─────────────────────────────────────────────────┘
```

---

## Technical Notes

### API Endpoints

**Create Invitation:**
```
POST /api/organizations/current/invitations
Authorization: Bearer {token} (OrgAdmin only)

Request:
{
  "email": "new@cisa.gov",
  "role": "OrgUser"
}

Response (201 Created):
{
  "id": "guid",
  "email": "new@cisa.gov",
  "role": "OrgUser",
  "expiresAt": "2025-02-05T00:00:00Z",
  "createdAt": "2025-01-29T15:30:00Z"
}

Response (409 Conflict - already member):
{
  "error": "AlreadyMember",
  "message": "This user is already a member of this organization"
}

Response (409 Conflict - pending invite):
{
  "error": "PendingInvitation",
  "existingInvitation": {
    "id": "guid",
    "createdAt": "2025-01-25T10:00:00Z",
    "expiresAt": "2025-02-01T10:00:00Z"
  }
}
```

**List Invitations:**
```
GET /api/organizations/current/invitations
Authorization: Bearer {token} (OrgAdmin only)

Query Parameters:
  - status: Pending|Expired|Accepted|Revoked (default: Pending)

Response:
{
  "items": [
    {
      "id": "guid",
      "email": "new@cisa.gov",
      "role": "OrgUser",
      "status": "Pending",
      "createdAt": "2025-01-28T10:00:00Z",
      "expiresAt": "2025-02-04T10:00:00Z",
      "createdBy": {
        "id": "guid",
        "name": "John Smith"
      }
    }
  ]
}
```

**Resend Invitation:**
```
POST /api/organizations/current/invitations/{id}/resend
Authorization: Bearer {token} (OrgAdmin only)

Response (200 OK):
{
  "id": "guid",
  "expiresAt": "2025-02-05T15:30:00Z",  // Extended
  "resentAt": "2025-01-29T15:30:00Z"
}
```

**Revoke Invitation:**
```
DELETE /api/organizations/current/invitations/{id}
Authorization: Bearer {token} (OrgAdmin only)

Response (200 OK):
{
  "revoked": true
}
```

**Accept Invitation (public endpoint):**
```
POST /api/invitations/{token}/accept
Authorization: Bearer {token} (authenticated user)

Response (200 OK):
{
  "organizationId": "guid",
  "organizationName": "CISA Region 4",
  "role": "OrgUser",
  "newToken": "eyJ..."  // JWT with new org membership
}

Response (400 - expired):
{
  "error": "InvitationExpired",
  "message": "This invitation has expired"
}

Response (400 - already used):
{
  "error": "InvitationUsed",
  "message": "This invitation has already been accepted"
}
```

### Invitation Token

- 32-character cryptographically random string
- One-time use (marked as used after acceptance)
- Stored hashed in database (like password)
- Link format: `https://cadence.app/invite/{token}`

### Email Service Integration

```csharp
public interface IInvitationEmailService
{
    Task SendInvitationAsync(OrganizationInvite invite, Organization org, User inviter);
}
```

---

## Test Scenarios

| Scenario | Test Type | Priority |
|----------|-----------|----------|
| OrgAdmin can create invitation | Integration | P0 |
| OrgManager cannot create invitation | Integration | P0 |
| Email is sent on invitation creation | Integration | P0 |
| Cannot invite existing member | Integration | P0 |
| Can resend pending invitation | Integration | P0 |
| Can revoke invitation | Integration | P0 |
| Invitation acceptance adds membership | Integration | P0 |
| Expired invitation cannot be accepted | Integration | P0 |
| Invitation token is single-use | Integration | P0 |
| Archived org cannot send invitations | Integration | P0 |

---

## Implementation Checklist

### Backend
- [ ] Create `OrganizationInvite` entity
- [ ] Create `POST /api/organizations/current/invitations` endpoint
- [ ] Create `GET /api/organizations/current/invitations` endpoint
- [ ] Create `POST /api/organizations/current/invitations/{id}/resend` endpoint
- [ ] Create `DELETE /api/organizations/current/invitations/{id}` endpoint
- [ ] Create `POST /api/invitations/{token}/accept` endpoint
- [ ] Implement invitation token generation and hashing
- [ ] Implement email service for invitations
- [ ] Create invitation email template
- [ ] Add expiration check job (optional: cleanup expired)
- [ ] Unit tests for invitation service
- [ ] Integration tests for endpoints

### Frontend
- [ ] Create `InviteUserDialog` component
- [ ] Create `PendingInvitationsTable` component
- [ ] Add invitation management to org users page
- [ ] Create invitation acceptance page
- [ ] Handle invitation link in registration flow
- [ ] Handle invitation link for logged-in users
- [ ] Add resend confirmation
- [ ] Add revoke confirmation
- [ ] Component tests

---

## Changelog

| Date | Change |
|------|--------|
| 2025-01-29 | Initial story creation |
