# Story: Evaluator Coverage Metrics

**Feature**: Metrics  
**Story ID**: S08  
**Priority**: P1 (Standard)  
**Phase**: Standard Implementation

---

## User Story

**As a** Director or Lead Evaluator,  
**I want** to see metrics on Evaluator observation coverage,  
**So that** I can ensure comprehensive evaluation and identify coverage gaps.

---

## Context

HSEEP requires thorough evaluation of exercise activities. Multiple Evaluators often observe different aspects:

- Different geographic locations
- Different capabilities or functional areas
- Different phases or time periods

Understanding coverage helps ensure no critical activities went unobserved.

---

## Acceptance Criteria

- [x] **Given** I am viewing exercise metrics, **when** I view Evaluator metrics, **then** I see observation count per Evaluator
- [x] **Given** the Evaluator view, **when** displayed, **then** I see objectives covered per Evaluator
- [x] **Given** the Evaluator view, **when** displayed, **then** I see capabilities covered per Evaluator
- [x] **Given** the Evaluator view, **when** displayed, **then** I see rating distribution per Evaluator
- [x] **Given** multiple Evaluators, **when** viewing, **then** I see overall coverage matrix (Evaluator × Objective)
- [x] **Given** objectives with no observations, **when** viewing, **then** coverage gaps are highlighted
- [x] **Given** I am an Evaluator, **when** viewing metrics, **then** I can see all Evaluator metrics (observation quality is collaborative)
- [x] **Given** rating variance exists, **when** viewing, **then** I see Evaluator consistency indicator

---

## Out of Scope

- Individual Evaluator performance ratings
- Evaluator calibration tools
- Historical Evaluator performance
- Evaluator assignment recommendations

---

## Dependencies

- S03: Exercise Observation Summary Metrics
- Observation records with Evaluator reference
- Objectives and capabilities defined

---

## Open Questions

- [ ] How to calculate "consistency" between Evaluators?
- [ ] Should we flag Evaluators with unusually harsh/lenient ratings?
- [ ] Do we need time-based coverage (observations per hour)?
- [ ] How to handle observations from non-Evaluator roles?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Evaluator | HSEEP role responsible for observing and documenting performance |
| Coverage | Which objectives/capabilities have been observed |
| Rating Consistency | How similar ratings are between Evaluators observing same activities |

---

## UI/UX Notes

### Evaluator Coverage Metrics

```
┌─────────────────────────────────────────────────────────────────────────┐
│  EVALUATOR COVERAGE                                                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Summary                                                                │
│  ───────                                                                │
│                                                                         │
│  Total Evaluators: 3           Objectives Covered: 6/8 (75%)           │
│  Total Observations: 24        Capabilities Covered: 4/6 (67%)         │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  Evaluator Activity                                                     │
│  ──────────────────                                                     │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Sarah Johnson (Lead Evaluator)                                   │ │
│  ├───────────────────────────────────────────────────────────────────┤ │
│  │                                                                   │ │
│  │  Observations: 12            Coverage: 4 objectives, 3 capabilities│ │
│  │                                                                   │ │
│  │  Rating Distribution:                                             │ │
│  │  P: 4 (33%)  S: 5 (42%)  M: 2 (17%)  U: 1 (8%)                   │ │
│  │  ████████████░░░░░░░░░░░░░░░▒▒▒▒▒▒▓▓▓                             │ │
│  │                                                                   │ │
│  │  Active Phases: 1, 2, 3                                          │ │
│  │                                                                   │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Mike Williams                                                    │ │
│  ├───────────────────────────────────────────────────────────────────┤ │
│  │                                                                   │ │
│  │  Observations: 8             Coverage: 3 objectives, 2 capabilities│ │
│  │                                                                   │ │
│  │  Rating Distribution:                                             │ │
│  │  P: 3 (38%)  S: 3 (38%)  M: 1 (12%)  U: 1 (12%)                  │ │
│  │                                                                   │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  Coverage Matrix                                                        │
│  ───────────────                                                        │
│                                                                         │
│                    │ Sarah │ Mike │ Lisa │ Total                       │
│  ──────────────────┼───────┼──────┼──────┼───────                       │
│  Obj 1: Notif.     │   3   │   2  │   1  │   6   ✓                     │
│  Obj 2: Evacuation │   2   │   1  │   0  │   3   ✓                     │
│  Obj 3: Shelter    │   2   │   2  │   1  │   5   ✓                     │
│  Obj 4: External   │   1   │   0  │   0  │   1   ⚠ Low                 │
│  Obj 5: Medical    │   2   │   2  │   2  │   6   ✓                     │
│  Obj 6: Comms      │   2   │   1  │   0  │   3   ✓                     │
│  Obj 7: Docs       │   0   │   0  │   0  │   0   ✗ None                │
│  Obj 8: Debrief    │   0   │   0  │   0  │   0   ✗ None                │
│                                                                         │
│  Legend: ✓ Good  ⚠ Low Coverage  ✗ No Coverage                         │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- API: `GET /api/exercises/{id}/metrics/evaluators`
- Build coverage matrix from observations
- Consider: calculate inter-rater reliability if same inject observed by multiple evaluators
- Flag objectives with < 2 observations as "low coverage"
- Include unlinked observations in evaluator totals

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 5
