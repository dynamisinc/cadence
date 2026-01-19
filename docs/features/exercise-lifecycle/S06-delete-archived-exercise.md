# Story: Permanently Delete Archived Exercise

## S06-delete-archived-exercise.md

**As an** Administrator,
**I want** to permanently delete an archived exercise,
**So that** I can free up storage and remove obsolete data that will never be needed.

### Context

Archived exercises preserve historical data, but eventually some need to be permanently removed—old test exercises, cancelled events, or data that's past its retention requirement. This is an admin-only operation with strong confirmation requirements because:

1. The data includes conduct history (fired injects, observations) that may have compliance value
2. Deletion is irreversible
3. The exercise was important enough to have been published at some point

The two-step safety pattern (archive first, then delete) ensures that exercises with historical value cannot be accidentally deleted in one click.

### Acceptance Criteria

- [ ] **Given** I am an Administrator viewing an archived exercise, **when** I see available actions, **then** I see a "Permanently Delete" action
- [ ] **Given** I am an Exercise Director viewing an archived exercise, **when** I see available actions, **then** I do NOT see "Permanently Delete"
- [ ] **Given** the exercise is NOT archived (Draft, Published, Active, Completed), **when** I view it as admin, **then** I do NOT see "Permanently Delete"
- [ ] **Given** I click "Permanently Delete", **when** the dialog appears, **then** it shows a summary of data that will be deleted
- [ ] **Given** the confirmation dialog is open, **when** I review it, **then** I must check a confirmation checkbox
- [ ] **Given** the confirmation dialog is open, **when** I review it, **then** I must type the exercise name to confirm
- [ ] **Given** I have not checked the checkbox OR name doesn't match, **when** I view the delete button, **then** it remains disabled
- [ ] **Given** I have checked the checkbox AND name matches, **when** I view the delete button, **then** it becomes enabled
- [ ] **Given** I confirm permanent deletion, **when** the operation completes, **then** the exercise and ALL related data are permanently removed
- [ ] **Given** permanent deletion completes, **when** I check audit logs, **then** a deletion record exists with summary of deleted data
- [ ] **Given** permanent deletion completes, **when** I view the archived exercises list, **then** the exercise no longer appears

### Out of Scope

- Bulk delete (S07 - Admin Archive Management)
- Scheduled/automatic deletion
- Deletion recovery/undo

### UI/UX Notes

**Permanently Delete Confirmation Dialog (Two-Step):**

```
┌─────────────────────────────────────────────────────────────┐
│  ⚠️  Permanently Delete Exercise                        [×] │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  This action CANNOT be undone. All data will be              │
│  permanently deleted including exercise history.             │
│                                                              │
│  The following will be deleted:                              │
│    • 156 injects (45 fired, 12 skipped)                      │
│    • 4 phases                                                │
│    • 38 observations                                         │
│    • 12 participants                                         │
│    • 89 expected outcomes                                    │
│                                                              │
│  ─────────────────────────────────────────────────────────   │
│                                                              │
│  To confirm, type the exercise name:                         │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                                                         ││
│  └─────────────────────────────────────────────────────────┘│
│  Expected: "2025 Full Scale Exercise"                        │
│                                                              │
│  ☐ I understand this action is permanent and irreversible   │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│                      [Cancel]    [Delete Permanently] 🔴     │
└─────────────────────────────────────────────────────────────┘
```

**Styling:**
- Use danger color (red) for action, button, and dialog header
- Button disabled until BOTH checkbox checked AND name matches
- Show detailed summary including conduct data (fired/skipped counts)
- More prominent warnings than draft delete (this has historical data)

### API Specification

**Uses same endpoints as S05:**

**Pre-Delete Summary:**
```
GET /api/exercises/{id}/delete-summary
```

**Response for archived exercise:**
```json
{
  "exerciseId": "guid",
  "exerciseName": "2025 Full Scale Exercise",
  "canDelete": true,
  "deleteReason": "Archived",
  "cannotDeleteReason": null,
  "summary": {
    "injectCount": 156,
    "firedInjectCount": 45,
    "skippedInjectCount": 12,
    "phaseCount": 4,
    "observationCount": 38,
    "participantCount": 12,
    "expectedOutcomeCount": 89
  }
}
```

**Delete Endpoint:**
```
DELETE /api/exercises/{id}
```

**Additional Authorization for Archived:**
- Only Administrator can delete archived exercises
- Non-admins receive `403 Forbidden`

### Domain Terms

| Term | Definition |
|------|------------|
| Permanent Delete | Irreversible removal of exercise and all related data |
| Conduct Data | Data created during exercise execution: fired timestamps, observations |
| Two-Step Delete | Pattern requiring archive before delete for published exercises |

### Dependencies

- S02 - Archive Exercise (creates archived exercises)
- S03 - View Archived Exercises (UI to find archived exercises)
- S05 - Delete Draft Exercise (shared delete infrastructure)

### Technical Notes

**Enhanced Delete Summary for Archived:**
Include conduct-specific counts:
- `firedInjectCount` — Injects that were fired
- `skippedInjectCount` — Injects that were skipped
- These help admin understand the historical value of the data

**Delete Eligibility (Updated from S05):**
```csharp
(bool CanDelete, string Reason) CanDeleteExercise(Exercise exercise, Guid currentUserId, bool isAdmin)
{
    // Case 1: Never published draft - creator or admin
    if (!exercise.HasBeenPublished && exercise.Status == ExerciseStatus.Draft)
    {
        if (exercise.CreatedById == currentUserId || isAdmin)
            return (true, "NeverPublished");
        return (false, "NotAuthorized");
    }
    
    // Case 2: Archived - admin only
    if (exercise.Status == ExerciseStatus.Archived)
    {
        if (isAdmin)
            return (true, "Archived");
        return (false, "NotAuthorized");
    }
    
    // Case 3: Published/Active/Completed - must archive first
    return (false, "MustArchiveFirst");
}
```

### Deliverables

1. Update `GetDeleteSummaryAsync` to include conduct data counts
2. Update `CanDeleteExercise` to handle archived exercises (admin only)
3. Update `DeleteExerciseDialog` to support two-step confirmation (checkbox + name)
4. Add "Permanently Delete" action to archived exercise cards
5. Integration tests for admin-only archived delete
6. Test that non-admins cannot delete archived exercises
7. Test that non-archived exercises return "MustArchiveFirst"
