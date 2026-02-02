# Feature: MSEL Version Management

**Phase:** MVP
**Status:** Ready

## Overview

The Master Scenario Events List (MSEL) is the collection of injects for an exercise. As exercises evolve during planning, multiple versions of the MSEL may be created. This feature allows exercise planners to manage MSEL versions, select the active version for conduct, and duplicate MSELs for reuse.

In Cadence's MVP, version management is simplified: each exercise has one MSEL, but the MSEL can be duplicated to create new exercises or archived for historical reference.

## Problem Statement

Exercise planners need to reuse inject content across recurring or similar exercises without rebuilding the MSEL from scratch. Organizations run periodic exercises (annual hurricane drills, quarterly tabletops) that benefit from duplicating and modifying existing MSELs rather than starting from zero each time.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-select-msel-version.md) | Select MSEL Version | P2 | 📋 Ready |
| [S02](./S02-duplicate-msel.md) | Duplicate MSEL | P1 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|-------------|
| Administrator | Full access to MSEL management |
| Exercise Director | Manages MSEL versions for their exercises |
| Controller | Views active MSEL during conduct |
| Evaluator | Views active MSEL |
| Observer | Views active MSEL (read-only) |

## Key Concepts

| Term | Definition |
|------|------------|
| MSEL | Master Scenario Events List - the collection of injects for an exercise |
| Active MSEL | The MSEL used during exercise conduct (in MVP, the only MSEL for each exercise) |
| Duplication | Creating a complete copy of an exercise and its MSEL for reuse |
| Source Exercise | The original exercise being duplicated |
| Target Exercise | The new exercise created by duplication |

## MVP Simplification

For MVP, Cadence uses a simplified MSEL model:
- One MSEL per exercise (created automatically with exercise)
- No formal versioning (version tracking deferred to Standard phase)
- Duplication creates a new exercise with copied MSEL
- Historical preservation through exercise archival

Future versions may add:
- Multiple MSEL versions per exercise
- Version comparison (diff view)
- Version rollback
- Merge capabilities

## Dependencies

- exercise-crud/S01: Create Exercise (MSEL created with exercise)
- inject-crud/S01: Create Inject (injects belong to MSEL)
- excel-import/S01: Upload Excel (import creates/updates MSEL)

## Acceptance Criteria (Feature-Level)

- [ ] Each exercise has exactly one MSEL
- [ ] Users can duplicate an exercise including its MSEL
- [ ] MSEL state is preserved when exercise is archived
- [ ] Conduct uses the exercise's single MSEL

## Notes

### Wireframes/Mockups

#### Exercise Actions Menu

```
┌─────────────────────────────────────────────────────────────────────┐
│  Hurricane Response 2025                                           │
│  Status: Draft  │  Type: TTX  │  43 Injects                        │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  [Edit Exercise]  [View MSEL]  [•••]                               │
│                                                                     │
│  ••• Menu:                                                         │
│  ┌─────────────────────┐                                           │
│  │ Duplicate Exercise  │  ← Creates new exercise with MSEL copy   │
│  │ Export to Excel     │                                           │
│  │ Archive Exercise    │                                           │
│  └─────────────────────┘                                           │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Additional Context

- MSEL duplication is a common workflow for recurring exercises
- Consider adding "duplicate as template" in future versions
- Archive preserves MSEL for after-action review
