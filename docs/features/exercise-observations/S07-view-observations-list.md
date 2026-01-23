# S07: View Observations List

## Story

**As an** Exercise Director,
**I want** to view all observations for an exercise,
**So that** I can monitor evaluation progress and prepare for the After-Action Review.

## Context

During and after exercise conduct, stakeholders need to review all captured observations. The list view provides a chronological record of what evaluators documented, with visual indicators for type, rating, and links. Different roles have different viewing needs: Evaluators track their own entries, Directors monitor overall coverage.

## Acceptance Criteria

### List Display
- [ ] **Given** I navigate to exercise observations, **when** the page loads, **then** I see all observations sorted by time (newest first)
- [ ] **Given** the observations list, **when** I view an entry, **then** I see: timestamp, type icon, rating badge, content preview, links, author
- [ ] **Given** many observations, **when** I scroll, **then** the list virtualizes or paginates for performance
- [ ] **Given** no observations exist, **when** I view the list, **then** I see "No observations recorded yet"

### Content Preview
- [ ] **Given** a long observation, **when** I view the list, **then** I see first 100 characters with "..."
- [ ] **Given** I click an observation row, **when** it expands, **then** I see full content and all metadata

### Visual Indicators
- [ ] **Given** a Strength observation, **when** I view it, **then** I see green up-arrow (⬆) icon
- [ ] **Given** an AFI observation, **when** I view it, **then** I see red down-arrow (⬇) icon
- [ ] **Given** a Neutral observation, **when** I view it, **then** I see gray dash (─) icon
- [ ] **Given** a rated observation, **when** I view it, **then** I see colored P/S/M/U badge

### Linked Items Display
- [ ] **Given** an observation with linked injects, **when** I view it, **then** I see inject numbers (📎 INJ-003)
- [ ] **Given** an observation with linked objectives, **when** I view it, **then** I see objective indicator (🎯 OBJ-1)
- [ ] **Given** clickable links, **when** I click one, **then** I navigate to that item

### Author Display
- [ ] **Given** an observation, **when** I view it, **then** I see creator's display name
- [ ] **Given** I am viewing my own observation, **when** I see the author, **then** it shows "You" or is highlighted

### Permissions
- [ ] **Given** I am an Evaluator, **when** I view the list, **then** I see all observations (not just my own)
- [ ] **Given** I am an Observer, **when** I view the list, **then** I see all observations (read-only)

## Out of Scope

- Export observations list
- Print view
- Observation comparison

## Dependencies

- S01 (Create Observation) - observations must exist
- authentication (role-based access)

## API Contract

### List Observations

```http
GET /api/exercises/{exerciseId}/observations?page=1&pageSize=50&sort=observedAt:desc
Authorization: Bearer {token}
```

**Response:**
```json
{
  "items": [
    {
      "id": "obs-guid-1",
      "content": "EOC activated within 30 minutes of notification - exceeded expectations",
      "type": "Strength",
      "rating": "P",
      "observedAt": "2026-01-21T09:15:00Z",
      "recordedAt": "2026-01-21T09:15:32Z",
      "createdBy": {
        "id": "user-guid",
        "displayName": "Robert Chen"
      },
      "linkedInjects": [
        { "id": "inj-guid", "injectNumber": "INJ-003" }
      ],
      "linkedObjectives": [
        { "id": "obj-guid", "name": "EOC Activation" }
      ],
      "isEdited": false
    }
  ],
  "totalCount": 24,
  "page": 1,
  "pageSize": 50
}
```

## UI/UX Notes

### Observations List View

```
┌─────────────────────────────────────────────────────────────────────┐
│  Observations (24)                               [+ New Observation] │
├─────────────────────────────────────────────────────────────────────┤
│  Filter: [All Types ▼] [All Ratings ▼] [All Objectives ▼] [Search] │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 10:05 │ ─  │   │ Shelter capacity reached at 10:00,         │   │
│  │       │    │   │ overflow procedures initiated...            │   │
│  │       │    │   │ 📎 INJ-012 │ 👤 Robert Chen                │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 09:42 │ ⬇  │ M │ Communication breakdown between EOC        │   │
│  │       │    │   │ and field units - radio protocol not...    │   │
│  │       │    │   │ 📎 INJ-007 │ 🎯 OBJ-2 │ 👤 Sarah Kim       │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │ 09:15 │ ⬆  │ P │ EOC activated within 30 minutes of         │   │
│  │       │    │   │ notification - excellent response time...   │   │
│  │       │    │   │ 📎 INJ-003 │ 🎯 OBJ-1 │ 👤 Robert Chen     │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ─────────────────────────────────────────────────────────────────  │
│                    Showing 1-24 of 24 observations                  │
└─────────────────────────────────────────────────────────────────────┘
```

### Expanded Observation

```
┌─────────────────────────────────────────────────────────────────────┐
│ 09:42 │ ⬇ Area for Improvement │ [M]                      [✏️] [🗑️]│
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│ Communication breakdown between EOC and field units - radio         │
│ protocol not followed. Field team used cell phones instead of       │
│ designated frequencies, causing delays in coordination.             │
│                                                                     │
│ Linked Injects: INJ-007 (Shelter capacity exceeded)                │
│ Linked Objectives: Multi-Agency Communication                       │
│                                                                     │
│ Recorded by Sarah Kim at 09:42 AM                                  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

*Story created: 2026-01-21*
