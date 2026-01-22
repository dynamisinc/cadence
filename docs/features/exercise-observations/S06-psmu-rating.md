# S06: Apply P/S/M/U Rating

## Story

**As an** Evaluator,
**I want** to rate observations using the HSEEP P/S/M/U scale,
**So that** the AAR has consistent, doctrine-compliant performance assessments.

## Context

HSEEP defines a four-level performance rating scale that provides standardized assessment across evaluators and exercises. Ratings are optional but strongly encouraged for observations linked to objectives, as they directly support AAR findings and improvement recommendations.

## Acceptance Criteria

### Rating Selection
- [ ] **Given** I am creating/editing an observation, **when** I view the rating options, **then** I see P, S, M, U, and "None"
- [ ] **Given** the rating options, **when** I hover over a rating, **then** I see the full definition tooltip
- [ ] **Given** I select a rating, **when** I save, **then** the rating is persisted
- [ ] **Given** I select "None", **when** I save, **then** no rating is stored

### Rating Definitions (Tooltips)
- [ ] **P (Performed)**: "The capability/task was completed without challenges"
- [ ] **S (Some Challenges)**: "The capability/task was completed with some challenges"
- [ ] **M (Major Challenges)**: "The capability/task was not completed effectively"
- [ ] **U (Unable)**: "The capability/task could not be performed"

### Rating Display
- [ ] **Given** an observation with rating P, **when** I view it, **then** I see green "P" badge
- [ ] **Given** an observation with rating S, **when** I view it, **then** I see yellow "S" badge
- [ ] **Given** an observation with rating M, **when** I view it, **then** I see orange "M" badge
- [ ] **Given** an observation with rating U, **when** I view it, **then** I see red "U" badge
- [ ] **Given** an observation without rating, **when** I view it, **then** no rating badge is shown

### Rating Change
- [ ] **Given** an observation with a rating, **when** I edit and change the rating, **then** the new rating is saved
- [ ] **Given** an observation with a rating, **when** I edit and select "None", **then** the rating is removed

### Rating Aggregation (Review Mode)
- [ ] **Given** multiple observations for an objective, **when** I view Review Mode, **then** I see rating distribution
- [ ] **Given** observations with mixed ratings, **when** I view summary, **then** I see count per rating level

## Out of Scope

- Weighted rating calculations
- Required ratings for certain observation types
- Rating justification/comments
- Rating approval workflow

## Dependencies

- S01 (Create Observation)
- S02 (Edit Observation)

## Domain Terms

| Term | Definition |
|------|------------|
| **P - Performed** | The targets and critical tasks were completed in a manner that achieved the objective(s) and did not negatively impact the performance of other activities |
| **S - Performed with Some Challenges** | The targets and critical tasks were completed in a manner that achieved the objective(s) and did not negatively impact the performance of other activities. However, opportunities to enhance effectiveness and/or efficiency were identified |
| **M - Performed with Major Challenges** | The targets and critical tasks were completed in a manner that achieved the objective(s); however, the completion negatively impacted the performance of other activities |
| **U - Unable to be Performed** | The targets and critical tasks were not completed in a manner that achieved the objective(s) |

## API Contract

Rating is included in observation create/update:

```json
{
  "content": "...",
  "rating": "P"  // "P", "S", "M", "U", or null
}
```

### Rating Summary (for Review Mode)

```http
GET /api/exercises/{exerciseId}/observations/rating-summary
Authorization: Bearer {token}
```

**Response:**
```json
{
  "total": 24,
  "rated": 18,
  "unrated": 6,
  "byRating": {
    "P": 5,
    "S": 7,
    "M": 4,
    "U": 2
  },
  "byObjective": [
    {
      "objectiveId": "guid-1",
      "objectiveName": "EOC Activation",
      "P": 2, "S": 2, "M": 1, "U": 0
    }
  ]
}
```

## UI/UX Notes

### Rating Selector

```
Rating (optional):

┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌──────┐
│  P  │ │  S  │ │  M  │ │  U  │ │ None │
│ ✓   │ │     │ │     │ │     │ │      │
└─────┘ └─────┘ └─────┘ └─────┘ └──────┘
  🟢      🟡      🟠      🔴

ⓘ P: Performed without challenges
```

### Rating Badge Display

```
┌─────────────────────────────────────────────────────────────┐
│  09:15 │ ⬆ Strength │ [P] │ EOC activated within 30 min... │
│  09:42 │ ⬇ AFI      │ [M] │ Communication breakdown...     │
│  10:05 │ ─ Neutral  │     │ Shelter capacity reached...    │
└─────────────────────────────────────────────────────────────┘
```

### Rating Color Legend

| Rating | Color | Hex | Meaning |
|--------|-------|-----|---------|
| P | Green | #4CAF50 | Positive - performed well |
| S | Yellow | #FFC107 | Caution - some issues |
| M | Orange | #FF9800 | Warning - significant issues |
| U | Red | #F44336 | Critical - unable to perform |

---

*Story created: 2026-01-21*
