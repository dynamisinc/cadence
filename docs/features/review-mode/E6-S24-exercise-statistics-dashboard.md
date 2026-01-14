# E6-S24: Exercise Statistics Dashboard

**Feature:** review-mode  
**Priority:** P2  
**Estimate:** 2 days

## User Story

**As** James (Exercise Director),  
**I want** to see visual statistics about exercise execution,  
**So that** I can quickly communicate performance to stakeholders.

## Context

Exercise Directors often brief leadership after exercises. Visual charts and statistics help communicate results quickly without reading detailed observations.

## Acceptance Criteria

- [ ] **Given** I am in Review Mode, **when** I view the Dashboard tab, **then** I see visual charts of exercise performance
- [ ] **Given** the Dashboard, **when** I view the timeline chart, **then** I see a visual representation of when injects were fired vs. scheduled
- [ ] **Given** the Dashboard, **when** I view the phase progress chart, **then** I see completion percentage per phase
- [ ] **Given** the Dashboard, **when** I view the rating distribution, **then** I see a pie/bar chart of P/S/M/U ratings
- [ ] **Given** the Dashboard, **when** I view the objectives coverage, **then** I see which objectives were evaluated and which have gaps

## Dependencies

- E6-S20: Access Review Mode
- E6-S22: Inject Outcome Summary (data calculations)

## UI/UX Notes

### Dashboard Layout

```
┌─────────────────────────────────────────────────────────────────────────┐
│ EXERCISE DASHBOARD                                                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────────────┐  ┌─────────────────────────────────┐  │
│  │ INJECT TIMELINE             │  │ PHASE COMPLETION                │  │
│  │                             │  │                                 │  │
│  │ Scheduled: ──●──●──●──●──   │  │ Phase 1: ████████████ 100%     │  │
│  │ Actual:    ──●───●──●─●──   │  │ Phase 2: ████████░░░░  80%     │  │
│  │                             │  │ Phase 3: ░░░░░░░░░░░░   0%     │  │
│  │ [visual timeline chart]     │  │                                 │  │
│  └─────────────────────────────┘  └─────────────────────────────────┘  │
│                                                                         │
│  ┌─────────────────────────────┐  ┌─────────────────────────────────┐  │
│  │ OBSERVATION RATINGS         │  │ OBJECTIVE COVERAGE              │  │
│  │                             │  │                                 │  │
│  │      P: ████████ 45%        │  │ Obj 1: ✓ 3 observations        │  │
│  │      S: █████    30%        │  │ Obj 2: ✓ 2 observations        │  │
│  │      M: ███      18%        │  │ Obj 3: ⚠ 0 observations        │  │
│  │      U: █         7%        │  │ Obj 4: ✓ 1 observation         │  │
│  │                             │  │                                 │  │
│  └─────────────────────────────┘  └─────────────────────────────────┘  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Chart Components

| Chart | Type | Library |
|-------|------|---------|
| Inject Timeline | Scatter/Line | Recharts |
| Phase Completion | Horizontal Bar | Recharts |
| Observation Ratings | Pie or Bar | Recharts |
| Objective Coverage | List with indicators | Custom |

### Technical Notes

- Use Recharts or similar for visualizations
- Charts should be exportable as images (nice-to-have)
- Dashboard should print cleanly for briefings
- Consider responsive layout for tablet viewing
