# S06: EEG Entry Form

**Feature:** Exercise Evaluation Guide (EEG)
**Priority:** P0
**Status:** Not Started
**Points:** 8

## User Story

**As an** Evaluator,
**I want** to record structured observations against specific Critical Tasks,
**So that** my assessments are organized by capability and ready for After-Action Review.

## Context

This is the primary evaluator workflow during exercise conduct. Unlike free-form observations, EEG entries are:

1. **Structured:** Tied to a specific Critical Task
2. **Rated:** Require a P/S/M/U performance rating
3. **Traceable:** Can link to the triggering inject

EEG entries capture the evaluator's assessment of how well players performed a specific task, with the context needed for AAR analysis.

## Acceptance Criteria

### Access & Entry Points

- [ ] **Given** I am on the Conduct view, **when** I look for observation options, **then** I see both "Quick Observation" and "EEG Entry" buttons
- [ ] **Given** I am an Evaluator+, **when** I click "EEG Entry", **then** the EEG entry form opens
- [ ] **Given** I am an Observer, **when** I view the conduct page, **then** I do not see the EEG Entry button
- [ ] **Given** an inject just fired, **when** I click "Assess" on that inject, **then** EEG form opens with inject pre-selected

### Form Fields

- [ ] **Given** the EEG Entry form, **when** displayed, **then** I see Capability Target selector (required)
- [ ] **Given** I select a Capability Target, **when** displayed, **then** Critical Task selector populates with that target's tasks
- [ ] **Given** I select a Critical Task, **when** displayed, **then** I see the task's Standard (if defined)
- [ ] **Given** the form, **when** displayed, **then** I see Observation text field (required, multi-line)
- [ ] **Given** the form, **when** displayed, **then** I see P/S/M/U rating selector (required)
- [ ] **Given** the form, **when** displayed, **then** I see Triggering Inject selector (optional)
- [ ] **Given** the form, **when** displayed, **then** I see the current exercise time

### Rating Selection

- [ ] **Given** the P/S/M/U selector, **when** displayed, **then** each option shows full description on hover/focus
- [ ] **Given** I select a rating, **when** selected, **then** the selection is visually distinct
- [ ] **Given** the rating descriptions, **when** displayed, **then** they match HSEEP definitions exactly

### Smart Defaults

- [ ] **Given** I opened the form from an inject's "Assess" button, **when** form loads, **then** that inject is pre-selected as trigger
- [ ] **Given** the pre-selected inject has linked Critical Tasks, **when** form loads, **then** those tasks are suggested first
- [ ] **Given** I have assigned Capability Targets, **when** form loads, **then** my assigned targets appear at top of list

### Validation

- [ ] **Given** the form, **when** I try to save without selecting a Critical Task, **then** I see validation error
- [ ] **Given** the form, **when** I try to save without observation text, **then** I see validation error
- [ ] **Given** the form, **when** I try to save without P/S/M/U rating, **then** I see validation error
- [ ] **Given** observation text, **when** shorter than 10 characters, **then** I see warning (but can save)

### Save Behavior

- [ ] **Given** I complete the form and click Save, **when** online, **then** entry is saved and form resets for next entry
- [ ] **Given** I save an entry, **when** successful, **then** I see success confirmation with option to view entry
- [ ] **Given** I am offline, **when** I save, **then** entry queues locally with pending indicator
- [ ] **Given** I click Cancel, **when** form has content, **then** I see discard confirmation
- [ ] **Given** the form, **when** I save, **then** RecordedAt captures wall clock time

### Quick Entry Mode

- [ ] **Given** I want to enter multiple assessments quickly, **when** I save, **then** form resets but stays open
- [ ] **Given** quick entry mode, **when** previous entry saved, **then** Capability Target selection persists
- [ ] **Given** I want to close the form, **when** I click the X or Done, **then** form closes

### Offline Support

- [ ] **Given** I am offline, **when** I open EEG Entry form, **then** all selectors work from cached data
- [ ] **Given** I am offline, **when** I save an entry, **then** it saves locally with sync pending indicator
- [ ] **Given** I created entries offline, **when** I come online, **then** entries sync automatically
- [ ] **Given** sync completes, **when** checking entries, **then** pending indicators are removed

## Wireframes

### EEG Entry Form (Panel)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  + EEG Entry                                     Exercise Time: 10:45   │
│                                                             [Minimize]  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Capability Target *                                                    │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Operational Communications                                   ▼  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│  "Establish interoperable communications within 30 minutes"             │
│                                                                         │
│  Critical Task *                                                        │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Activate emergency communication plan                        ▼  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│  Standard: Per SOP 5.2, using emergency notification system             │
│                                                                         │
│  Observation *                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ EOC issued activation notification at 09:15. All stakeholders   │   │
│  │ confirmed receipt within 10 minutes. Communication plan         │   │
│  │ followed correctly per SOP 5.2. Minor delay in reaching Field   │   │
│  │ Unit 3 due to radio interference.                               │   │
│  │                                                                  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Performance Rating *                                                   │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ [○ P] [● S] [○ M] [○ U]                                        │   │
│  │       ▲                                                         │   │
│  │  Performed with Some Challenges                                 │   │
│  │  Tasks achieved objective(s) with challenges that should        │   │
│  │  be addressed through corrective action                         │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Triggered by Inject (optional)                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ INJ-003: EOC Activation Notice (09:00)                       ▼  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│                                                                         │
│  [Cancel]                                    [Save & Continue]  [Save]  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Rating Selector with Descriptions

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Performance Rating *                                                   │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │                                                                   │ │
│  │  ┌─────┐  ┌─────┐  ┌─────┐  ┌─────┐                              │ │
│  │  │  P  │  │  S  │  │  M  │  │  U  │                              │ │
│  │  │     │  │ ●●● │  │     │  │     │                              │ │
│  │  └─────┘  └─────┘  └─────┘  └─────┘                              │ │
│  │                                                                   │ │
│  │  S - Performed with Some Challenges                              │ │
│  │  ─────────────────────────────────────────────────────────────── │ │
│  │  The critical task was completed successfully, achieving the     │ │
│  │  objective. However, opportunities to enhance effectiveness      │ │
│  │  and/or efficiency were identified.                              │ │
│  │                                                                   │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Inject-Triggered Entry (From "Assess" Button)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  + EEG Entry                                     Exercise Time: 10:45   │
│  Assessing: INJ-007 - Communication System Test                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  💡 This inject tests these Critical Tasks:                            │
│     • Activate emergency communication plan                             │
│     • Establish radio net with field units                             │
│                                                                         │
│  Select task to assess:                                                 │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ [●] Activate emergency communication plan                       │   │
│  │ [○] Establish radio net with field units                        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  [Rest of form...]                                                      │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## P/S/M/U Rating Definitions

Per HSEEP 2020 Doctrine:

| Rating | Name | Definition |
|--------|------|------------|
| **P** | Performed without Challenges | The targets and critical tasks associated with the capability were completed in a manner that achieved the objective(s) and did not negatively impact the performance of other activities. Performance of this activity did not contribute to additional health and/or safety risks for the public or for emergency workers, and it was conducted in accordance with applicable plans, policies, procedures, regulations, and laws. |
| **S** | Performed with Some Challenges | The targets and critical tasks associated with the capability were completed in a manner that achieved the objective(s) and did not negatively impact the performance of other activities. Performance of this activity did not contribute to additional health and/or safety risks for the public or for emergency workers, and it was conducted in accordance with applicable plans, policies, procedures, regulations, and laws. However, opportunities to enhance effectiveness and/or efficiency were identified. |
| **M** | Performed with Major Challenges | The targets and critical tasks associated with the capability were completed in a manner that achieved the objective(s); however, the completion negatively impacted the performance of other activities, contributed to additional health and/or safety risks for the public or for emergency workers, and/or was not conducted in accordance with applicable plans, policies, procedures, regulations, and laws. |
| **U** | Unable to be Performed | The targets and critical tasks associated with the capability were not performed in a manner that achieved the objective(s). |

## Out of Scope

- Voice-to-text input (future enhancement)
- Photo attachment (future enhancement)
- Evaluator assignment to specific tasks (future enhancement)
- Auto-suggest observation text (AI - future)

## Dependencies

- S01-S04: Capability Targets and Critical Tasks exist
- S05: Inject-Task linking (for smart defaults)
- Conduct view exists
- Authentication (evaluator identity)
- Offline sync service

## Technical Notes

- EEG Entry form can be a slide-out panel or modal
- Keep form visible during conduct (don't require navigation away)
- Cache Capability Targets and Critical Tasks for offline use
- ObservedAt should use exercise time context, RecordedAt uses wall clock
- Consider keyboard shortcuts for rating selection (1=P, 2=S, 3=M, 4=U)

## Test Scenarios

### Component Tests
- Form validation for all required fields
- Rating selector accessibility
- Smart defaults populate correctly
- Cancel with unsaved changes prompts

### Integration Tests
- Create EEG entry online
- Create EEG entry offline, verify sync
- Entry from inject "Assess" button pre-populates
- Multiple entries in quick succession
- Verify ObservedAt vs RecordedAt timestamps

---

*Story created: 2026-02-03*
