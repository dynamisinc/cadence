# S08: Filter Observations

## Story

**As an** Exercise Director preparing for the AAR,
**I want** to filter observations by type, rating, objective, and author,
**So that** I can focus on specific areas during review.

## Context

Exercises can generate dozens or hundreds of observations. Filtering helps users find relevant observations quickly - for example, viewing only Areas for Improvement, or observations from a specific evaluator, or those linked to a particular objective. Filters can be combined for precise results.

## Acceptance Criteria

### Filter by Type
- [ ] **Given** the filter dropdown for Type, **when** I select "Strength", **then** only Strength observations are shown
- [ ] **Given** the filter for Type, **when** I select "Area for Improvement", **then** only AFI observations are shown
- [ ] **Given** the filter for Type, **when** I select "Neutral", **then** only Neutral observations are shown
- [ ] **Given** the filter for Type, **when** I select "All Types", **then** all observations are shown

### Filter by Rating
- [ ] **Given** the filter dropdown for Rating, **when** I select "P", **then** only P-rated observations are shown
- [ ] **Given** the filter for Rating, **when** I select "Unrated", **then** only observations without ratings are shown
- [ ] **Given** the filter for Rating, **when** I select "All Ratings", **then** all observations are shown

### Filter by Objective
- [ ] **Given** the filter dropdown for Objective, **when** I select an objective, **then** only observations linked to it are shown
- [ ] **Given** the filter for Objective, **when** I select "No Objective", **then** only unlinked observations are shown
- [ ] **Given** the filter for Objective, **when** I select "All Objectives", **then** all observations are shown

### Filter by Author
- [ ] **Given** the filter dropdown for Author, **when** I select an evaluator, **then** only their observations are shown
- [ ] **Given** the filter for Author, **when** I select "My Observations", **then** only my observations are shown

### Combined Filters
- [ ] **Given** I set Type=AFI and Rating=M, **when** I view results, **then** only AFI observations with M rating are shown
- [ ] **Given** multiple filters applied, **when** I view count, **then** it shows "Showing X of Y observations"
- [ ] **Given** combined filters result in no matches, **when** I view list, **then** I see "No observations match your filters"

### Text Search
- [ ] **Given** the search input, **when** I type text, **then** observations containing that text are shown
- [ ] **Given** search is active, **when** I combine with filters, **then** both are applied
- [ ] **Given** search text, **when** I clear it, **then** filter-only results are shown

### Filter Persistence
- [ ] **Given** I set filters, **when** I navigate away and return, **then** my filters are preserved (session)
- [ ] **Given** I click "Clear Filters", **when** the list updates, **then** all observations are shown

## Out of Scope

- Saved filter presets
- Filter by date range
- Advanced query syntax
- Export filtered results

## Dependencies

- S07 (View Observations List)

## API Contract

### Filter Parameters

```http
GET /api/exercises/{exerciseId}/observations
  ?type=AreaForImprovement
  &rating=M
  &objectiveId=guid
  &authorId=guid
  &search=communication
  &page=1
  &pageSize=50
Authorization: Bearer {token}
```

**Query Parameters:**
| Param | Values | Description |
|-------|--------|-------------|
| type | Strength, AreaForImprovement, Neutral | Filter by observation type |
| rating | P, S, M, U, Unrated | Filter by rating |
| objectiveId | guid, none | Filter by linked objective |
| authorId | guid, me | Filter by creator |
| search | string | Full-text search in content |

## UI/UX Notes

### Filter Bar

```
┌─────────────────────────────────────────────────────────────────────┐
│  Filter: [All Types ▼] [All Ratings ▼] [All Objectives ▼]          │
│          [All Authors ▼] [🔍 Search observations...]                │
│                                                                     │
│  Active: Type: AFI, Rating: M                    [Clear Filters]    │
├─────────────────────────────────────────────────────────────────────┤
│  Showing 3 of 24 observations                                       │
└─────────────────────────────────────────────────────────────────────┘
```

### Filter Dropdown Options

**Type Dropdown:**
```
┌─────────────────────┐
│ All Types           │
│ ─────────────────── │
│ ⬆ Strength (8)      │
│ ⬇ Area for Impr (12)│
│ ─ Neutral (4)       │
└─────────────────────┘
```

**Rating Dropdown:**
```
┌─────────────────────┐
│ All Ratings         │
│ ─────────────────── │
│ 🟢 P (5)            │
│ 🟡 S (7)            │
│ 🟠 M (4)            │
│ 🔴 U (2)            │
│ ─ Unrated (6)       │
└─────────────────────┘
```

**Objective Dropdown:**
```
┌───────────────────────────────┐
│ All Objectives                │
│ ───────────────────────────── │
│ EOC Activation (5)            │
│ Multi-Agency Comm (8)         │
│ Resource Management (3)       │
│ ───────────────────────────── │
│ No Objective Linked (8)       │
└───────────────────────────────┘
```

### Mobile Considerations

- Filters collapse to icon button on small screens
- Opens filter sheet/modal when tapped
- Apply button confirms filter selection

---

*Story created: 2026-01-21*
