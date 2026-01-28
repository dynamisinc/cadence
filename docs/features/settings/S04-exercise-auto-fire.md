# Story: Exercise Auto-Fire Setting

**Feature**: Settings  
**Story ID**: S04  
**Priority**: P0 (MVP)  
**Phase**: MVP

---

## User Story

**As an** Exercise Director,  
**I want** to enable or disable automatic inject firing,  
**So that** I can choose between having injects fire automatically at their scheduled time or requiring manual Controller action.

---

## Context

MSEL injects have scheduled times, but how they're "fired" (delivered to players) varies by exercise design:

- **Auto-Fire Enabled**: Injects automatically fire at their scheduled time. Controllers monitor but don't need to manually trigger each inject. Good for large MSELs or when timing precision is critical.
- **Auto-Fire Disabled**: Controllers must manually fire each inject. Provides more control, allows for real-time adjustments, and is better for exercises where player pace varies.

Many exercises use a hybrid approach—some injects auto-fire while others require manual action. This setting controls the default behavior; individual inject overrides may be added in a future story.

---

## Acceptance Criteria

- [ ] **Given** I am a Director viewing exercise settings, **when** I access inject delivery settings, **then** I see an Auto-Fire toggle
- [ ] **Given** Auto-Fire is enabled, **when** exercise clock reaches an inject's scheduled time, **then** the inject automatically fires (status changes to Fired, actual time recorded)
- [ ] **Given** Auto-Fire is enabled, **when** an inject auto-fires, **then** all connected users see the status change in real-time
- [ ] **Given** Auto-Fire is disabled, **when** exercise clock reaches an inject's scheduled time, **then** the inject remains Pending until manually fired
- [ ] **Given** Auto-Fire is disabled, **when** an inject passes its scheduled time, **then** it visually indicates it's overdue (yellow/warning state)
- [ ] **Given** Auto-Fire is enabled, **when** an inject fires, **then** the system logs "Auto-fired" vs "Manually fired" for audit
- [ ] **Given** the exercise has not started, **when** I toggle Auto-Fire, **then** the change saves immediately
- [ ] **Given** the exercise is running, **when** I toggle Auto-Fire, **then** the change takes effect immediately (no pause required)
- [ ] **Given** I am a Controller or lower role, **when** I view exercise settings, **then** I can see but not modify Auto-Fire setting

---

## Out of Scope

- Per-inject auto-fire override (all injects follow exercise-level setting)
- Auto-fire with approval workflow (fire after Controller confirms)
- Auto-fire delay/buffer (fire X minutes after scheduled time)
- Automatic skipping of missed injects

---

## Dependencies

- Exercise clock implementation (Phase D)
- Inject firing mechanism (Phase D)
- Exercise settings panel
- Real-time sync (Phase H)

---

## Open Questions

- [ ] Should Auto-Fire only work when clock is running, or also catch up on paused injects when resumed?
- [ ] If clock starts at 14:00 and injects are scheduled for 13:30, should they auto-fire immediately?
- [ ] Should there be a "preview" of upcoming auto-fires (next 3 injects that will fire)?
- [ ] Do we need a warning/countdown before auto-fire (e.g., "Inject firing in 30 seconds")?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Auto-Fire | Automatic inject delivery when exercise clock reaches scheduled time |
| Manual Fire | Controller must explicitly trigger inject delivery |
| Overdue Inject | Inject whose scheduled time has passed but hasn't been fired |
| Fire | Deliver/release an inject to exercise players |

---

## UI/UX Notes

### Exercise Settings - Auto-Fire Toggle

```
┌─────────────────────────────────────────────────────────────┐
│  Exercise Settings                          [Director Only] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Inject Delivery                                            │
│  ─────────────────────────────────────────────              │
│                                                             │
│  Auto-Fire Injects                                          │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                                          [  ON  ]   │   │
│  │  When enabled, injects automatically fire at their  │   │
│  │  scheduled time. Controllers can still manually     │   │
│  │  fire or skip injects at any time.                  │   │
│  │                                                     │   │
│  │  When disabled, Controllers must manually fire      │   │
│  │  each inject.                                       │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ⓘ Tip: Enable for large MSELs where timing is critical.  │
│     Disable when pace depends on player responses.         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### MSEL View - Auto-Fire Indicator

When Auto-Fire is enabled, show indicator on upcoming injects:

```
┌─────────────────────────────────────────────────────────────┐
│ ⏱ 14:28  │  INJ-015  │  ⚡ Auto-fires in 2 min  │ Pending │
├─────────────────────────────────────────────────────────────┤
│ ⏱ 14:35  │  INJ-016  │                         │ Pending │
└─────────────────────────────────────────────────────────────┘
```

### Overdue Inject (Auto-Fire Disabled)

```
┌─────────────────────────────────────────────────────────────┐
│ ⏱ 14:15  │  INJ-012  │  ⚠ Overdue (5 min)      │ Pending │
│          │           │  [🔥 Fire]  [⏭ Skip]     │         │
└─────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- Store as boolean `AutoFireEnabled` on Exercise entity
- Background service or SignalR hub should check for due injects when:
  - Clock ticks (if running)
  - Clock resumes from pause
  - Auto-fire setting toggles ON
- Inject record should store `FiredBy`: "System" (auto) vs User ID (manual)
- Consider: batch fire multiple overdue injects if clock jumps forward
- Real-time notification when inject auto-fires: "Inject INJ-015 auto-fired"

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 5
