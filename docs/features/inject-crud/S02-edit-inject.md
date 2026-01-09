# Story: S02 - Edit Inject

## User Story

**As a** Controller or Exercise Director,
**I want** to edit existing injects,
**So that** I can refine inject content and correct errors.

## Context

Injects often need refinement as exercise planning progresses. This story covers editing all inject fields including times, content, and organizational links. Editing behavior varies based on exercise status - more fields are editable during Draft status than during Active conduct.

## Acceptance Criteria

### Basic Editing
- [ ] **Given** I am viewing the MSEL list, **when** I click on an inject row, **then** I navigate to the inject detail view
- [ ] **Given** I am on inject detail, **when** I click "Edit", **then** the form becomes editable
- [ ] **Given** I am editing, **when** I modify any field, **then** the field shows as modified (visual indicator)
- [ ] **Given** I have unsaved changes, **when** I click "Save", **then** changes are persisted and I see a success message
- [ ] **Given** I have unsaved changes, **when** I click "Cancel", **then** I see a confirmation dialog
- [ ] **Given** I confirm cancel, **when** the dialog closes, **then** all changes are discarded

### Field Validation
- [ ] **Given** I clear the Title field, **when** I try to save, **then** I see a validation error
- [ ] **Given** I clear the Scheduled Time, **when** I try to save, **then** I see a validation error
- [ ] **Given** I enter Scenario Time without Scenario Day, **when** I try to save, **then** I see a validation error

### Exercise Status Impact
- [ ] **Given** the exercise is in Draft status, **when** I edit an inject, **then** all fields are editable
- [ ] **Given** the exercise is in Active status (during conduct), **when** I edit an inject, **then** I can edit: Title, Description, Expected Action, Notes
- [ ] **Given** the exercise is Active and inject is Pending, **when** I edit, **then** I can also edit Scheduled Time
- [ ] **Given** the exercise is Active and inject is Fired, **when** I try to edit, **then** only Notes field is editable
- [ ] **Given** the exercise is Archived, **when** I try to edit, **then** I see a message that archived exercises cannot be modified

### Inject Number
- [ ] **Given** I am editing an inject, **when** I view the Inject Number field, **then** it is read-only (never editable)

### Conflict Detection
- [ ] **Given** another user has modified the inject, **when** I try to save my changes, **then** I see a conflict notification
- [ ] **Given** a conflict is detected, **when** I view the notification, **then** I can choose to reload or overwrite

## Out of Scope

- Inline editing in the MSEL list view (future consideration)
- Bulk editing multiple injects
- Edit history/versioning for individual injects
- Collaborative real-time editing

## Dependencies

- inject-crud/S01: Create Inject (inject must exist)
- _cross-cutting/S03: Auto-save (saves drafts during editing)
- exercise-conduct: Exercise Conduct (determines edit restrictions)

## Open Questions

- [ ] Should there be an "undo" feature for recent edits?
- [ ] Should editing during conduct require approval?
- [ ] Should we track edit history per inject?

## Domain Terms

| Term | Definition |
|------|------------|
| Edit Mode | State where inject fields can be modified |
| View Mode | State where inject is displayed read-only |
| Conflict | When changes collide with another user's modifications |

## UI/UX Notes

### Edit Mode Indicators

```
┌─────────────────────────────────────────────────────────────────────────┐
│  INJ-003: Evacuation order issued                    [Cancel] [Save]   │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ ✎ EDITING                                    Unsaved changes   │   │
│  └─────────────────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Title *                                               [Modified ●]    │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ County issues mandatory evacuation order for Zones A and B     │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ...                                                                   │
└─────────────────────────────────────────────────────────────────────────┘
```

### Active Exercise Edit Warning

```
┌─────────────────────────────────────────────────────────────────────────┐
│  ⚠️ Exercise In Progress                                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  This exercise is currently active. Some fields are restricted:        │
│                                                                         │
│  ✓ Editable: Title, Description, Expected Action, Notes                │
│  🔒 Locked: Scheduled Time (inject already fired)                      │
│  🔒 Locked: Scenario Day/Time, From, To, Method                        │
│                                                                         │
│  Changes will be visible to all participants immediately.              │
│                                                                         │
│                                    [Cancel]  [Edit Anyway]              │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Conflict Resolution

```
┌─────────────────────────────────────────────────────────────────────────┐
│  ⚠️ Conflict Detected                                               ✕  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  This inject was modified by James Washington at 2:34 PM               │
│  while you were editing.                                               │
│                                                                         │
│  Your changes:                                                         │
│  • Title: "County issues mandatory evacuation order"                   │
│                                                                         │
│  Their changes:                                                        │
│  • Description updated                                                 │
│  • Expected Action updated                                             │
│                                                                         │
│  [Discard My Changes]  [Reload & Merge]  [Overwrite Their Changes]    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Implement optimistic locking using version/timestamp field
- Use ETag headers for conflict detection
- Auto-save should not trigger conflict (merge silently if possible)
- Consider field-level conflict resolution in future
