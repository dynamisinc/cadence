# Story: S01 - Filter Injects

## User Story

**As a** Controller or Exercise Director,
**I want** to filter the MSEL by various criteria,
**So that** I can focus on specific subsets of injects relevant to my current task.

## Context

During exercise preparation and conduct, users need to focus on specific injects. A Controller might want to see only "Pending" injects, or injects in a specific phase. Filtering provides dropdown-based selection of inject attributes to narrow the displayed list.

## Acceptance Criteria

### Filter Interface
- [ ] **Given** I am viewing the MSEL, **when** I look at the toolbar, **then** I see filter dropdown buttons
- [ ] **Given** I click a filter dropdown, **when** it expands, **then** I see available options with checkboxes
- [ ] **Given** I select filter options, **when** I click away or press Enter, **then** the filter is applied

### Status Filter
- [ ] **Given** I click the Status filter, **when** I view options, **then** I see: Pending, Fired, Skipped, All
- [ ] **Given** I select "Pending", **when** filter applies, **then** only injects with Pending status are shown
- [ ] **Given** I select multiple statuses, **when** filter applies, **then** injects matching any selected status are shown

### Phase Filter
- [ ] **Given** I click the Phase filter, **when** I view options, **then** I see all defined phases plus "Unassigned"
- [ ] **Given** I select a phase, **when** filter applies, **then** only injects in that phase are shown
- [ ] **Given** I select "Unassigned", **when** filter applies, **then** only injects without a phase are shown

### Objective Filter
- [ ] **Given** I click the Objective filter, **when** I view options, **then** I see all exercise objectives
- [ ] **Given** I select an objective, **when** filter applies, **then** only injects linked to that objective are shown
- [ ] **Given** I select multiple objectives, **when** filter applies, **then** injects linked to any selected objective are shown

### Method Filter
- [ ] **Given** I click the Method filter, **when** I view options, **then** I see all method types used in the MSEL
- [ ] **Given** I select a method, **when** filter applies, **then** only injects with that delivery method are shown

### Time Range Filter
- [ ] **Given** I click the Time filter, **when** I view options, **then** I can set a time range (from/to)
- [ ] **Given** I set a time range, **when** filter applies, **then** only injects within that scheduled time range are shown

### Combined Filters
- [ ] **Given** I have multiple filters active, **when** they apply, **then** injects must match ALL filter criteria (AND logic)
- [ ] **Given** multiple filters are active, **when** I view the filter bar, **then** I see indicators for each active filter
- [ ] **Given** filters are active, **when** I view the inject count, **then** I see "Showing X of Y injects"

### Clear Filters
- [ ] **Given** filters are active, **when** I click "Clear all", **then** all filters are removed and all injects are shown
- [ ] **Given** a single filter is active, **when** I click its X button, **then** only that filter is cleared

### Filter Persistence
- [ ] **Given** I apply filters, **when** I navigate away and return, **then** my filters are preserved (within session)
- [ ] **Given** I refresh the page, **when** it reloads, **then** filters are reset to defaults

## Out of Scope

- Saved filter presets
- Sharing filtered views via URL
- Custom filter criteria
- Filter by assigned controller (future)

## Dependencies

- exercise-phases/S01: Define Phases (phase filter options)
- exercise-objectives/S01: Create Objective (objective filter options)
- inject-crud/S01: Create Inject (injects to filter)

## Open Questions

- [ ] Should filter state be stored in URL for sharing?
- [ ] Should there be quick filter buttons (e.g., "My injects")?
- [ ] Should filters be savable as presets?

## Domain Terms

| Term | Definition |
|------|------------|
| Filter | Criteria-based narrowing of displayed injects |
| Active Filter | Currently applied filter criterion |
| Filter Indicator | Visual badge showing filter is active |

## UI/UX Notes

### Filter Dropdown

```
┌─────────────────────────────────────────────────────────────────────────┐
│  [Status ▼]  [Phase ▼]  [Objective ▼]  [Method ▼]  [Time ▼]           │
└─────────────────────────────────────────────────────────────────────────┘

Status dropdown expanded:
┌─────────────────────────┐
│ ☑ Pending              │
│ ☐ Fired                │
│ ☐ Skipped              │
│ ─────────────────────  │
│ [Clear]     [Apply]    │
└─────────────────────────┘

Phase dropdown expanded:
┌─────────────────────────┐
│ ☐ All phases           │
│ ─────────────────────  │
│ ☑ Initial Response     │
│ ☐ Sustained Operations │
│ ☐ Recovery             │
│ ☐ Unassigned           │
│ ─────────────────────  │
│ [Clear]     [Apply]    │
└─────────────────────────┘
```

### Active Filters Display

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Active filters:                                                       │
│  [Status: Pending ✕]  [Phase: Initial Response ✕]        [Clear all]  │
│                                                                         │
│  Showing 15 of 47 injects                                              │
└─────────────────────────────────────────────────────────────────────────┘
```

### Time Range Filter

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Scheduled Time Range                                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  From:  [09:00 AM     ▼]                                               │
│  To:    [12:00 PM     ▼]                                               │
│                                                                         │
│  Quick select:                                                         │
│  [Next Hour]  [Next 2 Hours]  [Morning]  [Afternoon]                   │
│                                                                         │
│                               [Clear]  [Apply]                         │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Store filter state in component/context state for session persistence
- Consider URL query parameters for shareable filter states
- Filter logic should be efficient for large MSELs (100+ injects)
- Debounce filter application to avoid excessive re-renders
