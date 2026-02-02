# Feature: Exercise Objectives Management

**Phase:** Standard
**Status:** Not Started

## Overview

Exercise planners can define HSEEP-compliant objectives, link them to injects, and track objective coverage during exercise conduct to support evaluation and after-action reporting.

## Problem Statement

HSEEP-compliant exercises must be built around core objectives that define what the exercise aims to achieve or test. Without a structured objective management system, planners struggle to ensure all objectives are adequately exercised, and evaluators lack context for performance assessment. This feature provides the framework for objective-based exercise design and evaluation.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-create-objective.md) | Create Objective | P1 | 📋 Ready |
| [S02](./S02-edit-objective.md) | Edit Objective | P1 | 📋 Ready |
| [S03](./S03-link-objective-inject.md) | Link Objective to Inject | P1 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|-------------|
| Administrator | Full CRUD access to objectives |
| Exercise Director | Creates, edits, and manages objectives for their exercises |
| Controller | Views objectives linked to their assigned injects |
| Evaluator | Views objectives for evaluation focus areas |
| Observer | Views objectives (read-only) |

## Key Concepts

| Term | Definition |
|------|------------|
| Exercise Objective | HSEEP-defined goal that the exercise aims to achieve or test |
| Core Capability | FEMA-defined capability area (e.g., Mass Care, Emergency Operations Coordination) |
| Objective Linkage | Association between an objective and one or more injects |
| Objective Coverage | Tracking which objectives are addressed by the MSEL |
| SMART Objectives | Specific, Measurable, Achievable, Relevant, Time-bound objective criteria |

## Dependencies

- exercise-crud/S01 - Create Exercise (objectives belong to exercises)
- inject-crud/S01 - Create Inject (injects can be linked to objectives)
- Evaluation module (future) - Objectives guide evaluation criteria

## Acceptance Criteria (Feature-Level)

- [ ] Users can create exercise objectives with name and description
- [ ] Users can edit and delete objectives before conduct begins
- [ ] Users can link multiple objectives to a single inject (many-to-many)
- [ ] Users can link a single objective to multiple injects (many-to-many)
- [ ] Objectives are visible during inject delivery for Controller context
- [ ] Objectives can be filtered/grouped in the MSEL view
- [ ] Objective coverage is visible (which objectives have linked injects)

## Notes

- Cadence focuses on objective tracking during exercise conduct, not the full objective development lifecycle
- Per HSEEP doctrine, objectives should be SMART: Specific, Measurable, Achievable, Relevant, Time-bound
- Objectives imported from Excel should map to this structure
- Consider future integration with FEMA Core Capabilities list
- Objective tracking feeds into after-action report generation (Advanced phase)

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
