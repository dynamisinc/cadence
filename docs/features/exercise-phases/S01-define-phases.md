# Story: S01 - Define Exercise Phases

## User Story

**As an** Administrator or Exercise Director,
**I want** to define phases for my exercise,
**So that** I can organize injects into logical groups that reflect the exercise flow.

## Context

Phases provide structure for exercises by grouping injects into meaningful segments. They help Controllers understand exercise progression and can represent time periods, operational stages, or scenario developments. Phases are optional - simple exercises may not need them.

## Acceptance Criteria

- [ ] **Given** I am on Exercise Setup, **when** I navigate to Phases, **then** I see a list of current phases and an "Add Phase" button
- [ ] **Given** I click "Add Phase", **when** the form appears, **then** I see fields for Phase Number, Name, and optional Description
- [ ] **Given** I am creating a phase, **when** I enter only a Name (minimum 2 characters), **then** I can save (Number auto-assigned)
- [ ] **Given** I save a phase without specifying a number, **when** it is created, **then** it receives the next sequential number
- [ ] **Given** I want to specify a Phase Number, **when** I enter it manually, **then** it accepts integers 1-99
- [ ] **Given** I enter a duplicate Phase Number, **when** I try to save, **then** I see a validation error
- [ ] **Given** I am entering a Description, **when** I type, **then** I can enter up to 500 characters
- [ ] **Given** I save a valid phase, **when** the save completes, **then** the phase appears in the list ordered by Phase Number
- [ ] **Given** I have multiple phases, **when** I drag a phase row, **then** I can reorder phases (numbers update automatically)
- [ ] **Given** I click on an existing phase, **when** the form opens, **then** I can edit Name, Description (Number is read-only after creation)
- [ ] **Given** I delete a phase, **when** injects are assigned to it, **then** I see a warning showing the count of affected injects
- [ ] **Given** I confirm deleting a phase with injects, **when** deletion completes, **then** those injects' phase assignment becomes "Unassigned"
- [ ] **Given** I am a Controller, Evaluator, or Observer, **when** I view Phases, **then** I see read-only mode

## Out of Scope

- Phase time boundaries (automatic inject assignment based on time)
- Phase status during conduct (tracking which phase is "active")
- Nested/hierarchical phases
- Phase templates or presets

## Dependencies

- exercise-crud/S01: Create Exercise (phases belong to exercises)
- exercise-phases/S02: Assign Inject to Phase (assigns injects to phases)

## Open Questions

- [ ] Should phases have start/end times that auto-assign injects?
- [ ] Should there be a visual phase indicator during conduct?
- [ ] Maximum number of phases per exercise?

## Domain Terms

| Term | Definition |
|------|------------|
| Phase | A named segment of an exercise representing a time period or operational stage |
| Phase Number | Sequential identifier for ordering phases |
| Unassigned | An inject not belonging to any phase |

## UI/UX Notes

### Phase List with Drag-Drop

```
┌─────────────────────────────────────────────────────────────┐
│  Exercise Phases                              [+ Add Phase]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ≡  1. Initial Response                      [Edit] [🗑️]   │
│      Covers first 4 hours after incident notification       │
│      12 injects assigned                                    │
│                                                             │
│  ≡  2. Sustained Operations                  [Edit] [🗑️]   │
│      Hours 4-12, full EOC operations                        │
│      18 injects assigned                                    │
│                                                             │
│  ≡  3. Recovery                              [Edit] [🗑️]   │
│      Transition to recovery operations                      │
│      8 injects assigned                                     │
│                                                             │
│  ───────────────────────────────────────────────────────── │
│  ⚠️ 5 injects are not assigned to any phase                │
│                                                             │
└─────────────────────────────────────────────────────────────┘

≡ = Drag handle for reordering
```

### Add/Edit Phase Form

```
┌─────────────────────────────────────────────────────────────┐
│  New Phase                                              ✕   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Phase Number                                               │
│  ┌───────┐                                                 │
│  │ 1     │  Auto-assigned if blank                         │
│  └───────┘                                                 │
│                                                             │
│  Name *                                                     │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Initial Response                                    │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Description                                                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Covers the first 4 hours after incident            │   │
│  │ notification through initial EOC activation.       │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                  89/500     │
│                                                             │
│                       [Cancel]  [Save Phase]                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Store phase order as SortOrder column for efficient reordering
- When a phase is deleted, update injects' PhaseId to NULL
- Consider cascade options for database constraints
