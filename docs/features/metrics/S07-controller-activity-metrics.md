# Story: Controller Activity Metrics

**Feature**: Metrics  
**Story ID**: S07  
**Priority**: P1 (Standard)  
**Phase**: Standard Implementation

---

## User Story

**As a** Director,  
**I want** to see metrics on Controller activity during the exercise,  
**So that** I can assess Controller workload distribution and identify potential resource allocation improvements.

---

## Context

Controllers are responsible for inject delivery. Understanding their activity helps:

- Balance workload in future exercises
- Identify Controllers who may need additional support
- Recognize high-performing Controllers
- Plan staffing for similar exercises

This supports operational efficiency, not performance evaluation of individuals.

---

## Acceptance Criteria

- [ ] **Given** I am viewing exercise metrics, **when** I view Controller metrics, **then** I see activity summary per Controller
- [ ] **Given** the Controller view, **when** displayed, **then** I see injects fired per Controller
- [ ] **Given** the Controller view, **when** displayed, **then** I see average timing variance per Controller
- [ ] **Given** the Controller view, **when** displayed, **then** I see on-time rate per Controller
- [ ] **Given** the Controller view, **when** displayed, **then** I see which phases each Controller was active in
- [ ] **Given** multiple Controllers, **when** viewing, **then** I see workload distribution (percentage of total injects)
- [ ] **Given** a Controller with no activity, **when** viewing, **then** they appear with zero counts (not hidden)
- [ ] **Given** I am a Controller, **when** viewing metrics, **then** I can only see my own detailed activity (not others)

---

## Out of Scope

- Controller performance scoring/ranking
- Historical Controller performance
- Recommendations for Controller assignment
- Real-time Controller dashboards

---

## Dependencies

- S02: Exercise Inject Summary Metrics
- Inject records with "FiredBy" user reference
- Controller role assignments

---

## Open Questions

- [ ] Should Controllers see each other's metrics?
- [ ] Do we track skipped injects per Controller?
- [ ] Should we show time-on-task or just inject counts?
- [ ] How to handle injects auto-fired (no Controller)?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Controller | HSEEP role responsible for inject delivery and exercise flow |
| Workload | Distribution of inject responsibility across Controllers |

---

## UI/UX Notes

### Controller Activity Metrics

```
┌─────────────────────────────────────────────────────────────────────────┐
│  CONTROLLER ACTIVITY                                                    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Workload Distribution                                                  │
│  ─────────────────────                                                  │
│                                                                         │
│  John Smith     ████████████████████████████░░░░░░░░░░  58% (22 injects)│
│  Jane Doe       ███████████████░░░░░░░░░░░░░░░░░░░░░░░  42% (16 injects)│
│  System (Auto)  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   0% ( 0 injects)│
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  Detailed Activity                                                      │
│  ─────────────────                                                      │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  John Smith (Primary Controller)                                  │ │
│  ├───────────────────────────────────────────────────────────────────┤ │
│  │                                                                   │ │
│  │  Injects Fired:    22              On-Time Rate:   91%            │ │
│  │  Injects Skipped:   1              Avg Variance:  +1.8 min        │ │
│  │                                                                   │ │
│  │  Active Phases:                                                   │ │
│  │  • Phase 1: Initial Response (8 injects)                         │ │
│  │  • Phase 2: Activation (10 injects)                              │ │
│  │  • Phase 3: Operations (4 injects)                               │ │
│  │                                                                   │ │
│  │  Timing Performance:                                              │ │
│  │  ███████████████████████████████████████░░░░  On-Time: 20/22     │ │
│  │                                                                   │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Jane Doe (Secondary Controller)                                  │ │
│  ├───────────────────────────────────────────────────────────────────┤ │
│  │                                                                   │ │
│  │  Injects Fired:    16              On-Time Rate:   69%            │ │
│  │  Injects Skipped:   2              Avg Variance:  +5.1 min        │ │
│  │                                                                   │ │
│  │  Active Phases:                                                   │ │
│  │  • Phase 2: Activation (5 injects)                               │ │
│  │  • Phase 3: Operations (6 injects)                               │ │
│  │  • Phase 4: Demobilization (5 injects)                           │ │
│  │                                                                   │ │
│  │  ⚠ Higher variance during Phase 3 - consider additional support  │ │
│  │                                                                   │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- API: `GET /api/exercises/{id}/metrics/controllers`
- Calculate per-Controller metrics from inject records
- Handle auto-fired injects separately (FiredBy = "System")
- Role check: Controllers see only their own details
- Directors see all Controller metrics

---

## Estimation

**T-Shirt Size**: S  
**Story Points**: 3
