# Feature: Exercise CRUD

**Parent Epic:** Exercise Setup (E3)

## Description

Core exercise lifecycle management allowing users to create, view, edit, and archive exercises. This feature provides the foundation for all exercise-related functionality in Cadence.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-create-exercise.md) | Create Exercise | P0 | 📋 Ready |
| [S02](./S02-edit-exercise.md) | Edit Exercise | P0 | 📋 Ready |
| [S03](./S03-view-exercise-list.md) | View Exercise List | P0 | 📋 Ready |
| [S04](./S04-archive-exercise.md) | Archive Exercise | P1 | 📋 Ready |
| [S05](./S05-practice-mode.md) | Practice Mode | P1 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|------------|
| Administrator | Full CRUD access, manages all exercises |
| Exercise Director | Creates/edits own exercises, archives when complete |
| Controller | Views exercises, no create/edit access |
| Evaluator | Views exercises, no create/edit access |
| Observer | Views assigned exercises only |

## Dependencies

- User authentication and authorization
- Organization management (exercises belong to organizations)
- Core entity definitions (Exercise, MSEL)

## Acceptance Criteria (Feature-Level)

- [ ] Users can create new exercises with required fields
- [ ] Users can view a list of exercises they have access to
- [ ] Users can edit exercise details before conduct begins
- [ ] Users can archive completed exercises
- [ ] Practice exercises are clearly distinguished from production

## Wireframes/Mockups

### Exercise List View
```
┌─────────────────────────────────────────────────────────────────────┐
│  Exercises                                      [+ New Exercise]     │
├─────────────────────────────────────────────────────────────────────┤
│  [Filter ▼]  [Search exercises...]                                  │
├─────────────────────────────────────────────────────────────────────┤
│  Name                    │ Type │ Date       │ Status  │ Actions   │
│  ─────────────────────────────────────────────────────────────────  │
│  Hurricane Response 2025 │ TTX  │ Jun 15     │ 🟢 Active │ ••• │
│  Cyber Incident FSE 🔧   │ FSE  │ Jul 22     │ 🟡 Draft  │ ••• │
│  Mass Casualty FE        │ FE   │ Aug 10     │ 🟡 Draft  │ ••• │
│  ─────────────────────────────────────────────────────────────────  │
│                                           < 1 2 3 >                 │
└─────────────────────────────────────────────────────────────────────┘

🔧 = Practice Mode indicator
```

## Notes

- Exercise creation automatically creates a draft MSEL
- Archiving is soft-delete; exercises can be viewed but not modified
- Practice mode allows testing without affecting production metrics
