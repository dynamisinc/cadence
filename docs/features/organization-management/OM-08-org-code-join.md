# Story OM-08: Join via Organization Code

**Priority:** P1 (Standard)  
**Feature:** Organization Management  
**Created:** 2025-01-29

---

## User Story

**As an** Organization Administrator,  
**I want** to generate a shareable organization code,  
**So that** multiple users can join my organization without individual invitations.

---

## Context

Organization codes are useful for:
- Training sessions where many participants need quick access
- Open enrollment periods
- Onboarding contractors or temporary staff
- Situations where individual email invitations are impractical

**Key differences from direct invites (OM-07):**
- Codes can be used multiple times (configurable)
- Codes are not email-specific
- All users joining via a code get the same role

---

## Acceptance Criteria

### Code Generation

- [ ] **Given** I am an OrgAdmin, **when** I navigate to Organization Settings, **then** I see an "Organization Codes" section
- [ ] **Given** I click "Generate Code", **when** the dialog opens, **then** I see options for Role, Expiration, and Max Uses
- [ ] **Given** I am generating a code, **when** I select a role, **then** I can choose from: OrgManager, OrgUser (not OrgAdmin for security)
- [ ] **Given** I am generating a code, **when** I set expiration, **then** I can choose: 24 hours, 7 days, 30 days, or Custom date
- [ ] **Given** I am generating a code, **when** I set max uses, **then** I can enter a number or select "Unlimited"
- [ ] **Given** I submit the form, **when** the code is created, **then** I see the generated code and can copy it

### Code Format

- [ ] **Given** a code is generated, **when** viewing it, **then** it is 8 characters alphanumeric (easy to type/share)
- [ ] **Given** a code is generated, **when** viewing it, **then** I also see a shareable link: `cadence.app/join/{code}`

### Code Management

- [ ] **Given** I am an OrgAdmin, **when** I view Organization Codes, **then** I see all active and expired codes
- [ ] **Given** I view the code list, **when** looking at each code, **then** I see: Code, Role, Created, Expires, Uses/Max, Status
- [ ] **Given** I view an active code, **when** I click "Deactivate", **then** the code can no longer be used
- [ ] **Given** I view a deactivated code, **when** looking at it, **then** it shows "Deactivated" status

### Using a Code - New User

- [ ] **Given** I am not registered, **when** I visit `/join/{code}`, **then** I see a registration form with the organization name displayed
- [ ] **Given** I complete registration with a valid code, **when** I submit, **then** I am registered AND added to the organization
- [ ] **Given** I join via code, **when** my account is created, **then** I have the role specified by the code
- [ ] **Given** the code has reached max uses, **when** I try to use it, **then** I see "This code has reached its maximum number of uses"

### Using a Code - Existing User

- [ ] **Given** I am logged in, **when** I navigate to `/join/{code}`, **then** I see a confirmation page
- [ ] **Given** I am on the confirmation page, **when** I click "Join Organization", **then** I am added to the organization
- [ ] **Given** I am already a member of this organization, **when** I try to use a code, **then** I see "You are already a member of this organization"

### Using a Code - Pending User

- [ ] **Given** I am a pending user (logged in, no org), **when** I enter a code on the pending page, **then** I am added to the organization
- [ ] **Given** I am added to an organization, **when** it was my first org, **then** my status changes from Pending to Active

### Code Expiration and Limits

- [ ] **Given** a code has expired, **when** someone tries to use it, **then** they see "This code has expired"
- [ ] **Given** a code has reached max uses, **when** someone tries to use it, **then** they see "This code is no longer available"
- [ ] **Given** a code is deactivated, **when** someone tries to use it, **then** they see "This code is no longer valid"

---

## Out of Scope

- Multiple roles per code (single role only)
- Code-specific permissions beyond role
- Analytics on who used which code (just count)
- QR code generation (future enhancement)
- Time-of-day restrictions

---

## Dependencies

- OM-06: Organization Switcher (new member needs to see org)
- User registration flow
- OM-05: User status management

---

## Domain Terms

| Term | Definition |
|------|------------|
| Organization Code | Shareable alphanumeric code for joining an organization |
| Max Uses | Maximum number of times a code can be used (null = unlimited) |
| Code Status | Active, Expired, Deactivated, or Exhausted (max uses reached) |

---

## UI/UX Notes

### Organization Codes Section (Settings)
```
┌─────────────────────────────────────────────────────────────────┐
│ Organization Codes                            [+ Generate Code] │
├─────────────────────────────────────────────────────────────────┤
│ Active Codes                                                    │
├─────────────────────────────────────────────────────────────────┤
│ Code      │ Role    │ Expires    │ Uses    │ Actions           │
├───────────┼─────────┼────────────┼─────────┼───────────────────┤
│ ABC12345  │ User    │ Feb 5      │ 8/50    │ [📋] [🔗] [✕]    │
│ XYZ98765  │ Manager │ Feb 1      │ 3/∞     │ [📋] [🔗] [✕]    │
└───────────┴─────────┴────────────┴─────────┴───────────────────┘

│ Inactive Codes                                      [Show/Hide] │
├─────────────────────────────────────────────────────────────────┤
│ Code      │ Role    │ Status     │ Uses    │ Deactivated       │
├───────────┼─────────┼────────────┼─────────┼───────────────────┤
│ OLD11111  │ User    │ Expired    │ 12/20   │ -                 │
│ DEF00000  │ User    │ Deactivated│ 5/10    │ Jan 20            │
└───────────┴─────────┴────────────┴─────────┴───────────────────┘
```

### Generate Code Dialog
```
┌─────────────────────────────────────────────────┐
│ Generate Organization Code               [X]   │
├─────────────────────────────────────────────────┤
│                                                 │
│ Role for new members *                         │
│ ┌─────────────────────────────────────────┐    │
│ │ Organization User                    ▼  │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Expiration *                                    │
│ ○ 24 hours                                     │
│ ○ 7 days                                       │
│ ● 30 days                                      │
│ ○ Custom: [Date picker]                        │
│                                                 │
│ Maximum Uses                                    │
│ ○ Unlimited                                    │
│ ● Limited: [    50    ]                        │
│                                                 │
│                    [Cancel]  [Generate Code]    │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Code Generated Success
```
┌─────────────────────────────────────────────────┐
│ ✅ Code Generated                         [X]   │
├─────────────────────────────────────────────────┤
│                                                 │
│ Share this code with people who should join    │
│ CISA Region 4 as Organization Users.          │
│                                                 │
│ Code:                                          │
│ ┌─────────────────────────────────────────┐    │
│ │     ABC12345                     [📋]   │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Or share this link:                            │
│ ┌─────────────────────────────────────────┐    │
│ │ cadence.app/join/ABC12345        [📋]   │    │
│ └─────────────────────────────────────────┘    │
│                                                 │
│ Expires: February 28, 2025                     │
│ Max uses: 50                                   │
│                                                 │
│                                       [Done]    │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Join Page (New User with Code)
```
┌─────────────────────────────────────────────────────────────────┐
│                                                                  │
│     ┌─────────────────────────────────────────────────┐         │
│     │                                                 │         │
│     │  Join CISA Region 4 on Cadence                 │         │
│     │                                                 │         │
│     │  Create your account to join this organization │         │
│     │                                                 │         │
│     │  Email *                                       │         │
│     │  ┌─────────────────────────────────────────┐  │         │
│     │  │                                          │  │         │
│     │  └─────────────────────────────────────────┘  │         │
│     │                                                 │         │
│     │  Password *                                    │         │
│     │  ┌─────────────────────────────────────────┐  │         │
│     │  │                                          │  │         │
│     │  └─────────────────────────────────────────┘  │         │
│     │                                                 │         │
│     │  First Name *            Last Name *           │         │
│     │  ┌─────────────────┐    ┌─────────────────┐   │         │
│     │  │                  │    │                  │   │         │
│     │  └─────────────────┘    └─────────────────┘   │         │
│     │                                                 │         │
│     │                    [Create Account & Join]      │         │
│     │                                                 │         │
│     │  Already have an account? [Sign in]            │         │
│     │                                                 │         │
│     └─────────────────────────────────────────────────┘         │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Join Page (Existing User with Code)
```
┌─────────────────────────────────────────────────────────────────┐
│                                                                  │
│     ┌─────────────────────────────────────────────────┐         │
│     │                                                 │         │
│     │  Join CISA Region 4                            │         │
│     │                                                 │         │
│     │  You're about to join CISA Region 4 as an     │         │
│     │  Organization User.                            │         │
│     │                                                 │         │
│     │  Signed in as: john@example.com               │         │
│     │                                                 │         │
│     │         [Cancel]  [Join Organization]          │         │
│     │                                                 │         │
│     └─────────────────────────────────────────────────┘         │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Invalid Code Page
```
┌─────────────────────────────────────────────────────────────────┐
│                                                                  │
│     ┌─────────────────────────────────────────────────┐         │
│     │                                                 │         │
│     │  ❌ Invalid Code                               │         │
│     │                                                 │         │
│     │  This organization code is no longer valid.   │         │
│     │  It may have expired or reached its maximum   │         │
│     │  number of uses.                              │         │
│     │                                                 │         │
│     │  Please contact the organization administrator │         │
│     │  for a new code.                              │         │
│     │                                                 │         │
│     │                          [Go to Home]          │         │
│     │                                                 │         │
│     └─────────────────────────────────────────────────┘         │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

### API Endpoints

**Generate Code:**
```
POST /api/organizations/current/codes
Authorization: Bearer {token} (OrgAdmin only)

Request:
{
  "role": "OrgUser",
  "expiresAt": "2025-02-28T00:00:00Z",
  "maxUses": 50  // null for unlimited
}

Response (201 Created):
{
  "id": "guid",
  "code": "ABC12345",
  "role": "OrgUser",
  "expiresAt": "2025-02-28T00:00:00Z",
  "maxUses": 50,
  "useCount": 0,
  "joinUrl": "https://cadence.app/join/ABC12345",
  "createdAt": "2025-01-29T15:30:00Z"
}
```

**List Codes:**
```
GET /api/organizations/current/codes
Authorization: Bearer {token} (OrgAdmin only)

Query Parameters:
  - includeInactive: bool (default: false)

Response:
{
  "items": [
    {
      "id": "guid",
      "code": "ABC12345",
      "role": "OrgUser",
      "status": "Active",
      "expiresAt": "2025-02-28T00:00:00Z",
      "maxUses": 50,
      "useCount": 8,
      "createdAt": "2025-01-29T15:30:00Z"
    }
  ]
}
```

**Deactivate Code:**
```
DELETE /api/organizations/current/codes/{id}
Authorization: Bearer {token} (OrgAdmin only)

Response (200 OK):
{
  "deactivated": true
}
```

**Validate Code (public):**
```
GET /api/join/{code}

Response (200 OK - valid):
{
  "valid": true,
  "organizationName": "CISA Region 4",
  "role": "OrgUser"
}

Response (200 OK - invalid):
{
  "valid": false,
  "reason": "expired" | "exhausted" | "deactivated" | "notFound"
}
```

**Use Code (authenticated):**
```
POST /api/join/{code}
Authorization: Bearer {token} (any authenticated user)

Response (200 OK):
{
  "organizationId": "guid",
  "organizationName": "CISA Region 4",
  "role": "OrgUser",
  "newToken": "eyJ..."
}
```

**Use Code with Registration (public):**
```
POST /api/join/{code}/register
Authorization: None

Request:
{
  "email": "new@example.com",
  "password": "SecurePassword123!",
  "firstName": "New",
  "lastName": "User"
}

Response (201 Created):
{
  "userId": "guid",
  "organizationId": "guid",
  "organizationName": "CISA Region 4",
  "role": "OrgUser",
  "token": "eyJ..."
}
```

### Code Generation

```csharp
public string GenerateCode()
{
    // 8 characters, alphanumeric, easy to read (no 0/O, 1/l confusion)
    const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    var random = RandomNumberGenerator.Create();
    var bytes = new byte[8];
    random.GetBytes(bytes);
    
    return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
}
```

### Code Status Calculation

```csharp
public enum CodeStatus
{
    Active,      // Valid and usable
    Expired,     // Past expiration date
    Exhausted,   // Reached max uses
    Deactivated  // Manually deactivated
}

public CodeStatus GetStatus(OrganizationCode code)
{
    if (code.DeactivatedAt.HasValue) return CodeStatus.Deactivated;
    if (code.ExpiresAt < DateTime.UtcNow) return CodeStatus.Expired;
    if (code.MaxUses.HasValue && code.UseCount >= code.MaxUses) return CodeStatus.Exhausted;
    return CodeStatus.Active;
}
```

---

## Test Scenarios

| Scenario | Test Type | Priority |
|----------|-----------|----------|
| OrgAdmin can generate code | Integration | P0 |
| Code format is valid (8 chars, alphanumeric) | Unit | P0 |
| New user can register with code | Integration | P0 |
| Existing user can join with code | Integration | P0 |
| Pending user can use code | Integration | P0 |
| Expired code cannot be used | Integration | P0 |
| Exhausted code cannot be used | Integration | P0 |
| Deactivated code cannot be used | Integration | P0 |
| Use count increments on use | Integration | P0 |
| Already-member cannot use code | Integration | P0 |
| OrgAdmin cannot generate OrgAdmin codes | Integration | P0 |

---

## Implementation Checklist

### Backend
- [ ] Create `OrganizationCode` entity
- [ ] Create `POST /api/organizations/current/codes` endpoint
- [ ] Create `GET /api/organizations/current/codes` endpoint
- [ ] Create `DELETE /api/organizations/current/codes/{id}` endpoint
- [ ] Create `GET /api/join/{code}` endpoint (validation)
- [ ] Create `POST /api/join/{code}` endpoint (authenticated join)
- [ ] Create `POST /api/join/{code}/register` endpoint (registration + join)
- [ ] Implement code generation with collision check
- [ ] Implement code status calculation
- [ ] Implement use count increment (thread-safe)
- [ ] Unit tests for code generation
- [ ] Unit tests for status calculation
- [ ] Integration tests for endpoints

### Frontend
- [ ] Create `OrganizationCodesSection` component
- [ ] Create `GenerateCodeDialog` component
- [ ] Create `CodeGeneratedDialog` component (with copy buttons)
- [ ] Create `JoinPage` component (handles both new and existing users)
- [ ] Create `InvalidCodePage` component
- [ ] Add code entry to pending user page
- [ ] Add copy-to-clipboard functionality
- [ ] Component tests

---

## Changelog

| Date | Change |
|------|--------|
| 2025-01-29 | Initial story creation |
