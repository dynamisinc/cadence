# exercise-crud/S04: Archive Exercise

## Story

**As an** Administrator or Exercise Director,
**I want** to archive completed exercises,
**So that** they are removed from the active list while preserving data for future reference and after-action review.

## Context

Exercises accumulate over time. Completed exercises clutter the active list, making it harder to find current work. Archiving provides a "soft delete" that hides exercises from default views while retaining all data for compliance, auditing, and historical reference.

Archiving differs from deletion:
- **Archive**: Exercise hidden but fully preserved, can be unarchived
- **Delete**: Permanent removal (not implemented in MVP for data safety)

Only Administrators and Exercise Directors can archive exercises, and only exercises in Completed status can be archived.

## Acceptance Criteria

### Archive Action

- [ ] **Given** I am an Administrator or Exercise Director, **when** I view a Completed exercise, **then** I see an "Archive" option
- [ ] **Given** I am logged in as Controller, Evaluator, or Observer, **when** I view a Completed exercise, **then** I do NOT see an "Archive" option
- [ ] **Given** an exercise is in Draft or Active status, **when** I view that exercise, **then** the "Archive" option is not available
- [ ] **Given** I click "Archive", **when** the confirmation dialog appears, **then** I see "Archive this exercise? It will be hidden from the exercise list but can be restored later."
- [ ] **Given** I confirm the archive action, **when** the action completes, **then** the exercise status changes to "Archived"
- [ ] **Given** I confirm the archive action, **when** the action completes, **then** I see a success message "Exercise archived successfully"
- [ ] **Given** I cancel the archive action, **when** the dialog closes, **then** no changes are made to the exercise

### Behavior of Archived Exercises

- [ ] **Given** an exercise is Archived, **when** I view the default exercise list, **then** the archived exercise is NOT displayed
- [ ] **Given** an exercise is Archived, **when** I enable "Show Archived" filter, **then** the archived exercise IS displayed
- [ ] **Given** I view an Archived exercise, **when** the page loads, **then** it is displayed in read-only mode
- [ ] **Given** I view an Archived exercise, **when** I look at the MSEL and injects, **then** all data is visible but not editable
- [ ] **Given** an Archived exercise, **when** displayed in any list, **then** it shows an "Archived" badge/indicator

### Unarchive Action

- [ ] **Given** I view an Archived exercise as Administrator or Exercise Director, **when** I look for actions, **then** I see an "Unarchive" option
- [ ] **Given** I click "Unarchive", **when** the action completes, **then** the exercise status changes back to "Completed"
- [ ] **Given** I unarchive an exercise, **when** I view the default exercise list, **then** the exercise appears again

### Audit Trail

- [ ] **Given** I archive an exercise, **when** the action completes, **then** an audit record is created with: action type, user ID, timestamp
- [ ] **Given** I unarchive an exercise, **when** the action completes, **then** an audit record is created with: action type, user ID, timestamp

## Out of Scope

- Bulk archive multiple exercises
- Permanent deletion of exercises
- Auto-archiving based on age or date
- Exporting before archiving
- Archive retention policies

## Dependencies

- exercise-crud/S01: Create Exercise
- exercise-crud/S03: View Exercise List (for archived filter)
- Exercise entity with status field (see `_core/exercise-entity.md`)

## Open Questions

- [ ] Should there be a time limit after which archived exercises auto-delete? (Likely policy decision)
- [ ] Can an archived exercise be duplicated/cloned for a new exercise?
- [ ] Should archiving require entering a reason or comment?

## Domain Terms

| Term | Definition |
|------|------------|
| Archive | Action to hide a completed exercise from default views while preserving all data |
| Unarchive | Action to restore an archived exercise to visible (Completed) status |
| Completed Status | Exercise has finished, all conduct activities concluded |
| Archived Status | Exercise hidden from default views, data preserved |

## UI/UX Notes

### Archive Confirmation Dialog
```
┌─────────────────────────────────────────────┐
│  Archive Exercise                           │
├─────────────────────────────────────────────┤
│                                             │
│  Archive "Hurricane Response 2025"?         │
│                                             │
│  The exercise will be hidden from the       │
│  exercise list but can be restored later.   │
│                                             │
│              [Cancel]  [Archive]            │
└─────────────────────────────────────────────┘
```

- Archive action should be accessible from exercise detail view, not list view (reduces accidental archiving)
- Archived exercises should have distinct visual treatment (grayed out, badge)
- Unarchive should be equally prominent when viewing archived exercise

## Technical Notes

- Archive is a status change, not a separate flag
- Status transition: Completed → Archived (archive) and Archived → Completed (unarchive)
- Ensure database indexes support filtering by status for performance
- Consider soft-delete pattern for future permanent deletion feature
