# S04: Link Observation to Inject

## Story

**As an** Evaluator,
**I want** to link my observations to specific injects,
**So that** the AAR can show which injects revealed performance issues or strengths.

## Context

Observations are often triggered by specific inject deliveries. Linking observations to injects provides context for the After-Action Review and helps identify which scenario elements tested capabilities effectively. An observation can link to multiple injects, and an inject can have multiple observations.

## Acceptance Criteria

### Link During Creation
- [ ] **Given** I am creating an observation, **when** I see "Link to Inject", **then** I can select from fired injects
- [ ] **Given** the inject selector, **when** I search, **then** I can filter by inject number or description
- [ ] **Given** the inject selector, **when** I select an inject, **then** it appears as a chip/tag
- [ ] **Given** I've selected injects, **when** I save, **then** the links are persisted

### Link Multiple Injects
- [ ] **Given** an observation, **when** I select multiple injects, **then** all are linked
- [ ] **Given** I've linked injects, **when** I click X on a chip, **then** that inject is unlinked
- [ ] **Given** linked injects, **when** I save, **then** all current links are saved

### Link During Edit
- [ ] **Given** I am editing an observation, **when** I view inject links, **then** I see currently linked injects
- [ ] **Given** I am editing, **when** I add a new inject link, **then** it is added
- [ ] **Given** I am editing, **when** I remove an inject link, **then** it is removed

### View Linked Injects
- [ ] **Given** an observation with linked injects, **when** I view it, **then** I see inject numbers (e.g., "INJ-003, INJ-007")
- [ ] **Given** linked inject numbers, **when** I click one, **then** I navigate to that inject's detail view

### Inject Availability
- [ ] **Given** the inject selector, **when** I view options, **then** I see all injects (not just fired)
- [ ] **Given** the inject list, **when** I view it, **then** fired injects are visually distinguished
- [ ] **Given** a Pending inject, **when** I link it, **then** the link is allowed (observation may precede firing)

## Out of Scope

- Auto-suggest injects based on observation timing
- Inject-side "Add Observation" action
- Observation count on inject list

## Dependencies

- S01 (Create Observation)
- inject-crud/S01 (injects must exist)

## Domain Terms

| Term | Definition |
|------|------------|
| Linked Inject | Association between observation and inject |
| Fired Inject | Inject that has been delivered to players |

## API Contract

Links are managed through the observation create/update endpoints:

```json
{
  "content": "...",
  "linkedInjectIds": ["inject-guid-1", "inject-guid-2"]
}
```

### Get Available Injects (for selector)

```http
GET /api/exercises/{exerciseId}/injects?fields=id,injectNumber,description,status
Authorization: Bearer {token}
```

**Response:**
```json
{
  "items": [
    {
      "id": "guid-1",
      "injectNumber": "INJ-001",
      "description": "Hurricane warning issued",
      "status": "Delivered"
    },
    {
      "id": "guid-2",
      "injectNumber": "INJ-002",
      "description": "EOC activation ordered",
      "status": "Pending"
    }
  ]
}
```

## UI/UX Notes

### Inject Selector

```
Link to Inject:
┌─────────────────────────────────────────────────────────────┐
│ 🔍 Search injects...                                        │
├─────────────────────────────────────────────────────────────┤
│  ✓ INJ-003 │ EOC activation ordered          │ Delivered   │
│    INJ-007 │ Shelter capacity exceeded       │ Delivered   │
│    INJ-012 │ Hospital surge notification     │ Pending     │
│    INJ-015 │ Media inquiry received          │ Pending     │
└─────────────────────────────────────────────────────────────┘

Selected: [INJ-003 ×] [INJ-007 ×]
```

### Observation Display with Links

```
09:15 │ ⬆ Strength │ P │ EOC activated within 30 minutes...
      │            │   │ 📎 INJ-003, INJ-007  ← Clickable links
      │            │   │ 🎯 OBJ-1
      │            │   │ 👤 Robert Chen
```

---

*Story created: 2026-01-21*
