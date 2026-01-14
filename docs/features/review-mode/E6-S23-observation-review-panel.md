# E6-S23: Observation Review Panel

**Feature:** review-mode  
**Priority:** P2  
**Estimate:** 1.5 days

## User Story

**As** Robert (Evaluator),  
**I want** to see all observations organized by inject and objective,  
**So that** I can verify evaluation coverage and prepare AAR findings.

## Context

During conduct, observations are captured quickly. Review Mode allows evaluators to see all observations in context, verify nothing was missed, and organize findings.

## Acceptance Criteria

- [ ] **Given** I am in Review Mode, **when** I view the Observations panel, **then** I see all observations for this exercise
- [ ] **Given** the Observations panel, **when** I group by inject, **then** observations are organized under the inject they reference
- [ ] **Given** the Observations panel, **when** I group by objective, **then** observations are organized under the objective they reference
- [ ] **Given** an objective with no observations, **when** I view it, **then** I see a "No observations" indicator (coverage gap)
- [ ] **Given** the Observations panel, **when** I filter by rating, **then** I can show only "Needs Improvement" or "Unable to Perform" observations
- [ ] **Given** an observation, **when** I view it, **then** I see: Timestamp, Observer name, Linked inject, Rating (P/S/M/U), Notes, Recommendation

## Dependencies

- E6-S20: Access Review Mode
- Phase E complete (Observations)

## UI/UX Notes

### Grouping Options

```
[Group by: Inject ▼]  [Filter: All Ratings ▼]

─── By Inject ───

▼ #3 School District Inquiry (2 observations)
  ┌────────────────────────────────────────────────────────────┐
  │ 9:35 AM • Robert Okonkwo • Rating: S (Some Difficulty)    │
  │ "EOC took 8 minutes to provide guidance. Target is 5 min."│
  │ Recommendation: Review decision-making protocols          │
  ├────────────────────────────────────────────────────────────┤
  │ 9:36 AM • Lisa Davis • Rating: P (Performed)              │
  │ "Communication with school district was clear and helpful"│
  └────────────────────────────────────────────────────────────┘

▼ #4 Hurricane Warning Upgraded (1 observation)
  ...

▶ General Observations (no linked inject) (3 observations)
  ...
```

### By Objective View

```
[Group by: Objective ▼]  [Filter: All Ratings ▼]

─── By Objective ───

▼ Objective 1: Activate EOC within 30 minutes (3 observations)
  ...

▼ Objective 2: Coordinate evacuation routes (2 observations)
  ...

⚠️ Objective 3: Establish shelter operations (0 observations)
   No observations recorded - coverage gap
```
