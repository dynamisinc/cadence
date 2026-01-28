# Story: Exercise Timeline Summary

**Feature**: Metrics  
**Story ID**: S04  
**Priority**: P0 (MVP)  
**Phase**: MVP

---

## User Story

**As a** Director conducting after-action review,  
**I want** to see timeline metrics for the exercise,  
**So that** I can understand how actual exercise pacing compared to the planned schedule.

---

## Context

Exercise planning includes expected duration and phase timing. Actual execution often differs due to:

- Player pace variations
- Unexpected discussions or issues
- Technical problems requiring pauses
- Scenario adaptations

Understanding timeline variance helps improve future exercise planning and identifies operational issues.

---

## Acceptance Criteria

- [ ] **Given** I am viewing exercise metrics, **when** I open the Timeline tab, **then** I see exercise duration statistics
- [ ] **Given** the timeline summary, **when** displayed, **then** I see planned duration vs actual duration
- [ ] **Given** the timeline summary, **when** displayed, **then** I see exercise start time and end time
- [ ] **Given** the timeline summary, **when** pauses occurred, **then** I see total pause count and cumulative pause duration
- [ ] **Given** the timeline summary, **when** phases are defined, **then** I see time spent in each phase
- [ ] **Given** the timeline summary, **when** clock mode was accelerated, **then** I see both exercise time and wall clock time
- [ ] **Given** the timeline summary, **when** displayed, **then** I see "active time" (total minus pauses)
- [ ] **Given** the metrics page, **when** I click on a phase, **then** I can see detailed timing for that phase

---

## Out of Scope

- Visual timeline/Gantt chart (P2 feature)
- Inject-level timing breakdown (in S02)
- Real-time timeline during conduct
- Comparison to other exercises

---

## Dependencies

- Exercise clock implementation (Phase D)
- Clock state tracking (start, pause, resume, stop events)
- Phase timing records

---

## Open Questions

- [ ] How do we define "planned duration" (sum of inject times? explicit field?)
- [ ] Should we track reason for each pause?
- [ ] What if exercise spans multiple days (pause overnight)?
- [ ] How to display accelerated time exercises (show both real and exercise time)?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Planned Duration | Expected exercise length based on MSEL timing |
| Actual Duration | Real elapsed time from start to end (including pauses) |
| Active Time | Actual duration minus total pause time |
| Wall Clock Time | Real-world time elapsed |
| Exercise Time | Time as displayed on exercise clock (may be accelerated) |

---

## UI/UX Notes

### Timeline Summary Panel

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Exercise Metrics: Hurricane Response TTX                               │
├─────────────────────────────────────────────────────────────────────────┤
│  [Inject Summary]  [Observations]  [Timeline]  [Participation]          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  TIMELINE SUMMARY                                                       │
│  ═══════════════════════════════════════════════════════════════════   │
│                                                                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │
│  │  2h 45m     │  │  3h 10m     │  │   25m       │  │  2h 45m     │    │
│  │  Planned    │  │  Actual     │  │  Paused     │  │  Active     │    │
│  │  Duration   │  │  Duration   │  │  Time       │  │  Time       │    │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘    │
│                                                                         │
│  Variance: +25 minutes (15% longer than planned)                       │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  EXERCISE WINDOW                                                        │
│  ───────────────                                                        │
│                                                                         │
│  Started:    January 15, 2026 at 09:00                                 │
│  Ended:      January 15, 2026 at 12:10                                 │
│  Clock Mode: Real-time (1x)                                            │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  PAUSE HISTORY                                                          │
│  ─────────────                                                          │
│                                                                         │
│  Total Pauses: 3                                                       │
│                                                                         │
│  #  │ Time       │ Duration │ Notes                                    │
│  ───┼────────────┼──────────┼──────────────────────────────────────    │
│  1  │ 09:45      │  10 min  │ Technical issue with projector           │
│  2  │ 10:30      │   5 min  │ Bio break                                │
│  3  │ 11:15      │  10 min  │ Extended discussion on procedures        │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  TIME BY PHASE                                                          │
│  ─────────────                                                          │
│                                                                         │
│  Phase                        │ Planned │ Actual  │ Variance           │
│  ─────────────────────────────┼─────────┼─────────┼─────────           │
│  1. Initial Response          │   30m   │   35m   │  +5m               │
│  2. Activation & Mobilization │   45m   │   55m   │ +10m               │
│  3. Operations                │   60m   │   70m   │ +10m               │
│  4. Demobilization            │   30m   │   30m   │   0m               │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  TIMELINE VISUALIZATION                                                 │
│  ──────────────────────                                                 │
│                                                                         │
│  09:00        10:00        11:00        12:00                          │
│  │─────────────│─────────────│─────────────│                           │
│  ▓▓▓▓▓▓░▓▓▓▓▓▓▓▓░▓▓▓▓▓▓▓▓▓▓░▓▓▓▓▓▓▓▓▓▓▓▓│                           │
│      Phase 1    Phase 2       Phase 3    Phase 4                       │
│           ↑          ↑            ↑                                    │
│        Pause 1   Pause 2      Pause 3                                  │
│                                                                         │
│  Legend: ▓ Active  ░ Paused                                            │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Accelerated Exercise Display

When clock mode was not 1x:

```
┌─────────────────────────────────────────────────────────────────────────┐
│  EXERCISE WINDOW                                                        │
│  ───────────────                                                        │
│                                                                         │
│  Started:        January 15, 2026 at 09:00 (wall clock)                │
│  Ended:          January 15, 2026 at 10:30 (wall clock)                │
│  Clock Mode:     5x Accelerated                                        │
│                                                                         │
│  Exercise Time:  08:00 → 15:30 (7h 30m exercise time)                  │
│  Wall Clock:     09:00 → 10:30 (1h 30m real time)                      │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- API endpoint: `GET /api/exercises/{id}/metrics/timeline`
- Track clock events in database:
  - `ClockEvent` table: ExerciseId, EventType (Start, Pause, Resume, Stop), Timestamp, Notes
- Calculate planned duration from inject timing or explicit exercise field
- Active time = Actual duration - Sum(pause durations)
- Store phase start/end times for phase breakdown
- Consider: allow notes on pause events for AAR context

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 5
