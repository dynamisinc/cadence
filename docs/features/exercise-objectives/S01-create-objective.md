# Story: S01 - Create Exercise Objective

## User Story

**As an** Administrator or Exercise Director,
**I want** to create objectives for my exercise,
**So that** I can define what capabilities will be tested and provide context for inject delivery.

## Context

Exercise objectives define the purpose and focus areas for an exercise. In HSEEP methodology, objectives guide exercise design, inject development, and evaluation criteria. While Cadence doesn't cover full objective development (typically done during planning), it allows objectives to be defined or imported so they can be linked to injects and displayed during conduct.

## Acceptance Criteria

- [ ] **Given** I am on the Exercise Setup screen, **when** I navigate to Objectives, **then** I see a list of current objectives and an "Add Objective" button
- [ ] **Given** I click "Add Objective", **when** the form appears, **then** I see fields for Objective Number, Name, and Description
- [ ] **Given** I am creating an objective, **when** I enter only the Name (minimum 3 characters), **then** I can save successfully (Objective Number auto-assigned, Description optional)
- [ ] **Given** I save an objective without specifying a number, **when** it is created, **then** it receives the next sequential number (1, 2, 3...)
- [ ] **Given** I want to specify an Objective Number, **when** I enter it manually, **then** it accepts alphanumeric values (e.g., "1", "1.1", "A", "EOC-1")
- [ ] **Given** I enter a duplicate Objective Number, **when** I try to save, **then** I see a validation error
- [ ] **Given** I am entering a Description, **when** I type, **then** I can enter up to 1000 characters with a character counter
- [ ] **Given** I save a valid objective, **when** the save completes, **then** I see a success message and the objective appears in the list
- [ ] **Given** I am a Controller, Evaluator, or Observer, **when** I view the Objectives section, **then** I do not see the "Add Objective" button

## Out of Scope

- Linking objectives to FEMA Core Capabilities (future enhancement)
- Objective templates or library
- Importing objectives from external systems (handled by Excel import)
- Objective evaluation criteria or metrics
- Nested/hierarchical objectives

## Dependencies

- exercise-crud/S01: Create Exercise (objectives belong to exercises)
- excel-import/S01: Upload Excel (objectives may come from import)

## Open Questions

- [ ] Should there be a maximum number of objectives per exercise?
- [ ] Should objectives be categorized or tagged?
- [ ] Should we support rich text formatting in descriptions?

## Domain Terms

| Term | Definition |
|------|------------|
| Objective | A specific capability or outcome the exercise aims to test or demonstrate |
| Objective Number | Identifier for the objective (can be numeric or alphanumeric) |
| Core Capability | FEMA-defined national preparedness capabilities (future integration) |

## UI/UX Notes

### Create Objective Form

```
┌─────────────────────────────────────────────────────────────┐
│  New Objective                                          ✕   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Objective Number (optional)                                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 1                                                   │   │
│  └─────────────────────────────────────────────────────┘   │
│  Auto-assigned if blank                                     │
│                                                             │
│  Name *                                                     │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Demonstrate EOC activation procedures               │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Description                                                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Test the ability of county emergency management    │   │
│  │ to activate the Emergency Operations Center within │   │
│  │ 2 hours of incident notification, including        │   │
│  │ staffing, communications setup, and initial        │   │
│  │ situational awareness briefing.                    │   │
│  │                                                     │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                245/1000     │
│                                                             │
│                    [Cancel]  [Create Objective]             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Objective Number should support sorting (numeric then alpha)
- Consider indexing objectives for quick lookup during inject linking
- Objectives should be included in exercise export
