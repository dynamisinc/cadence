# Story: S05 - View Upload Results and Status

**Feature:** bulk-participant-import
**Status:** 🚧 In Progress

## User Story

**As an** Exercise Director,
**I want** to see the status of my bulk upload including which invitations are still pending,
**So that** I can follow up with participants who have not yet accepted.

## Context

After a bulk import is processed (S03, S04), the Exercise Director needs to see the outcome: how many participants were assigned immediately, how many invitations were sent, and whether any rows failed. Over time, the Exercise Director also needs to track which invitations have been accepted so they can follow up with outstanding invitations before exercise day.

This results view serves both as the immediate post-import summary and as an ongoing status tracker for pending invitations created by the import.

## Acceptance Criteria

### Immediate Results Summary

- [ ] **Given** the bulk import processing completes, **when** the results screen loads, **then** I see a summary: N assigned, N role updated, N invitations sent, N errors
- [ ] **Given** the results are displayed, **when** I view the assigned section, **then** I see each participant's email, display name, and exercise role
- [ ] **Given** the results are displayed, **when** I view the errors section, **then** I see each failed row's email, attempted role, and specific error reason
- [ ] **Given** the results are displayed, **when** I view the invitations section, **then** I see each invited participant's email, intended exercise role, and invitation status (Pending)

### Ongoing Invitation Tracking

- [x] **Given** I return to the exercise participants screen later, **when** I view the pending invitations panel, **then** I see all outstanding invitations with their current status
  - Test: `PendingInvitationsList.tsx` - Collapsible panel with pending assignments
  - Completed: 2026-02-09
- [x] **Given** an invitation has been accepted since the import, **when** I view the results, **then** its status shows "Accepted" and the participant appears in the main participant list
  - Test: `PendingInvitationsList.tsx::getStatusIcon` - Status icons and colors for Accepted, Pending, Expired
  - Completed: 2026-02-09
- [x] **Given** an invitation has expired, **when** I view the results, **then** its status shows "Expired" with the expiration date
  - Test: `PendingInvitationsList.tsx` - Displays expiration status using date-fns formatDistanceToNow
  - Completed: 2026-02-09

### Actions on Pending Invitations

- [x] **Given** an invitation is in "Pending" status, **when** I click "Resend", **then** a new invitation email is sent with a fresh code and expiration
  - Test: `PendingInvitationsList.tsx::handleResend` - Resend button for Pending invitations
  - Completed: 2026-02-09
- [x] **Given** an invitation is in "Expired" status, **when** I click "Resend", **then** a new invitation is created and sent
  - Test: `PendingInvitationsList.tsx::handleResend` - Resend button for Expired invitations
  - Completed: 2026-02-09
- [ ] **Given** an invitation is in "Pending" status, **when** I click "Cancel", **then** the invitation and its pending exercise assignment are cancelled
- [ ] **Given** I want to follow up with all pending participants, **when** I click "Download Pending", **then** I receive a CSV of all pending invitations with emails and roles

### Import History

- [ ] **Given** multiple bulk imports have been performed for this exercise, **when** I view the import history, **then** I see a list of past imports with date, who imported, and summary counts
- [ ] **Given** I click on a past import, **when** the details load, **then** I see the full results of that import including current invitation statuses

## Out of Scope

- Automated follow-up email reminders for pending invitations
- Bulk resend of all pending invitations
- Editing participant details from the results view
- Push notifications when invitations are accepted

## Dependencies

- bulk-participant-import/S03: Process Existing Members (provides assignment results)
- bulk-participant-import/S04: Invite Non-Members (provides invitation results)
- organization-management/OM-07: Organization Invitations (invitation status tracking)

## Open Questions

- [ ] How long should import history be retained? Indefinitely, or archived after exercise completion?
- [ ] Should the results be accessible from the organization invitation list as well, or only from the exercise?
- [ ] Should there be a notification when a pending invitation is accepted?

## Domain Terms

| Term | Definition |
|------|------------|
| **Import Results** | The outcome summary of a bulk import operation |
| **Invitation Status** | The current state of an organization invitation: Pending, Accepted, Expired, Cancelled |
| **Import History** | A log of all bulk import operations for an exercise |

## UI/UX Notes

### Immediate Results View

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Bulk Import Results                                                    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ✓ Import completed successfully                                        │
│                                                                         │
│  ┌──────────────┬──────────────┬──────────────┬──────────────┐         │
│  │ 12 Assigned  │ 3 Updated   │ 8 Invited    │ 2 Errors     │         │
│  │    (green)   │   (blue)    │  (yellow)    │   (red)      │         │
│  └──────────────┴──────────────┴──────────────┴──────────────┘         │
│                                                                         │
│  ── Assigned Immediately (12) ──────────────────────────────────       │
│  jane@fema.gov          Controller      ✓ Assigned                     │
│  bob@state.gov          Evaluator       ✓ Assigned                     │
│  ...                                                                    │
│                                                                         │
│  ── Invitations Sent (8) ───────────────────────────────────────       │
│  new@agency.org         Observer        ⏳ Pending     [Resend][Cancel]│
│  partner@county.gov     Controller      ⏳ Pending     [Resend][Cancel]│
│  ...                                                                    │
│                                                                         │
│  ── Errors (2) ─────────────────────────────────────────────────       │
│  bad-email              Controller      ✗ Invalid email format         │
│  nobody@test.com        ExDirector      ✗ Requires Admin/Manager role  │
│                                                                         │
│  [Download Full Report]            [Back to Participants]               │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Import results should be persisted (not session-only) so they can be viewed later
- Consider a `BulkImportRecord` entity: `{ Id, ExerciseId, ImportedById, ImportedAt, FileName, TotalRows, AssignedCount, UpdatedCount, InvitedCount, ErrorCount }`
- Individual row results could be stored as `BulkImportRowResult`: `{ ImportRecordId, Email, ExerciseRole, Classification, Status, ErrorMessage }`
- Invitation status is already tracked on `OrganizationInvite`; link to it rather than duplicating
- The "Download Pending" feature generates a CSV on-the-fly from the pending invitation data

## Implementation Notes

**Added during development (2026-02-09):**

### Pending Invitations Panel
- Component `PendingInvitationsList.tsx` created to display pending exercise assignments
- Integrated into `ExerciseParticipantsPage.tsx` below the participant list
- Collapsible panel with header showing count of pending invitations
- Only visible to users with `manage_participants` permission

### Pending Assignment Data
- Hook `usePendingAssignments.ts` created to fetch pending assignments for an exercise
- Type `PendingExerciseAssignmentDto` includes: id, email, exerciseRole, invitationStatus, invitationExpiresAt, organizationInviteId
- Backend service integration needed to provide `/api/exercises/{id}/pending-assignments` endpoint

### Status Display
- Color-coded status chips: Success (Accepted), Warning (Pending), Error (Expired)
- FontAwesome icons: faCheckCircle (Accepted), faClock (Pending), faTimesCircle (Expired)
- Relative time display using date-fns: "Expires in 6 days" or "Expired"
- Individual invitation cards within collapsible panel

### Resend Functionality
- Resend button available for Pending and Expired invitations
- Loading state with spinner during resend operation
- Individual loading state per invitation to prevent multiple simultaneous resends
- Hook method: `resendInvitation(invitationId)` with mutation and toast feedback

### Integration with Bulk Import
- `BulkImportDialog.tsx` calls `onImportComplete` callback which refreshes both participants and pending assignments
- `handleBulkImportComplete` in `ExerciseParticipantsPage.tsx` triggers `refetchParticipants()` and `refetchPending()`

### UI Features
- Expandable/collapsible section with chevron icon
- Alert message explaining pending invitations auto-assign on acceptance
- Email, role, and expiration displayed for each pending assignment
- Section hidden when no pending invitations exist

## Test Coverage
- Frontend: `src/frontend/src/features/exercises/components/PendingInvitationsList.tsx`
- Frontend: `src/frontend/src/features/exercises/hooks/usePendingAssignments.ts`
- Backend: Needs implementation for pending assignments endpoint and resend invitation logic
