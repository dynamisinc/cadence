# Story: S01 - Sort Injects

## User Story

**As a** Controller or Exercise Director,
**I want** to sort the MSEL by different columns,
**So that** I can organize injects in the order most useful for my current task.

## Context

Different tasks require different sort orders. During authoring, sorting by inject number maintains sequence. During conduct, sorting by scheduled time shows what's coming next. For review, sorting by status shows what's been completed. Column sorting is a fundamental data grid capability.

## Acceptance Criteria

### Basic Sorting
- [ ] **Given** I am viewing the MSEL list, **when** I click a column header, **then** the list sorts by that column ascending
- [ ] **Given** a column is sorted ascending, **when** I click it again, **then** it sorts descending
- [ ] **Given** a column is sorted descending, **when** I click it again, **then** sorting is cleared (returns to default)
- [ ] **Given** a column is sorted, **when** I view the header, **then** I see a sort indicator (▲ or ▼)

### Sortable Columns
- [ ] **Given** I want to sort, **when** I view column headers, **then** I can sort by: Inject #, Title, Scheduled Time, Scenario Time, Status, Phase
- [ ] **Given** I sort by Inject #, **when** I view results, **then** injects order numerically (1, 2, 3...)
- [ ] **Given** I sort by Scheduled Time, **when** I view results, **then** injects order chronologically
- [ ] **Given** I sort by Scenario Time, **when** I view results, **then** injects order by Day first, then Time (nulls last)
- [ ] **Given** I sort by Status, **when** I view results, **then** injects order by: Pending → Fired → Skipped
- [ ] **Given** I sort by Phase, **when** I view results, **then** injects order by phase number (unassigned last)

### Sort Persistence
- [ ] **Given** I apply a sort, **when** I navigate away and return, **then** my sort preference is maintained for the session
- [ ] **Given** I refresh the page, **when** it reloads, **then** sort returns to default (Scheduled Time ascending)

### Sort with Filters
- [ ] **Given** I have filters active, **when** I sort, **then** sorting applies to filtered results only
- [ ] **Given** I sort then filter, **when** results update, **then** sort order is maintained

### Default Sort
- [ ] **Given** I load the MSEL, **when** no sort is specified, **then** default sort is Scheduled Time ascending

## Out of Scope

- Multi-column sort (sort by A then by B)
- Custom sort orders
- Saving sort preferences permanently
- Sort by custom fields

## Dependencies

- inject-crud/S01: Create Inject (injects must exist)
- inject-filtering/S01: Filter Injects (sorting works with filters)

## Open Questions

- [ ] Should we support secondary sort (e.g., by Time within Phase)?
- [ ] Should sort preferences persist across sessions?
- [ ] Should there be a "Reset sort" button?

## Domain Terms

| Term | Definition |
|------|------------|
| Sort | Ordering items by a specific column value |
| Ascending | Smallest to largest (A-Z, 1-9, oldest to newest) |
| Descending | Largest to smallest (Z-A, 9-1, newest to oldest) |
| Sort Indicator | Visual arrow showing current sort direction |

## UI/UX Notes

### Column Header with Sort

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  #  ▲│ Scheduled ▼│ Scenario   │ Title                      │ Status │     │
│ ─────┼────────────┼────────────┼────────────────────────────┼────────┼──── │
│  1   │ 09:00 AM   │ D1 08:00   │ Hurricane warning issued   │ Pending│ ••• │
│  2   │ 09:15 AM   │ D1 10:00   │ EOC activation ordered     │ Pending│ ••• │
│  3   │ 09:30 AM   │ D1 14:00   │ Evacuation order issued    │ Pending│ ••• │
└─────────────────────────────────────────────────────────────────────────────┘

▲ = Ascending (current sort)
▼ = Descending available on click
Unsorted columns show no indicator until hovered
```

### Sort State Cycle

```
Click 1: Column A ▲ (ascending)
         ↓
Click 2: Column A ▼ (descending)
         ↓
Click 3: No sort indicator (default order)
         ↓
Click 4: Column A ▲ (ascending again)
```

### Hover State

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  # │ Scheduled ▲ │ Scenario △  │ Title                      │ Status │     │
└─────────────────────────────────────────────────────────────────────────────┘

△ = Hover indicator showing "click to sort ascending"
Sorted column shows filled arrow ▲▼
Hoverable columns show outline arrow △▽
```

## Technical Notes

- Implement client-side sorting for small datasets (<500 rows)
- Consider server-side sorting for large datasets
- Store sort state in component state or URL query params
- Ensure stable sort (equal values maintain relative order)
