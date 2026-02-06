# S05: Link Injects to Critical Tasks

**Feature:** Exercise Evaluation Guide (EEG)
**Priority:** P0
**Status:** Not Started
**Points:** 5

## User Story

**As an** Exercise Planner,
**I want** to link MSEL injects to the Critical Tasks they are designed to test,
**So that** evaluators know which tasks to assess when each inject fires and we can verify MSEL coverage.

## Context

The MSEL (Master Scenario Events List) is designed to prompt players to demonstrate capabilities. Each inject should test one or more Critical Tasks. This linkage:

1. **For Evaluators:** Shows which task(s) to assess when an inject fires
2. **For Planners:** Reveals gaps where Critical Tasks have no injects testing them
3. **For AAR:** Traces observations back through the MSEL design

This is a many-to-many relationship: one inject can test multiple tasks, and one task can be tested by multiple injects.

## Acceptance Criteria

### From MSEL View (Inject → Tasks)

- [ ] **Given** I am editing an inject, **when** I view the form, **then** I see a "Critical Tasks" multi-select field
- [ ] **Given** the Critical Tasks selector, **when** displayed, **then** it shows tasks grouped by Capability Target
- [ ] **Given** the selector, **when** I select tasks, **then** selections persist when I save the inject
- [ ] **Given** an inject with linked tasks, **when** displayed in MSEL list, **then** I see task count indicator
- [ ] **Given** I am a Director+ role, **when** I edit an inject, **then** I can modify task links
- [ ] **Given** I am an Evaluator, **when** I view an inject, **then** I see linked tasks (read-only)

### From EEG Setup View (Task → Injects)

- [ ] **Given** I am viewing a Critical Task in EEG Setup, **when** expanded, **then** I see linked inject count
- [ ] **Given** I click on the inject count, **when** dialog opens, **then** I see list of linked injects
- [ ] **Given** the linked injects dialog, **when** displayed, **then** I can add/remove inject links
- [ ] **Given** a task with no linked injects, **when** displayed, **then** I see a warning indicator

### Inject Detail View

- [ ] **Given** I view inject details, **when** the inject has linked tasks, **then** I see the tasks listed
- [ ] **Given** the linked tasks list, **when** displayed, **then** each task shows its Capability Target context
- [ ] **Given** I click on a linked task, **when** navigating, **then** I go to the EEG Setup for that target

### Coverage Indicators

- [ ] **Given** the EEG Setup page, **when** displayed, **then** I see overall coverage indicator
- [ ] **Given** a Critical Task without linked injects, **when** displayed, **then** I see "⚠️ No injects test this task"
- [ ] **Given** the MSEL view, **when** filtering, **then** I can filter to "Injects without Critical Tasks"

### API Endpoints

- [ ] **Given** I call PUT `/api/injects/{injectId}/critical-tasks`, **when** valid task IDs provided, **then** links are updated
- [ ] **Given** I call PUT `/api/critical-tasks/{taskId}/injects`, **when** valid inject IDs provided, **then** links are updated
- [ ] **Given** I call GET `/api/injects/{injectId}`, **when** the inject has tasks, **then** response includes linked tasks
- [ ] **Given** invalid task ID in request, **when** processed, **then** I receive 400 Bad Request
- [ ] **Given** task from different exercise, **when** linking, **then** I receive 400 Bad Request

### Offline Support

- [ ] **Given** I am offline, **when** I link tasks to an inject, **then** changes queue for sync
- [ ] **Given** I come online, **when** sync runs, **then** link changes are applied

## Wireframes

### Inject Edit Form - Critical Tasks Section

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Edit Inject: INJ-007                                           [X]    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  [Other inject fields...]                                               │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  Critical Tasks Tested                                                  │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Select tasks this inject is designed to test...              ▼  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Selected:                                                              │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ [x] Activate emergency communication plan                       │   │
│  │     └─ Operational Communications                               │   │
│  │ [x] Establish radio net with field units                        │   │
│  │     └─ Operational Communications                               │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  💡 Linking injects to Critical Tasks enables evaluators to know       │
│     what to assess and helps identify MSEL coverage gaps.              │
│                                                                         │
│                                          [Cancel]  [Save Inject]        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Critical Tasks Selector (Dropdown Expanded)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Select tasks this inject is designed to test...                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  🔍 Search tasks...                                                     │
│                                                                         │
│  ▼ Operational Communications                                           │
│    □ Activate emergency communication plan                              │
│    ☑ Establish radio net with field units                              │
│    □ Test backup communication systems                                  │
│                                                                         │
│  ▼ Mass Care Services                                                   │
│    □ Activate shelter team                                              │
│    □ Open designated shelter facility                                   │
│    □ Begin shelter registration process                                 │
│                                                                         │
│  ▼ Emergency Operations Coordination                                    │
│    □ Issue EOC activation notification                                  │
│    ☑ Staff EOC positions per roster                                    │
│    □ Establish resource tracking system                                 │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### MSEL View with Task Indicators

```
┌─────────────────────────────────────────────────────────────────────────┐
│  MSEL - Hurricane Response TTX                                         │
├─────────────────────────────────────────────────────────────────────────┤
│  Filter: [All Phases ▼] [All Status ▼] [📋 Has Tasks ▼]               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  # │ Time  │ Title                        │ Status  │ Tasks │ Actions  │
│ ───┼───────┼──────────────────────────────┼─────────┼───────┼───────── │
│  3 │ 09:00 │ EOC Activation Notice        │ Pending │ 📋 2  │ [•••]    │
│  4 │ 09:15 │ First Responder Dispatch     │ Pending │ 📋 1  │ [•••]    │
│  5 │ 09:30 │ Media Inquiry                │ Pending │ ⚠️ 0  │ [•••]    │
│  6 │ 09:45 │ Shelter Activation Request   │ Pending │ 📋 3  │ [•••]    │
│                                                                         │
│  ⚠️ 1 inject has no linked Critical Tasks                              │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Task's Linked Injects Dialog

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Linked Injects                                                 [X]    │
│  Task: Activate emergency communication plan                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Injects testing this task:                          [+ Link Inject]   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ INJ-003  │  09:00  │  EOC Activation Notice           [Unlink] │   │
│  │ INJ-007  │  09:30  │  Communication System Test       [Unlink] │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Available injects to link:                                             │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ 🔍 Search injects...                                            │   │
│  │                                                                  │   │
│  │ INJ-012  │  10:00  │  Backup Radio Test              [+ Link]  │   │
│  │ INJ-015  │  10:30  │  Interop Channel Check          [+ Link]  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│                                                         [Done]          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## API Specification

### PUT /api/injects/{injectId}/critical-tasks

**Request:**
```json
{
  "criticalTaskIds": ["guid1", "guid2", "guid3"]
}
```

**Response 200:**
```json
{
  "injectId": "guid",
  "criticalTasks": [
    {
      "id": "guid1",
      "taskDescription": "Activate emergency communication plan",
      "capabilityTarget": {
        "id": "guid",
        "targetDescription": "Establish communications within 30 min",
        "capability": { "name": "Operational Communications" }
      }
    }
  ]
}
```

### PUT /api/critical-tasks/{taskId}/injects

**Request:**
```json
{
  "injectIds": ["guid1", "guid2"]
}
```

**Response 200:** Similar structure with inject details

## Out of Scope

- Automatic task suggestions based on inject content (AI - future)
- Bulk linking operations (future)
- Import task links from Excel (future)

## Dependencies

- S01: Capability Target Entity and API
- S02: Critical Task Entity and API (includes InjectCriticalTask junction)
- Inject CRUD feature (inject entity and forms)

## Technical Notes

- Use existing multi-select patterns from Cadence
- Group tasks by Capability Target in selector for better UX
- Include search/filter for exercises with many tasks
- Validate that all task IDs belong to the same exercise as the inject
- Consider virtualized list for large MSEL views

## Test Scenarios

### Unit Tests
- InjectCriticalTask junction operations
- Validation: cross-exercise linking blocked
- Multi-select component renders grouped tasks

### Integration Tests
- Link tasks from inject edit form
- Link injects from task dialog
- Coverage indicators update correctly
- Filter MSEL by "has tasks" / "no tasks"
- Offline link changes sync correctly

---

*Story created: 2026-02-03*
