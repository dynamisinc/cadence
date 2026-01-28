# Story: Exercise Progress Dashboard

**Feature**: Metrics  
**Story ID**: S01  
**Priority**: P0 (MVP)  
**Phase**: MVP

---

## User Story

**As a** Director or Controller,  
**I want** to see exercise progress at a glance during conduct,  
**So that** I can quickly assess how the exercise is proceeding without leaving the MSEL view.

---

## Context

During exercise conduct, leadership needs situational awareness: How far along are we? Are we on schedule? How many injects remain? This information should be visible without navigating away from the primary conduct view.

This is a compact, real-time display—not a full analytics dashboard. It provides just enough information for operational awareness without overwhelming or distracting from conduct activities.

---

## Acceptance Criteria

- [ ] **Given** I am in exercise conduct view, **when** I look at the header/toolbar area, **then** I see a progress summary component
- [ ] **Given** the progress component, **when** injects exist in the MSEL, **then** I see "X of Y injects completed" with progress bar
- [ ] **Given** the progress component, **when** the exercise clock is running, **then** I see current exercise time prominently
- [ ] **Given** the progress component, **when** observations have been recorded, **then** I see observation count
- [ ] **Given** the progress component, **when** an inject fires or is skipped, **then** the counts update in real-time (within 2 seconds)
- [ ] **Given** I click on the progress component, **when** expanded, **then** I see additional detail (next 3 upcoming injects, quick stats)
- [ ] **Given** I am on mobile/tablet, **when** viewing progress, **then** it adapts to smaller screen without losing key information
- [ ] **Given** I am an Observer, **when** viewing conduct, **then** I can see progress but not modify anything

---

## Out of Scope

- Detailed analytics charts (separate metrics page)
- Historical comparison to past exercises
- Per-controller or per-evaluator breakdowns
- Export functionality

---

## Dependencies

- Exercise clock implementation (Phase D)
- Inject firing/status updates (Phase D)
- Observation capture (Phase E)
- Real-time sync (Phase H)

---

## Open Questions

- [ ] Should progress be visible during exercise setup, or only once started?
- [ ] Should there be visual/audio alert when all injects are complete?
- [ ] How do we handle exercises with no injects (observation-only)?
- [ ] Should the progress bar show time-based progress or inject-count-based?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Progress | Ratio of completed injects (Fired + Skipped) to total injects |
| Completed | Inject that has been either Fired or Skipped |
| Pending | Inject that has not yet been addressed |

---

## UI/UX Notes

### Progress Component - Collapsed (Default)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  MSEL: Hurricane Response TTX                                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ⏱ 14:32:15  │  Progress: 18/42 injects  ████████░░░░░░░░ 43%  │  📝 7 │
│  [▶ Running]  │                                               │  obs  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Progress Component - Expanded (Click to Expand)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Exercise Progress                                               [▲]   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Injects                              Observations                      │
│  ──────────────                       ────────────                      │
│  🔥 Fired: 16                          📝 Total: 7                      │
│  ⏭ Skipped: 2                          P: 3  S: 2  M: 1  U: 1          │
│  ⏳ Pending: 24                                                         │
│                                                                         │
│  ────────────────────────────────────────────────────────────────────── │
│                                                                         │
│  Upcoming Injects                                                       │
│  ─────────────────                                                      │
│  14:35  INJ-019  Patient surge notification                            │
│  14:40  INJ-020  Media inquiry arrives                                 │
│  14:45  INJ-021  Power outage report                                   │
│                                                                         │
│  ────────────────────────────────────────────────────────────────────── │
│                                                                         │
│  Timeline                                                               │
│  ─────────                                                              │
│  Started: 13:00  │  Elapsed: 1h 32m  │  Est. remaining: ~1h 15m        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Mobile/Tablet View - Compact

```
┌─────────────────────────────────┐
│  ⏱ 14:32  │  18/42  │  📝 7    │
│  [▶]       │ ███░░░  │          │
└─────────────────────────────────┘
```

---

## Technical Notes

- Component should subscribe to real-time updates via SignalR
- Calculate progress: `(fired + skipped) / total * 100`
- Estimated remaining time: based on inject timing, not just count
- Consider: store progress state in React context to avoid recalculation
- Progress bar should animate smoothly on update
- Ensure accessible: progress bar needs ARIA attributes

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 5
