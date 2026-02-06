# S04: Define Critical Tasks UI

**Feature:** Exercise Evaluation Guide (EEG)
**Priority:** P0
**Status:** Not Started
**Points:** 5

## User Story

**As an** Exercise Director,
**I want** to define Critical Tasks under each Capability Target,
**So that** evaluators know exactly what actions to observe during exercise conduct.

## Context

Critical Tasks are the specific, observable actions that demonstrate whether a Capability Target is being achieved. This story provides the UI for creating and managing Critical Tasks within the EEG Setup interface.

Per HSEEP methodology, Critical Tasks should be:
- Observable (evaluators can see them happen)
- Assessable (can be rated P/S/M/U)
- Derived from plans, SOPs, or standards

## Acceptance Criteria

### Access & Display

- [ ] **Given** I am on the EEG Setup page with a Capability Target expanded, **when** I view the tasks section, **then** I see all Critical Tasks for that target
- [ ] **Given** I am a Director+ role, **when** I view tasks, **then** I see Add/Edit/Delete controls
- [ ] **Given** I am an Evaluator or Observer, **when** I view tasks, **then** I see read-only list
- [ ] **Given** no Critical Tasks exist for a target, **when** displayed, **then** I see empty state with prompt

### Create Task

- [ ] **Given** I am viewing a Capability Target, **when** I click "+ Add Task", **then** I see a create form
- [ ] **Given** the create form, **when** displayed, **then** I see fields for Task Description and Standard
- [ ] **Given** the create form, **when** I enter a description and submit, **then** the task is created
- [ ] **Given** the create form, **when** description is empty, **then** I see validation error
- [ ] **Given** the create form, **when** Standard is left blank, **then** task creates successfully (optional field)
- [ ] **Given** I create a task, **when** saved, **then** it appears at the end of the task list

### Edit Task

- [ ] **Given** a Critical Task in the list, **when** I click the edit button, **then** I see the edit form
- [ ] **Given** the edit form, **when** displayed, **then** current values are populated
- [ ] **Given** the edit form, **when** I update and save, **then** changes are persisted
- [ ] **Given** the edit form, **when** I click Cancel, **then** changes are discarded

### Delete Task

- [ ] **Given** a Critical Task in the list, **when** I click delete, **then** I see confirmation dialog
- [ ] **Given** the confirmation, **when** task has EEG entries, **then** dialog warns about cascade delete
- [ ] **Given** the confirmation, **when** task has linked injects, **then** dialog shows inject count
- [ ] **Given** I confirm delete, **when** processed, **then** task, links, and entries are deleted
- [ ] **Given** I cancel delete, **when** dialog closes, **then** nothing is deleted

### Reorder Tasks

- [ ] **Given** multiple Critical Tasks, **when** I drag a task, **then** I can reorder within the target
- [ ] **Given** I reorder tasks, **when** I drop, **then** new sort order is saved
- [ ] **Given** mobile view, **when** displayed, **then** up/down buttons are available

### Inline Information

- [ ] **Given** a Critical Task, **when** displayed, **then** I see linked inject count
- [ ] **Given** a task with no linked injects, **when** displayed, **then** I see warning indicator
- [ ] **Given** a task with EEG entries, **when** displayed, **then** I see entry count

### Offline Support

- [ ] **Given** I am offline, **when** I create a task, **then** it saves locally with pending indicator
- [ ] **Given** I am offline, **when** I edit a task, **then** changes save locally
- [ ] **Given** I come back online, **when** sync completes, **then** pending indicators clear

## Wireframes

### Task List within Expanded Target

```
┌───────────────────────────────────────────────────────────────────────┐
│ ≡  📋 Operational Communications                       [Edit] [🗑️]   │
│     "Establish interoperable communications within 30 minutes"        │
│     [▲ Collapse]                                                      │
│  ─────────────────────────────────────────────────────────────────── │
│     Critical Tasks:                                    [+ Add Task]   │
│                                                                       │
│     ┌─────────────────────────────────────────────────────────────┐  │
│     │ ≡  1. Activate emergency communication plan    [Edit] [🗑️] │  │
│     │       Standard: Per SOP 5.2                                 │  │
│     │       📎 2 injects  •  📝 1 EEG entry                       │  │
│     ├─────────────────────────────────────────────────────────────┤  │
│     │ ≡  2. Establish radio net with field units     [Edit] [🗑️] │  │
│     │       📎 1 inject  •  📝 0 EEG entries                      │  │
│     ├─────────────────────────────────────────────────────────────┤  │
│     │ ≡  3. Test backup communication systems        [Edit] [🗑️] │  │
│     │       ⚠️ No injects linked  •  📝 0 EEG entries             │  │
│     └─────────────────────────────────────────────────────────────┘  │
│                                                                       │
│     💡 Link injects to tasks in the MSEL view to enable traceability │
│                                                                       │
└───────────────────────────────────────────────────────────────────────┘
```

### Create/Edit Task Dialog

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Add Critical Task                                              [X]    │
│  Capability Target: Operational Communications                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Task Description *                                                     │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Activate emergency communication plan                           │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│  What specific action should be observed and assessed?                 │
│                                                                         │
│  Standard (optional)                                                    │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Per SOP 5.2, using emergency notification system within 10     │   │
│  │ minutes of EOC activation decision                              │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│  Reference the plan, SOP, or standard that defines how this task       │
│  should be performed.                                                  │
│                                                                         │
│  Examples of good task descriptions:                                    │
│  • "Issue EOC activation notification to all stakeholders"             │
│  • "Establish unified command structure"                               │
│  • "Complete initial damage assessment report"                         │
│                                                                         │
│                                          [Cancel]  [Save Task]         │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Delete Confirmation with Warnings

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Delete Critical Task?                                          [X]    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Are you sure you want to delete this Critical Task?                   │
│                                                                         │
│  "Activate emergency communication plan"                                │
│                                                                         │
│  ⚠️ This will also delete:                                             │
│     • 2 linked inject associations                                      │
│     • 3 EEG entries recorded against this task                         │
│                                                                         │
│  This action cannot be undone.                                          │
│                                                                         │
│                                          [Cancel]  [Delete Task]        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Out of Scope

- Linking injects to tasks (S05)
- EEG entry against tasks (S06)
- Bulk task import (future)
- Task templates/library (future)
- Evaluator assignment to tasks (future)

## Dependencies

- S01: Capability Target Entity and API
- S02: Critical Task Entity and API
- S03: Define Capability Targets UI (tasks appear within target cards)

## Technical Notes

- Tasks are displayed inline within the expanded Capability Target accordion
- Use same drag-and-drop library as S03 for consistency
- Consider inline editing for simple updates (click to edit description)
- Task numbers are display-only (based on sort order), not stored

## Test Scenarios

### Component Tests
- CriticalTaskList renders within target
- CriticalTaskForm validation
- Delete confirmation shows correct counts
- Drag-and-drop reorder works

### Integration Tests
- Create task within target
- Edit task updates correctly
- Delete cascades properly
- Reorder persists across page reload
- Offline operations queue correctly

---

*Story created: 2026-02-03*
