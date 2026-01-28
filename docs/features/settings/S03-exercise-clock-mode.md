# Story: Exercise Clock Mode Setting

**Feature**: Settings  
**Story ID**: S03  
**Priority**: P0 (MVP)  
**Phase**: MVP

---

## User Story

**As an** Exercise Director,  
**I want** to configure the exercise clock mode (real-time vs. accelerated),  
**So that** I can run table-top exercises faster than real-time while keeping full-scale exercises synchronized with actual time.

---

## Context

Different exercise types have different time requirements:

- **Full-Scale Exercises**: Run in real-time (1x) to coordinate with physical responders and real-world activities
- **Functional Exercises**: May run in real-time or slightly accelerated depending on scope
- **Table-Top Exercises (TTX)**: Often run accelerated (2x, 5x, even 10x) since discussion replaces physical action
- **Drills**: Typically real-time for specific skill practice

HSEEP allows flexibility in exercise pacing. The clock mode determines how quickly "exercise time" advances relative to "wall clock time."

---

## Acceptance Criteria

- [ ] **Given** I am a Director viewing exercise settings, **when** I access clock configuration, **then** I see clock mode options
- [ ] **Given** I am configuring clock mode, **when** I view options, **then** I can select: Real-time (1x), 2x, 5x, 10x, or Custom multiplier
- [ ] **Given** I select Real-time (1x), **when** the exercise clock runs, **then** 1 minute of exercise time = 1 minute of wall clock time
- [ ] **Given** I select 5x speed, **when** the exercise clock runs, **then** 5 minutes of exercise time = 1 minute of wall clock time
- [ ] **Given** I select Custom, **when** I enter a multiplier (e.g., 3), **then** the clock advances at that rate
- [ ] **Given** Custom multiplier, **when** I enter the value, **then** it accepts values from 0.5 to 20
- [ ] **Given** the exercise has not started, **when** I change clock mode, **then** the change is saved immediately
- [ ] **Given** the exercise is running, **when** I try to change clock mode, **then** I am prompted to pause the exercise first
- [ ] **Given** the exercise is paused, **when** I change clock mode, **then** the change takes effect when the clock resumes
- [ ] **Given** I am a Controller or lower role, **when** I view exercise settings, **then** I cannot modify clock mode

---

## Out of Scope

- Variable speed during exercise (speed changes mid-exercise)
- Time jumping (skip ahead to specific time)
- Slow motion (less than 0.5x) for training purposes
- Automatic speed changes based on phase

---

## Dependencies

- Exercise clock implementation (Phase D)
- Exercise settings panel
- Director role permissions

---

## Open Questions

- [ ] Should clock mode be changeable during exercise, or locked once started?
- [ ] Do we need "pause between phases" option that pairs with this?
- [ ] Should scheduled inject times auto-adjust when mode changes, or stay fixed?
- [ ] What happens to "time remaining until inject" displays at different speeds?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Clock Mode | The speed at which exercise time advances relative to real (wall clock) time |
| Real-time | 1:1 ratio - exercise time matches actual time |
| Accelerated | Exercise time advances faster than real time (e.g., 5x means 5 exercise minutes per 1 real minute) |
| TTX | Table-Top Exercise - discussion-based, often runs accelerated |
| Full-Scale | Exercise involving actual resource deployment, typically runs real-time |

---

## UI/UX Notes

### Exercise Settings Panel - Clock Mode

```
┌─────────────────────────────────────────────────────────────┐
│  Exercise Settings                          [Director Only] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Exercise Clock                                             │
│  ─────────────────────────────────────────────              │
│                                                             │
│  Clock Mode                                                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  ● Real-time (1x)                                   │   │
│  │    Exercise time = wall clock time                  │   │
│  │                                                     │   │
│  │  ○ Accelerated (2x)                                 │   │
│  │    2 exercise minutes per 1 real minute             │   │
│  │                                                     │   │
│  │  ○ Accelerated (5x)                                 │   │
│  │    5 exercise minutes per 1 real minute             │   │
│  │                                                     │   │
│  │  ○ Accelerated (10x)                                │   │
│  │    10 exercise minutes per 1 real minute            │   │
│  │                                                     │   │
│  │  ○ Custom: [____] x                                 │   │
│  │    (0.5 - 20)                                       │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ⓘ Tip: Use real-time for full-scale exercises,           │
│     accelerated for table-tops.                            │
│                                                             │
│  ⚠ Clock mode can only be changed when exercise is paused  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Clock Display with Mode Indicator

```
┌─────────────────────────────────────┐
│  EXERCISE TIME        ⏱ 5x speed   │
│  14:30:00                          │
│  [▶ Running]                       │
└─────────────────────────────────────┘
```

---

## Technical Notes

- Store clock mode as decimal multiplier (1.0, 2.0, 5.0, etc.)
- Clock calculation: `exerciseTimeElapsed = realTimeElapsed * multiplier`
- SignalR should broadcast multiplier to all connected clients
- Consider: when multiplier changes, scheduled inject times stay the same (exercise time), but "time until" changes
- Validate custom multiplier: 0.5 ≤ multiplier ≤ 20

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 3-5
