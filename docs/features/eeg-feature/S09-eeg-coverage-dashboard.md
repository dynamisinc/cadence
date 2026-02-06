# S09: EEG Coverage Dashboard

**Feature:** Exercise Evaluation Guide (EEG)
**Priority:** P1
**Status:** Not Started
**Points:** 5

## User Story

**As an** Exercise Director,
**I want** to see real-time coverage of Critical Task evaluation,
**So that** I can identify gaps and ensure comprehensive assessment during conduct.

## Context

During exercise conduct, Directors need visibility into evaluation progress:
- Which Critical Tasks have been assessed?
- Which tasks still need evaluation?
- What is the overall performance distribution?
- Are evaluators covering their assigned areas?

This dashboard provides actionable metrics to guide evaluation efforts and identify gaps before the exercise ends.

## Acceptance Criteria

### Dashboard Access

- [ ] **Given** I am on the Conduct view, **when** I view the dashboard area, **then** I see EEG coverage metrics
- [ ] **Given** I am a Director+, **when** I view coverage, **then** I see all metrics including evaluator activity
- [ ] **Given** I am an Evaluator, **when** I view coverage, **then** I see overall coverage (not evaluator breakdown)
- [ ] **Given** the exercise is Active, **when** dashboard displays, **then** data updates in real-time

### Overall Coverage Metrics

- [ ] **Given** the dashboard, **when** displayed, **then** I see total EEG entries count
- [ ] **Given** the dashboard, **when** displayed, **then** I see task coverage: X of Y Critical Tasks evaluated
- [ ] **Given** the dashboard, **when** displayed, **then** I see coverage percentage with visual progress bar
- [ ] **Given** low coverage (<50%), **when** displayed, **then** I see warning indicator

### Rating Distribution

- [ ] **Given** the dashboard, **when** displayed, **then** I see P/S/M/U distribution as counts
- [ ] **Given** the dashboard, **when** displayed, **then** I see P/S/M/U distribution as percentages
- [ ] **Given** the distribution, **when** displayed, **then** I see visual bar chart or pie chart
- [ ] **Given** I click on a rating segment, **when** navigating, **then** I go to EEG entries list filtered by that rating

### By Capability Target

- [ ] **Given** the dashboard, **when** displayed, **then** I see each Capability Target with its coverage
- [ ] **Given** a Capability Target row, **when** displayed, **then** I see: tasks evaluated / total tasks
- [ ] **Given** a Capability Target row, **when** displayed, **then** I see mini rating distribution
- [ ] **Given** a target with 0% coverage, **when** displayed, **then** I see warning indicator
- [ ] **Given** I click on a Capability Target, **when** navigating, **then** I see its tasks and entries

### Unevaluated Tasks List

- [ ] **Given** the dashboard, **when** tasks are unevaluated, **then** I see a "Tasks Needing Evaluation" section
- [ ] **Given** the unevaluated list, **when** displayed, **then** tasks are grouped by Capability Target
- [ ] **Given** an unevaluated task, **when** I click "Assess", **then** EEG Entry form opens for that task
- [ ] **Given** all tasks are evaluated, **when** displayed, **then** I see "All tasks evaluated" success message

### Evaluator Activity (Director+ Only)

- [ ] **Given** I am a Director+, **when** viewing dashboard, **then** I see evaluator contribution summary
- [ ] **Given** I am an Evaluator, **when** viewing dashboard, **then** I do NOT see evaluator activity section
- [ ] **Given** the evaluator summary, **when** displayed, **then** I see entry count per evaluator
- [ ] **Given** evaluators with 0 entries, **when** displayed, **then** I see them flagged

### Compact Dashboard Mode

- [ ] **Given** the exercise is Active and I am on the Conduct tab, **when** displayed, **then** I see the compact EEG dashboard summary bar
- [ ] **Given** the compact dashboard, **when** displayed, **then** I see: task coverage fraction, rating mini-distribution, pending task count
- [ ] **Given** the compact dashboard, **when** I click "Details", **then** the full dashboard expands or opens

### Exercise Status Variations

- [ ] **Given** the exercise is in Draft status, **when** viewing dashboard, **then** I see "No exercise conduct data yet" with link to setup
- [ ] **Given** the exercise is Active, **when** viewing dashboard, **then** metrics update in real-time
- [ ] **Given** the exercise is Completed, **when** viewing dashboard, **then** metrics show final values with "Exercise Completed" indicator
- [ ] **Given** the exercise is Archived, **when** viewing dashboard, **then** metrics are read-only with archive notice

### Real-Time Updates

- [ ] **Given** the dashboard is open, **when** a new EEG entry is created, **then** metrics update automatically
- [ ] **Given** an entry is deleted, **when** dashboard is open, **then** metrics update automatically
- [ ] **Given** SignalR connection, **when** updates arrive, **then** visual refresh indicates new data

### Error & Empty States

- [ ] **Given** the coverage API fails, **when** error occurs, **then** I see an error message with retry button
- [ ] **Given** no Capability Targets exist, **when** viewing dashboard, **then** I see "Define Capability Targets to enable EEG evaluation" with link to setup
- [ ] **Given** Capability Targets exist but no entries, **when** dashboard displays, **then** coverage shows "0 of X tasks" with 0% progress

### Performance

- [ ] **Given** 5 evaluators are entering data simultaneously, **when** dashboard updates, **then** each update reflects within 3 seconds
- [ ] **Given** dashboard data is stale (>30 seconds), **when** displayed, **then** a "Last updated X seconds ago" indicator shows

## Permission Matrix

| Action | Admin | Director | Controller | Evaluator | Observer |
|--------|-------|----------|------------|-----------|----------|
| View overall coverage | ✅ | ✅ | ✅ | ✅ | ✅* |
| View evaluator activity | ✅ | ✅ | ❌ | ❌ | ❌ |
| Click "Assess" to create entry | ✅ | ✅ | ❌ | ✅ | ❌ |
| Navigate to filtered entry list | ✅ | ✅ | ✅ | ✅ | ✅* |

*Observer view-only access determined by exercise settings

## API Specification

### GET /api/exercises/{exerciseId}/eeg-coverage

**Response 200:**
```json
{
  "totalEntries": 24,
  "taskCoverage": {
    "evaluated": 8,
    "total": 12,
    "percentage": 67
  },
  "ratingDistribution": {
    "P": { "count": 5, "percentage": 42 },
    "S": { "count": 3, "percentage": 25 },
    "M": { "count": 2, "percentage": 17 },
    "U": { "count": 2, "percentage": 17 }
  },
  "byCapabilityTarget": [
    {
      "targetId": "guid",
      "targetDescription": "Establish communications within 30 minutes",
      "capabilityName": "Operational Communications",
      "sources": "Metro County EOP, Annex F",
      "tasksEvaluated": 3,
      "tasksTotal": 3,
      "ratings": ["P", "S", "P"],
      "coveragePercentage": 100
    },
    {
      "targetId": "guid",
      "targetDescription": "Open shelter within 2 hours",
      "capabilityName": "Mass Care Services",
      "sources": null,
      "tasksEvaluated": 2,
      "tasksTotal": 4,
      "ratings": ["M", "S"],
      "coveragePercentage": 50
    }
  ],
  "unevaluatedTasks": [
    {
      "taskId": "guid",
      "taskDescription": "Coordinate with Red Cross",
      "capabilityTargetId": "guid",
      "capabilityName": "Mass Care Services"
    },
    {
      "taskId": "guid",
      "taskDescription": "Track shelter population",
      "capabilityTargetId": "guid",
      "capabilityName": "Mass Care Services"
    }
  ],
  "evaluatorActivity": [
    {
      "evaluatorId": "guid",
      "evaluatorName": "Robert Chen",
      "entryCount": 6,
      "lastEntryAt": "2026-02-03T10:45:00Z"
    },
    {
      "evaluatorId": "guid",
      "evaluatorName": "Sarah Kim",
      "entryCount": 4,
      "lastEntryAt": "2026-02-03T10:32:00Z"
    },
    {
      "evaluatorId": "guid",
      "evaluatorName": "Lisa Park",
      "entryCount": 0,
      "lastEntryAt": null
    }
  ],
  "lastUpdatedAt": "2026-02-03T10:47:00Z"
}
```

**Response 401:** Unauthorized
**Response 403:** Forbidden (not exercise member)
**Response 404:** Exercise not found

**Note:** `evaluatorActivity` array is only included for Director+ roles.

## Wireframes

### EEG Coverage Dashboard

```
┌─────────────────────────────────────────────────────────────────────────┐
│  EEG Coverage                                        Exercise: Active   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────────────────┐  ┌─────────────────────────────┐  │
│  │  Task Coverage                  │  │  Rating Distribution        │  │
│  │                                 │  │                             │  │
│  │  8 of 12 tasks evaluated        │  │  ┌───┐                      │  │
│  │                                 │  │  │   │ P: 5 (42%)           │  │
│  │  ████████████░░░░░░  67%        │  │  │   │ S: 3 (25%)           │  │
│  │                                 │  │  │   │ M: 2 (17%)           │  │
│  │  ⚠️ 4 tasks need evaluation    │  │  │   │ U: 2 (17%)           │  │
│  │                                 │  │  └───┘                      │  │
│  └─────────────────────────────────┘  └─────────────────────────────┘  │
│                                                                         │
│  By Capability Target                                                   │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Target                          │ Coverage │ Ratings            │   │
│  │─────────────────────────────────┼──────────┼────────────────────│   │
│  │ Operational Communications      │ 3/3 ✅   │ [P][S][P]          │   │
│  │ Mass Care Services              │ 2/4 ⚠️   │ [M][S]             │   │
│  │ Emergency Operations Coord.     │ 3/5 ⚠️   │ [P][P][U]          │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ⚠️ Tasks Needing Evaluation                              [Collapse ▲] │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                                                                  │   │
│  │ Mass Care Services:                                              │   │
│  │   □ Coordinate with Red Cross                      [Assess →]   │   │
│  │   □ Track shelter population                       [Assess →]   │   │
│  │                                                                  │   │
│  │ Emergency Operations Coordination:                               │   │
│  │   □ Establish resource tracking                    [Assess →]   │   │
│  │   □ Conduct shift change briefing                  [Assess →]   │   │
│  │                                                                  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Evaluator Activity (Director View)                                     │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Evaluator        │ Entries │ Last Entry │                       │   │
│  │──────────────────┼─────────┼────────────┼───────────────────────│   │
│  │ Robert Chen      │    6    │ 10:45      │ ████████              │   │
│  │ Sarah Kim        │    4    │ 10:32      │ █████                 │   │
│  │ Mike Jones       │    2    │ 09:55      │ ██                    │   │
│  │ Lisa Park        │    0    │ —          │ ⚠️ No entries yet    │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Last updated: 10:47 AM                                    [Refresh]   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Compact Dashboard (During Active Conduct)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  EEG: 8/12 tasks (67%) │ P:5 S:3 M:2 U:2 │ ⚠️ 4 tasks pending [Details]│
└─────────────────────────────────────────────────────────────────────────┘
```

### All Tasks Evaluated State

```
┌─────────────────────────────────────────────────────────────────────────┐
│  ✅ All Critical Tasks Evaluated                                       │
│                                                                         │
│  12 of 12 tasks have at least one EEG entry.                           │
│                                                                         │
│  Rating Summary: P:5 (42%) │ S:4 (33%) │ M:2 (17%) │ U:1 (8%)          │
│                                                                         │
│  Consider reviewing tasks with M or U ratings before exercise ends.    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Empty State (No Capability Targets)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  EEG Coverage                                                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  📋 No Capability Targets Defined                                      │
│                                                                         │
│  Define Capability Targets and Critical Tasks to enable                 │
│  structured EEG evaluation during exercise conduct.                     │
│                                                                         │
│                              [Go to EEG Setup →]                       │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Out of Scope

- Historical coverage trends (future enhancement)
- Coverage alerts/notifications (future enhancement)
- Evaluator assignment tracking (future enhancement)
- Export coverage report (future enhancement)

## Dependencies

- S01-S02: Capability Targets and Critical Tasks
- S06: EEG Entry creation (entries to count)
- S07: View EEG Entries (click-through navigation)
- S11: Sources field (display in target details)
- SignalR for real-time updates

## Technical Notes

- Dashboard can be a collapsible panel in Conduct view
- Use SignalR to push coverage updates when entries change
- Cache coverage data with short TTL (10-30 seconds) for performance
- Consider using Recharts for visual charts
- Rating colors: P=green (#4caf50), S=yellow (#ff9800), M=orange (#f57c00), U=red (#f44336)
- Evaluator activity section conditionally rendered based on role

## Test Scenarios

### Unit Tests
- Coverage calculation with various entry counts
- Rating distribution percentages
- Unevaluated tasks list filtering
- Evaluator activity aggregation
- Compact dashboard rendering

### Integration Tests
- Dashboard reflects actual entry data
- Real-time update when entry created
- Real-time update when entry deleted
- Click-through to filtered entry list
- "Assess" button opens correct form
- Permission check for evaluator activity section
- Exercise status variations display correctly
- Empty state shows when no targets defined

---

*Story created: 2026-02-03*
*Revised: 2026-02-05 — Added compact mode, exercise status variations, error/empty states, performance criteria*
