# Feature: Exercise Phase Management

**Parent Epic:** Exercise Setup (E3)

## Description

Exercises are typically organized into phases that represent distinct time periods or operational stages. Phases help structure the MSEL, guide exercise flow, and provide natural breakpoints for Controller coordination. This feature allows planners to define phases and assign injects to them.

Common phase patterns include:
- **Time-based**: Phase 1 (Morning), Phase 2 (Afternoon)
- **Operational**: Initial Response, Sustained Operations, Recovery
- **Scenario-based**: Pre-Event, Event Onset, Escalation, Stabilization

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-define-phases.md) | Define Exercise Phases | P1 | 📋 Ready |
| [S02](./S02-assign-inject-phase.md) | Assign Inject to Phase | P1 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|------------|
| Administrator | Full CRUD access to phases |
| Exercise Director | Creates, edits, and manages phases |
| Controller | Views phases, uses for inject organization |
| Evaluator | Views phases for evaluation context |
| Observer | Views phases (read-only) |

## Dependencies

- exercise-crud/S01: Create Exercise (phases belong to exercises)
- inject-crud/S01: Create Inject (injects can be assigned to phases)
- inject-organization/S02: Group Injects (grouping by phase)

## Acceptance Criteria (Feature-Level)

- [ ] Users can create named phases for an exercise
- [ ] Users can edit and delete phases
- [ ] Users can assign injects to phases
- [ ] MSEL can be grouped by phase
- [ ] Phases display in exercise timeline views

## Wireframes/Mockups

### Phase List View

```
┌─────────────────────────────────────────────────────────────────────┐
│  Exercise Phases                                      [+ New Phase]  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ≡  │ Phase                    │ Injects │ Scenario Time │         │
│  ───┼──────────────────────────┼─────────┼───────────────┼──────── │
│  ≡  │ 1. Initial Response      │ 15      │ Day 1 08:00   │ ••• ▲▼ │
│  ≡  │ 2. Sustained Operations  │ 22      │ Day 1 12:00   │ ••• ▲▼ │
│  ≡  │ 3. Recovery              │ 10      │ Day 2 08:00   │ ••• ▲▼ │
│                                                                     │
│  ≡ = Drag to reorder                                               │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### MSEL Grouped by Phase

```
┌─────────────────────────────────────────────────────────────────────┐
│  ▼ Phase 1: Initial Response (15 injects)                          │
│  ├─ INJ-001 │ 09:00 │ Hurricane warning issued       │ Pending    │
│  ├─ INJ-002 │ 09:15 │ EOC activation ordered         │ Pending    │
│  └─ ...                                                             │
│                                                                     │
│  ▼ Phase 2: Sustained Operations (22 injects)                      │
│  ├─ INJ-016 │ 12:00 │ Shelter capacity exceeded      │ Pending    │
│  └─ ...                                                             │
│                                                                     │
│  ► Phase 3: Recovery (10 injects)                      [collapsed] │
└─────────────────────────────────────────────────────────────────────┘
```

## Notes

- Phases are optional; exercises can run without defined phases
- Phase order can be changed via drag-and-drop
- Consider visual phase indicators during conduct (e.g., sidebar showing current phase)
- Phases from Excel import should map to this structure
