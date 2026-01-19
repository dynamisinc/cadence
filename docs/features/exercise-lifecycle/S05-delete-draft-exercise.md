# Story: Delete Draft Exercise

## S05-delete-draft-exercise.md

**As the** creator of an exercise OR an Administrator,
**I want** to permanently delete an exercise that has never been published,
**So that** I can remove test exercises or mistakes without cluttering the archive.

### Context

Exercises that were created but never published (never left Draft status) are safe to delete permanently since no conduct data exists—no injects have been fired, no observations recorded during an actual exercise. This allows cleanup of test data and mistakes without requiring administrator intervention for simple cases.

The key safeguard is the `HasBeenPublished` flag: once an exercise has ever been Published, Active, or Completed, it cannot be directly deleted—it must be archived first (and then only admins can permanently delete it).

### Acceptance Criteria

- [ ] **Given** I created the exercise AND it has status Draft AND `HasBeenPublished` is false, **when** I view the exercise, **then** I see a "Delete" action
- [ ] **Given** I am an Administrator AND the exercise has status Draft AND `HasBeenPublished` is false, **when** I view the exercise, **then** I see a "Delete" action
- [ ] **Given** the exercise has `HasBeenPublished` = true (regardless of current status), **when** I view the exercise, **then** I do NOT see the "Delete" action
- [ ] **Given** the exercise is in Published, Active, or Completed status, **when** I view the exercise, **then** I do NOT see the "Delete" action
- [ ] **Given** I am not the creator AND not an Administrator, **when** I view a draft exercise, **then** I do NOT see the "Delete" action
- [ ] **Given** I click "Delete", **when** the confirmation dialog appears, **then** it requires me to type the exercise name to confirm
- [ ] **Given** I type the exercise name incorrectly, **when** I view the delete button, **then** it remains disabled
- [ ] **Given** I type the exercise name correctly (case-insensitive), **when** I view the delete button, **then** it becomes enabled
- [ ] **Given** I confirm deletion, **when** the operation completes, **then** the exercise and ALL related data are permanently removed
- [ ] **Given** I confirm deletion, **when** the operation completes, **then** I am redirected to the exercise list
- [ ] **Given** deletion completes, **when** I check audit logs, **then** a deletion record exists with deleted item summary

### Out of Scope

- Soft delete / trash with recovery period
- Undo after deletion
- Deleting archived exercises (S06)

### UI/UX Notes

**Delete Action Location:**
- Exercise card dropdown menu (list view) — only shown when eligible
- Exercise detail page header — only shown when eligible

**Delete Confirmation Dialog:**

```
┌─────────────────────────────────────────────────────────────┐
│  🗑️  Delete Exercise                                    [×] │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  This action CANNOT be undone.                               │
│                                                              │
│  The following data will be permanently deleted:             │
│    • 12 injects                                              │
│    • 2 phases                                                │
│    • 0 observations                                          │
│                                                              │
│  To confirm, type the exercise name:                         │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                                                         ││
│  └─────────────────────────────────────────────────────────┘│
│  Expected: "Test Exercise 2026"                              │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│                          [Cancel]    [Delete] (disabled)     │
└─────────────────────────────────────────────────────────────┘
```

**Styling:**
- Use danger color (red) for delete action and button
- Button disabled until name matches exactly
- Show data summary to make impact clear

### API Specification

**Pre-Delete Summary Endpoint:**
```
GET /api/exercises/{id}/delete-summary
```

**Response (200 OK):**
```json
{
  "exerciseId": "guid",
  "exerciseName": "Test Exercise 2026",
  "canDelete": true,
  "deleteReason": "NeverPublished",
  "cannotDeleteReason": null,
  "summary": {
    "injectCount": 12,
    "phaseCount": 2,
    "observationCount": 0,
    "participantCount": 3,
    "expectedOutcomeCount": 24
  }
}
```

**When cannot delete:**
```json
{
  "exerciseId": "guid",
  "exerciseName": "Published Exercise",
  "canDelete": false,
  "deleteReason": null,
  "cannotDeleteReason": "MustArchiveFirst",
  "summary": null
}
```

**Delete Endpoint:**
```
DELETE /api/exercises/{id}
```

**Response:** `204 No Content`

**Error Responses:**
- `403 Forbidden` — User lacks permission (not creator and not admin)
- `404 Not Found` — Exercise does not exist
- `409 Conflict` — Exercise has been published (must archive first)

### Domain Terms

| Term | Definition |
|------|------------|
| Never Published | Exercise where `HasBeenPublished` flag is false |
| Creator | User whose ID matches `CreatedById` on the exercise |
| Cascade Delete | Deletion that removes all related child records |

### Dependencies

- S01 - Lifecycle Tracking Fields (`HasBeenPublished`, `CreatedById`)

### Technical Notes

**Cascade Delete Order:**
1. ExpectedOutcomes (child of Inject)
2. Observations (child of Inject)
3. Injects
4. Phases
5. ExerciseParticipants
6. Exercise

**Delete Eligibility Check:**
```csharp
bool CanDelete(Exercise exercise, Guid currentUserId, bool isAdmin)
{
    // Must be Draft and never published
    if (exercise.HasBeenPublished || exercise.Status != ExerciseStatus.Draft)
        return false;
    
    // Creator or Admin can delete
    return exercise.CreatedById == currentUserId || isAdmin;
}
```

### Deliverables

1. Add `CanDeleteExercise` method to determine eligibility
2. Add `GetDeleteSummaryAsync` method to `IExerciseService`
3. Add `DeleteExerciseAsync` method with cascade delete
4. Add `GET /api/exercises/{id}/delete-summary` endpoint
5. Add `DELETE /api/exercises/{id}` endpoint
6. Create `DeleteExerciseDialog` React component with name confirmation
7. Add `useDeleteSummary` hook
8. Add `useDeleteExercise` mutation hook
9. Add delete action to `ExerciseCard` (conditional)
10. Configure cascade delete in EF if not already present
11. Integration tests for all delete scenarios
12. Test authorization: creator can delete own, admin can delete any, others cannot
