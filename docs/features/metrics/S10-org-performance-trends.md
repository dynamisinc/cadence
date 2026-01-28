# Story: Organization Performance Trends

**Feature**: Metrics  
**Story ID**: S10  
**Priority**: P1 (Standard)  
**Phase**: Standard Implementation

---

## User Story

**As an** Administrator or Emergency Manager,  
**I want** to see performance trends across multiple exercises over time,  
**So that** I can demonstrate program improvement and identify persistent capability gaps.

---

## Context

HSEEP emphasizes continuous improvement through the exercise cycle. Organizations need to demonstrate:

- Are we improving in areas identified as weaknesses?
- Which Core Capabilities consistently need attention?
- Is our exercise program producing measurable results?
- Where should we focus training and resources?

This metrics view aggregates performance data across exercises to reveal trends.

---

## Acceptance Criteria

- [ ] **Given** I am an Admin or Director, **when** I view organization metrics, **then** I see a Performance Trends section
- [ ] **Given** the trends view, **when** displayed, **then** I see P/S/M/U distribution over time (line or area chart)
- [ ] **Given** the trends view, **when** displayed, **then** I see average rating per Core Capability across all exercises
- [ ] **Given** the trends view, **when** displayed, **then** I see "Top Improvement Areas" (capabilities with most M/U ratings)
- [ ] **Given** the trends view, **when** I select a date range, **then** all trend data updates
- [ ] **Given** the trends view, **when** I select a specific capability, **then** I see detailed trend for that capability
- [ ] **Given** sufficient data (3+ exercises), **when** viewing trends, **then** I see trend direction indicators (improving/declining)
- [ ] **Given** limited data (< 3 exercises), **when** viewing trends, **then** I see appropriate messaging ("More data needed")

---

## Out of Scope

- Predictive analytics (forecasting future performance)
- Benchmark comparison to other organizations
- Automated improvement recommendations
- Statistical significance testing

---

## Dependencies

- S09: Organization Exercise History
- Completed exercises with observation data
- Core Capability tracking

---

## Open Questions

- [ ] How many exercises needed before showing trend lines?
- [ ] Should we weight recent exercises more heavily?
- [ ] Do we need to filter by exercise type (TTX trends separate from Full-Scale)?
- [ ] How to handle capability changes (FEMA updates list)?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Performance Trend | Change in ratings/metrics over time across multiple exercises |
| Improvement Area | Capability or area consistently receiving M/U ratings |
| Trend Direction | Whether performance is improving, stable, or declining |

---

## UI/UX Notes

### Performance Trends Dashboard

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Organization Metrics                                                   │
├─────────────────────────────────────────────────────────────────────────┤
│  [Exercise History]  [Performance Trends]                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Date Range: [ Last 12 Months ▼ ]     Exercises in range: 18           │
│                                                                         │
│  ═══════════════════════════════════════════════════════════════════   │
│                                                                         │
│  OVERALL PERFORMANCE TREND                                              │
│  ─────────────────────────                                              │
│                                                                         │
│  100%│                                                                  │
│   80%│    ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░          │
│   60%│    ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒         │
│   40%│    ████████████████████████████████████████████████████         │
│   20%│    ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓          │
│    0%│────────────────────────────────────────────────────────         │
│       Q1 '25    Q2 '25    Q3 '25    Q4 '25    Jan '26                  │
│                                                                         │
│  Legend: █ Performed  ▒ Satisfactory  ░ Marginal  ▓ Unsatisfactory     │
│                                                                         │
│  ↑ Trend: Improving (+8% P/S ratings vs. prior year)                   │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  CORE CAPABILITY TRENDS                                                 │
│  ──────────────────────                                                 │
│                                                                         │
│  Capability                   │ Avg Rating │ Trend  │ Exercises        │
│  ────────────────────────────┼────────────┼────────┼─────────          │
│  Operational Coordination     │    S (1.8) │   ↑    │    14            │
│  Mass Care Services           │    M (2.6) │   ↓    │    10            │
│  Public Information           │    S (2.1) │   →    │    12            │
│  Planning                     │    P (1.4) │   ↑    │    16            │
│  Logistics & Supply Chain     │    M (2.8) │   →    │     8            │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ⚠ TOP IMPROVEMENT AREAS (Most M/U Ratings)                            │
│  ──────────────────────────────────────────                            │
│                                                                         │
│  1. Mass Care Services                                                  │
│     15 observations │ 40% M/U │ ↓ declining over 3 exercises           │
│     [View Details]                                                      │
│                                                                         │
│  2. Logistics & Supply Chain                                           │
│     12 observations │ 33% M/U │ → stable                               │
│     [View Details]                                                      │
│                                                                         │
│  3. Environmental Response/Health & Safety                             │
│      8 observations │ 25% M/U │ ↑ improving                            │
│     [View Details]                                                      │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ✓ STRENGTHS (Consistently P/S)                                        │
│  ──────────────────────────────                                        │
│                                                                         │
│  • Planning (92% P/S across 16 exercises)                              │
│  • Operational Coordination (88% P/S across 14 exercises)              │
│  • Situational Assessment (85% P/S across 11 exercises)                │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Trend Direction Indicators

| Symbol | Meaning | Criteria |
|--------|---------|----------|
| ↑ | Improving | P/S% increased 5%+ over period |
| → | Stable | Change within ±5% |
| ↓ | Declining | P/S% decreased 5%+ over period |

---

## Technical Notes

- API: `GET /api/organizations/{id}/metrics/trends`
- Query parameters: `startDate`, `endDate`, `capabilityId`
- Calculate trends from observation aggregates across exercises
- Store pre-calculated aggregates for performance (update on exercise completion)
- Minimum 3 exercises for trend calculation
- Consider: exponential weighting (recent exercises count more)

---

## Estimation

**T-Shirt Size**: L  
**Story Points**: 8
