# Story OM-03: Edit Organization

**Priority:** P0 (MVP)  
**Feature:** Organization Management  
**Created:** 2025-01-29

---

## User Story

**As a** System Administrator or Organization Administrator,  
**I want** to edit an organization's details,  
**So that** I can keep organization information accurate and up-to-date.

---

## Context

Organization details may need to change over time - rebranding, new contact information, updated descriptions. Both SysAdmins (for any org) and OrgAdmins (for their own org) need this capability.

**Key difference from Create:**
- Slug cannot be changed after creation (used in URLs, references)
- Status changes are handled separately (OM-04)
- User management is handled separately (OM-05, OM-07)

---

## Acceptance Criteria

### Access Control

- [ ] **Given** I am a SysAdmin, **when** I navigate to any organization's edit page, **then** I can view and edit it
- [ ] **Given** I am an OrgAdmin, **when** I navigate to my organization's edit page, **then** I can view and edit it
- [ ] **Given** I am an OrgAdmin, **when** I try to access another organization's edit page, **then** I receive an access denied error
- [ ] **Given** I am an OrgManager or OrgUser, **when** I try to access my organization's edit page, **then** I receive an access denied error

### Form Display

- [ ] **Given** I navigate to edit organization, **when** the page loads, **then** I see the current organization values pre-filled
- [ ] **Given** I am viewing the edit form, **when** I look at the Slug field, **then** it is displayed as read-only with an explanation
- [ ] **Given** I am an OrgAdmin (not SysAdmin), **when** viewing the form, **then** I do not see the Status field (SysAdmin only)

### Editable Fields

- [ ] **Given** I am editing an organization, **when** I change the Name, **then** the change is saved when I submit
- [ ] **Given** I am editing an organization, **when** I change the Description, **then** the change is saved when I submit
- [ ] **Given** I am editing an organization, **when** I change the Contact Email, **then** the change is saved when I submit

### Validation

- [ ] **Given** I am editing an organization, **when** I clear the Name field, **then** I see validation error "Organization name is required"
- [ ] **Given** I am editing an organization, **when** Name exceeds 200 characters, **then** I see validation error "Name must be 200 characters or less"
- [ ] **Given** I am editing an organization, **when** Contact Email is invalid format, **then** I see validation error "Please enter a valid email address"

### Save Behavior

- [ ] **Given** I have made changes, **when** I click Save, **then** the organization is updated
- [ ] **Given** I save successfully, **when** the save completes, **then** I see a success toast message
- [ ] **Given** I save successfully as SysAdmin, **when** the save completes, **then** I return to the organization list
- [ ] **Given** I save successfully as OrgAdmin, **when** the save completes, **then** I stay on the organization settings page
- [ ] **Given** I have not made changes, **when** I view the Save button, **then** it is disabled

### Cancel Behavior

- [ ] **Given** I have unsaved changes, **when** I click Cancel, **then** I see a confirmation dialog
- [ ] **Given** I have no unsaved changes, **when** I click Cancel, **then** I navigate away without confirmation
- [ ] **Given** I confirm cancellation, **when** the dialog closes, **then** my changes are discarded

### Audit Trail

- [ ] **Given** I save changes, **when** the update completes, **then** the UpdatedAt timestamp is set to current time
- [ ] **Given** I save changes, **when** the update completes, **then** the change is logged with who made it (future: audit log)

### Concurrent Edit Warning

- [ ] **Given** I am editing an organization, **when** another user saves changes while I'm editing, **then** I see a warning when I try to save
- [ ] **Given** I see a concurrent edit warning, **when** I choose to reload, **then** I see the latest data with my changes discarded

---

## Out of Scope

- Changing organization slug (immutable after creation)
- Changing organization status (see OM-04)
- Managing organization users (see OM-05, OM-07)
- Managing agencies (see OM-09)
- Managing capability libraries (see OM-11)
- Detailed audit log viewing

---

## Dependencies

- OM-01: Organization List (navigation for SysAdmin)
- OM-02: Create Organization (shared form component)
- Authentication with role-based access

---

## Domain Terms

| Term | Definition |
|------|------------|
| SysAdmin | System Administrator - can edit any organization |
| OrgAdmin | Organization Administrator - can edit only their organization |
| Optimistic Locking | Detecting concurrent edits using version/timestamp |

---

## UI/UX Notes

### Form Layout (SysAdmin View)
```
┌─────────────────────────────────────────────────────────────────┐
│ ← Back to Organizations                                          │
│                                                                  │
│ Edit Organization: CISA Region 4                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Organization Name *                                             │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ CISA Region 4                                            │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  Slug                                                            │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ cisa-region-4                                       🔒   │    │
│  └─────────────────────────────────────────────────────────┘    │
│  Slug cannot be changed after creation                          │
│                                                                  │
│  Description                                                     │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ Regional emergency management coordination center for    │    │
│  │ the southeastern United States.                          │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  Contact Email                                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ admin@cisa.gov                                           │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ─────────────────────────────────────────────────────────────  │
│                                                                  │
│  Organization Info                                               │
│  Status: 🟢 Active                                              │
│  Created: January 15, 2025 by system.admin@cadence.app         │
│  Last Updated: January 20, 2025                                 │
│  Users: 12  •  Exercises: 8                                     │
│                                                                  │
│                                  [Cancel]  [Save Changes]        │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Form Layout (OrgAdmin View)
```
┌─────────────────────────────────────────────────────────────────┐
│ Organization Settings                                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  [Same fields, but accessed from Org Settings menu]             │
│  [No "Back to Organizations" - they don't have access]          │
│  [Status shown as read-only info, not editable]                 │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Navigation Paths
- **SysAdmin:** Organization List → Click Row → Edit Page
- **OrgAdmin:** Settings Menu → Organization Settings

### Dirty State Indicator
- Show dot or asterisk in page title when unsaved changes exist
- Browser beforeunload warning if navigating away with changes

---

## Technical Notes

### API Endpoints

**Get Organization (for form population):**
```
GET /api/admin/organizations/{id}
Authorization: Bearer {token} (SysAdmin only)

-- OR --

GET /api/organizations/current
Authorization: Bearer {token} (Any authenticated user)
Returns current org context details
```

**Update Organization:**
```
PUT /api/admin/organizations/{id}
Authorization: Bearer {token} (SysAdmin only)

-- OR --

PUT /api/organizations/current
Authorization: Bearer {token} (OrgAdmin only)

Request:
{
  "name": "CISA Region 4 - Updated",
  "description": "Updated description",
  "contactEmail": "new.admin@cisa.gov",
  "rowVersion": "base64-encoded-timestamp"  // for optimistic locking
}

Response (200 OK):
{
  "id": "guid",
  "name": "CISA Region 4 - Updated",
  "slug": "cisa-region-4",
  "description": "Updated description",
  "contactEmail": "new.admin@cisa.gov",
  "status": "Active",
  "updatedAt": "2025-01-29T15:30:00Z",
  "rowVersion": "new-base64-encoded-timestamp"
}

Response (409 Conflict - concurrent edit):
{
  "error": "ConcurrentEdit",
  "message": "This organization was modified by another user",
  "currentData": { ... latest org data ... }
}
```

### Optimistic Locking

Use EF Core's `RowVersion` (SQL Server) or manual timestamp comparison:

```csharp
public class Organization
{
    // ... other properties
    
    [Timestamp]
    public byte[] RowVersion { get; set; }
}
```

---

## Test Scenarios

| Scenario | Test Type | Priority |
|----------|-----------|----------|
| SysAdmin can edit any org | Integration | P0 |
| OrgAdmin can edit own org | Integration | P0 |
| OrgAdmin cannot edit other org | Integration | P0 |
| OrgManager cannot edit org | Integration | P0 |
| Save updates organization | Integration | P0 |
| Validation - empty name rejected | Unit | P0 |
| Validation - invalid email rejected | Unit | P0 |
| Concurrent edit detection | Integration | P1 |
| Unsaved changes warning | Component | P1 |
| Dirty state tracking | Component | P1 |

---

## Implementation Checklist

### Backend
- [ ] Create `GET /api/admin/organizations/{id}` endpoint
- [ ] Create `PUT /api/admin/organizations/{id}` endpoint
- [ ] Create `GET /api/organizations/current` endpoint
- [ ] Create `PUT /api/organizations/current` endpoint
- [ ] Add optimistic locking with RowVersion
- [ ] Add authorization checks
- [ ] Unit tests for service
- [ ] Integration tests for endpoints

### Frontend
- [ ] Create `EditOrganizationPage` component
- [ ] Reuse `OrganizationForm` from OM-02
- [ ] Add read-only slug display
- [ ] Add dirty state tracking
- [ ] Add unsaved changes confirmation
- [ ] Add concurrent edit handling
- [ ] Add routes:
  - `/admin/organizations/:id/edit` (SysAdmin)
  - `/settings/organization` (OrgAdmin)
- [ ] Component tests

---

## Changelog

| Date | Change |
|------|--------|
| 2025-01-29 | Initial story creation |
