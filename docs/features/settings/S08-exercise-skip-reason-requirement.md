# Story: Exercise Skip Reason Requirement

**Feature**: Settings  
**Story ID**: S08  
**Priority**: P1 (Standard)  
**Phase**: Standard Implementation

---

## User Story

**As an** Exercise Director,  
**I want** to require Controllers to provide a reason when skipping injects,  
**So that** the after-action review has context for why certain injects were not delivered.

---

## Context

Skipped injects are normal in exercise conduct—scenarios may evolve, players may need more time, or external factors may intervene. However, for after-action review (AAR), evaluators need to understand WHY injects were skipped:

- **Timing**: Exercise pace required skipping ahead
- **Scenario divergence**: Player actions made inject irrelevant
- **Resource constraint**: Insufficient staff to deliver
- **Technical issue**: Communication failure prevented delivery
- **Exercise design**: Discovered inject was unnecessary

Requiring a reason ensures AAR has complete information.

---

## Acceptance Criteria

- [ ] **Given** I am a Director viewing exercise settings, **when** I access skip settings, **then** I see "Require Skip Reason" toggle
- [ ] **Given** "Require Skip Reason" is enabled, **when** a Controller clicks Skip, **then** a dialog prompts for reason before skipping
- [ ] **Given** the skip reason dialog, **when** I leave reason blank and click Skip, **then** the action is blocked with validation error
- [ ] **Given** the skip reason dialog, **when** I enter a reason and click Skip, **then** the inject is skipped with reason recorded
- [ ] **Given** "Require Skip Reason" is disabled, **when** a Controller clicks Skip, **then** the inject is skipped without reason prompt (optional field)
- [ ] **Given** an inject was skipped with reason, **when** viewing inject detail, **then** the skip reason is displayed
- [ ] **Given** I am viewing skipped injects in AAR, **when** I look at the inject, **then** the skip reason is visible
- [ ] **Given** defaults, **when** a new exercise is created, **then** "Require Skip Reason" is enabled (conservative default)

---

## Out of Scope

- Predefined skip reason categories/dropdown
- Skip reason approval workflow
- Retroactive reason entry for already-skipped injects

---

## Dependencies

- Inject skip functionality (Phase D)
- Exercise settings panel

---

## Open Questions

- [ ] Should we provide suggested reasons (dropdown + custom option)?
- [ ] Minimum character count for reason?
- [ ] Can reasons be edited after the fact?
- [ ] Should skipped injects be reportable in metrics by reason?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Skip | Intentionally not delivering an inject during exercise |
| Skip Reason | Explanation for why an inject was not delivered |
| AAR | After-Action Review - post-exercise analysis process |

---

## UI/UX Notes

### Exercise Settings - Skip Reason

```
┌─────────────────────────────────────────────────────────────┐
│  Exercise Settings                          [Director Only] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Inject Skipping                                            │
│  ─────────────────────────────────────────────              │
│                                                             │
│  Require Skip Reason                                        │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                                          [  ON  ]   │   │
│  │  Controllers must provide an explanation when       │   │
│  │  skipping an inject. Useful for after-action review.│   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Skip Reason Dialog

```
┌─────────────────────────────────────────────────────────────┐
│  Skip Inject                                           [X]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  You are about to skip:                                     │
│                                                             │
│  INJ-015: Building evacuation notification                  │
│  Scheduled: 14:30                                           │
│                                                             │
│  Reason for skipping: *                                     │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                                                     │   │
│  │ Players already evacuated building during previous  │   │
│  │ inject response, making this inject redundant.     │   │
│  │                                                     │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│                            [Cancel]   [⏭ Skip Inject]      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Skipped Inject Display

```
┌─────────────────────────────────────────────────────────────┐
│  INJ-015 | Building evacuation notification     │ SKIPPED  │
├─────────────────────────────────────────────────────────────┤
│  Scheduled: 14:30                                           │
│  Skipped at: 14:28 by John Smith                           │
│                                                             │
│  Reason: Players already evacuated building during         │
│  previous inject response, making this inject redundant.   │
└─────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- Add `SkipReason` nullable string field to Inject entity
- Add `RequireSkipReason` boolean to Exercise entity
- Skip reason stored alongside status change timestamp
- Consider: max length for skip reason (500 characters?)
- Include skip reason in export/reports

---

## Estimation

**T-Shirt Size**: S  
**Story Points**: 3
