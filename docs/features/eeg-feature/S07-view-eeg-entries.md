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
4. **By Evaluator:** Review an evaluator's entries (Director+ only)

This story covers the list views and organization of EEG entries.

## Acceptance Criteria

### Access Points

- [ ] **Given** I am on the Exercise page, **when** I view tabs, **then** I see "EEG Review" (or entries appear in Review Mode)
- [ ] **Given** I am a Director+, **when** I access EEG Review, **then** I see all entries for the exercise
- [ ] **Given** I am an Evaluator, **when** I access EEG Review, **then** I see all entries (read access to all)
- [ ] **Given** I am a Controller, **when** I access EEG Review, **then** I see all entries (read access to all)
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

### Grouped View - By Evaluator (Director+ Only)

- [ ] **Given** I am a Director+, **when** I select "Group by Evaluator" view, **then** entries are grouped by evaluator name
- [ ] **Given** the By Evaluator view, **when** displayed, **then** each evaluator section shows entry count and rating distribution
- [ ] **Given** I am an Evaluator or Observer, **when** I look for "Group by Evaluator" toggle, **then** I do not see it

### Filtering

- [ ] **Given** the entries list, **when** I filter by rating, **then** only entries with that rating display
- [ ] **Given** the entries list, **when** I filter by evaluator, **then** only that evaluator's entries display
- [ ] **Given** the entries list, **when** I filter by Capability Target, **then** only entries for that target display
- [ ] **Given** the entries list, **when** I filter by time range, **then** only entries in that range display
- [ ] **Given** active filters, **when** displayed, **then** I see filter chips that can be cleared
- [ ] **Given** the entries list, **when** I enter text in the search field, **then** entries filter to those containing that text in the observation
- [ ] **Given** search is active, **when** combined with other filters, **then** all filters apply together (AND logic)

### Sorting

- [ ] **Given** the entries list, **when** I click a sortable column header, **then** sort order toggles (asc/desc)
- [ ] **Given** the entries list, **when** displayed, **then** I can sort by: Observed Time, Recorded Time, Rating, Task Name

### Entry Detail View

- [ ] **Given** I click on an entry, **when** detail opens, **then** I see full observation text
- [ ] **Given** entry detail, **when** displayed, **then** I see: Capability Target, Critical Task, Standard
- [ ] **Given** entry detail, **when** the entry's parent target has Sources (S11), **then** Sources are visible
- [ ] **Given** entry detail, **when** displayed, **then** I see: Rating with description, ObservedAt, RecordedAt
- [ ] **Given** entry detail, **when** displayed, **then** I see: Evaluator name, Triggering inject (if linked)
- [ ] **Given** entry detail, **when** the entry has a triggering inject, **then** I can click to view inject details
- [ ] **Given** entry detail, **when** entry was edited, **then** I see "Edited by [Name] at [Time]" indicator

### Summary Statistics

- [ ] **Given** the EEG Review page, **when** displayed, **then** I see total entry count
- [ ] **Given** the summary, **when** displayed, **then** I see rating distribution (P/S/M/U counts)
- [ ] **Given** the summary, **when** displayed, **then** I see evaluator contribution counts (Director+ only)

### Pagination & Performance

- [ ] **Given** the exercise has more than 20 entries, **when** I view the list, **then** I see first 20 with pagination controls
- [ ] **Given** I scroll to the bottom of the list, **when** more entries exist, **then** I see "Load More" or automatic infinite scroll loads next page
- [ ] **Given** an exercise with 200+ entries, **when** I view the list, **then** the initial load completes within 2 seconds

### Real-Time Updates

- [ ] **Given** I am viewing the entries list, **when** another user creates a new EEG entry, **then** I see a "New entries available" indicator
- [ ] **Given** I click the refresh indicator, **when** triggered, **then** the list updates with new entries
- [ ] **Given** an entry I am viewing is deleted by another user, **when** refresh occurs, **then** the entry disappears from my list

### Offline Support

- [ ] **Given** I am offline, **when** I view EEG entries, **then** cached entries display
- [ ] **Given** entries with pending sync, **when** displayed, **then** I see sync status indicator
- [ ] **Given** I come online, **when** viewing list, **then** it refreshes with synced data

### Error Handling

- [ ] **Given** the API fails to load entries, **when** error occurs, **then** I see an error message with retry option
- [ ] **Given** a network timeout, **when** loading entries, **then** I see a timeout message after 10 seconds

### Accessibility

- [ ] **Given** keyboard navigation, **when** I tab through the list, **then** I can navigate to and activate each entry
- [ ] **Given** a screen reader, **when** an entry is focused, **then** the rating, task, and observation preview are announced
- [ ] **Given** the filter dropdowns, **when** using keyboard, **then** I can open, navigate, and select options

## Permission Matrix

| Action | Admin | Director | Controller | Evaluator | Observer |
|--------|:-----:|:--------:|:----------:|:---------:|:--------:|
| View all entries | ✅ | ✅ | ✅ | ✅ | ✅* |
| View entry detail | ✅ | ✅ | ✅ | ✅ | ✅* |
| View evaluator breakdown | ✅ | ✅ | ❌ | ❌ | ❌ |
| Group by Evaluator | ✅ | ✅ | ❌ | ❌ | ❌ |

*Observer view-only access determined by exercise settings

## API Specification

### GET /api/exercises/{exerciseId}/eeg-entries

**Query Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| page | int | 1 | Page number for pagination |
| pageSize | int | 20 | Items per page (max: 100) |
| rating | string[] | - | Filter by ratings (P, S, M, U) |
| evaluatorId | guid[] | - | Filter by evaluator IDs |
| capabilityTargetId | guid | - | Filter by capability target |
| criticalTaskId | guid | - | Filter by critical task |
| fromDate | datetime | - | Filter entries observed after this time |
| toDate | datetime | - | Filter entries observed before this time |
| sortBy | string | observedAt | Sort field: observedAt, recordedAt, rating |
| sortOrder | string | desc | Sort direction: asc, desc |
| search | string | - | Free-text search in observation text |

**Response 200:**
```json
{
  "items": [
    {
      "id": "guid",
      "criticalTaskId": "guid",
      "criticalTaskDescription": "string",
      "capabilityTargetId": "guid",
      "capabilityTargetDescription": "string",
      "capabilityName": "string",
      "observationText": "string",
      "rating": "P|S|M|U",
      "observedAt": "datetime",
      "recordedAt": "datetime",
      "wasEdited": false,
      "evaluator": {
        "id": "guid",
        "name": "string"
      },
      "triggeringInject": {
        "id": "guid",
        "injectNumber": "string",
        "title": "string"
      }
    }
  ],
  "totalCount": 47,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

**Response 401:** Unauthorized
**Response 403:** Forbidden (not exercise member)
**Response 404:** Exercise not found

### GET /api/eeg-entries/{entryId}

**Response 200:**
```json
{
  "id": "guid",
  "criticalTask": {
    "id": "guid",
    "taskDescription": "string",
    "standard": "string"
  },
  "capabilityTarget": {
    "id": "guid",
    "targetDescription": "string",
    "sources": "string",
    "capability": {
      "id": "guid",
      "name": "string"
    }
  },
  "observationText": "string",
  "rating": "P",
  "observedAt": "datetime",
  "recordedAt": "datetime",
  "wasEdited": true,
  "updatedAt": "datetime",
  "updatedBy": {
    "id": "guid",
    "name": "string"
  },
  "evaluator": {
    "id": "guid",
    "name": "string"
  },
  "triggeringInject": {
    "id": "guid",
    "injectNumber": "INJ-003",
    "title": "string"
  }
}
```

## Wireframes

### EEG Entries List View

```
┌─────────────────────────────────────────────────────────────────────────┐
│  EEG Review                                          Exercise: Active   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Summary: 24 entries │ P: 8 │ S: 10 │ M: 4 │ U: 2                       │
│                                                                         │
│  View: [● List] [○ By Capability] [○ By Evaluator*]                    │
│                                                                         │
│  Filter: [All Ratings ▼] [All Evaluators ▼] [All Targets ▼]            │
│  Search: [🔍 Search observations...                              ]      │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Time ▼ │ Task                              │ Rating │ Evaluator │      │
│ ────────┼───────────────────────────────────┼────────┼───────────┼───── │
│  10:45  │ Activate emergency comm plan      │   S    │ R. Chen   │ [>]  │
│         │ EOC issued activation at 09:15... │        │           │      │
│ ────────┼───────────────────────────────────┼────────┼───────────┼───── │
│  10:32  │ Staff EOC positions per roster    │   P    │ S. Kim    │ [>]  │
│         │ All positions filled within 45... │        │           │      │
│ ────────┼───────────────────────────────────┼────────┼───────────┼───── │
│  10:18  │ Establish radio net               │   M    │ R. Chen   │ [>]  │
│         │ Radio net established but Field.. │        │  Edited   │      │
│                                                                         │
│  [Load More...]                            Page 1 of 3                  │
│                                                                         │
│  *By Evaluator view visible to Director+ only                          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Grouped by Capability View

```
┌─────────────────────────────────────────────────────────────────────────┐
│  EEG Review                                          Exercise: Active   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  View: [○ List] [● By Capability] [○ By Evaluator]                     │
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
│  Sources: Metro County EOP, Annex F; SOP 5.2                           │
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
│  Edited by Sarah Kim at 11:30 AM                                       │
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
- S11: Sources field on Capability Target
- Review Mode feature (if integrating there)

## Technical Notes

- Use virtualized list for exercises with many entries
- Grouped view uses accordion/tree pattern
- Entry detail can be side panel (desktop) or full page (mobile)
- Use infinite scroll with "Load More" fallback for pagination
- Rating chips should use consistent colors: P=green (#4caf50), S=yellow (#ff9800), M=orange (#f57c00), U=red (#f44336)
- Use SignalR to push entry creation/deletion notifications

## Test Scenarios

### Component Tests
- List renders with entries
- Grouped view expands/collapses correctly
- Filters apply correctly
- Empty state displays when no entries
- Search filters observation text
- Sort toggles work correctly

### Integration Tests
- View entries after creation
- Filter by multiple criteria
- Navigate to inject from entry detail
- Offline viewing of cached entries
- Real-time updates when new entries are created
- Pagination loads additional entries
- Permission checks for evaluator breakdown view

---

*Story created: 2026-02-03*
*Revised: 2026-02-05 — Added API spec, pagination, real-time, permissions, accessibility criteria*
