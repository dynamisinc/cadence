# Story: Comparative Analysis (Exercise vs Exercise)

**Feature**: Metrics  
**Story ID**: S12  
**Priority**: P2 (Future)  
**Phase**: Future Enhancement

---

## User Story

**As a** Director or Emergency Manager,  
**I want** to compare metrics between two or more exercises,  
**So that** I can assess improvement over time and evaluate different exercise approaches.

---

## Context

Organizations often want to compare:

- This year's hurricane exercise vs. last year's
- Different exercise types (did the TTX prepare us for the functional?)
- Before/after a specific training intervention
- Performance across similar scenarios

Side-by-side comparison helps demonstrate improvement and justify exercise program investment.

---

## Acceptance Criteria

- [ ] **Given** I am viewing organization metrics, **when** I click "Compare Exercises", **then** I can select 2-4 exercises
- [ ] **Given** I select exercises, **when** I view comparison, **then** I see side-by-side metric summaries
- [ ] **Given** comparison view, **when** displayed, **then** I see inject delivery rates compared
- [ ] **Given** comparison view, **when** displayed, **then** I see P/S/M/U distributions compared
- [ ] **Given** comparison view, **when** displayed, **then** I see capability performance compared
- [ ] **Given** comparison view, **when** metrics differ significantly, **then** differences are highlighted
- [ ] **Given** comparison, **when** I want to save, **then** I can export comparison report (PDF)
- [ ] **Given** exercises of different types, **when** comparing, **then** I see appropriate caveats

---

## Out of Scope

- Automated comparison recommendations
- Statistical significance testing
- Comparison templates
- Real-time comparison during exercise

---

## Dependencies

- All exercise metrics stories complete
- Organization exercise history (S09)

---

## Open Questions

- [ ] Maximum exercises to compare at once?
- [ ] Should we auto-suggest comparison candidates (similar type, same capability focus)?
- [ ] How to handle vastly different exercise sizes (5 injects vs 50 injects)?

---

## UI/UX Notes

### Comparison Selection

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Compare Exercises                                                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Select 2-4 exercises to compare:                                       │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  [✓] Hurricane Response TTX (Jan 2026)                          │   │
│  │  [✓] Hurricane Response TTX (Jan 2025)                          │   │
│  │  [ ] Mass Casualty Full-Scale (Dec 2025)                        │   │
│  │  [ ] Hurricane Functional (Aug 2025)                            │   │
│  │  ...                                                             │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Selected: 2 exercises                    [Cancel]  [Compare →]        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Comparison View

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Exercise Comparison                                       [📥 Export] │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│                      │ Hurricane TTX 2025 │ Hurricane TTX 2026 │ Δ     │
│  ────────────────────┼────────────────────┼────────────────────┼───────│
│  Total Injects       │        35          │        42          │ +20%  │
│  On-Time Rate        │        72%         │        85%         │ ↑ +13%│
│  Observations        │        18          │        24          │ +33%  │
│  P/S Rating %        │        65%         │        78%         │ ↑ +13%│
│  Duration            │      2h 15m        │      2h 45m        │ +22%  │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  P/S/M/U Comparison                                                     │
│                                                                         │
│  2025:  ████████████████████░░░░░░░░░░░░▒▒▒▒▒▒▒▒▓▓▓▓                   │
│         P: 28%   S: 37%    M: 22%    U: 13%                             │
│                                                                         │
│  2026:  ████████████████████████████░░░░░░░░▒▒▒▒▓▓                     │
│         P: 38%   S: 40%    M: 15%    U: 7%                              │
│                                                                         │
│  ✓ Improvement: +13% in P/S ratings                                    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- API: `GET /api/organizations/{id}/metrics/compare?exerciseIds=1,2,3`
- Return normalized metrics for fair comparison
- Calculate deltas and percentage changes
- Flag statistically significant differences

---

## Estimation

**T-Shirt Size**: L  
**Story Points**: 8
