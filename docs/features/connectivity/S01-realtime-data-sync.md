# S01: Real-Time Data Sync

## Story

**As a** Controller or Evaluator participating in an exercise
**I want to** see updates from other users in real-time
**So that** I have an accurate view of exercise status without refreshing

## Priority

P0 - MVP Required

## Acceptance Criteria

### AC1: SignalR Connection Management
- [x] Client automatically connects to SignalR hub when entering Conduct page
- [x] Client joins exercise-specific group on connection
- [x] Client leaves exercise group when navigating away
- [x] Connection automatically reconnects on temporary disconnection

### AC2: Clock State Synchronization
- [x] When Exercise Director starts clock, all users see clock running within 1 second
- [x] When Exercise Director pauses clock, all users see clock paused within 1 second
- [x] When Exercise Director stops/resets clock, all users see updated state within 1 second
- [x] Clock time display stays synchronized across all clients

### AC3: Inject Status Synchronization
- [x] When Controller fires an inject, all users see status change to "Delivered" within 1 second
- [x] When Controller skips an inject, all users see status change to "Skipped" within 1 second
- [x] When inject is reset, all users see status change to "Pending" within 1 second
- [x] Inject list updates without full page refresh

### AC4: Observation Synchronization
- [x] When Evaluator creates observation, all users see it appear within 1 second
- [x] When Evaluator updates observation, all users see changes within 1 second
- [x] When Evaluator deletes observation, all users see it removed within 1 second

### AC5: Connection State Indicator
- [x] Visual indicator shows connection state (connected/connecting/disconnected)
- [x] Indicator visible in header or status bar area
- [x] Green = connected, Yellow = connecting/reconnecting, Red = disconnected

## Technical Notes

### SignalR Events to Broadcast

| Event | Trigger | Payload |
|-------|---------|---------|
| `ClockStarted` | Clock starts | `ClockStateDto` |
| `ClockPaused` | Clock pauses | `ClockStateDto` |
| `ClockStopped` | Clock stops/resets | `ClockStateDto` |
| `InjectFired` | Inject fired | `InjectDto` |
| `InjectSkipped` | Inject skipped | `InjectDto` |
| `InjectReset` | Inject reset | `InjectDto` |
| `InjectStatusChanged` | Any status change | `InjectDto` |
| `ObservationAdded` | Observation created | `ObservationDto` |
| `ObservationUpdated` | Observation updated | `ObservationDto` |
| `ObservationDeleted` | Observation deleted | `Guid observationId` |

### Existing Infrastructure

- `ExerciseHub` - SignalR hub with JoinExercise/LeaveExercise
- `IExerciseHubContext` - Interface for broadcasting from services
- `useSignalR` hook - Generic connection management
- `useExerciseSignalR` hook - Exercise-specific with group management

### Gap to Fill

- `ExerciseClockService` does not currently inject `IExerciseHubContext`
- Clock events are defined but never broadcast
- Need to add hub context injection and broadcast calls

## Out of Scope

- Offline handling (S02-S05)
- Conflict resolution (S06)
- Azure SignalR Service configuration (infrastructure task)

## Test Scenarios

1. Open exercise in two browser tabs, fire inject in Tab A, verify Tab B updates
2. Open exercise in two tabs, start clock in Tab A, verify Tab B shows running
3. Open exercise in two tabs, add observation in Tab A, verify Tab B shows it
4. Disconnect network briefly, verify reconnection and group rejoin
5. Navigate away from Conduct page, verify group leave is called
