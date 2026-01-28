# Story: Exercise Inject Summary Metrics

**Feature**: Metrics  
**Story ID**: S02  
**Priority**: P0 (MVP)  
**Phase**: MVP

---

## User Story

**As a** Director conducting after-action review,  
**I want** to see comprehensive inject delivery metrics,  
**So that** I can assess how well the exercise execution matched the plan.

---

## Context

After an exercise concludes, the AAR process requires analysis of inject delivery. Key questions:

- Were injects delivered on schedule?
- How many were skipped, and why?
- Did certain phases or controllers have issues?
- What was the actual pacing compared to planned?

This metrics view provides the quantitative foundation for AAR discussions.

---

## Acceptance Criteria

- [ ] **Given** I am viewing exercise metrics (post-conduct), **when** I open the Inject Summary tab, **then** I see comprehensive inject statistics
- [ ] **Given** the inject summary, **when** displayed, **then** I see total inject count
- [ ] **Given** the inject summary, **when** displayed, **then** I see breakdown by status: Fired (count/%), Skipped (count/%), Not Executed (count/%)
- [ ] **Given** the inject summary, **when** timing data exists, **then** I see On-Time Rate (delivered within ±5 min of scheduled)
- [ ] **Given** the inject summary, **when** timing data exists, **then** I see average timing variance (± minutes)
- [ ] **Given** the inject summary, **when** I view "By Phase" breakdown, **then** I see inject counts per exercise phase
- [ ] **Given** the inject summary, **when** I view "By Controller" breakdown, **then** I see inject counts per controller who fired them
- [ ] **Given** skipped injects, **when** viewing list, **then** I can see the skip reasons (if captured)
- [ ] **Given** the metrics page, **when** I click on a metric, **then** I can drill down to the underlying inject list

---

## Out of Scope

- Real-time metrics during conduct (see S01)
- Inject content analysis (what injects contained)
- Comparison to other exercises
- Predictive analytics

---

## Dependencies

- Exercise conduct complete (Phase D)
- Inject status tracking with timestamps
- Phase and controller associations on injects

---

## Open Questions

- [ ] What tolerance defines "on time" (±5 min suggested)?
- [ ] Should "Not Executed" include pending injects when exercise ended early?
- [ ] How do we handle exercises with accelerated clock (adjust timing metrics)?
- [ ] Should auto-fired vs manually-fired be broken out?

---

## Domain Terms

| Term | Definition |
|------|------------|
| On-Time Rate | Percentage of injects delivered within acceptable variance of scheduled time |
| Timing Variance | Difference between scheduled and actual delivery time |
| Not Executed | Injects that remained Pending when exercise concluded |

---

## UI/UX Notes

### Inject Summary Metrics Panel

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Exercise Metrics: Hurricane Response TTX                               │
├─────────────────────────────────────────────────────────────────────────┤
│  [Inject Summary]  [Observations]  [Timeline]  [Participation]          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  INJECT DELIVERY SUMMARY                                                │
│  ═══════════════════════════════════════════════════════════════════   │
│                                                                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │
│  │     42      │  │     38      │  │      3      │  │      1      │    │
│  │   Total     │  │   Fired     │  │   Skipped   │  │ Not Executed│    │
│  │  Injects    │  │   (90%)     │  │    (7%)     │  │    (3%)     │    │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘    │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  TIMING PERFORMANCE                                                     │
│  ─────────────────────                                                  │
│                                                                         │
│  On-Time Rate:        82%  (31 of 38 fired injects)                    │
│  Average Variance:    +3.2 minutes                                      │
│  Earliest:            -8 minutes (INJ-007)                             │
│  Latest:              +22 minutes (INJ-034)                            │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  BY PHASE                                                               │
│  ────────                                                               │
│                                                                         │
│  Phase                        │ Total │ Fired │ Skipped │ On-Time     │
│  ─────────────────────────────┼───────┼───────┼─────────┼─────────    │
│  1. Initial Response          │   12  │   12  │    0    │   92%       │
│  2. Activation & Mobilization │   15  │   14  │    1    │   79%       │
│  3. Operations                │   10  │    9  │    1    │   78%       │
│  4. Demobilization            │    5  │    3  │    1    │   67%       │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  BY CONTROLLER                                                          │
│  ─────────────                                                          │
│                                                                         │
│  Controller          │ Fired │ Avg Variance │ On-Time                  │
│  ────────────────────┼───────┼──────────────┼─────────                  │
│  John Smith          │   22  │   +1.8 min   │   91%                    │
│  Jane Doe            │   16  │   +5.1 min   │   69%                    │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  SKIPPED INJECTS                                           [View All]  │
│  ───────────────                                                        │
│                                                                         │
│  INJ-018  │  Phase 2  │  "Players completed objective early"           │
│  INJ-029  │  Phase 3  │  "Technical issue with display system"         │
│  INJ-041  │  Phase 4  │  "Exercise ended before scheduled time"        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Status Breakdown Visual

Consider showing a horizontal stacked bar:

```
Inject Status:
█████████████████████████████████████░░░░░ ● Fired (90%)
                                      ░░░ ○ Skipped (7%)
                                        ░ ◌ Not Executed (3%)
```

---

## Technical Notes

- Calculate metrics server-side for accuracy
- API endpoint: `GET /api/exercises/{id}/metrics/injects`
- Response should include:
  - Summary counts
  - Timing statistics
  - Per-phase breakdown
  - Per-controller breakdown
  - Skipped injects with reasons
- Consider caching metrics for completed exercises
- On-time tolerance should be configurable (default ±5 min)

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 5
