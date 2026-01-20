# Gap Analysis: Exercise Clock Modes & Timing Configuration

> **Analysis Date:** 2026-01-20
> **Requirements Document:** `exercise-clock-modes-requirements.md`
> **Analyst:** Business Analyst Agent

---

## Executive Summary

The Cadence codebase has a **solid foundation** for exercise clock functionality with comprehensive real-time synchronization via SignalR, a working clock UI, and inject firing capabilities. However, the **dual-mode timing system** (Clock-driven vs Facilitator-paced) described in the requirements is **not yet implemented**. The current implementation assumes a single "clock-driven" approach without the configurability needed for discussion-based exercises like TTXs.

**Key Gaps:**
1. Missing `DeliveryMode`, `TimelineMode`, and `TimeScale` fields on Exercise entity
2. No auto-Ready functionality for injects (current: all manual firing)
3. No facilitator-paced view with "Current Inject" / "Up Next" presentation
4. Inject uses `ScheduledTime` (TimeOnly) instead of `DeliveryTime` (TimeSpan from start)
5. No configuration UI for timing mode selection during exercise creation

---

## Inventory: Current Implementation

### Backend Entities

| File | Key Fields | Notes |
|------|------------|-------|
| [Exercise.cs](../../../src/Cadence.Core/Models/Entities/Exercise.cs) | `ClockState`, `ClockStartedAt`, `ClockElapsedBeforePause`, `ClockStartedBy` | Clock tracking exists ✅ |
| [Inject.cs](../../../src/Cadence.Core/Models/Entities/Inject.cs) | `Sequence`, `ScheduledTime` (TimeOnly), `ScenarioDay`, `ScenarioTime` | Close to spec but differs |
| [Enums.cs](../../../src/Cadence.Core/Models/Entities/Enums.cs) | `ExerciseClockState`, `InjectStatus`, `ExerciseStatus` | Core enums present ✅ |

### Backend Services

| Service | Capabilities | Notes |
|---------|--------------|-------|
| [ExerciseClockService.cs](../../../src/Cadence.Core/Features/ExerciseClock/Services/ExerciseClockService.cs) | Start, Pause, Stop, Reset clock | Full clock lifecycle ✅ |
| [InjectService.cs](../../../src/Cadence.Core/Features/Injects/Services/InjectService.cs) | Fire, Skip, Reset injects | Manual operations only |

### Frontend Components

| Component | Purpose | Notes |
|-----------|---------|-------|
| [ClockDisplay.tsx](../../../src/frontend/src/features/exercise-clock/components/ClockDisplay.tsx) | Shows elapsed time + state | Works well ✅ |
| [ClockControls.tsx](../../../src/frontend/src/features/exercise-clock/components/ClockControls.tsx) | Start/Pause/Stop buttons | COBRA styled ✅ |
| [useExerciseClock.ts](../../../src/frontend/src/features/exercise-clock/hooks/useExerciseClock.ts) | Clock state management | Optimistic updates ✅ |
| [ExerciseConductPage.tsx](../../../src/frontend/src/features/exercises/pages/ExerciseConductPage.tsx) | Main conduct view | Single mode only |
| [InjectRow.tsx](../../../src/frontend/src/features/injects/components/InjectRow.tsx) | Inject display | Has "due soon" highlighting |

### SignalR Events

| Event | Status | Notes |
|-------|--------|-------|
| `ClockStarted`, `ClockPaused`, `ClockStopped` | ✅ Implemented | Full broadcast support |
| `InjectFired`, `InjectSkipped`, `InjectReset` | ✅ Implemented | Real-time updates |
| `InjectReadyToFire` | 🔲 Missing | Needed for auto-Ready notification |

---

## Gap Analysis

### Exercise Entity - Timing Configuration

| Requirement | Status | Current Implementation | Gap/Action |
|-------------|--------|------------------------|------------|
| `DeliveryMode` enum | 🔲 **Missing** | Not present | Add enum + field to Exercise |
| `TimelineMode` enum | 🔲 **Missing** | Not present | Add enum + field to Exercise |
| `TimeScale` decimal | 🔲 **Missing** | Not present | Add nullable field |
| Locked when Active | ⚠️ **Partial** | Some fields locked, timing not | Extend validation |

### Inject Entity - Timing Fields

| Requirement | Status | Current Implementation | Gap/Action |
|-------------|--------|------------------------|------------|
| `SequenceNumber` | ✅ **Complete** | `Sequence` (int) exists | Just naming difference |
| `DeliveryTime` (TimeSpan) | ⚠️ **Different** | `ScheduledTime` (TimeOnly) | Different concept - need both |
| `StoryDay` | ✅ **Complete** | `ScenarioDay` exists | Just naming convention |
| `StoryTime` | ✅ **Complete** | `ScenarioTime` exists | Just naming convention |

**Important Distinction:**
- **ScheduledTime (TimeOnly)** = Wall clock time "deliver at 10:30 AM"
- **DeliveryTime (TimeSpan)** = Offset from exercise start "deliver at +00:30:00"

The requirements want offset-based timing. Current implementation uses wall clock time. This is a **fundamental design difference** that needs resolution.

### Exercise Clock UI

| Requirement | Status | Current Implementation | Gap/Action |
|-------------|--------|------------------------|------------|
| Elapsed time display | ✅ **Complete** | `ClockDisplay` component | Format HH:MM:SS works |
| Start/Pause/Stop controls | ✅ **Complete** | `ClockControls` component | Full lifecycle |
| Story Time display | ⚠️ **Partial** | Per-inject only, not in clock header | Add to clock area |
| Clock state SignalR sync | ✅ **Complete** | All events broadcast | Working well |

### Clock-Driven Conduct View

| Requirement | Status | Current Implementation | Gap/Action |
|-------------|--------|------------------------|------------|
| "Ready to Fire" section | 🔲 **Missing** | All injects shown in flat list | Need sectioned view |
| Auto-transition to Ready | 🔲 **Missing** | Manual only | Need auto-Ready logic |
| Countdown to upcoming | ⚠️ **Partial** | "due soon" highlight exists | Need countdown timer display |
| Visual highlight for Ready | ⚠️ **Partial** | Pulse animation on due-soon | Enhance for Ready state |

### Facilitator-Paced Conduct View

| Requirement | Status | Current Implementation | Gap/Action |
|-------------|--------|------------------------|------------|
| No elapsed clock | 🔲 **Missing** | Clock always shown | Conditional render |
| "Current Inject" section | 🔲 **Missing** | Not implemented | New component |
| Full inject preview | 🔲 **Missing** | Row only, no expansion | Expand current inject |
| "Up Next" section | 🔲 **Missing** | Not implemented | New component |
| Manual sequence progression | 🔲 **Missing** | Random access only | Sequence tracking |
| Jump with confirmation | 🔲 **Missing** | Not implemented | Dialog + logic |

### Fire/Skip Actions

| Requirement | Status | Current Implementation | Gap/Action |
|-------------|--------|------------------------|------------|
| Fire button | ✅ **Complete** | On InjectRow | Works |
| Confirmation dialog | 🔲 **Missing** | Direct fire | Add confirmation option |
| Timestamp recording | ✅ **Complete** | `FiredAt` captured | UTC timestamp |
| Skip action | ✅ **Complete** | Skip button with reason | Works |
| Status workflow | ⚠️ **Partial** | Pending→Fired/Skipped | Missing "Ready" state |

**New Status Needed:** The requirements imply a `Ready` status between `Pending` and `Fired` for clock-driven mode. Current statuses are just: `Pending`, `Fired`, `Skipped`.

### Exercise Configuration UI

| Requirement | Status | Current Implementation | Gap/Action |
|-------------|--------|------------------------|------------|
| Delivery Mode selection | 🔲 **Missing** | Not present | Add to exercise create/edit |
| Timeline Mode selection | 🔲 **Missing** | Not present | Add to exercise create/edit |
| TimeScale input | 🔲 **Missing** | Not present | Conditional input |
| Smart defaults by type | 🔲 **Missing** | Not present | TTX→Facilitator, FSE→Clock |
| Settings locked when Active | ⚠️ **Partial** | Some fields | Extend to timing |

---

## Recommended User Stories

### Phase 1: Foundation (Database & API)

---

#### Story CLK-01: Add Timing Configuration Fields to Exercise Entity

**As a** developer,
**I want** the Exercise entity to have DeliveryMode, TimelineMode, and TimeScale fields,
**So that** timing configuration can be persisted and used throughout the application.

##### Scope
- Add `DeliveryMode` enum (ClockDriven, FacilitatorPaced)
- Add `TimelineMode` enum (RealTime, Compressed, StoryOnly)
- Add `TimeScale` nullable decimal field
- Create EF migration
- Update ExerciseDto and mapper
- Add validation (TimeScale required when Compressed, max 60)

##### Acceptance Criteria
- [ ] Given the Exercise entity, when inspected, then it includes DeliveryMode, TimelineMode, and TimeScale properties
- [ ] Given DeliveryMode is not specified, when an exercise is created, then it defaults to ClockDriven
- [ ] Given TimelineMode is not specified, when an exercise is created, then it defaults to RealTime
- [ ] Given TimelineMode is Compressed, when TimeScale is null or ≤0, then validation fails
- [ ] Given TimeScale is provided, when TimelineMode is not Compressed, then TimeScale is ignored (not validated)
- [ ] Given TimeScale is provided, when greater than 60, then validation fails

##### Technical Notes
- Add enums to `Enums.cs`
- Migration should set defaults for existing exercises
- No frontend changes in this story

##### Estimate
- [x] Medium (1-2 days)

---

#### Story CLK-02: Add DeliveryTime Field to Inject Entity

**As a** developer,
**I want** the Inject entity to have a DeliveryTime (TimeSpan) field,
**So that** injects can be scheduled relative to exercise start rather than wall clock time.

##### Scope
- Add `DeliveryTime` (TimeSpan?) to Inject entity
- Keep existing `ScheduledTime` for backward compatibility
- Create EF migration
- Update InjectDto and mapper
- Consider migration strategy for existing injects

##### Acceptance Criteria
- [ ] Given the Inject entity, when inspected, then it includes DeliveryTime as TimeSpan?
- [ ] Given an inject with DeliveryTime = 00:30:00, when the exercise clock reaches 30 minutes, then the inject should be considered "due"
- [ ] Given an inject without DeliveryTime, when displayed, then it uses ScheduledTime as fallback
- [ ] Given existing injects in database, when migration runs, then existing data is preserved

##### Technical Notes
- `DeliveryTime` is offset from exercise start (e.g., +00:30:00 means 30 minutes in)
- `ScheduledTime` (TimeOnly) is wall clock time (e.g., 10:30 AM)
- For MVP, support both; future stories may deprecate ScheduledTime
- Consider: Calculate DeliveryTime from ScheduledTime using exercise StartTime?

##### Estimate
- [x] Small (< 1 day)

---

### Phase 2: Configuration UI

---

#### Story CLK-03: Exercise Timing Configuration in Create/Edit Form

**As an** Exercise Director,
**I want** to configure Delivery Mode and Timeline Mode when creating an exercise,
**So that** the exercise runs with the appropriate timing behavior.

##### Scope
- Add Delivery Mode radio buttons to exercise create/edit form
- Add Timeline Mode radio buttons with conditional TimeScale input
- Implement smart defaults based on ExerciseType
- Make fields read-only when exercise status is Active

##### Acceptance Criteria
- [ ] Given I am creating a new exercise, when I reach the configuration step, then I see Delivery Mode selection with "Clock-driven" and "Facilitator-paced" options
- [ ] Given I am creating a new exercise, when I select ExerciseType = TTX, then Delivery Mode defaults to "Facilitator-paced"
- [ ] Given I am creating a new exercise, when I select ExerciseType = FSE, then Delivery Mode defaults to "Clock-driven"
- [ ] Given I select "Compressed" timeline mode, when the option is selected, then a TimeScale input appears
- [ ] Given an exercise is Active, when I view the settings, then timing fields are disabled
- [ ] Given I change TimeScale to 4, when I save, then the value persists

##### Technical Notes
- Follow existing exercise form patterns
- Use COBRA styled components (CobraTextField, radio groups)
- Reference UI wireframe in requirements document

##### Estimate
- [x] Medium (1-2 days)

---

### Phase 3: Conduct View - Clock-Driven Mode

---

#### Story CLK-04: Add "Ready" Status to Inject Workflow

**As a** system,
**I want** injects to have a "Ready" status between "Pending" and "Fired",
**So that** auto-transitioned injects can be distinguished from manually selected ones.

##### Scope
- Add `Ready` to InjectStatus enum
- Update inject status workflow validation
- Update frontend types and status chips
- Ensure backward compatibility

##### Acceptance Criteria
- [ ] Given InjectStatus enum, when inspected, then it includes: Pending, Ready, Fired, Skipped
- [ ] Given an inject is Pending, when it becomes Ready, then status = Ready
- [ ] Given an inject is Ready, when fired, then status = Fired with FiredAt timestamp
- [ ] Given an inject is Ready, when displayed, then it shows a distinct "Ready" chip (different from Pending)
- [ ] Given existing Pending injects, when upgrade runs, then they remain Pending (not auto-Ready)

##### Technical Notes
- Status order: Pending → Ready → Fired (or Pending → Skipped, Ready → Skipped)
- Ready is only set by auto-Ready logic, not manually
- Consider if Ready injects can be "un-readied" (probably not needed)

##### Estimate
- [x] Small (< 1 day)

---

#### Story CLK-05: Auto-Ready Injects When Clock Reaches DeliveryTime

**As a** system,
**I want** pending injects to automatically transition to "Ready" when the exercise clock reaches their DeliveryTime,
**So that** Controllers are notified without manual intervention in clock-driven mode.

##### Scope
- Backend service to check injects against elapsed time
- Transition matching injects from Pending → Ready
- Broadcast `InjectReadyToFire` SignalR event
- Only applies when DeliveryMode = ClockDriven

##### Acceptance Criteria
- [ ] Given a clock-driven exercise with clock running, when elapsed time ≥ inject's DeliveryTime, then inject status changes to Ready
- [ ] Given a facilitator-paced exercise, when time passes, then injects do NOT auto-Ready
- [ ] Given an inject transitions to Ready, when the transition occurs, then SignalR broadcasts `InjectReadyToFire`
- [ ] Given multiple injects reach their DeliveryTime simultaneously, when processed, then all become Ready
- [ ] Given an inject is already Ready/Fired/Skipped, when clock passes DeliveryTime, then no change occurs

##### Technical Notes
- Implementation options:
  1. **Background timer in App Service** - check every N seconds
  2. **Client-side calculation** - clients determine Ready locally, server validates on Fire
  3. **Hybrid** - server authoritative, client optimistic
- Recommend option 1 for consistency across clients
- Consider batching updates to avoid SignalR flood

##### Estimate
- [x] Large (3-5 days) - Complex real-time logic with multiple edge cases

---

#### Story CLK-06: Clock-Driven Conduct View Sections

**As a** Controller in a clock-driven exercise,
**I want** the conduct view to show "Ready to Fire" and "Upcoming" sections,
**So that** I can focus on injects that need immediate attention.

##### Scope
- Refactor conduct page for sectioned layout
- "Ready to Fire" section with highlighted injects
- "Upcoming" section with countdown timers
- "Completed" section (collapsed by default)
- Only show this layout when DeliveryMode = ClockDriven

##### Acceptance Criteria
- [ ] Given DeliveryMode = ClockDriven, when I view conduct page, then injects are grouped into sections
- [ ] Given injects with status = Ready, when displayed, then they appear in "Ready to Fire" section with visual emphasis
- [ ] Given injects with DeliveryTime within 30 minutes, when displayed in Upcoming, then they show countdown ("in 12:45")
- [ ] Given injects with status = Fired or Skipped, when displayed, then they appear in collapsed "Completed" section
- [ ] Given the conduct page, when an inject becomes Ready via SignalR, then it moves to "Ready to Fire" section in real-time

##### Technical Notes
- Reuse existing InjectRow component with variant prop
- Countdown calculation: `inject.DeliveryTime - elapsedTime`
- Consider virtual scrolling if many injects

##### Estimate
- [x] Medium (1-2 days)

---

### Phase 4: Conduct View - Facilitator-Paced Mode

---

#### Story CLK-07: Facilitator-Paced Conduct View

**As a** Facilitator in a discussion-based exercise,
**I want** a conduct view focused on the current inject with manual progression,
**So that** I can control the pace of discussion without a running clock.

##### Scope
- New conduct view layout for FacilitatorPaced mode
- "Current Inject" section with full content preview
- "Up Next" section showing next 2-3 injects
- Track current position in sequence
- Hide elapsed time clock

##### Acceptance Criteria
- [ ] Given DeliveryMode = FacilitatorPaced, when I view conduct page, then no elapsed time clock is displayed
- [ ] Given I am in facilitator mode, when I view the page, then I see "Current Inject" with full description text
- [ ] Given the current inject is #3, when I fire it, then #4 becomes the current inject
- [ ] Given I am viewing current inject, when I look at "Up Next", then I see the next 2-3 injects by sequence
- [ ] Given I want to skip ahead, when I click an "Up Next" inject's "Jump to" button, then a confirmation dialog appears
- [ ] Given I confirm the jump, when processed, then skipped injects are marked Skipped and selected inject becomes current

##### Technical Notes
- "Current inject" = first Pending inject in Sequence order
- Track in React state or derive from inject list
- Story Time display per inject (not running clock)
- Consider keyboard shortcuts for Fire (F), Skip (S), Next (N)

##### Estimate
- [x] Large (3-5 days) - New view pattern with different UX

---

#### Story CLK-08: Display Story Time in Clock Area

**As an** Evaluator,
**I want** to see the current Story Time alongside the exercise clock,
**So that** I understand the narrative timeline for my observations.

##### Scope
- Add Story Time display to clock header area
- Calculate based on TimelineMode and TimeScale
- Format as "Day N HH:MM"
- Update in real-time when clock running (for Compressed mode)

##### Acceptance Criteria
- [ ] Given TimelineMode = RealTime, when clock is running, then Story Time = elapsed time (same as clock)
- [ ] Given TimelineMode = Compressed with TimeScale = 4, when 15 real minutes pass, then Story Time advances 60 minutes
- [ ] Given TimelineMode = StoryOnly, when viewing conduct page, then only Story Time is displayed (no elapsed clock)
- [ ] Given Story Time crosses midnight, when displayed, then Day increments (Day 1 → Day 2)
- [ ] Given exercise has a starting Story Time configured, when calculating, then that is the baseline

##### Technical Notes
- Implement `calculateStoryTime()` function from requirements
- Need starting Story Time field on Exercise (new field or derived from first inject?)
- Consider: Where is exercise start Story Time configured?

##### Estimate
- [x] Medium (1-2 days)

---

### Phase 5: Polish & Edge Cases

---

#### Story CLK-09: Fire Confirmation Dialog for Critical Injects

**As a** Controller,
**I want** a confirmation dialog before firing critical injects,
**So that** I don't accidentally fire the wrong inject.

##### Scope
- Add confirmation dialog option for inject firing
- Make configurable per inject (optional field: RequireConfirmation)
- Or: always confirm for certain inject types (Contingency, Complexity)

##### Acceptance Criteria
- [ ] Given an inject with RequireConfirmation = true, when I click Fire, then a confirmation dialog appears
- [ ] Given the confirmation dialog, when I confirm, then the inject is fired
- [ ] Given the confirmation dialog, when I cancel, then nothing happens
- [ ] Given an inject with RequireConfirmation = false, when I click Fire, then it fires immediately (no dialog)

##### Technical Notes
- Use MUI Dialog component
- Consider: Global setting vs per-inject setting
- For MVP, could make it a user preference rather than inject property

##### Estimate
- [x] Small (< 1 day)

---

#### Story CLK-10: Sequence Number Reordering via Drag-Drop

**As a** Controller viewing the MSEL,
**I want** to reorder injects by dragging them,
**So that** sequence numbers update automatically.

##### Scope
- Add drag-drop functionality to inject list
- Update sequence numbers on drop
- Persist reordering to backend
- Broadcast changes via SignalR

##### Acceptance Criteria
- [ ] Given I drag inject #3 above inject #2, when I drop it, then #3 becomes #2 and former #2 becomes #3
- [ ] Given I reorder injects, when saved, then the new order persists
- [ ] Given another client is viewing the MSEL, when I reorder, then they see the update in real-time
- [ ] Given the exercise is Active, when I try to reorder, then drag-drop is disabled

##### Technical Notes
- Use `@dnd-kit/core` or `react-beautiful-dnd`
- Batch update endpoint: `PATCH /api/msels/{mselId}/reorder`
- Only allow reordering in Draft status

##### Estimate
- [x] Medium (1-2 days)

---

## Implementation Roadmap

### Recommended Order

```
Phase 1: Foundation
├── CLK-01: Timing fields on Exercise (DEPENDENCY for all others)
├── CLK-02: DeliveryTime field on Inject
└── CLK-04: Add Ready status to workflow

Phase 2: Configuration UI
└── CLK-03: Exercise timing configuration form

Phase 3: Clock-Driven Mode
├── CLK-05: Auto-Ready logic (complex - start early)
├── CLK-06: Sectioned conduct view
└── CLK-08: Story Time display

Phase 4: Facilitator-Paced Mode
└── CLK-07: Facilitator-paced conduct view

Phase 5: Polish
├── CLK-09: Fire confirmation dialog
└── CLK-10: Drag-drop reordering
```

### Dependency Graph

```
CLK-01 ─────┬─── CLK-03 (UI needs backend fields)
            │
            ├─── CLK-05 (auto-Ready needs DeliveryMode)
            │       │
            │       └─── CLK-06 (sections need Ready status)
            │
            └─── CLK-07 (facilitator view needs DeliveryMode)

CLK-02 ─────────── CLK-05 (auto-Ready needs DeliveryTime)

CLK-04 ─────────── CLK-05 (auto-Ready sets Ready status)
                   │
                   └─── CLK-06 (sections filter by Ready)

CLK-08 ─────────── CLK-01 (needs TimelineMode, TimeScale)
```

### Effort Summary

| Story | Estimate | Dependencies |
|-------|----------|--------------|
| CLK-01 | Medium | None |
| CLK-02 | Small | None |
| CLK-03 | Medium | CLK-01 |
| CLK-04 | Small | None |
| CLK-05 | Large | CLK-01, CLK-02, CLK-04 |
| CLK-06 | Medium | CLK-05 |
| CLK-07 | Large | CLK-01 |
| CLK-08 | Medium | CLK-01 |
| CLK-09 | Small | None |
| CLK-10 | Medium | None |

**Total Estimated Effort:**
- 3 Small stories (~3 days)
- 5 Medium stories (~7.5 days)
- 2 Large stories (~8 days)
- **Approximate: 15-20 developer days**

---

## Open Questions

1. **DeliveryTime vs ScheduledTime:** Should we migrate existing `ScheduledTime` values to `DeliveryTime`, or support both indefinitely?

2. **Exercise Start Story Time:** Where is the starting story time configured? Do we need a new `ExerciseStartStoryTime` field, or derive from the first inject's StoryDay/StoryTime?

3. **Auto-Ready Implementation:** Server-side timer vs client-side calculation? Server is more reliable but adds complexity.

4. **Ready Status Persistence:** Should injects transition back to Pending if clock is reset? Or once Ready, always Ready until fired/skipped?

5. **Facilitator-Paced Clock:** In facilitator mode, should there be an optional stopwatch for session timing (different from inject timing)?

---

## Appendix: Field Mapping

| Requirement Term | Current Entity Field | Status |
|------------------|---------------------|--------|
| DeliveryMode | (missing) | Add to Exercise |
| TimelineMode | (missing) | Add to Exercise |
| TimeScale | (missing) | Add to Exercise |
| SequenceNumber | `Sequence` (Inject) | ✅ Exists |
| DeliveryTime | `ScheduledTime` (TimeOnly) | ⚠️ Different type |
| StoryDay | `ScenarioDay` (Inject) | ✅ Exists |
| StoryTime | `ScenarioTime` (Inject) | ✅ Exists |
| ClockState | `ClockState` (Exercise) | ✅ Exists |
| ElapsedTime | Calculated from `ClockStartedAt` + `ClockElapsedBeforePause` | ✅ Exists |
