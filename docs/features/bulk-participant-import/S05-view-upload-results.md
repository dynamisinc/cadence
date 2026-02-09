# Story: S05 - View Upload Results and Status

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

- [ ] **Given** I return to the exercise participants screen later, **when** I view the pending invitations panel, **then** I see all outstanding invitations with their current status
- [ ] **Given** an invitation has been accepted since the import, **when** I view the results, **then** its status shows "Accepted" and the participant appears in the main participant list
- [ ] **Given** an invitation has expired, **when** I view the results, **then** its status shows "Expired" with the expiration date

### Actions on Pending Invitations

- [ ] **Given** an invitation is in "Pending" status, **when** I click "Resend", **then** a new invitation email is sent with a fresh code and expiration
- [ ] **Given** an invitation is in "Expired" status, **when** I click "Resend", **then** a new invitation is created and sent
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
