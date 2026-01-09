# Story: S02 - Duplicate MSEL / Exercise

## User Story

**As an** Administrator or Exercise Director,
**I want** to duplicate an exercise including its MSEL,
**So that** I can reuse inject content for recurring or similar exercises.

## Context

Many organizations run similar exercises periodically (annual hurricane exercises, quarterly tabletops, etc.). Rather than rebuilding the MSEL from scratch, duplicating an existing exercise provides a starting point that can be modified. This is one of the most requested features based on SME feedback.

In MVP, duplication creates a new exercise with a copy of all injects. The new exercise is independent - changes to one don't affect the other.

## Acceptance Criteria

- [ ] **Given** I am viewing an exercise, **when** I open the actions menu (•••), **then** I see a "Duplicate Exercise" option
- [ ] **Given** I click "Duplicate Exercise", **when** the dialog appears, **then** I see a form to enter the new exercise name
- [ ] **Given** I am duplicating, **when** I view the form, **then** the default name is "[Original Name] (Copy)"
- [ ] **Given** I enter a new exercise name, **when** I click "Duplicate", **then** the system creates a new exercise with:
  - New exercise with entered name
  - Status set to "Draft"
  - All injects copied with new IDs
  - All objectives copied
  - All phases copied
  - Inject-objective links preserved
  - Inject-phase assignments preserved
- [ ] **Given** duplication is in progress, **when** I wait, **then** I see a progress indicator (for large MSELs)
- [ ] **Given** duplication completes, **when** I see the success message, **then** I have options to "View New Exercise" or "Stay Here"
- [ ] **Given** I view the new exercise, **when** I check the injects, **then** all inject Scheduled Times are preserved but dates may need updating
- [ ] **Given** the duplicated exercise, **when** I modify its injects, **then** the original exercise is not affected
- [ ] **Given** I am duplicating an exercise with 100+ injects, **when** I start duplication, **then** it completes within 30 seconds
- [ ] **Given** I am a Controller, Evaluator, or Observer, **when** I view the actions menu, **then** I do not see "Duplicate Exercise"

## Out of Scope

- Selective duplication (choosing which injects to copy)
- Duplicating to a different organization
- Template library of reusable MSELs
- Scheduled/automatic duplication
- Duplication with date shifting (auto-update scheduled times)

## Dependencies

- exercise-crud/S01: Create Exercise (duplication creates new exercise)
- exercise-objectives/S01: Create Objective (objectives are duplicated)
- exercise-phases/S01: Define Phases (phases are duplicated)
- inject-crud/S01: Create Inject (injects are duplicated)

## Open Questions

- [ ] Should conduct history be excluded from duplication? (Yes - new exercise starts fresh)
- [ ] Should participants be copied to the new exercise?
- [ ] Should there be an option to shift all scheduled dates?
- [ ] Should the original exercise link to duplicates for reference?

## Domain Terms

| Term | Definition |
|------|------------|
| Duplication | Creating a complete copy of an exercise and its MSEL |
| Source Exercise | The original exercise being duplicated |
| Target Exercise | The new exercise created by duplication |

## UI/UX Notes

### Duplicate Exercise Dialog

```
┌─────────────────────────────────────────────────────────────┐
│  Duplicate Exercise                                     ✕   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Source: Hurricane Response 2025                            │
│  • 43 injects                                              │
│  • 4 objectives                                            │
│  • 3 phases                                                │
│                                                             │
│  New Exercise Name *                                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Hurricane Response 2026                             │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Options:                                                   │
│  ☑ Copy objectives                                         │
│  ☑ Copy phases                                             │
│  ☐ Copy participant assignments                            │
│                                                             │
│  ℹ️ The new exercise will be created in Draft status.      │
│     Scheduled times will be preserved - update dates       │
│     as needed for your new exercise.                       │
│                                                             │
│                    [Cancel]  [Duplicate Exercise]           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Duplication Progress

```
┌─────────────────────────────────────────────────────────────┐
│  Duplicating Exercise...                                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ████████████████████░░░░░░░░░░ 65%                        │
│                                                             │
│  Creating exercise...     ✓                                │
│  Copying objectives...    ✓                                │
│  Copying phases...        ✓                                │
│  Copying injects...       28 of 43                         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Duplication Complete

```
┌─────────────────────────────────────────────────────────────┐
│  ✓ Exercise Duplicated                                  ✕   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  "Hurricane Response 2026" has been created.                │
│                                                             │
│  • 43 injects copied                                       │
│  • 4 objectives copied                                     │
│  • 3 phases copied                                         │
│                                                             │
│         [Stay Here]  [View New Exercise]                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Use database transaction for atomic duplication
- Generate new GUIDs for all duplicated entities
- Consider background job for large MSELs (100+ injects)
- Log duplication for audit trail (source → target)
- Preserve relative inject ordering (SortOrder)
