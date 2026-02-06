# S03: Define Capability Targets UI

**Feature:** Exercise Evaluation Guide (EEG)
**Priority:** P0
**Status:** Not Started
**Points:** 8

## User Story

**As an** Exercise Director,
**I want** a user interface to define Capability Targets for my exercise,
**So that** I can establish measurable evaluation criteria before exercise conduct.

## Context

During the exercise planning phase (Initial Planning Meeting per HSEEP), the Exercise Director defines what capabilities will be evaluated and what "success" looks like for each. This story provides the UI for creating and managing Capability Targets.

Capability Targets bridge the gap between the organization's capability library (generic) and the specific performance thresholds for this exercise (measurable).

## Acceptance Criteria

### Navigation & Access

- [ ] **Given** I am on the Exercise detail page, **when** I view the tabs, **then** I see an "EEG Setup" tab
- [ ] **Given** I am a Director+ role, **when** I click "EEG Setup", **then** I see the Capability Targets management interface
- [ ] **Given** I am an Evaluator or Observer, **when** I click "EEG Setup", **then** I see a read-only view of targets and tasks
- [ ] **Given** I am a Controller, **when** I view EEG Setup, **then** I can see but not edit targets

### List View

- [ ] **Given** I am on the EEG Setup page, **when** it loads, **then** I see all Capability Targets for this exercise
- [ ] **Given** Capability Targets exist, **when** displayed, **then** each shows: capability name, target description, task count
- [ ] **Given** no Capability Targets exist, **when** page loads, **then** I see an empty state with guidance
- [ ] **Given** the list is displayed, **when** targets have different sort orders, **then** they display in sort order

### Create Target

- [ ] **Given** I am on the EEG Setup page, **when** I click "+ Add Target", **then** I see a create dialog/form
- [ ] **Given** the create form, **when** displayed, **then** I see a dropdown of capabilities from the org library
- [ ] **Given** the create form, **when** I select a capability, **then** I can enter a target description
- [ ] **Given** the create form, **when** I submit with valid data, **then** the target is created and appears in the list
- [ ] **Given** the create form, **when** I submit with empty description, **then** I see a validation error
- [ ] **Given** the create form, **when** I click Cancel, **then** the dialog closes without changes

### Edit Target

- [ ] **Given** a Capability Target in the list, **when** I click the edit button, **then** I see the edit form
- [ ] **Given** the edit form, **when** displayed, **then** the capability dropdown shows the current selection
- [ ] **Given** the edit form, **when** I can change the capability, **then** existing Critical Tasks remain attached
- [ ] **Given** the edit form, **when** I update and save, **then** changes are persisted and list updates
- [ ] **Given** the edit form, **when** I click Cancel, **then** changes are discarded

### Delete Target

- [ ] **Given** a Capability Target in the list, **when** I click the delete button, **then** I see a confirmation dialog
- [ ] **Given** the confirmation dialog, **when** the target has Critical Tasks, **then** the dialog warns about cascade delete
- [ ] **Given** the confirmation dialog, **when** I confirm, **then** the target and its tasks are deleted
- [ ] **Given** the confirmation dialog, **when** I cancel, **then** nothing is deleted

### Reorder Targets

- [ ] **Given** multiple Capability Targets, **when** I drag a target, **then** I can reorder them
- [ ] **Given** I reorder targets, **when** I drop, **then** the new sort order is saved
- [ ] **Given** drag-and-drop is not supported (mobile), **when** displayed, **then** I see up/down arrow buttons

### Inline Task Preview

- [ ] **Given** a Capability Target, **when** I expand it, **then** I see its Critical Tasks listed
- [ ] **Given** the expanded view, **when** tasks exist, **then** I see task descriptions and linked inject counts
- [ ] **Given** the expanded view, **when** no tasks exist, **then** I see a prompt to add tasks

### Offline Support

- [ ] **Given** I am offline, **when** I create a target, **then** it saves locally with pending sync indicator
- [ ] **Given** I am offline, **when** I view the list, **then** cached targets display with offline indicator
- [ ] **Given** I come back online, **when** sync completes, **then** pending indicators clear

## Wireframes

### EEG Setup Tab

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Exercise: Hurricane Response TTX                                       │
│  ══════════════════════════════════════════════════════════════════════ │
│  Details │ Objectives │ Participants │ MSEL │ [EEG Setup] │ Conduct    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Capability Targets                              [+ Add Target]         │
│                                                                         │
│  Define measurable performance thresholds for this exercise.            │
│  Each target should specify what "success" looks like.                  │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ ≡  📋 Operational Communications                    [Edit] [🗑️]  │ │
│  │     "Establish interoperable communications within 30 minutes"    │ │
│  │     3 Critical Tasks • 4 linked injects                          │ │
│  │     [▼ Expand]                                                    │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ ≡  📋 Mass Care Services                            [Edit] [🗑️]  │ │
│  │     "Open and staff shelter within 2 hours of activation"         │ │
│  │     4 Critical Tasks • 6 linked injects                          │ │
│  │     [▼ Expand]                                                    │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ ≡  📋 Emergency Operations Coordination             [Edit] [🗑️]  │ │
│  │     "Achieve full EOC staffing within 60 minutes"                 │ │
│  │     5 Critical Tasks • 8 linked injects                          │ │
│  │     [▼ Expand]                                                    │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  💡 Tip: After defining targets, add Critical Tasks to specify the     │
│     exact actions evaluators should observe.                            │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Create/Edit Dialog

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Add Capability Target                                          [X]    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Capability *                                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Operational Communications                                   ▼  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│  Select the organizational capability this target measures              │
│                                                                         │
│  Target Description *                                                   │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Establish interoperable communications within 30 minutes of     │   │
│  │ EOC activation                                                  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│  Describe the measurable performance threshold (what does success      │
│  look like?)                                                           │
│                                                                         │
│  Examples:                                                              │
│  • "Activate EOC within 60 minutes of notification"                    │
│  • "Complete damage assessment of critical facilities within 4 hours"  │
│  • "Issue public alert within 15 minutes of decision"                  │
│                                                                         │
│                                        [Cancel]  [Save Target]         │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Expanded Target with Tasks

```
┌───────────────────────────────────────────────────────────────────────┐
│ ≡  📋 Operational Communications                       [Edit] [🗑️]   │
│     "Establish interoperable communications within 30 minutes"        │
│     3 Critical Tasks • 4 linked injects                               │
│     [▲ Collapse]                                                      │
│  ─────────────────────────────────────────────────────────────────── │
│     Critical Tasks:                                    [+ Add Task]   │
│     ┌─────────────────────────────────────────────────────────────┐  │
│     │ 1. Activate emergency communication plan                    │  │
│     │    Standard: Per SOP 5.2                                    │  │
│     │    📎 2 injects                                             │  │
│     ├─────────────────────────────────────────────────────────────┤  │
│     │ 2. Establish radio net with field units                     │  │
│     │    📎 1 inject                                              │  │
│     ├─────────────────────────────────────────────────────────────┤  │
│     │ 3. Test backup communication systems                        │  │
│     │    ⚠️ No injects linked                                     │  │
│     └─────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────┘
```

## Out of Scope

- Critical Task management UI (S04)
- Linking injects to tasks (S05)
- EEG entry during conduct (S06)
- Import targets from templates (future)
- Copy targets from previous exercise (future)

## Dependencies

- S01: Capability Target Entity and API
- Capability library exists (Exercise Capabilities feature)
- Exercise detail page and tab structure

## Technical Notes

- Use existing MUI components (Accordion for expand/collapse, Dialog for create/edit)
- Implement drag-and-drop with @dnd-kit or react-beautiful-dnd
- Capability dropdown should filter to active capabilities only
- Cache capability list to avoid repeated API calls
- Use optimistic updates for better UX

## Test Scenarios

### Component Tests
- CapabilityTargetList renders correctly
- CapabilityTargetForm validation
- Empty state displays correctly
- Expand/collapse behavior

### Integration Tests
- Create target flow end-to-end
- Edit target flow end-to-end
- Delete with cascade confirmation
- Drag-and-drop reorder persists
- Offline create queues correctly

---

*Story created: 2026-02-03*
