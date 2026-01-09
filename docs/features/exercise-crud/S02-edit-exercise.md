# exercise-crud/S02: Edit Exercise

## Story

**As an** Administrator or Exercise Director,
**I want** to edit an existing exercise's details,
**So that** I can update information as exercise planning evolves or correct initial entries.

## Context

Exercise planning is iterative. Details change as the exercise design matures, venues are confirmed, dates shift, or lessons from planning meetings require updates. Users need to modify exercise metadata without affecting the underlying MSEL content.

Editing is restricted based on exercise status:
- **Draft**: All fields editable
- **Active**: Limited fields editable (name, description, end date)
- **Completed/Archived**: No editing allowed (read-only)

## Acceptance Criteria

### Access Control

- [ ] **Given** I am logged in as Administrator or Exercise Director, **when** I view an exercise I have access to, **then** I see an "Edit" option
- [ ] **Given** I am logged in as Controller, Evaluator, or Observer, **when** I view an exercise, **then** I do NOT see an "Edit" option
- [ ] **Given** I am an Exercise Director, **when** I try to edit an exercise I don't have access to, **then** I receive an "Access Denied" error

### Editing Draft Exercises

- [ ] **Given** the exercise is in Draft status, **when** I open the edit form, **then** all fields are editable: Name, Exercise Type, Description, Start Date, End Date
- [ ] **Given** I modify any field, **when** I save changes, **then** the exercise is updated with the new values
- [ ] **Given** I change the Exercise Type, **when** I save, **then** the type is updated (no downstream impacts in MVP)
- [ ] **Given** I edit an exercise, **when** I save, **then** the ModifiedAt timestamp is updated to current UTC time
- [ ] **Given** I edit an exercise, **when** I save, **then** the ModifiedBy field is set to my user ID

### Editing Active Exercises

- [ ] **Given** the exercise is in Active status, **when** I open the edit form, **then** only Name, Description, and End Date are editable
- [ ] **Given** the exercise is in Active status, **when** I view the edit form, **then** Start Date and Exercise Type are displayed but disabled with tooltip "Cannot modify during active exercise"
- [ ] **Given** I modify allowed fields on an active exercise, **when** I save changes, **then** the exercise is updated

### Editing Completed/Archived Exercises

- [ ] **Given** the exercise is in Completed status, **when** I try to edit, **then** I see a message "Completed exercises cannot be modified"
- [ ] **Given** the exercise is in Archived status, **when** I try to edit, **then** I see a message "Archived exercises cannot be modified"

### Validation

- [ ] **Given** I clear the Name field, **when** I try to save, **then** I see validation error "Exercise name is required"
- [ ] **Given** I enter a Name longer than 200 characters, **when** I try to save, **then** I see validation error "Name must be 200 characters or less"
- [ ] **Given** I set End Date before Start Date, **when** I try to save, **then** I see validation error "End date must be after start date"

### Cancellation

- [ ] **Given** I have unsaved changes, **when** I click "Cancel", **then** I am prompted to confirm "Discard unsaved changes?"
- [ ] **Given** I confirm discard, **when** the dialog closes, **then** no changes are saved
- [ ] **Given** I have no changes, **when** I click "Cancel", **then** I return to the previous view without confirmation

## Out of Scope

- Editing exercise time zone (see exercise-config/S03)
- Editing Practice Mode flag (see S05)
- Managing exercise participants (see exercise-config/S02)
- Changing exercise status (implicit through workflow actions)
- Bulk editing multiple exercises

## Dependencies

- exercise-crud/S01: Create Exercise
- Exercise entity schema (see `_core/exercise-entity.md`)
- Auto-save functionality (_cross-cutting/S03) - edit form should auto-save

## Open Questions

- [ ] Should there be an audit log showing all edits to an exercise?
- [ ] For Active exercises, should we allow extending Start Date if not yet reached?
- [ ] Should description support rich text formatting?

## Domain Terms

| Term | Definition |
|------|------------|
| Draft Status | Exercise can be fully configured, not yet activated |
| Active Status | Exercise is in progress, limited modifications allowed |
| Completed Status | Exercise has ended, read-only state |
| Archived Status | Exercise hidden from default views, read-only state |

## UI/UX Notes

- Edit form should mirror Create form layout for consistency
- Show clear indicator of exercise status affecting editability
- Disabled fields should explain why they're locked
- Consider inline editing for simple fields (name, description)
- Auto-save indicator should show "Saved" / "Saving..." / "Error"

## Technical Notes

- Implement optimistic locking using ModifiedAt timestamp
- If concurrent edit detected, show conflict resolution UI
- Validate server-side to enforce status-based edit restrictions
