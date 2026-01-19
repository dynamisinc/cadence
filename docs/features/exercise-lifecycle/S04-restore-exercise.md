# Story: Restore Archived Exercise

## S04-restore-exercise.md

**As an** Administrator,
**I want** to restore an archived exercise,
**So that** I can make it active again if it was archived by mistake or needs to be resumed.

### Context

Archive should be reversible. Mistakes happen, and sometimes an archived exercise needs to be brought back. Restoration returns the exercise to its previous status (the status it had before being archived), making archive a safe "undo-able" operation.

Only Administrators can restore exercises to prevent confusion about exercise availability across the organization.

### Acceptance Criteria

- [ ] **Given** I am an Administrator viewing an archived exercise, **when** I see the available actions, **then** I see a "Restore" action
- [ ] **Given** I am an Exercise Director viewing an archived exercise, **when** I see available actions, **then** I do NOT see a "Restore" action
- [ ] **Given** the exercise is NOT archived, **when** I view it, **then** I do NOT see a "Restore" action
- [ ] **Given** I click "Restore", **when** the confirmation dialog appears, **then** it shows what status the exercise will be restored to
- [ ] **Given** I confirm restoration, **when** the operation completes, **then** the exercise status returns to `PreviousStatus`
- [ ] **Given** `PreviousStatus` is null, **when** I restore, **then** the status defaults to "Draft"
- [ ] **Given** an exercise is restored, **when** the operation completes, **then** `ArchivedAt`, `ArchivedById`, and `PreviousStatus` are cleared (set to null)
- [ ] **Given** an exercise is restored, **when** I view the default exercise list, **then** the exercise appears in the list
- [ ] **Given** an exercise is restored, **when** the action completes, **then** an audit record is created with user and timestamp
- [ ] **Given** an exercise is not archived, **when** I attempt to restore it, **then** I receive an error message

### Out of Scope

- Bulk restore (S07 - Admin Archive Management)
- Choosing a different status on restore (always restores to previous)

### UI/UX Notes

**Restore Confirmation Dialog:**

```
┌─────────────────────────────────────────────────────────────┐
│  Restore Exercise                                       [×] │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Restore this exercise to make it visible again?             │
│                                                              │
│  Exercise: "2026 Full Scale Exercise"                        │
│  Will be restored to: Completed                              │
│                                                              │
│  The exercise will appear in the normal exercise list and    │
│  be accessible to all participants.                          │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│                          [Cancel]    [Restore Exercise]      │
└─────────────────────────────────────────────────────────────┘
```

**Styling:**
- Use success/positive color (green) for restore action
- Simple confirmation—no typing required (restore is safe)

### API Specification

**Endpoint:**
```
POST /api/exercises/{id}/restore
```

**Request:** No body required

**Response (200 OK):**
```json
{
  "id": "guid",
  "name": "2026 Full Scale Exercise",
  "status": "Completed",
  "message": "Exercise restored successfully"
}
```

**Error Responses:**
- `403 Forbidden` — User is not Administrator
- `404 Not Found` — Exercise does not exist
- `409 Conflict` — Exercise is not archived (cannot restore)

### Domain Terms

| Term | Definition |
|------|------------|
| Restore | Return archived exercise to its previous status, making it visible again |
| PreviousStatus | The status stored when archiving, used as the restoration target |

### Dependencies

- S02 - Archive Exercise (creates archived exercises with PreviousStatus)
- S03 - View Archived Exercises (provides UI to find archived exercises)

### Deliverables

1. Add `RestoreAsync` method to `IExerciseService`
2. Add `POST /api/exercises/{id}/restore` controller action
3. Add authorization check (Administrator only)
4. Create `RestoreExerciseDialog` React component
5. Add restore action to archived `ExerciseCard`
6. Add `useRestoreExercise` mutation hook
7. Integration tests for restore operation
8. Test that non-admins cannot restore
9. Test restore when PreviousStatus is null (should default to Draft)
