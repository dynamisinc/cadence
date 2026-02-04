# S07: View EEG Entries

**Feature:** Exercise Evaluation Guide (EEG)
**Priority:** P0
**Status:** Not Started
**Points:** 5

## User Story

**As an** Exercise Director,
**I want** to view all EEG entries organized by capability and task,
**So that** I can review evaluator assessments and prepare for After-Action Review.

## Context

EEG entries need to be viewable in multiple contexts:
1. **During Conduct:** Quick access to see what's been assessed
2. **Post-Conduct:** Organized review for AAR preparation
3. **By Capability:** See all assessments for a capability target
4. **By Evaluator:** Review an evaluator's entries

This story covers the list views and organization of EEG entries.

## Acceptance Criteria

### Access Points

- [ ] **Given** I am on the Exercise page, **when** I view tabs, **then** I see "EEG Review" (or entries appear in Review Mode)
- [ ] **Given** I am a Director+, **when** I access EEG Review, **then** I see all entries for the exercise
- [ ] **Given** I am an Evaluator, **when** I access EEG Review, **then** I see all entries (read access to all)
- [ ] **Given** I am an Observer, **when** permitted, **then** I can view entries read-only

### List View - All Entries

- [ ] **Given** I view the EEG entries list, **when** displayed, **then** I see: timestamp, task, rating, evaluator, observation preview
- [ ] **Given** the list, **when** entries exist, **then** they are sorted by timestamp (newest first by default)
- [ ] **Given** the list, **when** I click an entry, **then** I see the full entry details in a panel/dialog
- [ ] **Given** no entries exist, **when** displayed, **then** I see empty state with guidance

### Grouped View - By Capability

- [ ] **Given** I select "Group by Capability" view, **when** displayed, **then** entries are grouped under Capability Targets
- [ ] **Given** grouped view, **when** a Capability Target has entries, **then** I see aggregate rating summary
- [ ] **Given** grouped view, **when** I expand a Capability Target, **then** I see entries grouped by Critical Task
- [ ] **Given** a Critical Task, **when** expanded, **then** I see all EEG entries for that task

### Filtering

- [ ] **Given** the entries list, **when** I filter by rating, **then** only entries with that rating display
- [ ] **Given** the entries list, **when** I filter by evaluator, **then** only that evaluator's entries display
- [ ] **Given** the entries list, **when** I filter by Capability Target, **then** only entries for that target display
- [ ] **Given** the entries list, **when** I filter by time range, **then** only entries in that range display
- [ ] **Given** active filters, **when** displayed, **then** I see filter chips that can be cleared

### Entry Detail View

- [ ] **Given** I click on an entry, **when** detail opens, **then** I see full observation text
- [ ] **Given** entry detail, **when** displayed, **then** I see: Capability Target, Critical Task, Standard
- [ ] **Given** entry detail, **when** displayed, **then** I see: Rating with description, ObservedAt, RecordedAt
- [ ] **Given** entry detail, **when** displayed, **then** I see: Evaluator name, Triggering inject (if linked)
- [ ] **Given** entry detail, **when** the entry has a triggering inject, **then** I can click to view inject details

### Summary Statistics

- [ ] **Given** the EEG Review page, **when** displayed, **then** I see total entry count
- [ ] **Given** the summary, **when** displayed, **then** I see rating distribution (P/S/M/U counts)
- [ ] **Given** the summary, **when** displayed, **then** I see evaluator contribution counts

### Offline Support

- [ ] **Given** I am offline, **when** I view EEG entries, **then** cached entries display
- [ ] **Given** entries with pending sync, **when** displayed, **then** I see sync status indicator
- [ ] **Given** I come online, **when** viewing list, **then** it refreshes with synced data

## Wireframes

### EEG Entries List View

```
┌─────────────────────────────────────────────────────────────────────────┐
│  EEG Review                                          Exercise: Active   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Summary: 24 entries │ P: 8 │ S: 10 │ M: 4 │ U: 2                       │
│                                                                         │
│  View: [● List] [○ By Capability]     Filter: [All Ratings ▼]          │
│                                                [All Evaluators ▼]       │
│                                                [All Targets ▼]          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Time  │ Task                              │ Rating │ Evaluator │       │
│ ───────┼───────────────────────────────────┼────────┼───────────┼────── │
│  10:45 │ Activate emergency comm plan      │   S    │ R. Chen   │ [>]   │
│        │ EOC issued activation at 09:15... │        │           │       │
│ ───────┼───────────────────────────────────┼────────┼───────────┼────── │
│  10:32 │ Staff EOC positions per roster    │   P    │ S. Kim    │ [>]   │
│        │ All positions filled within 45... │        │           │       │
│ ───────┼───────────────────────────────────┼────────┼───────────┼────── │
│  10:18 │ Establish radio net               │   M    │ R. Chen   │ [>]   │
│        │ Radio net established but Field.. │        │           │       │
│ ───────┼───────────────────────────────────┼────────┼───────────┼────── │
│  09:55 │ Open shelter facility             │   U    │ M. Jones  │ [>]   │
│        │ Shelter could not be opened due.. │        │           │       │
│                                                                         │
│  [Load More...]                                                         │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Grouped by Capability View

```
┌─────────────────────────────────────────────────────────────────────────┐
│  EEG Review                                          Exercise: Active   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  View: [○ List] [● By Capability]                                       │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ ▼ Operational Communications                    8 entries         │ │
│  │   "Establish interoperable communications..."   [P:3 S:3 M:2 U:0] │ │
│  │                                                                   │ │
│  │   ┌─────────────────────────────────────────────────────────────┐│ │
│  │   │ ▼ Activate emergency communication plan        4 entries    ││ │
│  │   │                                                             ││ │
│  │   │   10:45 │ S │ R. Chen │ EOC issued activation at 09:15...  ││ │
│  │   │   10:12 │ P │ S. Kim  │ Notification sent within 5 min...  ││ │
│  │   │   09:48 │ S │ R. Chen │ Initial notification delayed...    ││ │
│  │   │   09:30 │ P │ M. Jones│ All stakeholders confirmed...      ││ │
│  │   ├─────────────────────────────────────────────────────────────┤│ │
│  │   │ ▶ Establish radio net with field units         2 entries    ││ │
│  │   ├─────────────────────────────────────────────────────────────┤│ │
│  │   │ ▶ Test backup communication systems            2 entries    ││ │
│  │   └─────────────────────────────────────────────────────────────┘│ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ ▶ Mass Care Services                           10 entries         │ │
│  │   "Open and staff shelter within 2 hours..."   [P:2 S:5 M:2 U:1] │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ ▶ Emergency Operations Coordination             6 entries         │ │
│  │   "Achieve full EOC staffing within 60 min..." [P:3 S:2 M:0 U:1] │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Entry Detail Panel

```
┌─────────────────────────────────────────────────────────────────────────┐
│  EEG Entry Detail                                               [X]    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Capability Target                                                      │
│  📋 Operational Communications                                         │
│  "Establish interoperable communications within 30 minutes"             │
│                                                                         │
│  Critical Task                                                          │
│  Activate emergency communication plan                                  │
│  Standard: Per SOP 5.2, using emergency notification system             │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  Rating: [S] Performed with Some Challenges                             │
│                                                                         │
│  Observation:                                                           │
│  EOC issued activation notification at 09:15. All stakeholders          │
│  confirmed receipt within 10 minutes. Communication plan followed       │
│  correctly per SOP 5.2. Minor delay in reaching Field Unit 3 due to    │
│  radio interference - switched to backup channel successfully.          │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  Observed at:    10:45 (exercise time)                                  │
│  Recorded at:    2026-02-03 10:47:23 EST                               │
│  Evaluator:      Robert Chen                                            │
│  Triggered by:   INJ-003: EOC Activation Notice  [View Inject →]       │
│                                                                         │
│                                                    [Edit]  [Close]      │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Out of Scope

- Bulk export of entries (S10)
- Entry comparison across exercises (future)
- Evaluator performance analytics (future)
- Print-friendly view (future)

## Dependencies

- S01-S04: Capability Targets and Critical Tasks exist
- S06: EEG Entry creation
- Review Mode feature (if integrating there)

## Technical Notes

- Use virtualized list for exercises with many entries
- Grouped view uses accordion/tree pattern
- Entry detail can be side panel (desktop) or full page (mobile)
- Consider infinite scroll vs. pagination for list
- Rating chips should use consistent colors (P=green, S=yellow, M=orange, U=red)

## Test Scenarios

### Component Tests
- List renders with entries
- Grouped view expands/collapses correctly
- Filters apply correctly
- Empty state displays when no entries

### Integration Tests
- View entries after creation
- Filter by multiple criteria
- Navigate to inject from entry detail
- Offline viewing of cached entries
- Real-time updates when new entries are created

---

*Story created: 2026-02-03*
