# S02: Edit Observation

## Story

**As an** Evaluator,
**I want** to edit my observations after initial entry,
**So that** I can add details, correct mistakes, or update ratings as the situation develops.

## Context

Initial observations are often brief notes captured in the moment. Evaluators need to expand these notes, add ratings they didn't have time for, or correct errors. Own observations are always editable during Active/Paused status; Exercise Directors can edit any observation.

## Acceptance Criteria

### Edit Own Observation
- [ ] **Given** I created an observation, **when** I click the edit icon, **then** I see the edit form with current values
- [ ] **Given** I am editing, **when** I update the content, **then** the new content is saved
- [ ] **Given** I am editing, **when** I change the type (Strength/AFI/Neutral), **then** the type is updated
- [ ] **Given** I am editing, **when** I add or change the rating, **then** the rating is updated
- [ ] **Given** I am editing, **when** I cancel, **then** no changes are saved

### Edit Others' Observations
- [ ] **Given** I am an Exercise Director, **when** I view any observation, **then** I see the edit option
- [ ] **Given** I am an Evaluator, **when** I view another's observation, **then** I do NOT see the edit option
- [ ] **Given** I am an Administrator, **when** I view any observation, **then** I see the edit option

### Modify Links
- [ ] **Given** I am editing, **when** I add an inject link, **then** the new link is saved
- [ ] **Given** I am editing, **when** I remove an inject link, **then** the link is removed
- [ ] **Given** I am editing, **when** I add/remove objective links, **then** the links are updated

### Status Restrictions
- [ ] **Given** the exercise is Active or Paused, **when** I edit, **then** the edit saves successfully
- [ ] **Given** the exercise is Completed, **when** I try to edit, **then** I see "Exercise is complete. Contact an administrator to make changes."
- [ ] **Given** the exercise is Archived, **when** I view observations, **then** no edit option is shown

### Audit Trail
- [ ] **Given** I edit an observation, **when** I view it, **then** I see "Edited" indicator with timestamp
- [ ] **Given** an observation was edited, **when** I hover on "Edited", **then** I see who edited and when

## Out of Scope

- Version history / undo
- Collaborative editing
- Edit comments/reasons

## Dependencies

- S01 (Create Observation)
- authentication (role-based access)
- exercise-status (status-based edit restrictions)

## API Contract

### Update Observation

```http
PUT /api/exercises/{exerciseId}/observations/{observationId}
Content-Type: application/json
Authorization: Bearer {token}

{
  "content": "EOC activated within 30 minutes of notification - exceeded expectations",
  "type": "Strength",
  "rating": "P",
  "linkedInjectIds": ["guid-1", "guid-2", "guid-4"],
  "linkedObjectiveIds": ["guid-3"]
}
```

**Response (200 OK):**
```json
{
  "id": "observation-guid",
  "content": "EOC activated within 30 minutes of notification - exceeded expectations",
  "type": "Strength",
  "rating": "P",
  "observedAt": "2026-01-21T09:15:00Z",
  "recordedAt": "2026-01-21T09:15:32Z",
  "updatedAt": "2026-01-21T09:45:00Z",
  "updatedBy": {
    "id": "user-guid",
    "displayName": "Robert Chen"
  },
  "createdBy": {
    "id": "user-guid",
    "displayName": "Robert Chen"
  },
  "linkedInjects": [...],
  "linkedObjectives": [...]
}
```

## UI/UX Notes

### Inline Edit
- Click observation row to expand detail view
- Edit button in expanded view (or pencil icon in row)
- Same form layout as create, pre-populated with current values

### Edit Indicator
```
09:15 │ ⬆ Strength │ P │ EOC activated within 30 minutes...
      │            │   │ 📎 INJ-003, OBJ-1
      │            │   │ 👤 Robert Chen • Edited 09:45
```

---

*Story created: 2026-01-21*
