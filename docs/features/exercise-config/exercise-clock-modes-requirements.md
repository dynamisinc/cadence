# Feature: Exercise Clock Modes & Timing Configuration

> **Epic:** Exercise Conduct  
> **MVP IDs:** MVP-J (Unified Exercise Clock), MVP-M (Inject Status Workflow)  
> **Phase:** D  
> **Status:** Ready for Implementation

---

## Overview

Cadence must support diverse exercise formats ranging from real-time full-scale exercises to facilitator-paced tabletop discussions. This feature introduces a flexible dual-mode timing system that adapts to different exercise needs while maintaining a consistent user experience.

---

## Business Value

| Stakeholder | Value |
|-------------|-------|
| **Exercise Directors** | Configure timing to match exercise type without workarounds |
| **Controllers** | Clear guidance on when to deliver injects regardless of mode |
| **Facilitators** | Run TTXs at their own pace without fighting a clock |
| **Evaluators** | Understand timeline context for observation timestamps |
| **Organizations** | Single platform supports all HSEEP exercise types |

---

## User Stories

### Story 1: Configure Exercise Delivery Mode

**As an** Exercise Director,  
**I want** to choose whether injects are clock-driven or facilitator-paced,  
**So that** I can run both timed operations exercises and discussion-based tabletops.

#### Acceptance Criteria

- [ ] **Given** I am creating a new exercise, **when** I reach the configuration step, **then** I see a "Delivery Mode" selection with two options: "Clock-driven" and "Facilitator-paced"
- [ ] **Given** I select "Clock-driven", **when** the exercise is active, **then** injects automatically transition to "Ready" status when the exercise clock reaches their Delivery Time
- [ ] **Given** I select "Facilitator-paced", **when** the exercise is active, **then** injects remain in "Pending" status until manually selected or fired by a Controller
- [ ] **Given** I create an exercise, **when** I select an Exercise Type, **then** the Delivery Mode defaults appropriately (Full-Scale/Functional/Drill → Clock-driven; TTX/Workshop/Seminar → Facilitator-paced)
- [ ] **Given** I am editing an exercise in Draft status, **when** I change the Delivery Mode, **then** the change is saved
- [ ] **Given** an exercise is Active, **when** I view exercise settings, **then** Delivery Mode is read-only

#### UI Notes

```
┌─────────────────────────────────────────────────────────────┐
│ How will injects be delivered?                 [?]          │
│                                                              │
│ ● Clock-driven                                               │
│   Injects automatically become Ready at their Delivery Time  │
│                                                              │
│ ○ Facilitator-paced                                          │
│   You control when each inject is delivered                  │
└─────────────────────────────────────────────────────────────┘
```

---

### Story 2: Configure Exercise Timeline Mode

**As an** Exercise Director,  
**I want** to choose between real-time, compressed, or story-only timelines,  
**So that** I can simulate scenarios that span hours or days within a shorter exercise window.

#### Acceptance Criteria

- [ ] **Given** I am creating a new exercise, **when** I reach the configuration step, **then** I see a "Timeline Mode" selection with options: "Real-time", "Compressed", and "Story-only"
- [ ] **Given** I select "Real-time", **when** the exercise clock runs, **then** 1 exercise minute = 1 real minute
- [ ] **Given** I select "Compressed", **when** I complete selection, **then** I am prompted to enter a time scale multiplier (e.g., 4x)
- [ ] **Given** I select "Compressed" with TimeScale = 4, **when** 15 real minutes pass, **then** the Story Time advances by 60 minutes
- [ ] **Given** I select "Story-only", **when** the exercise is active, **then** no elapsed time clock is displayed, only Story Time per inject
- [ ] **Given** I create an exercise, **when** I select an Exercise Type, **then** the Timeline Mode defaults to "Real-time" but can be changed
- [ ] **Given** an exercise is Active, **when** I view exercise settings, **then** Timeline Mode is read-only

#### UI Notes

```
┌─────────────────────────────────────────────────────────────┐
│ What timeline will the exercise use?           [?]          │
│                                                              │
│ ● Real-time                                                  │
│   Exercise clock matches wall clock (1:1)                    │
│                                                              │
│ ○ Compressed                                                 │
│   Simulate longer scenarios in less time                     │
│   Time scale: [4x ▾] (1 real minute = 4 story minutes)       │
│                                                              │
│ ○ Story-only                                                 │
│   No real-time clock, just narrative timestamps              │
└─────────────────────────────────────────────────────────────┘
```

---

### Story 3: Display Sequence Number on All Injects

**As a** Controller,  
**I want** to see a Sequence Number on every inject,  
**So that** I have a reference for inject order regardless of delivery mode.

#### Acceptance Criteria

- [ ] **Given** I am viewing the MSEL inject list, **when** the list renders, **then** every inject displays its Sequence Number as the first column
- [ ] **Given** an inject has Sequence Number 3, **when** displayed in the UI, **then** it appears as "#3"
- [ ] **Given** I am in Clock-driven mode, **when** I view the MSEL, **then** Sequence Numbers are still visible (not hidden)
- [ ] **Given** I am reordering injects, **when** I drag inject #3 above inject #2, **then** Sequence Numbers update (former #2 becomes #3, former #3 becomes #2)
- [ ] **Given** the system, **when** a new inject is created, **then** it is assigned the next available Sequence Number

#### Out of Scope
- Auto-resequencing to fill gaps (e.g., if #3 is deleted, #4 does NOT become #3)
- Sequence Number editing without drag-drop reorder

---

### Story 4: Clock-Driven Conduct View

**As a** Controller in a clock-driven exercise,  
**I want** to see the exercise clock, injects ready to fire, and upcoming injects,  
**So that** I can deliver injects on schedule.

#### Acceptance Criteria

- [ ] **Given** I am on the Conduct view of a Clock-driven exercise, **when** the view loads, **then** I see the Exercise Clock prominently displayed showing elapsed time
- [ ] **Given** the Exercise Clock is running, **when** it reaches an inject's Delivery Time, **then** that inject moves to a "Ready to Fire" section with visual highlight
- [ ] **Given** injects are in "Ready to Fire", **when** I view them, **then** they display: Sequence #, Title, Delivery Time, Story Time, and a [Fire] button
- [ ] **Given** injects have Delivery Times within the next 30 minutes, **when** I view "Upcoming", **then** they show a countdown (e.g., "in 12:45")
- [ ] **Given** the exercise has both Delivery Time and Story Time configured, **when** I view the clock area, **then** I see both times displayed

#### UI Notes

```
┌─────────────────────────────────────────────────────────────────────────┐
│  ▶ Exercise Clock: 00:32:15                    [⏸ Pause] [⏹ Stop]      │
│    Story Time: Day 1 18:32                                              │
├─────────────────────────────────────────────────────────────────────────┤
│ ⚠️ READY TO FIRE (1)                                                    │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ #3 │ Evacuation Order │ +00:30 │ Day 1 18:00 │       [🔥 FIRE]      │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────────────┤
│ UPCOMING (2)                                                            │
│   #4 │ Shelter Opens       │ +00:45 │ Day 2 06:00 │ in 12:45           │
│   #5 │ Medical Emergency   │ +01:00 │ Day 2 10:00 │ in 27:45           │
└─────────────────────────────────────────────────────────────────────────┘
```

---

### Story 5: Facilitator-Paced Conduct View

**As a** Facilitator in a discussion-based exercise,  
**I want** to control the pace of inject delivery manually,  
**So that** I can allow adequate discussion time before moving to the next scenario beat.

#### Acceptance Criteria

- [ ] **Given** I am on the Conduct view of a Facilitator-paced exercise, **when** the view loads, **then** I do NOT see an elapsed time clock
- [ ] **Given** the exercise is in Facilitator-paced mode, **when** I view the current inject, **then** it shows: Sequence #, Title, Story Time, inject content preview, and a [Fire] button
- [ ] **Given** I fire the current inject, **when** it completes, **then** the next inject in Sequence order becomes the "Current Inject"
- [ ] **Given** I am viewing the Conduct page, **when** I look at "Up Next", **then** I see the next 2-3 injects by Sequence order
- [ ] **Given** Story-only Timeline Mode, **when** I view an inject, **then** I see only Story Time (no Delivery Time column)
- [ ] **Given** I want to skip ahead, **when** I click on an "Up Next" inject, **then** I can optionally jump to it (with confirmation)

#### UI Notes

```
┌─────────────────────────────────────────────────────────────────────────┐
│  📖 Story Time: Day 1 18:00                                             │
│     Facilitator-Paced Mode                                              │
├─────────────────────────────────────────────────────────────────────────┤
│ ▶ CURRENT INJECT                                                        │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ #3 │ Evacuation Order │ Day 1 18:00 │                 [🔥 FIRE]     │ │
│ │                                                                     │ │
│ │ "The Governor has issued a mandatory evacuation order for all       │ │
│ │ coastal zones. Emergency Management Director, how do you proceed?"  │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────────────┤
│ UP NEXT                                                                 │
│   #4 │ Shelter Opens          │ Day 2 06:00 │ [Jump to →]             │
│   #5 │ Medical Emergency      │ Day 2 10:00 │                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Domain Terms

| Term | Definition |
|------|------------|
| **Delivery Time** | Elapsed time from exercise start when an inject should be delivered to players. Used in Clock-driven mode. Format: `+HH:MM:SS` or `HH:MM AM/PM` |
| **Story Time** | The fictional timestamp within the scenario narrative. Represents when the inject event occurs in the story world. Format: `Day N HH:MM` |
| **Sequence Number** | Manual ordering integer for injects. Primary ordering mechanism in Facilitator-paced mode. Always visible. |
| **Clock-driven** | Delivery mode where injects automatically become "Ready" when the exercise clock reaches their Delivery Time. |
| **Facilitator-paced** | Delivery mode where a Controller/Facilitator manually controls inject progression. No automatic timing. |
| **Real-time** | Timeline mode where exercise time matches wall clock time (1:1 ratio). |
| **Compressed** | Timeline mode where story time advances faster than real time (e.g., 4x means 15 real minutes = 1 story hour). |
| **Story-only** | Timeline mode with no real-time clock; only Story Time is displayed per inject. |
| **Time Scale** | Compression ratio for Compressed timeline mode. Multiplier applied to elapsed time to calculate Story Time. |

---

## Data Model

### Exercise Entity Additions

```csharp
/// <summary>
/// Delivery mode determines how injects transition to Ready status.
/// </summary>
public enum DeliveryMode
{
    /// <summary>
    /// Injects become Ready when exercise clock reaches DeliveryTime.
    /// </summary>
    ClockDriven = 0,
    
    /// <summary>
    /// Injects are fired manually by Controller in Sequence order.
    /// </summary>
    FacilitatorPaced = 1
}

/// <summary>
/// Timeline mode determines how exercise time relates to story time.
/// </summary>
public enum TimelineMode
{
    /// <summary>
    /// 1:1 ratio - exercise time matches wall clock.
    /// </summary>
    RealTime = 0,
    
    /// <summary>
    /// Story time advances faster than real time per TimeScale.
    /// </summary>
    Compressed = 1,
    
    /// <summary>
    /// No real-time clock; only Story Time is used.
    /// </summary>
    StoryOnly = 2
}

public class Exercise
{
    // ... existing properties ...
    
    public DeliveryMode DeliveryMode { get; set; } = DeliveryMode.ClockDriven;
    public TimelineMode TimelineMode { get; set; } = TimelineMode.RealTime;
    
    /// <summary>
    /// Time compression ratio. Only used when TimelineMode = Compressed.
    /// Example: 4.0 means 1 real minute = 4 story minutes.
    /// </summary>
    public decimal? TimeScale { get; set; }
}
```

### Inject Entity Additions/Clarifications

```csharp
public class Inject
{
    // ... existing properties ...
    
    /// <summary>
    /// Sequential order for inject delivery. Always visible.
    /// Primary ordering for FacilitatorPaced mode.
    /// </summary>
    public int SequenceNumber { get; set; }
    
    /// <summary>
    /// Elapsed time from exercise start when inject should be delivered.
    /// Used in ClockDriven mode. Format: TimeSpan from 00:00:00.
    /// </summary>
    public TimeSpan? DeliveryTime { get; set; }
    
    /// <summary>
    /// Day number in the scenario narrative (1-based).
    /// </summary>
    public int? StoryDay { get; set; }
    
    /// <summary>
    /// Time of day in the scenario narrative.
    /// Combined with StoryDay for full Story Time.
    /// </summary>
    public TimeOnly? StoryTime { get; set; }
}
```

---

## API Endpoints

### Update Exercise Configuration

```http
PATCH /api/exercises/{exerciseId}
Content-Type: application/json

{
  "deliveryMode": "ClockDriven",
  "timelineMode": "Compressed",
  "timeScale": 4.0
}
```

**Response:** `200 OK` with updated Exercise DTO

**Validation:**
- `deliveryMode` must be "ClockDriven" or "FacilitatorPaced"
- `timelineMode` must be "RealTime", "Compressed", or "StoryOnly"
- `timeScale` required when `timelineMode` = "Compressed"; must be > 0 and ≤ 60
- Exercise must be in Draft or Stopped status to modify timing settings (read-only when Active)

### Get Exercise Timing Configuration

```http
GET /api/exercises/{exerciseId}/timing
```

**Response:**
```json
{
  "deliveryMode": "ClockDriven",
  "timelineMode": "Compressed",
  "timeScale": 4.0,
  "isLocked": false,
  "clockState": {
    "status": "Running",
    "elapsedTime": "00:32:15",
    "currentStoryTime": "Day 1 18:08"
  }
}
```

---

## Technical Notes

### Clock State Calculation (Compressed Mode)

```typescript
function calculateStoryTime(
  elapsedMs: number, 
  timeScale: number, 
  exerciseStartStoryTime: { day: number, time: string }
): { day: number, time: string } {
  const storyMs = elapsedMs * timeScale;
  const storyMinutes = Math.floor(storyMs / 60000);
  
  // Parse start time
  const [startHour, startMin] = exerciseStartStoryTime.time.split(':').map(Number);
  const startTotalMinutes = exerciseStartStoryTime.day * 1440 + startHour * 60 + startMin;
  
  // Calculate current story time
  const currentTotalMinutes = startTotalMinutes + storyMinutes;
  const currentDay = Math.floor(currentTotalMinutes / 1440);
  const remainingMinutes = currentTotalMinutes % 1440;
  const currentHour = Math.floor(remainingMinutes / 60);
  const currentMinute = remainingMinutes % 60;
  
  return {
    day: currentDay,
    time: `${String(currentHour).padStart(2, '0')}:${String(currentMinute).padStart(2, '0')}`
  };
}
```

### Real-Time Sync Consideration

Clock state should be managed server-side to ensure consistency across clients:
- Exercise clock start/pause/resume events broadcast via SignalR
- Clients calculate local display based on server timestamp + elapsed offset
- Reconnecting clients request current clock state and reconcile

---

## Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| MVP-D (Exercise CRUD) | ✅ Complete | Exercise entity exists |
| MVP-E (Inject CRUD) | ✅ Complete | Inject entity exists |
| MVP-J (Exercise Clock) | 🔲 In Progress | Clock controls being built |
| MVP-O (Real-Time Sync) | ✅ Complete | SignalR infrastructure ready |

---

## Design Decisions

| Decision | Answer | Rationale |
|----------|--------|-----------|
| TimeScale maximum value | **60x** (1 real minute = 1 story hour) | Generous enough for any realistic exercise; prevents absurd values |
| TimeScale changes during Active | **Disallowed** | Changing compression mid-exercise would confuse Controllers about inject timing |
| Delivery Mode changes during Active | **Disallowed** | Switching modes mid-exercise would be chaotic; fundamental design shouldn't change on the fly |

**Implementation:** Timing configuration fields (DeliveryMode, TimelineMode, TimeScale) should be **read-only when Exercise.Status = Active**. Users must stop/restart the exercise to modify these settings.

---

## Version History

| Date | Author | Changes |
|------|--------|---------|
| 2026-01-20 | Claude | Initial requirements from analysis session |
| 2026-01-20 | Claude | Finalized design decisions: TimeScale max 60x, timing settings locked during Active |
