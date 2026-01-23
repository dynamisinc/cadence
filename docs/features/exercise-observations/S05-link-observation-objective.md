# S05: Link Observation to Objective

## Story

**As an** Evaluator,
**I want** to link my observations to exercise objectives,
**So that** the AAR can show how well each objective was demonstrated.

## Context

HSEEP requires exercises to have defined objectives that are evaluated. Linking observations to objectives enables coverage analysis (which objectives have observations?) and provides structured feedback for the After-Action Report. An observation can link to multiple objectives if it relates to multiple capability areas.

## Acceptance Criteria

### Link During Creation
- [ ] **Given** I am creating an observation, **when** I see "Link to Objective", **then** I can select from exercise objectives
- [ ] **Given** the objective selector, **when** I view options, **then** I see all objectives with their names
- [ ] **Given** the objective selector, **when** I select objectives, **then** they appear as chips/tags
- [ ] **Given** I've selected objectives, **when** I save, **then** the links are persisted

### Link Multiple Objectives
- [ ] **Given** an observation, **when** I select multiple objectives, **then** all are linked
- [ ] **Given** I've linked objectives, **when** I click X on a chip, **then** that objective is unlinked

### Link During Edit
- [ ] **Given** I am editing an observation, **when** I view objective links, **then** I see currently linked objectives
- [ ] **Given** I am editing, **when** I add/remove objective links, **then** the changes are saved

### View Linked Objectives
- [ ] **Given** an observation with linked objectives, **when** I view it, **then** I see objective names or numbers
- [ ] **Given** linked objective names, **when** I click one, **then** I see objective details (modal or navigate)

### Coverage Indication
- [ ] **Given** an objective with observations, **when** I view the objectives list, **then** I see observation count
- [ ] **Given** an objective without observations, **when** I view it in Review Mode, **then** it's highlighted as "No observations"

## Out of Scope

- Auto-suggest objectives based on linked injects
- Objective coverage requirements/minimums
- Required objective selection

## Dependencies

- S01 (Create Observation)
- exercise-objectives/S01 (objectives must exist)

## Domain Terms

| Term | Definition |
|------|------------|
| Linked Objective | Association between observation and exercise objective |
| Coverage | Whether an objective has at least one observation |
| Coverage Gap | Objective with no linked observations |

## API Contract

Links are managed through the observation create/update endpoints:

```json
{
  "content": "...",
  "linkedObjectiveIds": ["objective-guid-1", "objective-guid-2"]
}
```

### Get Available Objectives (for selector)

```http
GET /api/exercises/{exerciseId}/objectives?fields=id,name,description
Authorization: Bearer {token}
```

**Response:**
```json
{
  "items": [
    {
      "id": "guid-1",
      "name": "EOC Activation",
      "description": "Demonstrate EOC activation procedures within 30 minutes"
    },
    {
      "id": "guid-2",
      "name": "Multi-Agency Communication",
      "description": "Test communication between EOC and partner agencies"
    }
  ]
}
```

### Objective Coverage (for Review Mode)

```http
GET /api/exercises/{exerciseId}/objectives/coverage
Authorization: Bearer {token}
```

**Response:**
```json
{
  "items": [
    {
      "objectiveId": "guid-1",
      "name": "EOC Activation",
      "observationCount": 5,
      "strengthCount": 3,
      "afiCount": 2,
      "ratings": { "P": 2, "S": 2, "M": 1, "U": 0 }
    },
    {
      "objectiveId": "guid-2",
      "name": "Multi-Agency Communication",
      "observationCount": 0,
      "strengthCount": 0,
      "afiCount": 0,
      "ratings": {}
    }
  ]
}
```

## UI/UX Notes

### Objective Selector

```
Link to Objective:
┌─────────────────────────────────────────────────────────────┐
│  ✓ EOC Activation                                           │
│      Demonstrate EOC activation procedures within 30 min    │
│  ─────────────────────────────────────────────────────────  │
│    Multi-Agency Communication                               │
│      Test communication between EOC and partner agencies    │
│  ─────────────────────────────────────────────────────────  │
│    Resource Management                                      │
│      Exercise resource request and allocation procedures    │
└─────────────────────────────────────────────────────────────┘

Selected: [EOC Activation ×]
```

### Objective Coverage in Review Mode

```
┌─────────────────────────────────────────────────────────────┐
│  Objective Coverage                                          │
├─────────────────────────────────────────────────────────────┤
│  Objective             │ Observations │ P │ S │ M │ U       │
│  ──────────────────────┼──────────────┼───┼───┼───┼───      │
│  EOC Activation        │ 5 (3⬆ 2⬇)   │ 2 │ 2 │ 1 │ -       │
│  Multi-Agency Comm     │ ⚠️ 0         │ - │ - │ - │ -       │  ← Coverage gap
│  Resource Management   │ 3 (1⬆ 2⬇)   │ - │ 1 │ 2 │ -       │
└─────────────────────────────────────────────────────────────┘
```

---

*Story created: 2026-01-21*
