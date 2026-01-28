# Story: Exercise Observation Summary Metrics

**Feature**: Metrics  
**Story ID**: S03  
**Priority**: P0 (MVP)  
**Phase**: MVP

---

## User Story

**As a** Director conducting after-action review,  
**I want** to see comprehensive observation metrics,  
**So that** I can understand evaluation coverage and performance ratings for the exercise.

---

## Context

HSEEP requires systematic evaluation of exercise performance. Observations capture evaluator assessments using the P/S/M/U (Performed, Satisfactory, Marginal, Unsatisfactory) rating scale. AAR needs to understand:

- How many observations were captured?
- What was the distribution of ratings?
- Were all objectives observed?
- Which capabilities showed strengths or weaknesses?

This metrics view summarizes observation data for AAR discussion.

---

## Acceptance Criteria

- [ ] **Given** I am viewing exercise metrics, **when** I open the Observations tab, **then** I see comprehensive observation statistics
- [ ] **Given** the observation summary, **when** displayed, **then** I see total observation count
- [ ] **Given** the observation summary, **when** displayed, **then** I see P/S/M/U rating distribution (counts and percentages)
- [ ] **Given** the observation summary, **when** displayed, **then** I see observations by evaluator
- [ ] **Given** the observation summary, **when** displayed, **then** I see coverage rate (% of objectives with at least one observation)
- [ ] **Given** the observation summary, **when** objectives exist, **then** I see which objectives have no observations (gaps)
- [ ] **Given** the observation summary, **when** injects are linked, **then** I see observations by inject/phase
- [ ] **Given** the metrics page, **when** I click on a rating count, **then** I can drill down to observations with that rating
- [ ] **Given** unlinked observations exist, **when** viewing summary, **then** I see count of observations not linked to inject/objective

---

## Out of Scope

- Observation content/text analysis
- Trend analysis across exercises
- Evaluator performance comparison (may raise sensitivity)
- Core capability breakdown (separate P1 story)

---

## Dependencies

- Observation capture (Phase E)
- P/S/M/U rating system
- Objectives linked to exercise
- Evaluator role assignment

---

## Open Questions

- [ ] Should we show "time to first observation" as a metric?
- [ ] How do we handle exercises with no objectives defined?
- [ ] Should observation density (obs per inject) be shown?
- [ ] Do we need observation timestamps in the summary?

---

## Domain Terms

| Term | Definition |
|------|------------|
| P/S/M/U | HSEEP rating scale: Performed (exceeded), Satisfactory (met), Marginal (partially met), Unsatisfactory (not met) |
| Coverage Rate | Percentage of defined objectives that have at least one observation |
| Unlinked Observation | Observation not associated with a specific inject or objective |

---

## UI/UX Notes

### Observation Summary Metrics Panel

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Exercise Metrics: Hurricane Response TTX                               │
├─────────────────────────────────────────────────────────────────────────┤
│  [Inject Summary]  [Observations]  [Timeline]  [Participation]          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  OBSERVATION SUMMARY                                                    │
│  ═══════════════════════════════════════════════════════════════════   │
│                                                                         │
│  ┌─────────────┐                                                       │
│  │     24      │   Total observations recorded                         │
│  │ Observations│                                                       │
│  └─────────────┘                                                       │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  PERFORMANCE RATING DISTRIBUTION                                        │
│  ───────────────────────────────                                        │
│                                                                         │
│  Performed (P)      ████████████████░░░░░░░░░░░░░░░░  8  (33%)         │
│  Satisfactory (S)   █████████████████████░░░░░░░░░░░ 10  (42%)         │
│  Marginal (M)       ██████░░░░░░░░░░░░░░░░░░░░░░░░░░  4  (17%)         │
│  Unsatisfactory (U) ███░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  2  ( 8%)         │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  OBJECTIVE COVERAGE                                                     │
│  ──────────────────                                                     │
│                                                                         │
│  Coverage Rate:  75%  (6 of 8 objectives have observations)            │
│                                                                         │
│  Objectives WITHOUT observations:                        [View All]    │
│  • OBJ-4: Coordinate with external agencies                            │
│  • OBJ-7: Complete incident documentation                              │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  BY EVALUATOR                                                           │
│  ────────────                                                           │
│                                                                         │
│  Evaluator           │ Observations │ Avg Rating                       │
│  ────────────────────┼──────────────┼────────────                       │
│  Sarah Johnson       │      12      │   S (2.1)                        │
│  Mike Williams       │       8      │   S (1.9)                        │
│  Lisa Chen           │       4      │   M (2.8)                        │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  BY PHASE                                                               │
│  ────────                                                               │
│                                                                         │
│  Phase                        │ Observations │ P │ S │ M │ U          │
│  ─────────────────────────────┼──────────────┼───┼───┼───┼───          │
│  1. Initial Response          │       8      │ 4 │ 3 │ 1 │ 0          │
│  2. Activation & Mobilization │      10      │ 3 │ 5 │ 1 │ 1          │
│  3. Operations                │       5      │ 1 │ 2 │ 1 │ 1          │
│  4. Demobilization            │       1      │ 0 │ 0 │ 1 │ 0          │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  DATA QUALITY                                                           │
│  ────────────                                                           │
│                                                                         │
│  Linked to inject:     20 (83%)                                        │
│  Linked to objective:  18 (75%)                                        │
│  Unlinked:              4 (17%)                         [View Unlinked]│
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Rating Distribution Chart

Consider pie chart or donut visualization:

```
        ┌───────────────┐
       ╱                 ╲
      │    P: 33%        │
      │    S: 42%        │
      │    M: 17%        │
      │    U:  8%        │
       ╲                 ╱
        └───────────────┘
```

---

## Technical Notes

- API endpoint: `GET /api/exercises/{id}/metrics/observations`
- Response should include:
  - Total count
  - Rating distribution
  - Per-evaluator breakdown
  - Per-phase breakdown
  - Coverage statistics
  - Unlinked observation count
- Rating numeric mapping: P=1, S=2, M=3, U=4 for averaging
- Consider: weighted average by observation importance?

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 5
