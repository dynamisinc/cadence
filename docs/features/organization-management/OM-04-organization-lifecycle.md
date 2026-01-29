# Story OM-04: Organization Lifecycle

**Priority:** P0 (MVP)  
**Feature:** Organization Management  
**Created:** 2025-01-29

---

## User Story

**As a** System Administrator,  
**I want** to archive, deactivate, and restore organizations,  
**So that** I can manage the organization lifecycle without losing historical data.

---

## Context

Organizations need different states throughout their lifecycle:

| Status | Use Case | Data Access | User Access |
|--------|----------|-------------|-------------|
| **Active** | Normal operation | Full read/write | Full access |
| **Archived** | Historical reference | Read-only | Read-only for existing members |
| **Inactive** | Soft delete | Hidden from queries | No access |

**Key principle:** No hard deletes. Even "deleted" organizations are recoverable.

---

## Acceptance Criteria

### Archive Organization

- [ ] **Given** I am a SysAdmin viewing an Active organization, **when** I click "Archive", **then** I see a confirmation dialog
- [ ] **Given** I confirm archival, **when** the action completes, **then** the organization status changes to "Archived"
- [ ] **Given** an organization is Archived, **when** members try to access it, **then** they see read-only access with an "Archived" banner
- [ ] **Given** an organization is Archived, **when** members try to create/edit exercises, **then** they receive an error "This organization is archived and read-only"
- [ ] **Given** an organization is Archived, **when** viewing the org list, **then** it appears with muted styling and "Archived" badge

### Deactivate Organization (Soft Delete)

- [ ] **Given** I am a SysAdmin viewing an Active or Archived organization, **when** I click "Deactivate", **then** I see a confirmation dialog with warning
- [ ] **Given** I confirm deactivation, **when** the action completes, **then** the organization status changes to "Inactive"
- [ ] **Given** an organization is Inactive, **when** former members log in, **then** they do not see this organization in their org list
- [ ] **Given** an organization is Inactive, **when** a SysAdmin views the org list, **then** it appears with "Inactive" status (filterable)
- [ ] **Given** an organization is Inactive, **when** anyone tries direct URL access, **then** they receive "Organization not found" (SysAdmin sees it)

### Restore Organization

- [ ] **Given** I am a SysAdmin viewing an Archived organization, **when** I click "Restore to Active", **then** the status changes to "Active"
- [ ] **Given** I am a SysAdmin viewing an Inactive organization, **when** I click "Restore to Active", **then** the status changes to "Active"
- [ ] **Given** an organization is restored, **when** members next log in, **then** they see the organization in their list again

### Confirmation Dialogs

- [ ] **Given** I click Archive, **when** the dialog appears, **then** it shows: "Archive [Org Name]? Members will have read-only access. This can be undone."
- [ ] **Given** I click Deactivate, **when** the dialog appears, **then** it shows: "Deactivate [Org Name]? Members will lose access. Exercises and data are preserved. This can be undone by a System Administrator."
- [ ] **Given** any confirmation dialog, **when** I click Cancel, **then** no action is taken

### Prevent Self-Lockout

- [ ] **Given** I am trying to deactivate my only organization, **when** I am also an OrgAdmin in that org, **then** the system warns me I will lose access to it
- [ ] **Given** deactivation would leave users with no organization, **when** confirming, **then** the dialog shows count of affected users

### Audit Requirements

- [ ] **Given** any status change occurs, **when** it completes, **then** the change is logged with timestamp, user, previous status, and new status
- [ ] **Given** I view organization details as SysAdmin, **when** looking at history, **then** I can see status change history (future: detailed audit log)

---

## Out of Scope

- Hard delete (data destruction)
- Scheduled deactivation (auto-archive after X days)
- Bulk status changes
- Detailed audit log UI (just capture data for now)
- Organization data export before deactivation

---

## Dependencies

- OM-01: Organization List (where actions are initiated)
- OM-03: Edit Organization (status display)
- EF Core global query filters for Inactive hiding

---

## Domain Terms

| Term | Definition |
|------|------------|
| Active | Normal operating state, full functionality |
| Archived | Read-only historical state, visible to members |
| Inactive | Soft-deleted, hidden from non-SysAdmin users |
| Restore | Return organization from Archived/Inactive to Active |

---

## UI/UX Notes

### Status Actions by Current State

| Current Status | Available Actions |
|----------------|-------------------|
| Active | Archive, Deactivate |
| Archived | Restore to Active, Deactivate |
| Inactive | Restore to Active |

### Action Buttons Location

**Option A: Organization Detail Page**
```
┌─────────────────────────────────────────────────────────────────┐
│ Edit Organization: CISA Region 4                    Status: 🟢  │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  [Form fields...]                                               │
│                                                                  │
│  ─────────────────────────────────────────────────────────────  │
│                                                                  │
│  ⚠️ Danger Zone                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ 📦 Archive Organization                    [Archive]      │   │
│  │ Make read-only. Members can view but not edit.           │   │
│  ├──────────────────────────────────────────────────────────┤   │
│  │ 🗑️ Deactivate Organization               [Deactivate]    │   │
│  │ Hide from members. Data is preserved.                    │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

**Option B: Row Actions in List**
```
┌─────────────────────────────────────────────────────────────────┐
│ Name          │ Status    │ Users │ Actions                     │
├───────────────┼───────────┼───────┼─────────────────────────────┤
│ CISA Region 4 │ 🟢 Active  │  12   │ [Edit] [Archive] [Deactivate]│
│ State EMA     │ 🟡 Archived│  45   │ [Edit] [Restore] [Deactivate]│
│ Test Org      │ 🔴 Inactive│   3   │ [Restore]                   │
└───────────────┴───────────┴───────┴─────────────────────────────┘
```

**Recommendation:** Both. Quick actions in list, full controls on detail page.

### Archive Confirmation Dialog
```
┌─────────────────────────────────────────────────┐
│ Archive Organization                      [X]   │
├─────────────────────────────────────────────────┤
│                                                 │
│ 📦 Archive "CISA Region 4"?                    │
│                                                 │
│ • 12 members will have read-only access        │
│ • 8 exercises will become read-only            │
│ • No new exercises can be created              │
│                                                 │
│ This can be undone by restoring the            │
│ organization to active status.                 │
│                                                 │
│                    [Cancel]  [Archive]          │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Deactivate Confirmation Dialog
```
┌─────────────────────────────────────────────────┐
│ ⚠️ Deactivate Organization               [X]   │
├─────────────────────────────────────────────────┤
│                                                 │
│ Deactivate "CISA Region 4"?                    │
│                                                 │
│ ⚠️ Warning: This will remove access for all   │
│ organization members.                          │
│                                                 │
│ • 12 members will lose access                  │
│ • 3 members will have no remaining orgs       │
│ • Data and exercises are preserved             │
│                                                 │
│ Only a System Administrator can restore        │
│ this organization.                             │
│                                                 │
│ Type "CISA Region 4" to confirm:              │
│ ┌─────────────────────────────────────────┐   │
│ │                                          │   │
│ └─────────────────────────────────────────┘   │
│                                                 │
│                    [Cancel]  [Deactivate]       │
│                                                 │
└─────────────────────────────────────────────────┘
```

### Archived Organization Banner
```
┌─────────────────────────────────────────────────────────────────┐
│ ⚠️ This organization is archived and read-only                  │
│ Contact your administrator to restore access.         [Dismiss] │
└─────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

### API Endpoints

**Archive Organization:**
```
POST /api/admin/organizations/{id}/archive
Authorization: Bearer {token} (SysAdmin only)

Response (200 OK):
{
  "id": "guid",
  "status": "Archived",
  "statusChangedAt": "2025-01-29T15:30:00Z",
  "statusChangedBy": "sysadmin@cadence.app"
}
```

**Deactivate Organization:**
```
POST /api/admin/organizations/{id}/deactivate
Authorization: Bearer {token} (SysAdmin only)

Response (200 OK):
{
  "id": "guid",
  "status": "Inactive",
  "statusChangedAt": "2025-01-29T15:30:00Z",
  "statusChangedBy": "sysadmin@cadence.app",
  "affectedUsers": 12,
  "usersWithNoRemainingOrgs": 3
}
```

**Restore Organization:**
```
POST /api/admin/organizations/{id}/restore
Authorization: Bearer {token} (SysAdmin only)

Response (200 OK):
{
  "id": "guid",
  "status": "Active",
  "statusChangedAt": "2025-01-29T15:30:00Z",
  "statusChangedBy": "sysadmin@cadence.app"
}
```

### EF Core Global Query Filter

```csharp
// For non-admin queries, automatically exclude Inactive organizations
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Organization>()
        .HasQueryFilter(o => o.Status != OrganizationStatus.Inactive || _isSysAdmin);
}
```

### Status Change Logging

```csharp
public class OrganizationStatusChange
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public OrganizationStatus PreviousStatus { get; set; }
    public OrganizationStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public Guid ChangedById { get; set; }
    public string Reason { get; set; }  // Optional
}
```

### Read-Only Enforcement for Archived Orgs

Two approaches:

**Option A: API-level enforcement**
```csharp
[HttpPost]
public async Task<IActionResult> CreateExercise(...)
{
    var org = await _orgService.GetCurrentOrganization();
    if (org.Status == OrganizationStatus.Archived)
        return BadRequest("Organization is archived and read-only");
    // ...
}
```

**Option B: Service-level enforcement (preferred)**
```csharp
public class OrganizationGuard
{
    public void EnsureWriteAccess(Organization org)
    {
        if (org.Status != OrganizationStatus.Active)
            throw new OrganizationReadOnlyException(org.Status);
    }
}
```

---

## Test Scenarios

| Scenario | Test Type | Priority |
|----------|-----------|----------|
| Archive changes status to Archived | Integration | P0 |
| Archived org is read-only | Integration | P0 |
| Deactivate changes status to Inactive | Integration | P0 |
| Inactive org hidden from members | Integration | P0 |
| Inactive org visible to SysAdmin | Integration | P0 |
| Restore from Archived to Active | Integration | P0 |
| Restore from Inactive to Active | Integration | P0 |
| Non-SysAdmin cannot change status | Integration | P0 |
| Status change creates log entry | Integration | P1 |
| Confirmation dialog shows correct counts | Component | P1 |
| Deactivate requires name confirmation | Component | P1 |

---

## Implementation Checklist

### Backend
- [ ] Add `Status` enum to Organization entity
- [ ] Add `StatusChangedAt` and `StatusChangedById` fields
- [ ] Create `OrganizationStatusChange` entity for audit
- [ ] Create `POST /api/admin/organizations/{id}/archive` endpoint
- [ ] Create `POST /api/admin/organizations/{id}/deactivate` endpoint
- [ ] Create `POST /api/admin/organizations/{id}/restore` endpoint
- [ ] Implement EF Core global query filter for Inactive
- [ ] Create `OrganizationGuard` service for write-access checks
- [ ] Add guard checks to all org-scoped write operations
- [ ] Unit tests for status transitions
- [ ] Integration tests for endpoints
- [ ] Integration tests for read-only enforcement

### Frontend
- [ ] Add status action buttons to org list
- [ ] Add Danger Zone section to org detail/edit page
- [ ] Create `ArchiveConfirmationDialog` component
- [ ] Create `DeactivateConfirmationDialog` component (with name typing)
- [ ] Create `RestoreConfirmationDialog` component
- [ ] Add archived organization banner component
- [ ] Disable edit controls when org is archived
- [ ] Handle API errors for archived org writes
- [ ] Component tests for dialogs

### Database
- [ ] Add migration for Status field
- [ ] Add migration for OrganizationStatusChange table
- [ ] Update existing orgs to Status = Active

---

## Changelog

| Date | Change |
|------|--------|
| 2025-01-29 | Initial story creation |
