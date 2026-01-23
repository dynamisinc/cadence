# S01: Create Observation

## Story

**As an** Evaluator,
**I want** to quickly capture observations during exercise conduct,
**So that** I document player performance in real-time for the After-Action Review.

## Context

During exercise conduct, Evaluators need to document what they observe as it happens. The observation entry must be fast and not distract from watching the exercise. Observations can be brief notes that are expanded later, or detailed entries with ratings and links.

## Acceptance Criteria

### Basic Creation
- [ ] **Given** I am an Evaluator in an Active exercise, **when** I click "+ Observation", **then** I see a quick entry form
- [ ] **Given** the observation form, **when** I enter text and save, **then** the observation is created with current timestamp
- [ ] **Given** I save an observation, **when** I view the list, **then** my observation appears at the top (most recent)
- [ ] **Given** I create an observation, **when** it saves, **then** it shows my name as the creator

### Observation Types
- [ ] **Given** the observation form, **when** I select "Strength", **then** the observation is marked as a positive finding
- [ ] **Given** the observation form, **when** I select "Area for Improvement", **then** the observation is marked as a gap
- [ ] **Given** the observation form, **when** I don't select a type, **then** it defaults to "Neutral"

### Optional Fields
- [ ] **Given** the observation form, **when** I select a P/S/M/U rating, **then** the rating is saved with the observation
- [ ] **Given** the observation form, **when** I skip the rating, **then** the observation saves without a rating
- [ ] **Given** the observation form, **when** I link to an inject, **then** the inject association is saved
- [ ] **Given** the observation form, **when** I link to an objective, **then** the objective association is saved

### Validation
- [ ] **Given** the observation form, **when** I try to save empty content, **then** I see "Observation text is required"
- [ ] **Given** the observation form, **when** content exceeds 2000 characters, **then** I see a character limit warning

### Permissions
- [ ] **Given** I am an Observer (read-only), **when** I view the exercise, **then** I do NOT see the "+ Observation" button
- [ ] **Given** the exercise is Completed, **when** I try to add observation, **then** I see "Exercise is complete. Observations are read-only."

## Out of Scope

- Bulk observation import
- Voice-to-text observation entry
- Photo/media attachments
- Observation templates

## Dependencies

- exercise-crud/S01 (exercise must exist)
- authentication (role-based access)
- exercise-status (exercise must be Active or Paused)

## Domain Terms

| Term | Definition |
|------|------------|
| Observation | A documented finding during exercise conduct |
| Strength | Positive performance observation |
| Area for Improvement (AFI) | Performance gap identified |
| P/S/M/U | HSEEP performance rating scale |

## API Contract

### Create Observation

```http
POST /api/exercises/{exerciseId}/observations
Content-Type: application/json
Authorization: Bearer {token}

{
  "content": "EOC activated within 30 minutes of notification",
  "type": "Strength",
  "rating": "P",
  "linkedInjectIds": ["guid-1", "guid-2"],
  "linkedObjectiveIds": ["guid-3"]
}
```

**Response (201 Created):**
```json
{
  "id": "observation-guid",
  "content": "EOC activated within 30 minutes of notification",
  "type": "Strength",
  "rating": "P",
  "observedAt": "2026-01-21T09:15:00Z",
  "recordedAt": "2026-01-21T09:15:32Z",
  "createdBy": {
    "id": "user-guid",
    "displayName": "Robert Chen"
  },
  "linkedInjects": [
    { "id": "guid-1", "injectNumber": "INJ-003" }
  ],
  "linkedObjectives": [
    { "id": "guid-3", "name": "EOC Activation" }
  ]
}
```

## UI/UX Notes

### Quick Entry Panel (Docked)

The observation panel should be accessible without leaving the current view:
- Floating button in bottom-right corner during conduct
- Expands to quick entry form on click
- Minimizes after save to reduce screen clutter
- Keyboard shortcut: `Ctrl+O` or `O` to open

### Mobile Considerations

- Large touch targets for type and rating buttons
- Auto-focus on text area when opened
- Haptic feedback on save (if supported)

---

*Story created: 2026-01-21*
