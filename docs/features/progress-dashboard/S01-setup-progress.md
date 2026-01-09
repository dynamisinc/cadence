# Story: S01 - View Setup Progress

## User Story

**As an** Exercise Director or Administrator,
**I want** to see a visual overview of exercise setup progress,
**So that** I know what configuration steps remain before the exercise is ready for conduct.

## Context

Exercise setup involves multiple interconnected tasks. Without a progress indicator, users may forget steps or be unsure if setup is complete. This dashboard provides at-a-glance visibility into setup status, helping users track progress and navigate to incomplete areas.

This feature was identified from EXIS analysis, where users reported difficulty knowing if exercise setup was complete.

## Acceptance Criteria

### Progress Display
- [ ] **Given** I am viewing an exercise in Draft status, **when** I view the overview, **then** I see a Setup Progress section
- [ ] **Given** I view Setup Progress, **when** I look at the header, **then** I see overall percentage complete and a progress bar
- [ ] **Given** I view Setup Progress, **when** I look at the cards, **then** I see status for each setup area

### Setup Areas
- [ ] **Given** I view progress cards, **when** I count them, **then** I see 7 areas: Basic Info, Roles, Participants, Objectives, Phases, MSEL, Time Zone
- [ ] **Given** an area is complete, **when** I view its card, **then** it shows a green checkmark (✓) and summary
- [ ] **Given** an area is incomplete, **when** I view its card, **then** it shows a warning icon (⚠) and what's missing
- [ ] **Given** an area is optional (Phases), **when** not configured, **then** it shows as "Optional" not incomplete

### Completion Criteria
- [ ] **Given** Basic Info area, **when** exercise has name, type, and date, **then** it shows complete
- [ ] **Given** Roles area, **when** at least Admin and Director roles exist, **then** it shows complete
- [ ] **Given** Participants area, **when** at least one person is assigned, **then** it shows complete
- [ ] **Given** Objectives area, **when** at least one objective is defined, **then** it shows complete
- [ ] **Given** Phases area, **when** phases exist OR user hasn't enabled phases, **then** it shows complete/optional
- [ ] **Given** MSEL area, **when** at least one inject exists, **then** it shows complete
- [ ] **Given** Time Zone area, **when** time zone is set, **then** it shows complete

### Navigation
- [ ] **Given** I click on a progress card, **when** navigation occurs, **then** I go to that configuration section
- [ ] **Given** MSEL shows incomplete, **when** I click "Go to MSEL", **then** I navigate to the MSEL authoring view
- [ ] **Given** an area shows an action link, **when** I click it, **then** I go to the relevant setup page

### Percentage Calculation
- [ ] **Given** all required areas are complete, **when** I view percentage, **then** it shows 100%
- [ ] **Given** some areas are incomplete, **when** I view percentage, **then** it reflects weighted completion
- [ ] **Given** MSEL has injects but other areas are empty, **when** I view percentage, **then** MSEL contributes most to the score

### Real-Time Updates
- [ ] **Given** I complete a setup task, **when** I return to the overview, **then** progress updates reflect the change
- [ ] **Given** I am viewing progress, **when** another user adds content, **then** progress refreshes on next load

### Exercise Status Visibility
- [ ] **Given** an exercise is in Draft status, **when** I view overview, **then** I see the full progress dashboard
- [ ] **Given** an exercise is Active or Completed, **when** I view overview, **then** progress shows as "Setup Complete" summary

## Out of Scope

- Progress enforcement (blocking conduct until 100%)
- Progress notifications/reminders
- Multi-exercise progress comparison
- Historical progress tracking

## Dependencies

- exercise-crud/S01: Create Exercise (exercise must exist)
- exercise-config/S01: Configure Roles (roles area)
- exercise-config/S02: Assign Participants (participants area)
- exercise-objectives/S01: Create Objective (objectives area)
- exercise-phases/S01: Define Phases (phases area)
- inject-crud/S01: Create Inject (MSEL area)
- exercise-config/S03: Time Zone Configuration (time zone area)

## Open Questions

- [ ] Should we block "Start Exercise" if progress is below a threshold?
- [ ] Should progress persist after exercise completes (for reference)?
- [ ] Should we weight areas differently in percentage calculation?

## Domain Terms

| Term | Definition |
|------|------------|
| Setup Progress | Completion status of exercise configuration steps |
| Progress Card | UI element showing status of a single setup area |
| Setup Area | A category of exercise configuration (Roles, MSEL, etc.) |

## UI/UX Notes

### Full Progress Dashboard

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Setup Progress                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Overall: 85% Complete                                                 │
│  ████████████████████████████████████░░░░░░                            │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                                                                 │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐             │   │
│  │  │     ✓       │  │     ✓       │  │     ✓       │             │   │
│  │  │ Basic Info  │  │   Roles     │  │ Participants│             │   │
│  │  │  Complete   │  │  5 enabled  │  │ 10 assigned │             │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘             │   │
│  │                                                                 │   │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐             │   │
│  │  │     ✓       │  │     ⚠       │  │     ✓       │             │   │
│  │  │ Objectives  │  │    MSEL     │  │  Time Zone  │             │   │
│  │  │  4 defined  │  │  0 injects  │  │    EST      │             │   │
│  │  └─────────────┘  └─────────────┘  └─────────────┘             │   │
│  │                                                                 │   │
│  │  ┌─────────────┐                                               │   │
│  │  │     ○       │  ○ = Optional                                 │   │
│  │  │   Phases    │                                               │   │
│  │  │   Optional  │                                               │   │
│  │  └─────────────┘                                               │   │
│  │                                                                 │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ⚠️ Your MSEL has no injects yet.                                      │
│                                                                         │
│  [Import from Excel]  [Create First Inject]                            │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Progress Card States

```
COMPLETE                    INCOMPLETE                  OPTIONAL
┌─────────────┐            ┌─────────────┐            ┌─────────────┐
│     ✓       │            │     ⚠       │            │     ○       │
│   green     │            │   yellow    │            │    gray     │
│ Basic Info  │            │    MSEL     │            │   Phases    │
│  Complete   │            │  0 injects  │            │  Optional   │
│             │            │  [Add →]    │            │  [Setup →]  │
└─────────────┘            └─────────────┘            └─────────────┘
```

### Compact Progress (Alternative View)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Setup Progress: 6 of 7 complete                                       │
│                                                                         │
│  ✓ Basic Info  ✓ Roles  ✓ Participants  ✓ Objectives                  │
│  ⚠ MSEL (0)   ✓ Time Zone  ○ Phases (optional)                        │
│                                                                         │
│  [Complete MSEL Setup →]                                               │
└─────────────────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Calculate progress server-side for consistency
- Cache progress calculation, invalidate on related entity changes
- Consider progress API endpoint for dashboard widgets
- Weight MSEL heavily in percentage (40%), other areas split remaining
