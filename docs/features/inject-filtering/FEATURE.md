# Feature: Inject Filtering and Search

**Parent Epic:** MSEL Authoring (E4)

## Description

MSELs can contain dozens or hundreds of injects. This feature provides filtering and search capabilities to help users find specific injects quickly. Filters can be combined and saved, while search provides instant text matching across inject fields.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-filter-injects.md) | Filter Injects | P1 | 📋 Ready |
| [S02](./S02-search-injects.md) | Search Injects | P1 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|------------|
| Administrator | Full filter/search access |
| Exercise Director | Full filter/search access |
| Controller | Filter/search to find assigned injects |
| Evaluator | Filter/search for evaluation focus |
| Observer | Filter/search for monitoring |

## Filter vs. Search

| Capability | Filter | Search |
|------------|--------|--------|
| **Purpose** | Narrow by attributes | Find specific text |
| **UI** | Dropdowns, checkboxes | Text input |
| **Combinable** | Yes, multiple filters | Combined with filters |
| **Persistence** | Can be saved | Per-session only |

## Dependencies

- inject-crud/S01: Create Inject (injects must exist)
- exercise-phases/S01: Define Phases (filter by phase)
- exercise-objectives/S01: Create Objective (filter by objective)

## Acceptance Criteria (Feature-Level)

- [ ] Users can filter injects by multiple criteria
- [ ] Users can search inject text fields
- [ ] Filters and search can be combined
- [ ] Filter/search state persists during session
- [ ] Clear indication of active filters

## Wireframes/Mockups

### Filter Bar

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│  MSEL - Hurricane Response 2025                                                │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │ [Status ▼]  [Phase ▼]  [Objective ▼]  [Method ▼]  │  🔍 Search...    │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                 │
│  Active filters: Status = Pending  │  Phase = Initial Response  [Clear all]   │
│  Showing 15 of 47 injects                                                      │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │  #  │ Time     │ Title                    │ Status  │ Phase           │   │
│  │ ────┼──────────┼──────────────────────────┼─────────┼─────────────────│   │
│  │  1  │ 09:00 AM │ Hurricane warning issued │ Pending │ Initial Response│   │
│  │  2  │ 09:15 AM │ EOC activation ordered   │ Pending │ Initial Response│   │
│  │ ...                                                                     │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

## Notes

- Filter state should survive page refresh within session
- Consider URL query parameters for shareable filtered views
- Search should be instant (debounced, client-side for MVP)
