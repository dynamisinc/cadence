# Story OM-02: Create Organization

**Priority:** P0 (MVP)  
**Feature:** Organization Management  
**Created:** 2025-01-29

---

## User Story

**As a** System Administrator,  
**I want** to create a new organization with its first administrator,  
**So that** a new customer or department can begin using Cadence with proper access control.

---

## Context

Creating an organization is the first step in onboarding a new customer. The SysAdmin provides basic organization details and designates the first OrgAdmin who will then manage the organization's users and settings independently.

The first OrgAdmin can be:
1. An existing user in the system (already registered)
2. A new user (email invitation sent)

---

## Acceptance Criteria

### Form Display

- [ ] **Given** I am logged in as SysAdmin, **when** I click "Create Organization" from the org list, **then** I see a creation form
- [ ] **Given** I am on the create form, **when** viewing required fields, **then** I see: Name, Slug, First Admin Email
- [ ] **Given** I am on the create form, **when** viewing optional fields, **then** I see: Description, Contact Email

### Validation - Organization Name

- [ ] **Given** I am creating an organization, **when** I leave Name empty, **then** I see validation error "Organization name is required"
- [ ] **Given** I am creating an organization, **when** Name exceeds 200 characters, **then** I see validation error "Name must be 200 characters or less"
- [ ] **Given** I am creating an organization, **when** I enter a valid name, **then** the Slug field auto-generates from the name

### Validation - Slug

- [ ] **Given** I am creating an organization, **when** I leave Slug empty, **then** I see validation error "Slug is required"
- [ ] **Given** I am creating an organization, **when** Slug contains invalid characters, **then** I see validation error "Slug can only contain lowercase letters, numbers, and hyphens"
- [ ] **Given** I am creating an organization, **when** Slug already exists, **then** I see validation error "This slug is already in use"
- [ ] **Given** I am creating an organization, **when** Slug exceeds 50 characters, **then** I see validation error "Slug must be 50 characters or less"
- [ ] **Given** the name auto-generates a slug, **when** I manually edit the slug, **then** auto-generation stops and my manual value is preserved

### Validation - First Admin

- [ ] **Given** I am creating an organization, **when** I leave First Admin Email empty, **then** I see validation error "First administrator email is required"
- [ ] **Given** I am creating an organization, **when** First Admin Email is invalid format, **then** I see validation error "Please enter a valid email address"

### Successful Creation - Existing User

- [ ] **Given** I submit with a valid email of an existing user, **when** creation succeeds, **then** the organization is created with status "Active"
- [ ] **Given** I submit with an existing user's email, **when** creation succeeds, **then** that user is added to the organization as OrgAdmin
- [ ] **Given** the user was pending, **when** added to org, **then** their status changes to Active
- [ ] **Given** I submit with an existing user's email, **when** creation succeeds, **then** I see success message and am redirected to org list

### Successful Creation - New User

- [ ] **Given** I submit with an email not in the system, **when** creation succeeds, **then** a new user record is created with status "Pending"
- [ ] **Given** a new user is created, **when** the org is created, **then** an invitation email is sent to the new user
- [ ] **Given** a new user receives invitation, **when** they click the link, **then** they can set their password and access the organization

### Error Handling

- [ ] **Given** I submit the form, **when** a server error occurs, **then** I see an error message and the form retains my input
- [ ] **Given** I submit the form, **when** the slug uniqueness check fails (race condition), **then** I see an appropriate error

### Cancellation

- [ ] **Given** I am on the create form with unsaved changes, **when** I click Cancel, **then** I see a confirmation dialog
- [ ] **Given** I confirm cancellation, **when** the dialog closes, **then** I return to the organization list without creating anything

---

## Out of Scope

- Creating multiple OrgAdmins during initial setup (they can be added later)
- Uploading organization logo
- Setting organization timezone (P2 feature)
- Selecting capability libraries (P2 feature)
- Bulk organization import

---

## Dependencies

- OM-01: Organization List (navigation target after creation)
- User entity and service
- Email service for sending invitations

---

## Domain Terms

| Term | Definition |
|------|------------|
| Slug | URL-friendly unique identifier, auto-generated from name but editable |
| OrgAdmin | Organization Administrator - highest role within an organization |
| Pending User | User account created but not yet activated (hasn't set password) |

---

## UI/UX Notes

### Form Layout
```
┌─────────────────────────────────────────────────────────────────┐
│ Create Organization                                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Organization Name *                                             │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ CISA Region 4                                            │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  Slug *                              🔗 Auto-generated from name │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ cisa-region-4                                            │    │
│  └─────────────────────────────────────────────────────────┘    │
│  Will be used in URLs: cadence.app/org/cisa-region-4            │
│                                                                  │
│  Description                                                     │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                                                          │    │
│  │                                                          │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  Contact Email                                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ admin@cisa.gov                                           │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ─────────────────────────────────────────────────────────────  │
│                                                                  │
│  First Administrator *                                           │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ john.smith@cisa.gov                                      │    │
│  └─────────────────────────────────────────────────────────┘    │
│  This person will receive OrgAdmin access. If not already       │
│  registered, they'll receive an invitation email.               │
│                                                                  │
│                               [Cancel]  [Create Organization]    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Slug Auto-Generation Rules
1. Convert to lowercase
2. Replace spaces with hyphens
3. Remove special characters (keep only a-z, 0-9, -)
4. Collapse multiple hyphens to single
5. Trim hyphens from start/end

Examples:
- "CISA Region 4" → "cisa-region-4"
- "State Emergency Mgmt." → "state-emergency-mgmt"
- "Test & Demo Org!!!" → "test-demo-org"

### User Feedback States
- **Checking slug:** Inline spinner while checking uniqueness
- **Slug available:** Green checkmark
- **Slug taken:** Red X with suggestion
- **Submitting:** Button shows spinner, form disabled
- **Success:** Toast notification, redirect to list
- **Error:** Inline error message, form stays open

---

## Technical Notes

### API Endpoint
```
POST /api/admin/organizations
Authorization: Bearer {token} (SysAdmin only)

Request:
{
  "name": "CISA Region 4",
  "slug": "cisa-region-4",
  "description": "Regional emergency management",
  "contactEmail": "admin@cisa.gov",
  "firstAdminEmail": "john.smith@cisa.gov"
}

Response (201 Created):
{
  "id": "guid",
  "name": "CISA Region 4",
  "slug": "cisa-region-4",
  "status": "Active",
  "firstAdmin": {
    "id": "guid",
    "email": "john.smith@cisa.gov",
    "isNewUser": true,
    "invitationSent": true
  }
}
```

### Slug Uniqueness Check
```
GET /api/admin/organizations/check-slug?slug=cisa-region-4
Authorization: Bearer {token} (SysAdmin only)

Response:
{
  "available": false,
  "suggestion": "cisa-region-4-2"
}
```

### Transaction Requirements

The following must happen atomically:
1. Create Organization record
2. Create User record (if new)
3. Create OrganizationMembership record
4. Queue invitation email (if new user)

If any step fails, roll back all changes.

---

## Test Scenarios

| Scenario | Test Type | Priority |
|----------|-----------|----------|
| Create with existing user | Integration | P0 |
| Create with new user (invitation) | Integration | P0 |
| Slug uniqueness validation | Integration | P0 |
| Name to slug auto-generation | Unit | P0 |
| Invalid email format rejected | Unit | P0 |
| Form validation - required fields | Component | P0 |
| Cancel with unsaved changes | Component | P1 |
| Server error handling | Integration | P1 |
| Non-SysAdmin gets 403 | Integration | P0 |

---

## Implementation Checklist

### Backend
- [ ] Create `POST /api/admin/organizations` endpoint
- [ ] Create `GET /api/admin/organizations/check-slug` endpoint
- [ ] Implement organization creation service
- [ ] Handle existing vs new user logic
- [ ] Implement invitation email queue
- [ ] Add transaction wrapper
- [ ] Unit tests for slug generation
- [ ] Unit tests for service logic
- [ ] Integration tests for endpoints

### Frontend
- [ ] Create `CreateOrganizationPage` component
- [ ] Create `OrganizationForm` component (reusable for edit)
- [ ] Implement slug auto-generation with manual override
- [ ] Implement real-time slug availability check (debounced)
- [ ] Add form validation with error display
- [ ] Add unsaved changes confirmation
- [ ] Add loading states
- [ ] Add success/error toasts
- [ ] Add route `/admin/organizations/new`
- [ ] Component tests

### Email
- [ ] Create invitation email template
- [ ] Implement email sending service
- [ ] Add invitation link with token

---

## Changelog

| Date | Change |
|------|--------|
| 2025-01-29 | Initial story creation |
