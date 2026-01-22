# S03: Delete Observation

## Story

**As an** Exercise Director,
**I want** to delete erroneous or duplicate observations,
**So that** the observation record is accurate for the After-Action Review.

## Context

Occasionally observations are created in error (wrong exercise, duplicate entry, test data). Exercise Directors and Administrators can delete observations to maintain data quality. Evaluators cannot delete observations (even their own) to preserve the evaluation record.

## Acceptance Criteria

### Delete Permission
- [ ] **Given** I am an Exercise Director, **when** I view an observation, **then** I see the delete option
- [ ] **Given** I am an Administrator, **when** I view an observation, **then** I see the delete option
- [ ] **Given** I am an Evaluator, **when** I view my own observation, **then** I do NOT see the delete option
- [ ] **Given** I am a Controller or Observer, **when** I view observations, **then** I do NOT see the delete option

### Delete Confirmation
- [ ] **Given** I click delete, **when** the dialog appears, **then** I see "Delete this observation? This action cannot be undone."
- [ ] **Given** the confirmation dialog, **when** I click "Delete", **then** the observation is removed
- [ ] **Given** the confirmation dialog, **when** I click "Cancel", **then** the observation remains

### Status Restrictions
- [ ] **Given** the exercise is Active or Paused, **when** I delete, **then** the deletion succeeds
- [ ] **Given** the exercise is Completed, **when** I try to delete, **then** I see "Observations cannot be deleted from completed exercises"
- [ ] **Given** the exercise is Archived, **when** I view observations, **then** no delete option is shown

### Soft Delete Behavior
- [ ] **Given** an observation is deleted, **when** I view the list, **then** it no longer appears
- [ ] **Given** an observation is deleted, **when** an admin queries the database, **then** the record exists with IsDeleted=true

## Out of Scope

- Restore deleted observations (admin database operation)
- Bulk delete
- Delete reason/comment

## Dependencies

- S01 (Create Observation)
- authentication (role-based access)
- exercise-status (status-based restrictions)

## API Contract

### Delete Observation

```http
DELETE /api/exercises/{exerciseId}/observations/{observationId}
Authorization: Bearer {token}
```

**Response (204 No Content)** - Success

**Response (403 Forbidden):**
```json
{
  "error": {
    "code": "forbidden",
    "message": "You do not have permission to delete observations"
  }
}
```

**Response (400 Bad Request):**
```json
{
  "error": {
    "code": "exercise_completed",
    "message": "Observations cannot be deleted from completed exercises"
  }
}
```

## UI/UX Notes

### Delete Confirmation Dialog

```
┌─────────────────────────────────────────────────────────────┐
│  Delete Observation?                                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Are you sure you want to delete this observation?          │
│                                                             │
│  "EOC activated within 30 minutes of notification..."       │
│                                                             │
│  This action cannot be undone.                              │
│                                                             │
│                          [Cancel]  [Delete]                 │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

*Story created: 2026-01-21*
