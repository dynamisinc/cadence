# Story: S02 - Group Injects

## User Story

**As a** Controller or Exercise Director,
**I want** to group injects by category,
**So that** I can see related injects together and manage large MSELs more easily.

## Context

Large MSELs (50+ injects) become difficult to navigate as a flat list. Grouping allows users to collapse and expand sections, providing a hierarchical view that reduces cognitive load. Common groupings include by phase (operational stages), status (during conduct), or objective (for evaluation focus).

## Acceptance Criteria

### Group Options
- [ ] **Given** I am viewing the MSEL, **when** I click "Group by", **then** I see options: None, Phase, Status, Objective
- [ ] **Given** I select "Phase", **when** grouping applies, **then** injects are grouped under phase headers
- [ ] **Given** I select "Status", **when** grouping applies, **then** injects are grouped by: Pending, Fired, Skipped
- [ ] **Given** I select "Objective", **when** grouping applies, **then** injects are grouped by linked objective (injects with multiple objectives appear in each group)
- [ ] **Given** I select "None", **when** grouping clears, **then** I see a flat list

### Group Headers
- [ ] **Given** grouping is active, **when** I view group headers, **then** each shows: group name and inject count
- [ ] **Given** grouping by Phase, **when** I view a header, **then** I see: "Phase 1: Initial Response (15 injects)"
- [ ] **Given** grouping by Status, **when** I view headers, **then** I see: "Pending (30)", "Fired (15)", "Skipped (2)"
- [ ] **Given** some injects have no phase, **when** grouping by Phase, **then** they appear under "Unassigned"

### Expand/Collapse
- [ ] **Given** I view a group header, **when** I click the expand/collapse icon, **then** the group toggles open/closed
- [ ] **Given** groups are displayed, **when** I first load the view, **then** all groups are expanded by default
- [ ] **Given** I have groups, **when** I click "Collapse All", **then** all groups collapse to headers only
- [ ] **Given** groups are collapsed, **when** I click "Expand All", **then** all groups expand to show injects
- [ ] **Given** a group is collapsed, **when** I view the header, **then** I see ► indicator
- [ ] **Given** a group is expanded, **when** I view the header, **then** I see ▼ indicator

### Group Ordering
- [ ] **Given** grouping by Phase, **when** I view groups, **then** they order by phase number (Unassigned last)
- [ ] **Given** grouping by Status, **when** I view groups, **then** order is: Pending, Fired, Skipped
- [ ] **Given** grouping by Objective, **when** I view groups, **then** they order by objective number

### Sorting Within Groups
- [ ] **Given** grouping is active, **when** I apply a sort, **then** sorting applies within each group
- [ ] **Given** grouped by Phase and sorted by Time, **when** I view results, **then** each phase's injects are time-sorted

### Empty Groups
- [ ] **Given** a group has no injects, **when** grouping applies, **then** the empty group is hidden
- [ ] **Given** all injects are filtered out of a group, **when** I view the grouped list, **then** the empty group is hidden

## Out of Scope

- Multi-level grouping (group by Phase then by Status)
- Custom grouping fields
- Drag-and-drop between groups
- Group summaries/statistics

## Dependencies

- inject-crud/S01: Create Inject (injects must exist)
- exercise-phases/S01: Define Phases (for phase grouping)
- exercise-objectives/S01: Create Objective (for objective grouping)
- inject-organization/S01: Sort Injects (sorting within groups)

## Open Questions

- [ ] Should group expand/collapse state persist?
- [ ] Should we show group progress (e.g., "5 of 15 fired")?
- [ ] Should clicking a group header select all injects in that group?

## Domain Terms

| Term | Definition |
|------|------------|
| Grouping | Organizing items into collapsible categories |
| Group Header | Row showing category name and aggregate info |
| Expand/Collapse | Showing or hiding items within a group |

## UI/UX Notes

### Group By Control

```
┌─────────────────────────────────────────────────────────────────────────┐
│  [Filter ▼]  Group by: [Phase          ▼]  Sort: [Scheduled Time ▼]    │
│                        ┌────────────────┐                               │
│                        │ ○ None         │                               │
│                        │ ● Phase        │                               │
│                        │ ○ Status       │                               │
│                        │ ○ Objective    │                               │
│                        └────────────────┘                               │
└─────────────────────────────────────────────────────────────────────────┘
```

### Grouped MSEL View

```
┌─────────────────────────────────────────────────────────────────────────┐
│  [Collapse All] [Expand All]                                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ▼ Phase 1: Initial Response (15 injects)                              │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  1  │ 09:00 AM │ D1 08:00 │ Hurricane warning issued   │ Pending │ │
│  │  2  │ 09:15 AM │ D1 10:00 │ EOC activation ordered     │ Pending │ │
│  │  3  │ 09:30 AM │ D1 14:00 │ Evacuation order issued    │ Pending │ │
│  │  ... (12 more)                                                    │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ► Phase 2: Sustained Operations (22 injects)              [collapsed] │
│                                                                         │
│  ► Phase 3: Recovery (10 injects)                          [collapsed] │
│                                                                         │
│  ▼ Unassigned (5 injects)                                              │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ 43  │ 11:00 AM │ —        │ Administrative break       │ Pending │ │
│  │ 44  │ 12:30 PM │ —        │ Lunch break                │ Pending │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Grouped by Status (During Conduct)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│  ▼ 🟡 Pending (25 injects)                                             │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ 20  │ 10:30 AM │ Shelter capacity report   │ Pending              │ │
│  │ 21  │ 10:45 AM │ Resource request          │ Pending              │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ▼ 🟢 Fired (19 injects)                                               │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  1  │ 09:00 AM │ Hurricane warning issued  │ ✓ Fired 09:02 AM    │ │
│  │  2  │ 09:15 AM │ EOC activation ordered    │ ✓ Fired 09:15 AM    │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ► 🔴 Skipped (3 injects)                                  [collapsed] │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Client-side grouping for small datasets
- Maintain scroll position when expanding/collapsing
- Store expand/collapse state in component state
- Consider virtualization for groups with many items
