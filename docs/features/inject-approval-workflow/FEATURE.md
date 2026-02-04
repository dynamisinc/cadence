# Feature: Inject Approval Workflow

**Epic:** E3 - MSEL Management  
**Priority:** P0 (Required for HSEEP Compliance)  
**Target Phase:** D.5 (Between Exercise Conduct and Evaluator Observations)

## Overview

Organizations can require formal approval of injects before exercise conduct. When enabled, injects follow a Draft → Submitted → Approved workflow, and exercises cannot go live until all injects are approved. This supports HSEEP compliance and organizational governance requirements per FEMA PrepToolkit standards.

**Cross-Domain Support:** While HSEEP is the default framework for U.S. civilian emergency management, [research on inject status standards](../../research/inject-status-cross-domain-analysis.md) identified five distinct framework ecosystems with different terminology. Cadence supports configurable status workflows to serve organizations in DoD/NATO, cybersecurity (NIST/MITRE), healthcare (CMS/Joint Commission), financial (FFIEC), and international contexts.

## Business Value

- **Compliance:** Aligns with FEMA PrepToolkit inject status workflow by default
- **Quality Control:** Ensures all exercise content is reviewed before conduct
- **Audit Trail:** Documents who approved what and when
- **Flexibility:** Configurable per organization and exercise to match governance needs
- **Market Reach:** Supports multiple framework ecosystems beyond HSEEP (DoD, NATO, cybersecurity, healthcare, financial, international)

## User Personas

| Persona | Role in Approval | Key Actions |
|---------|------------------|-------------|
| Administrator | Policy setter, approver | Configure org policy, approve any inject, override settings |
| Exercise Director | Approver, exercise owner | Approve/reject injects, configure exercise settings, publish exercise |
| Controller | Author, submitter | Create injects, submit for approval, revise rejected injects |
| Evaluator | None | View-only access to MSEL |
| Observer | None | View-only access to MSEL |

## Framework Support

### HSEEP (Default for U.S. Civilian)

This feature implements the FEMA PrepToolkit inject status workflow as the default:

| FEMA Status | Cadence Status | Description |
|-------------|----------------|-------------|
| Draft | Draft | Initial authoring status |
| Submitted | Submitted | Sent for review (Cadence addition for workflow) |
| Approved | Approved | Director has approved for use |
| Synchronized | Synchronized | Scheduled with specific time |
| Released | Released | Delivered to players |
| Complete | Complete | Delivery confirmed |
| Deferred | Deferred | Cancelled before delivery |
| Obsolete | Obsolete | Ignored but retained for audit |

### Alternative Framework Templates (S10)

Research identified significant terminology variation across domains. Cadence provides pre-built templates:

| Framework | Primary Audience | Key Differences from HSEEP |
|-----------|------------------|----------------------------|
| **DoD/JTS** | Military exercises | STARTEX/ENDEX, Key/Enabling/Supporting inject types, JMSEL terminology |
| **NATO** | Allied military coordination | EXCON/DISTAFF roles, LIVEX/CPX/Study exercise types |
| **UK Cabinet Office** | UK government | "Main Events List" (not MSEL), Blue/Red/Green/White Cell structure |
| **Australian AIIMS** | Australia/NZ | "Special Ideas" (not injects), DISCEX/Functional/Field types |
| **Cybersecurity** | NIST/MITRE-aligned | ATT&CK mapping, Red/Blue/Purple team roles, Control Cell |
| **Healthcare** | CMS/Joint Commission | HICS roles, HVA categories, surge levels, 96-hour sustainability |
| **Financial** | FFIEC/FINRA | RTO/RPO/MTD metrics, mission-critical system classifications |
| **ISO 22301/BCI** | Private sector BC/DR | "Validation" terminology, BCMS principles |

See [S10: Configurable Status Workflow](S10-configurable-status-workflow.md) for implementation details.

## Feature Configuration

### Three-Tier Configuration Model

```
┌─────────────────────────────────────────────────────────┐
│                   ORGANIZATION LEVEL                     │
│  Policy: Disabled | Optional | Required                  │
│  (Set by Administrator)                                  │
└─────────────────────────┬───────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                    EXERCISE LEVEL                        │
│  Toggle: Enable/Disable Approval                         │
│  (Set by Director, constrained by Org policy)           │
│  Admin Override: Can disable even when Org = Required    │
└─────────────────────────┬───────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                    INJECT LEVEL                          │
│  Status workflow enforced per exercise setting           │
└─────────────────────────────────────────────────────────┘
```

### Policy Matrix

| Org Policy | Exercise Default | Director Can Change? | Admin Can Override? |
|------------|------------------|---------------------|---------------------|
| Disabled | OFF | No (hidden) | No |
| Optional | OFF | Yes (toggle) | N/A |
| Required | ON | No (locked) | Yes (can disable) |

## Status Workflow

### With Approval Enabled

```
┌────────┐  Submit   ┌───────────┐  Approve   ┌──────────┐  Schedule  ┌──────────────┐
│ Draft  │─────────►│ Submitted │──────────►│ Approved │──────────►│ Synchronized │
└────────┘          └───────────┘           └──────────┘           └──────────────┘
     ▲                    │                      │                        │
     │     Reject         │                      │ Revert                 │ Release
     └────────────────────┘                      │                        ▼
     │                                           │                  ┌──────────┐
     └───────────────────────────────────────────┘                  │ Released │
                                                                    └────┬─────┘
                                                                         │ Complete
                                                                         ▼
                                                                   ┌──────────┐
                                                                   │ Complete │
                                                                   └──────────┘

Parallel paths:
- Any status except Complete → Deferred (cancelled)
- Draft/Submitted/Approved → Obsolete (soft delete)
```

### Without Approval (Simplified)

```
┌────────┐  (auto)   ┌──────────┐  Schedule  ┌──────────────┐  Release  ┌──────────┐
│ Draft  │─────────►│ Approved │──────────►│ Synchronized │─────────►│ Released │
└────────┘          └──────────┘           └──────────────┘          └──────────┘
```

## Permission Matrix

Approval permissions are **configurable per organization** (see S11). Default settings shown below:

| Action | Admin | Director | Controller | Evaluator | Observer |
|--------|:-----:|:--------:|:----------:|:---------:|:--------:|
| Configure org approval policy | ✅ | ❌ | ❌ | ❌ | ❌ |
| Configure approval permissions | ✅ | ❌ | ❌ | ❌ | ❌ |
| Override org policy for exercise | ✅ | ❌ | ❌ | ❌ | ❌ |
| Configure exercise approval | ✅ | ✅ | ❌ | ❌ | ❌ |
| Create inject (Draft) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Edit Draft inject | ✅ | ✅ | Own | ❌ | ❌ |
| Submit for approval | ✅ | ✅ | ✅ | ❌ | ❌ |
| Approve inject | ✅ | ✅* | ⚙️* | ⚙️* | ❌ |
| Reject inject | ✅ | ✅* | ⚙️* | ⚙️* | ❌ |
| Approve own inject | ⚙️ | ⚙️ | ⚙️ | ⚙️ | ❌ |
| Batch approve | ✅ | ✅* | ⚙️* | ⚙️* | ❌ |
| Revert approval | ✅ | ✅ | ❌ | ❌ | ❌ |
| Publish exercise | ✅ | ✅ | ❌ | ❌ | ❌ |
| View approval queue | ✅ | ✅ | ✅ | ✅ | ✅ |

**Legend:**
- ✅ Always permitted
- ❌ Never permitted
- ✅* Default permitted, can be disabled by Admin
- ⚙️* Configurable by Admin (default: disabled)
- ⚙️ Configurable via Self-Approval Policy (default: never allowed)

**Self-Approval Policy Options:**
- Never allowed (default) - Users cannot approve their own submissions
- Allowed with warning - Users can self-approve with confirmation dialog
- Always allowed - No restrictions

## User Stories

| Story | Title | Priority | Points | Dependencies |
|-------|-------|----------|--------|--------------|
| S00 | HSEEP Inject Status Enum | P0 | 3 | None |
| S01 | Organization Approval Configuration | P1 | 3 | S00 |
| S02 | Exercise Approval Configuration | P1 | 3 | S01 |
| S03 | Submit Inject for Approval | P0 | 3 | S00 |
| S04 | Approve or Reject Inject | P0 | 5 | S03 |
| S05 | Batch Approval Actions | P1 | 5 | S04, S11 |
| S06 | Approval Queue View | P1 | 3 | S03 |
| S07 | Exercise Go-Live Gate | P0 | 3 | S04 |
| S08 | Approval Notifications | P2 | 5 | S04, S05 |
| S09 | Revert Approval Status | P1 | 2 | S04 |
| S10 | Configurable Status Workflow | P2 | 8 | S00, S01 |
| S11 | Configurable Approval Permissions | P1 | 5 | S01, S04 |
| **S12** | **Batch Approval Integration in MSEL View** | **P1** | **3** | **S05** |
| **S13** | **Quick Submit Action in MSEL Table Row** | **P1** | **2** | **S03** |
| **S14** | **Approval Actions in InjectDetailDrawer** | **P2** | **3** | **S03, S04** |
| **S15** | **Edit Invalidates Approval** | **P0** | **3** | **S03, S04** |
| **Total** | | | **59** | |

## Data Model Changes

### New Enum: InjectStatus (HSEEP-Compliant)

```csharp
/// <summary>
/// HSEEP-compliant inject status values per FEMA PrepToolkit.
/// </summary>
public enum InjectStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Synchronized = 3,
    Released = 4,
    Complete = 5,
    Deferred = 6,
    Obsolete = 7
}
```

### New Enum: ApprovalPolicy

```csharp
/// <summary>
/// Organization-level inject approval policy.
/// </summary>
public enum ApprovalPolicy
{
    Disabled = 0,   // Approval workflow not available
    Optional = 1,   // Directors can enable per exercise
    Required = 2    // Required for all exercises (Admin can override)
}
```

### Organization Entity (New Fields)

```csharp
/// <summary>Default inject approval policy for this organization.</summary>
public ApprovalPolicy InjectApprovalPolicy { get; set; } = ApprovalPolicy.Optional;
```

### Exercise Entity (New Fields)

```csharp
/// <summary>Whether inject approval workflow is enabled for this exercise.</summary>
public bool RequireInjectApproval { get; set; } = false;

/// <summary>If true, Admin has overridden org "Required" policy to disable approval.</summary>
public bool ApprovalPolicyOverridden { get; set; } = false;
```

### Inject Entity (New/Modified Fields)

```csharp
// Status field updated to new enum
public InjectStatus Status { get; set; } = InjectStatus.Draft;

// Submission tracking
public Guid? SubmittedById { get; set; }
public DateTime? SubmittedAt { get; set; }
public User? SubmittedBy { get; set; }

// Approval tracking
public Guid? ApprovedById { get; set; }
public DateTime? ApprovedAt { get; set; }
public User? ApprovedBy { get; set; }
public string? ApproverNotes { get; set; }  // Max 1000 chars

// Rejection tracking
public Guid? RejectedById { get; set; }
public DateTime? RejectedAt { get; set; }
public User? RejectedBy { get; set; }
public string? RejectionReason { get; set; }  // Max 1000 chars, required on reject
```

### New Entity: InjectStatusHistory

```csharp
/// <summary>
/// Audit trail for inject status changes.
/// </summary>
public class InjectStatusHistory : BaseEntity
{
    public Guid InjectId { get; set; }
    public Inject Inject { get; set; } = null!;
    
    public InjectStatus FromStatus { get; set; }
    public InjectStatus ToStatus { get; set; }
    
    public Guid ChangedById { get; set; }
    public User ChangedBy { get; set; } = null!;
    public DateTime ChangedAt { get; set; }
    
    public string? Notes { get; set; }  // Rejection reason, approver notes, etc.
}
```

### New Entity: ApprovalNotification

```csharp
/// <summary>
/// In-app notification for approval workflow events.
/// </summary>
public class ApprovalNotification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    
    public Guid? InjectId { get; set; }  // Null for batch notifications
    public Inject? Inject { get; set; }
    
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
}

public enum NotificationType
{
    InjectSubmitted = 0,      // To approvers: inject needs review
    InjectApproved = 1,       // To author: your inject was approved
    InjectRejected = 2,       // To author: your inject was rejected
    BatchApproved = 3,        // To authors: multiple injects approved
    BatchRejected = 4,        // To authors: multiple injects rejected
    ApprovalReminder = 5,     // To approvers: pending items reminder
    ExerciseReadyToPublish = 6 // To director: all injects approved
}
```

## API Endpoints

### Approval Actions

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/injects/{id}/submit` | Submit inject for approval |
| POST | `/api/injects/{id}/approve` | Approve single inject |
| POST | `/api/injects/{id}/reject` | Reject single inject |
| POST | `/api/injects/{id}/revert` | Revert approved inject to submitted |
| POST | `/api/injects/batch/approve` | Batch approve multiple injects |
| POST | `/api/injects/batch/reject` | Batch reject multiple injects |

### Queries

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/exercises/{id}/approval-status` | Get approval summary for exercise |
| GET | `/api/exercises/{id}/injects?status=submitted` | Get injects pending approval |
| GET | `/api/notifications` | Get user's notifications |
| PUT | `/api/notifications/{id}/read` | Mark notification as read |
| PUT | `/api/notifications/read-all` | Mark all notifications as read |

### Configuration

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/organizations/{id}/settings` | Get org settings including approval policy |
| PUT | `/api/organizations/{id}/settings` | Update org settings |
| PUT | `/api/exercises/{id}/settings` | Update exercise settings including approval |

## UI Components

### New Components

| Component | Location | Purpose |
|-----------|----------|---------|
| `ApprovalPolicySettings` | Org Settings page | Configure org-level policy |
| `ExerciseApprovalToggle` | Exercise Settings | Enable/disable per exercise |
| `SubmitForApprovalButton` | Inject detail/row | Submit action |
| `ApproveRejectButtons` | Inject detail | Approval actions |
| `ApprovalQueueTab` | MSEL view | Filter to pending items |
| `BatchApprovalToolbar` | MSEL view | Bulk actions when items selected |
| `ApprovalStatusChip` | Inject row/detail | Show current status |
| `RejectionReasonDialog` | Modal | Capture rejection reason |
| `ApproverNotesField` | Approve dialog | Optional review notes |
| `NotificationBell` | App header | Show unread count, dropdown |
| `NotificationList` | Dropdown/page | List of notifications |
| `GoLiveBlockerAlert` | Exercise publish | Show unapproved count |

### Status Chip Colors

| Status | Color | Icon |
|--------|-------|------|
| Draft | Gray | `fa-pencil` |
| Submitted | Amber/Yellow | `fa-clock` |
| Approved | Green | `fa-check` |
| Synchronized | Blue | `fa-calendar-check` |
| Released | Purple | `fa-paper-plane` |
| Complete | Dark Green | `fa-circle-check` |
| Deferred | Orange | `fa-ban` |
| Obsolete | Light Gray | `fa-archive` |

## Testing Requirements

### Unit Tests

- Status transition validation (valid/invalid transitions)
- Permission checks for each action
- Self-approval prevention
- Org policy enforcement
- Exercise override logic

### Integration Tests

- Full approval workflow (submit → approve → sync → release)
- Rejection and resubmission flow
- Batch approval with mixed authors
- Go-live gate blocking
- Notification generation

### E2E Tests

- Controller submits, Director approves
- Director rejects with reason, Controller sees feedback
- Batch approval from queue view
- Publish blocked, view unapproved, approve, publish succeeds

## Migration Strategy

### Database Migration

1. Add new `InjectStatus` enum values (keep existing as subset)
2. Add approval tracking columns to `Injects` table
3. Add `ApprovalPolicy` column to `Organizations` table
4. Add `RequireInjectApproval` column to `Exercises` table
5. Create `InjectStatusHistory` table
6. Create `ApprovalNotifications` table
7. Migrate existing status values:
   - `Pending` → `Draft`
   - `Fired` → `Released`
   - `Skipped` → `Deferred`

### Seed Data Updates

Update demo data to include:
- Mix of Draft, Submitted, Approved injects
- Sample rejection reasons
- Sample approver notes
- Notification examples

## Dependencies

- **Requires:** Phase D (Exercise Conduct) for status workflow integration
- **Blocks:** Phase E (Observations) - evaluators need stable inject status
- **Related:** Exercise Status Workflow (Draft → Published gate)

## Open Items for Future Enhancement

- [ ] Email notifications (requires email service integration)
- [ ] Approval delegation (Director assigns approval authority)
- [ ] Approval deadlines with reminders
- [ ] Approval analytics dashboard
- [ ] Mobile-optimized approval queue

## Files in This Feature

```
docs/features/inject-approval-workflow/
├── FEATURE.md                         # This file
├── S00-hseep-status-enum.md
├── S01-org-approval-config.md
├── S02-exercise-approval-config.md
├── S03-submit-for-approval.md
├── S04-approve-reject-inject.md
├── S05-batch-approval.md
├── S06-approval-queue.md
├── S07-exercise-golive-gate.md
├── S08-approval-notifications.md
├── S09-revert-approval.md
├── S10-configurable-status-workflow.md
├── S11-configurable-approval-permissions.md
├── S12-batch-approval-integration.md  # NEW: Integration of batch approval in MSEL view
├── S13-quick-submit-table-action.md   # NEW: Quick submit from table row
├── S14-drawer-approval-actions.md     # NEW: Approval actions in conduct drawer
└── S15-edit-invalidates-approval.md   # NEW: Auto-revert to Draft when editing submitted/approved injects
```
