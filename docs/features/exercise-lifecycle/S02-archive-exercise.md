# Story: Archive Exercise

## S02-archive-exercise.md

**As an** Exercise Director or Administrator,
**I want** to archive an exercise directly from the exercise list or detail page,
**So that** I can quickly remove it from active views without navigating the full status workflow.

### Context

The current status workflow (Draft → Published → Active → Completed) doesn't account for exercises that need to be hidden but not necessarily "completed"—such as abandoned drafts, cancelled exercises, or old exercises cluttering the list. Archive provides a quick "soft delete" that preserves data while decluttering the active exercise list.

Archive can be performed on exercises in ANY status (Draft, Published, Active, or Completed) and bypasses the normal workflow entirely.

### Acceptance Criteria

- [ ] **Given** I am an Exercise Director or Administrator, **when** I view an exercise (any status), **then** I see an "Archive" action in the menu
- [ ] **Given** I am a Controller, Evaluator, or Observer, **when** I view an exercise, **then** I do NOT see the Archive action
- [ ] **Given** I click "Archive", **when** the confirmation dialog appears, **then** it shows the exercise name and current status
- [ ] **Given** I click "Archive", **when** the confirmation dialog appears, **then** it warns that the exercise will be hidden from normal views
- [ ] **Given** I confirm the archive action, **when** the operation completes, **then** the exercise status changes to "Archived"
- [ ] **Given** I confirm the archive action, **when** the operation completes, **then** `PreviousStatus` stores the original status
- [ ] **Given** I confirm the archive action, **when** the operation completes, **then** `ArchivedAt` is set to current UTC timestamp
- [ ] **Given** I confirm the archive action, **when** the operation completes, **then** `ArchivedById` is set to current user
- [ ] **Given** an exercise is archived, **when** I view the default exercise list, **then** the archived exercise is NOT displayed
- [ ] **Given** an exercise is already archived, **when** I attempt to archive it again, **then** I receive an error message
- [ ] **Given** an exercise is archived, **when** the action completes, **then** an audit record is created with user, timestamp, and previous status

### Out of Scope

- Bulk archive (future enhancement)
- Automatic archive based on date (future enhancement)
- Viewing archived exercises (S03)
- Restoring archived exercises (S04)

### UI/UX Notes

**Archive Action Location:**
- Exercise card dropdown menu (list view) — kebab menu (⋮)
- Exercise detail page header — Actions menu

**Archive Confirmation Dialog:**

```
┌─────────────────────────────────────────────────────────────┐
│  Archive Exercise                                       [×] │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ⚠️  Are you sure you want to archive this exercise?        │
│                                                              │
│  Exercise: "2026 Full Scale Exercise"                        │
│  Current Status: Completed                                   │
│                                                              │
│  Archived exercises are hidden from normal views but can     │
│  be restored by an administrator.                            │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│                        [Cancel]    [Archive Exercise]        │
└─────────────────────────────────────────────────────────────┘
```

**Styling:**
- Use warning color (amber/orange) for archive action icon
- Confirmation button: Primary color (not danger red—archive is reversible)

### API Specification

**Endpoint:**
```
POST /api/exercises/{id}/archive
```

**Request:** No body required

**Response (200 OK):**
```json
{
  "id": "guid",
  "name": "2026 Full Scale Exercise",
  "status": "Archived",
  "previousStatus": "Completed",
  "archivedAt": "2026-01-18T14:30:00Z",
  "archivedBy": {
    "id": "guid",
    "name": "John Smith"
  }
}
```

**Error Responses:**
- `403 Forbidden` — User lacks permission (not Exercise Director or Admin)
- `404 Not Found` — Exercise does not exist
- `409 Conflict` — Exercise is already archived

### Domain Terms

| Term | Definition |
|------|------------|
| Archive | Soft delete that hides exercise from normal views but preserves all data |
| Exercise Status | Current state: Draft, Published, Active, Completed, Archived |
| PreviousStatus | The status before archiving, used for restoration |

### Dependencies

- S01 - Lifecycle Tracking Fields (entity changes)

### Deliverables

1. Add `ArchiveAsync` method to `IExerciseService`
2. Add `POST /api/exercises/{id}/archive` controller action
3. Add authorization check (Exercise Director or Administrator)
4. Create `ArchiveExerciseDialog` React component
5. Add archive action to `ExerciseCard` dropdown menu
6. Add archive action to exercise detail page
7. Add `useArchiveExercise` mutation hook
8. Update exercise list to exclude archived by default
9. Integration tests for archive operation
10. Test authorization rules
