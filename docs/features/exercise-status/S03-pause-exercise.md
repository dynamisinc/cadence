# exercise-status/S03: Pause Exercise (Active → Paused)

## Story

**As an** Exercise Director or Administrator,
**I want** to pause an active exercise when conduct needs to be temporarily suspended,
**So that** the exercise clock stops while preserving all progress and elapsed time.

## Context

During exercise conduct, real-world events may require temporary suspension:
- Player safety incidents requiring immediate attention
- Technical failures (communication systems down)
- Weather delays for field exercises
- Unplanned breaks (lunch extension, facility issues)
- SimCell needs time to prepare for complex inject sequence

Pausing differs from completing the exercise:
- **Pause:** Temporary hold, can resume conduct, clock preserves elapsed time
- **Complete:** Permanent end of conduct, clock cannot restart

The pause action is lightweight (no confirmation dialog) because it's non-destructive and can be easily undone by resuming.

## Acceptance Criteria

### Pause Action Availability

- [ ] **Given** I am an Exercise Director or Administrator viewing an Active exercise, **when** I look at the exercise actions, **then** I see a "Pause Exercise" button
- [ ] **Given** I am a Controller, Evaluator, or Observer viewing an Active exercise, **when** I look at the exercise actions, **then** I do NOT see the "Pause Exercise" button
- [ ] **Given** an exercise is in Draft, Paused, Completed, or Archived status, **when** I view the exercise, **then** the "Pause Exercise" button is not visible

### Pause Action Behavior

- [ ] **Given** I click "Pause Exercise", **when** the action processes, **then** no confirmation dialog appears (immediate action)
- [ ] **Given** I pause an exercise, **when** the action completes, **then** the exercise status changes to "Paused"
- [ ] **Given** I pause an exercise, **when** the action completes, **then** the exercise clock state changes to "Paused"
- [ ] **Given** I pause an exercise, **when** the action completes, **then** the clock's elapsed time before pause is updated to preserve total elapsed time
- [ ] **Given** I pause an exercise, **when** the action completes, **then** I see a success message "Exercise paused"

### Status Badge Update

- [ ] **Given** the exercise is paused, **when** I view the exercise detail page, **then** the status badge shows "Paused" with yellow/orange color
- [ ] **Given** the exercise is paused, **when** I view the exercise in the list, **then** the status badge shows "Paused"

### Real-Time Notification

- [ ] **Given** another user is viewing the same exercise, **when** I pause it, **then** they receive a real-time update and see the status change to Paused without refreshing
- [ ] **Given** a Controller is viewing the MSEL, **when** the exercise is paused, **then** they see the status badge update and the clock stops

### Post-Pause Behavior

- [ ] **Given** an exercise is Paused, **when** Controllers view the MSEL, **then** they can still fire pending injects (conduct is paused, not locked)
- [ ] **Given** an exercise is Paused, **when** Evaluators view observations, **then** they can still add observations
- [ ] **Given** an exercise is Paused, **when** I view the exercise clock, **then** it displays the elapsed time but is not incrementing
- [ ] **Given** an exercise is Paused, **when** I look at exercise actions, **then** I see a "Resume Exercise" button instead of "Pause Exercise"
- [ ] **Given** an exercise is Paused, **when** I look at exercise actions, **then** I see "Complete Exercise" and "Revert to Draft" options in the menu

### Clock State Synchronization

- [ ] **Given** an exercise is Paused, **when** I query the exercise clock API, **then** the ClockState is "Paused"
- [ ] **Given** an exercise is Paused, **when** I view the clock elapsed time, **then** it reflects the total time accumulated before the pause
- [ ] **Given** an exercise is paused and resumed multiple times, **when** I view elapsed time, **then** it correctly sums all running periods

## Out of Scope

- Pause reason/notes (future enhancement)
- Auto-pause on inactivity
- Scheduled pause (pause at specific time)
- Pause notification emails
- Pause duration tracking (how long exercise has been paused)

## Dependencies

- exercise-status/S01: View Status (status badge must display Paused state)
- exercise-status/S02: Activate Exercise (must be Active to pause)
- exercise-clock: Clock pause logic (clock must preserve elapsed time)

## Open Questions

- [ ] Should we track total paused duration separately from elapsed time? (Recommendation: Future enhancement)
- [ ] Should we limit how long an exercise can be paused? (Recommendation: No limit for MVP)
- [ ] Should pausing display a reason field? (Recommendation: Future enhancement - not critical for MVP)

## Domain Terms

| Term | Definition |
|------|------------|
| Pause | Temporarily suspend exercise conduct without ending it |
| Elapsed Time | Total time the exercise clock has been running (excludes paused periods) |
| Clock State | Current state of the exercise timer (Running, Paused, Stopped) |
| Resume | Restart exercise conduct from a Paused state |

## UI/UX Notes

### Pause Button Placement

```
┌────────────────────────────────────────────────────────────┐
│  Exercise: Hurricane Response 2025              [⋮ Menu]   │
│  [Active]  📍 Houston, TX                                  │
│  Clock: 2:15:30 elapsed                                    │
│                                                            │
│  [Pause Exercise]  [Complete Exercise]  [View MSEL]       │
└────────────────────────────────────────────────────────────┘
```

### After Pause (Status Changes)

```
┌────────────────────────────────────────────────────────────┐
│  Exercise: Hurricane Response 2025              [⋮ Menu]   │
│  [Paused]  📍 Houston, TX                                  │
│  Clock: 2:15:30 elapsed (paused)                           │
│                                                            │
│  [Resume Exercise]  [Complete Exercise]  [View MSEL]       │
│                   ▼ More Actions ▼                         │
│                   - Revert to Draft                        │
└────────────────────────────────────────────────────────────┘
```

### Success Message (Toast/Snackbar)

```
⏸ Exercise paused
```

### Paused Exercise Indicator in MSEL View

```
┌────────────────────────────────────────────────────────────┐
│  MSEL: Hurricane Response 2025                   [Paused]  │
│  Clock: 2:15:30 elapsed (paused)                           │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ⚠ Exercise is paused. Click "Resume Exercise" to         │
│     continue conduct.                                      │
│                                                            │
│  #   Time    Description                    Status        │
│  1   +00:00  Initial briefing               Fired         │
│  2   +00:15  Simulated 911 call             Fired         │
│  3   +00:30  Power outage notification      Pending       │
│                                              [Fire]        │
└────────────────────────────────────────────────────────────┘
```

## Technical Notes

### Backend Implementation

**Endpoint:** `POST /api/exercises/{exerciseId}/pause`

**Service Logic:**
```csharp
public async Task<ExerciseDto> PauseExerciseAsync(Guid exerciseId, Guid userId)
{
    var exercise = await _context.Exercises
        .FirstOrDefaultAsync(e => e.Id == exerciseId);

    if (exercise == null)
        throw new NotFoundException("Exercise not found");

    if (exercise.Status != ExerciseStatus.Active)
        throw new InvalidOperationException($"Cannot pause exercise in {exercise.Status} status");

    // Update status
    exercise.Status = ExerciseStatus.Paused;

    // Pause clock and preserve elapsed time
    exercise.ClockState = ExerciseClockState.Paused;

    // Calculate elapsed time before this pause
    if (exercise.ClockStartedAt.HasValue)
    {
        var currentElapsed = DateTime.UtcNow - exercise.ClockStartedAt.Value;
        exercise.ClockElapsedBeforePause = (exercise.ClockElapsedBeforePause ?? TimeSpan.Zero) + currentElapsed;
        exercise.ClockStartedAt = null; // Clear start time while paused
    }

    await _context.SaveChangesAsync();

    // Broadcast status change
    await _hubContext.NotifyExerciseStatusChanged(exerciseId, ExerciseStatus.Paused);
    await _hubContext.NotifyClockPaused(exerciseId, exercise.ClockElapsedBeforePause.Value);

    return _mapper.Map<ExerciseDto>(exercise);
}
```

### Frontend Implementation

**Hook:** `src/frontend/src/features/exercises/hooks/usePauseExercise.ts`

```typescript
export const usePauseExercise = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (exerciseId: string) => {
      const response = await api.post(`/exercises/${exerciseId}/pause`);
      return response.data;
    },
    onSuccess: (data, exerciseId) => {
      queryClient.invalidateQueries(['exercises', exerciseId]);
      queryClient.invalidateQueries(['exercises']);
    },
  });
};
```

**Component:** Update ExerciseActions component

```typescript
const { mutate: pauseExercise, isLoading: isPausing } = usePauseExercise();

const handlePause = () => {
  pauseExercise(exercise.id, {
    onSuccess: () => {
      showSnackbar('Exercise paused', 'info');
    },
    onError: () => {
      showSnackbar('Failed to pause exercise', 'error');
    }
  });
};

// Render button conditionally
{exercise.status === 'Active' && canManageExercise && (
  <CobraPrimaryButton
    onClick={handlePause}
    disabled={isPausing}
    startIcon={<FontAwesomeIcon icon={faPause} />}
  >
    Pause Exercise
  </CobraPrimaryButton>
)}
```

### SignalR Events

**Event Name:** `ExerciseStatusChanged`

**Payload:**
```typescript
{
  exerciseId: string;
  newStatus: ExerciseStatus; // "Paused"
  timestamp: string; // ISO timestamp
}
```

**Event Name:** `ClockPaused`

**Payload:**
```typescript
{
  exerciseId: string;
  elapsedTime: string; // ISO duration (e.g., "PT2H15M30S")
  timestamp: string;
}
```

### Validation Rules

1. **Status Check:** Current status MUST be Active
2. **Permission Check:** User MUST be Administrator or Exercise Director
3. **Clock State:** Clock must be Running before pausing

### No Database Schema Changes

This story reuses existing fields:
- `Exercise.Status` (enum already supports Paused value - needs to be added)
- `Exercise.ClockState` (already exists)
- `Exercise.ClockElapsedBeforePause` (already exists)

### Enum Update Required

```csharp
// src/Cadence.Core/Models/Entities/Enums.cs
public enum ExerciseStatus
{
    Draft,
    Active,
    Paused,      // ADD THIS
    Completed,
    Archived
}
```

---

**Acceptance Criteria Checklist:** 17 criteria
**Estimated Effort:** 0.5 days (backend endpoint + frontend component + tests)
**Testing:** Unit tests for service logic, integration tests for API endpoint, verify clock elapsed time calculation
