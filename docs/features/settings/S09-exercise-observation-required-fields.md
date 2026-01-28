# Story: Exercise Observation Required Fields

**Feature**: Settings  
**Story ID**: S09  
**Priority**: P1 (Standard)  
**Phase**: Standard Implementation

---

## User Story

**As an** Exercise Director,  
**I want** to configure which observation fields are required,  
**So that** I can ensure evaluators capture consistent, complete data while allowing flexibility for different exercise types.

---

## Context

HSEEP observations typically include multiple fields: description, P/S/M/U rating, linked inject/objective, core capability, and more. Different exercises have different documentation needs:

- **Simple TTX**: May only need description and rating
- **Full-Scale Exercise**: May require all fields for comprehensive AAR
- **Specialized Drills**: May focus on specific capabilities

Directors should configure what "complete" means for their exercise.

---

## Acceptance Criteria

- [ ] **Given** I am a Director viewing exercise settings, **when** I access observation settings, **then** I see a list of observation fields with required toggles
- [ ] **Given** "Description" is marked required, **when** an Evaluator submits an observation without description, **then** validation prevents submission
- [ ] **Given** "P/S/M/U Rating" is marked required, **when** an Evaluator submits without rating, **then** validation prevents submission
- [ ] **Given** "Linked Inject" is marked required, **when** an Evaluator submits without linking, **then** validation prevents submission
- [ ] **Given** "Core Capability" is marked required, **when** an Evaluator submits without selecting capability, **then** validation prevents submission
- [ ] **Given** a field is optional, **when** an Evaluator submits without that field, **then** the observation saves successfully
- [ ] **Given** I change required fields, **when** the change saves, **then** it applies to new observations (not retroactive)
- [ ] **Given** defaults, **when** a new exercise is created, **then** only Description is required (minimal barrier)

---

## Out of Scope

- Custom observation fields (predefined set only)
- Conditional requirements (e.g., rating required only if improvement noted)
- Field-level validation rules beyond required/optional

---

## Dependencies

- Observation entry form (Phase E)
- Exercise settings panel

---

## Open Questions

- [ ] Should we support "required for submission but can save as draft"?
- [ ] What's the full list of observation fields?
- [ ] Should there be templates (e.g., "Full HSEEP Mode" with all fields required)?
- [ ] Can requirements change mid-exercise?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Observation | Evaluator-recorded note about player/organization performance |
| P/S/M/U | Performance rating scale: Performed, Satisfactory, Marginal, Unsatisfactory |
| Core Capability | FEMA-defined capability area being evaluated |

---

## UI/UX Notes

### Exercise Settings - Observation Fields

```
┌─────────────────────────────────────────────────────────────┐
│  Exercise Settings                          [Director Only] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Observation Requirements                                   │
│  ─────────────────────────────────────────────              │
│                                                             │
│  Evaluators must complete these fields:                     │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  [✓]  Description / Observation text                │   │
│  │  [ ]  P/S/M/U Performance rating                    │   │
│  │  [ ]  Linked Inject                                 │   │
│  │  [ ]  Linked Objective                              │   │
│  │  [ ]  Core Capability                               │   │
│  │  [ ]  Corrective action recommendation              │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ⓘ Only Description is required by default. Add more       │
│     requirements for comprehensive exercise documentation. │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Observation Form with Required Indicators

```
┌─────────────────────────────────────────────────────────────┐
│  New Observation                                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Description *                                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Players delayed evacuation by 5 minutes while       │   │
│  │ coordinating with off-site personnel...             │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Rating *                                                   │
│  ○ Performed  ○ Satisfactory  ● Marginal  ○ Unsatisfactory │
│                                                             │
│  Linked Inject                                  (optional)  │
│  [ Select inject... ▼ ]                                    │
│                                                             │
│                            [Cancel]   [Save Observation]   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- Store required fields as JSON or separate booleans on Exercise:
  - `RequireObservationRating`
  - `RequireObservationInject`
  - `RequireObservationObjective`
  - `RequireObservationCapability`
  - `RequireObservationRecommendation`
- Description is always captured (can't be made optional)
- Frontend validation with clear error messages
- API validation as backup

---

## Estimation

**T-Shirt Size**: S  
**Story Points**: 3
