# Story: S03 - Process Existing Organization Members

## User Story

**As an** Exercise Director,
**I want** existing organization members from my upload to be immediately assigned to the exercise,
**So that** participants who already have accounts are ready without delay.

## Context

When the user confirms the import (from the preview in S02), the system processes rows classified as "Assign" and "Update". These are the simplest cases: the participant is already an organization member, so they can be immediately added to (or updated in) the exercise. This story handles the immediate-assignment portion of the bulk import; non-member invitations are handled in S04.

This builds on the existing `ExerciseParticipantService.AddParticipantAsync` and `BulkUpdateParticipantsAsync` methods, extending them to work within the bulk import workflow.

## Acceptance Criteria

### Immediate Assignment (Classify: Assign)

- [ ] **Given** the import is confirmed, **when** processing a row classified as "Assign", **then** an `ExerciseParticipant` record is created with the specified Exercise Role
- [ ] **Given** an "Assign" row specifies Exercise Director, **when** the user's System Role is Admin or Manager, **then** the assignment succeeds
- [ ] **Given** an "Assign" row specifies Exercise Director, **when** the user's System Role is User, **then** the row fails with an error and processing continues with the next row
- [ ] **Given** an "Assign" row's user was previously soft-deleted from this exercise, **when** processing, **then** the participant is reactivated with the new role

### Role Updates (Classify: Update)

- [ ] **Given** the import is confirmed, **when** processing a row classified as "Update - Role Change", **then** the participant's Exercise Role is updated to the new value
- [ ] **Given** the import is confirmed, **when** processing a row classified as "Update - No Change", **then** no database change is made and the row is counted as "skipped"

### Transaction and Error Handling

- [ ] **Given** the import is confirmed, **when** one row fails (e.g., Exercise Director validation), **then** other rows continue processing (no full rollback)
- [ ] **Given** all "Assign" and "Update" rows are processed, **when** processing completes, **then** a count is returned: N assigned, N updated, N skipped, N failed
- [ ] **Given** a database error occurs during processing, **when** the error is transient, **then** the row is marked as failed with "Database error - please retry"

### Audit Trail

- [ ] **Given** a participant is assigned via bulk import, **when** the record is created, **then** `AssignedById` is set to the importing user's ID
- [ ] **Given** a participant is assigned via bulk import, **when** the record is created, **then** `AssignedAt` is set to the current timestamp
- [ ] **Given** multiple participants are assigned, **when** viewing the exercise participant list, **then** bulk-imported participants are indistinguishable from individually-added participants

## Out of Scope

- Sending notification emails to assigned participants (future enhancement)
- Real-time SignalR updates during bulk processing (future enhancement)
- Undo/rollback of bulk assignments

## Dependencies

- bulk-participant-import/S02: Preview and Validate Import (provides confirmed import data)
- Existing `ExerciseParticipantService` (reuse assignment logic)

## Open Questions

- [ ] Should bulk-assigned participants receive an email notification of their assignment?
- [ ] Should there be a bulk import audit log entry separate from individual assignment records?
- [ ] What is the maximum number of immediate assignments that should be processed synchronously vs. queued as a background job?

## Domain Terms

| Term | Definition |
|------|------------|
| **Immediate Assignment** | Creating an ExerciseParticipant record for a user who is already an organization member |
| **Reactivation** | Restoring a previously soft-deleted participant record |
| **Partial Failure** | When some rows in a bulk operation fail while others succeed |

## Technical Notes

- Use the existing `ExerciseParticipantService.AddParticipantAsync` logic for per-row processing to ensure consistent validation
- Consider wrapping each row in its own try-catch to prevent one failure from stopping the batch
- For large batches (100+ assignments), consider processing in chunks with progress updates
- The existing `BulkUpdateParticipantsRequest` DTO accepts `UserId` + `Role` pairs; extend or create a new DTO that maps from email to UserId during processing
