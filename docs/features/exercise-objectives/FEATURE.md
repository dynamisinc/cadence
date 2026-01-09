# Feature: Exercise Objectives Management

**Parent Epic:** Exercise Setup (E3)

## Description

HSEEP-compliant exercises are built around core objectives that define what the exercise aims to achieve or test. This feature allows exercise planners to define objectives, link them to injects, and track which objectives are being exercised during conduct. Objectives provide the framework for evaluation and after-action reporting.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-create-objective.md) | Create Objective | P1 | 📋 Ready |
| [S02](./S02-edit-objective.md) | Edit Objective | P1 | 📋 Ready |
| [S03](./S03-link-objective-inject.md) | Link Objective to Inject | P1 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|------------|
| Administrator | Full CRUD access to objectives |
| Exercise Director | Creates, edits, and manages objectives for their exercises |
| Controller | Views objectives linked to their assigned injects |
| Evaluator | Views objectives for evaluation focus areas |
| Observer | Views objectives (read-only) |

## HSEEP Context

Per HSEEP doctrine, exercise objectives should be:
- **Linked to Core Capabilities**: Align with national preparedness goals
- **Specific**: Clearly define what will be demonstrated
- **Measurable**: Allow for evaluation of performance
- **Achievable**: Realistic within exercise scope
- **Relevant**: Address identified gaps or priorities
- **Time-bound**: Achievable within exercise timeframe

Cadence focuses on objective *tracking* during exercise conduct rather than the full objective development lifecycle covered by planning tools like EXIS.

## Dependencies

- exercise-crud/S01: Create Exercise (objectives belong to exercises)
- inject-crud/S01: Create Inject (injects can be linked to objectives)
- Evaluation module (future: objectives guide evaluation criteria)

## Acceptance Criteria (Feature-Level)

- [ ] Users can create exercise objectives with name and description
- [ ] Users can edit and delete objectives before conduct begins
- [ ] Users can link multiple objectives to a single inject
- [ ] Users can link a single objective to multiple injects
- [ ] Objectives are visible during inject delivery for Controller context
- [ ] Objectives can be filtered/grouped in the MSEL view

## Wireframes/Mockups

### Objectives List View

```
┌─────────────────────────────────────────────────────────────────────┐
│  Exercise Objectives                               [+ New Objective] │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  #  │ Objective                              │ Linked Injects │     │
│  ───┼────────────────────────────────────────┼────────────────┼──── │
│  1  │ Demonstrate EOC activation procedures  │ 5 injects      │ ••• │
│  2  │ Test multi-agency communication        │ 12 injects     │ ••• │
│  3  │ Evaluate resource request process      │ 8 injects      │ ••• │
│  4  │ Assess public information coordination │ 3 injects      │ ••• │
│                                                                     │
│  💡 Link objectives to injects to track which capabilities         │
│     are being exercised.                                            │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Notes

- Objectives imported from Excel should map to this structure
- Consider future integration with FEMA Core Capabilities list
- Objective tracking feeds into after-action report generation (Advanced phase)
