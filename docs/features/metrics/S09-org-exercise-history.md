# Story: Organization Exercise History

**Feature**: Metrics  
**Story ID**: S09  
**Priority**: P1 (Standard)  
**Phase**: Standard Implementation

---

## User Story

**As an** Administrator or Director,  
**I want** to see a summary of all exercises conducted by my organization,  
**So that** I can understand exercise program activity and access historical data.

---

## Context

Organizations need visibility into their overall exercise program:

- How many exercises have we conducted this year?
- What types of exercises do we run most often?
- When was our last full-scale exercise?
- Are we meeting regulatory exercise requirements?

This provides the foundation for organization-level analytics.

---

## Acceptance Criteria

- [ ] **Given** I am an Admin or Director, **when** I access organization metrics, **then** I see exercise history summary
- [ ] **Given** the history view, **when** displayed, **then** I see total exercises conducted (with date range filter)
- [ ] **Given** the history view, **when** displayed, **then** I see exercises by type (TTX, Functional, Full-Scale, Drill)
- [ ] **Given** the history view, **when** displayed, **then** I see exercises by status (Completed, Active, Draft)
- [ ] **Given** the history view, **when** displayed, **then** I see exercises over time (monthly/quarterly chart)
- [ ] **Given** I select a date range, **when** applied, **then** all metrics update to reflect that range
- [ ] **Given** the history view, **when** I click on a metric, **then** I can drill down to exercise list
- [ ] **Given** I am a Controller or Evaluator, **when** accessing org metrics, **then** I see only exercises I participated in

---

## Out of Scope

- Cross-organization comparisons
- Regulatory compliance tracking
- Scheduled/planned exercises
- Exercise cost tracking

---

## Dependencies

- Exercise entity with status and type
- Completed exercise history
- Date range filter component

---

## Open Questions

- [ ] What date range options (YTD, last 12 months, custom)?
- [ ] Should we show planned/scheduled exercises?
- [ ] Do we need quarterly/annual summary reports?
- [ ] How far back should history go?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Exercise Type | Category: Table-Top, Functional, Full-Scale, Drill |
| Exercise Status | State: Draft, Active, Completed |

---

## UI/UX Notes

### Organization Exercise History Dashboard

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Organization Metrics                                                   │
├─────────────────────────────────────────────────────────────────────────┤
│  [Exercise History]  [Performance Trends]                               │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Date Range: [ Last 12 Months ▼ ]  Jan 2025 - Jan 2026                 │
│                                                                         │
│  ═══════════════════════════════════════════════════════════════════   │
│                                                                         │
│  EXERCISE ACTIVITY SUMMARY                                              │
│  ─────────────────────────                                              │
│                                                                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │
│  │     18      │  │      8      │  │      2      │  │      4      │    │
│  │   Total     │  │ Table-Top   │  │ Full-Scale  │  │   Drills    │    │
│  │  Exercises  │  │   (TTX)     │  │             │  │             │    │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘    │
│                                                     + 4 Functional      │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  EXERCISES OVER TIME                                                    │
│  ───────────────────                                                    │
│                                                                         │
│    4│      ▄                                                           │
│    3│   ▄  █     ▄                    ▄                               │
│    2│   █  █  ▄  █     ▄     ▄     ▄  █                               │
│    1│▄  █  █  █  █  ▄  █  ▄  █  ▄  █  █  ▄                           │
│    0│────────────────────────────────────────                          │
│      Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec Jan               │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  BY TYPE                              BY STATUS                         │
│  ───────                              ─────────                         │
│                                                                         │
│  ┌──────────────────────┐            ┌──────────────────────┐          │
│  │    ╭───────╮         │            │    ╭───────╮         │          │
│  │   ╱  TTX   ╲         │            │   ╱Complete╲         │          │
│  │  │   44%    │        │            │  │   78%    │        │          │
│  │  │ FS 11%   │        │            │  │Act 11%   │        │          │
│  │   ╲Func 22%╱         │            │   ╲Draft11%╱         │          │
│  │    ╰Drill──╯         │            │    ╰───────╯         │          │
│  │      23%             │            │                      │          │
│  └──────────────────────┘            └──────────────────────┘          │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  RECENT EXERCISES                                              [View All]│
│  ─────────────────                                                      │
│                                                                         │
│  Date       │ Name                          │ Type       │ Status      │
│  ───────────┼───────────────────────────────┼────────────┼─────────    │
│  Jan 15     │ Hurricane Response TTX        │ Table-Top  │ Completed   │
│  Jan 08     │ Active Shooter Drill          │ Drill      │ Completed   │
│  Dec 20     │ Winter Storm Functional       │ Functional │ Completed   │
│  Dec 05     │ Mass Casualty Full-Scale      │ Full-Scale │ Completed   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- API: `GET /api/organizations/{id}/metrics/exercises`
- Query parameters: `startDate`, `endDate`, `type`, `status`
- Aggregate queries for counts by type/status
- Consider: cache aggregates for large organizations
- Role check: Filter exercises by participation for non-admin roles

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 5
