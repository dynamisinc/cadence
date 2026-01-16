# exercise-status/S04: Complete Exercise (Active/Paused → Completed)

## Story

**As an** Exercise Director or Administrator,
**I want** to mark an exercise as completed when conduct has finished,
**So that** the exercise transitions to after-action review phase and the clock is permanently stopped.

## Context

Completing an exercise is the formal end of the conduct phase. This transition:
- Stops the exercise clock permanently (cannot be restarted)
- Locks inject firing (Controllers can no longer fire injects)
- Preserves all data for after-action review (AAR)
- Allows continued observation recording (Evaluators can add/edit observations)
- Signals that the exercise is ready for review and analysis

Completion can occur from either Active or Paused status. Once completed, the exercise cannot return to Active/Paused status - the only next transition is to Archived.

This is a deliberate action requiring confirmation because it permanently ends conduct.

## Acceptance Criteria

### Complete Action Availability

- [ ] **Given** I am an Exercise Director or Administrator viewing an Active exercise, **when** I look at the exercise actions, **then** I see a "Complete Exercise" button
- [ ] **Given** I am an Exercise Director or Administrator viewing a Paused exercise, **when** I look at the exercise actions, **then** I see a "Complete Exercise" button
- [ ] **Given** I am a Controller, Evaluator, or Observer viewing an Active/Paused exercise, **when** I look at the exercise actions, **then** I do NOT see the "Complete Exercise" button
- [ ] **Given** an exercise is in Draft, Completed, or Archived status, **when** I view the exercise, **then** the "Complete Exercise" button is not visible

### Completion Confirmation Dialog

- [ ] **Given** I click "Complete Exercise", **when** the confirmation dialog appears, **then** I see the title "Complete Exercise"
- [ ] **Given** the confirmation dialog is open, **when** I read the message, **then** I see "Mark '[Exercise Name]' as completed? The exercise clock will stop permanently and conduct will end."
- [ ] **Given** the confirmation dialog is open, **when** I look at the details, **then** I see a note "Observations can still be added/edited for after-action review"
- [ ] **Given** the confirmation dialog is open, **when** I look at the actions, **then** I see two buttons: "Cancel" and "Complete"
- [ ] **Given** I click "Cancel", **when** the dialog closes, **then** the exercise remains in its current status (Active or Paused) and no changes are made
- [ ] **Given** I click "Complete", **when** the action processes, **then** the dialog shows a loading indicator

### Successful Completion

- [ ] **Given** I confirm completion, **when** the action completes successfully, **then** the exercise status changes to "Completed"
- [ ] **Given** the exercise is completed, **when** the action completes, **then** the exercise clock state changes to "Stopped"
- [ ] **Given** the exercise is completed, **when** the action completes, **then** the `CompletedAt` timestamp is set to current UTC time
- [ ] **Given** the exercise is completed, **when** the action completes, **then** the `CompletedBy` field is set to my user ID
- [ ] **Given** the exercise is completed, **when** the action completes, **then** I see a success message "Exercise completed successfully"
- [ ] **Given** the exercise is completed, **when** I view the exercise detail page, **then** the status badge shows "Completed" with gray color

### Real-Time Notification

- [ ] **Given** another user is viewing the same exercise, **when** I complete it, **then** they receive a real-time update and see the status change to Completed without refreshing
- [ ] **Given** a Controller is viewing the MSEL, **when** the exercise is completed, **then** they see the status badge update and all "Fire" buttons disappear

### Post-Completion Behavior

- [ ] **Given** an exercise is Completed, **when** Controllers view the MSEL, **then** they cannot fire any injects (all "Fire" buttons are hidden)
- [ ] **Given** an exercise is Completed, **when** Evaluators view observations, **then** they can still add and edit observations
- [ ] **Given** an exercise is Completed, **when** I view the exercise clock, **then** it displays the final elapsed time and is stopped
- [ ] **Given** an exercise is Completed, **when** I attempt to edit inject details, **then** editing is disabled with message "Exercise is completed. Data is read-only."
- [ ] **Given** an exercise is Completed, **when** I look at exercise actions, **then** I see "Archive Exercise" button (if Director/Admin)
- [ ] **Given** an exercise is Completed, **when** I look at exercise actions, **then** I do NOT see "Resume Exercise" or "Pause Exercise" buttons

### Clock Finalization

- [ ] **Given** an exercise is completed from Active status, **when** the clock stops, **then** the final elapsed time includes the current running period
- [ ] **Given** an exercise is completed from Paused status, **when** the clock stops, **then** the final elapsed time reflects the elapsed time before pause (does not include paused time)
- [ ] **Given** an exercise is Completed, **when** I query the exercise API, **then** the response includes the final `elapsedTime` value

## Out of Scope

- Auto-complete when all injects are fired (future enhancement)
- Completion checklist (verify all objectives met)
- Completion notes/summary field
- Email notifications on completion
- Completion report generation

## Dependencies

- exercise-status/S01: View Status (status badge must display Completed state)
- exercise-status/S02: Activate Exercise (must be Active or Paused to complete)
- exercise-status/S03: Pause Exercise (can complete from Paused status)
- exercise-clock: Clock stop logic (clock must finalize elapsed time)

## Open Questions

- [ ] Should we show a completion summary (total injects fired, observations recorded)? (Recommendation: Future enhancement)
- [ ] Should completion require entering a final report or summary? (Recommendation: No - AAR is separate)
- [ ] Should we validate that all injects are fired before allowing completion? (Recommendation: No - Director may intentionally skip injects)

## Domain Terms

| Term | Definition |
|------|------------|
| Complete | Permanent end of exercise conduct, transitioning to AAR phase |
| After-Action Review (AAR) | Post-exercise analysis phase where observations are reviewed |
| Final Elapsed Time | Total time the exercise clock was running (excludes paused periods) |
| Conduct Lock | Restriction preventing further inject firing after completion |

## UI/UX Notes

### Complete Button Placement

```
┌────────────────────────────────────────────────────────────┐
│  Exercise: Hurricane Response 2025              [⋮ Menu]   │
│  [Active]  📍 Houston, TX                                  │
│  Clock: 3:45:20 elapsed                                    │
│                                                            │
│  [Pause Exercise]  [Complete Exercise]  [View MSEL]       │
└────────────────────────────────────────────────────────────┘
```

### Completion Confirmation Dialog

```
┌─────────────────────────────────────────────────────┐
│  Complete Exercise                                  │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Mark "Hurricane Response 2025" as completed?       │
│                                                     │
│  • The exercise clock will stop permanently         │
│  • Conduct will end (no more inject firing)         │
│  • Observations can still be added/edited           │
│                                                     │
│  Final elapsed time: 3:45:20                        │
│                                                     │
│                       [Cancel]  [Complete]          │
└─────────────────────────────────────────────────────┘
```

### After Completion (Status Changes)

```
┌────────────────────────────────────────────────────────────┐
│  Exercise: Hurricane Response 2025              [⋮ Menu]   │
│  [Completed]  📍 Houston, TX                               │
│  Final Time: 3:45:20 | Completed Jan 15, 2026 5:00 PM     │
│                                                            │
│  [View AAR]  [Archive Exercise]  [View MSEL]              │
└────────────────────────────────────────────────────────────┘
```

### Success Message (Toast/Snackbar)

```
✓ Exercise completed successfully
```

### Completed Exercise Indicator in MSEL View

```
┌────────────────────────────────────────────────────────────┐
│  MSEL: Hurricane Response 2025                 [Completed] │
│  Final Time: 3:45:20                                       │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ℹ Exercise is completed. Conduct has ended.              │
│                                                            │
│  #   Time    Description                    Status        │
│  1   +00:00  Initial briefing               Fired         │
│  2   +00:15  Simulated 911 call             Fired         │
│  3   +00:30  Power outage notification      Fired         │
│  4   +01:00  Hospital evacuation            Skipped       │
└────────────────────────────────────────────────────────────┘
```

## Technical Notes

### Backend Implementation

**Endpoint:** `POST /api/exercises/{exerciseId}/complete`

**Service Logic:**
```csharp
public async Task<ExerciseDto> CompleteExerciseAsync(Guid exerciseId, Guid userId)
{
    var exercise = await _context.Exercises
        .FirstOrDefaultAsync(e => e.Id == exerciseId);

    if (exercise == null)
        throw new NotFoundException("Exercise not found");

    if (exercise.Status != ExerciseStatus.Active && exercise.Status != ExerciseStatus.Paused)
        throw new InvalidOperationException($"Cannot complete exercise in {exercise.Status} status");

    // Calculate final elapsed time
    TimeSpan finalElapsedTime;
    if (exercise.ClockState == ExerciseClockState.Running && exercise.ClockStartedAt.HasValue)
    {
        // If clock is running, add current running period to accumulated time
        var currentPeriod = DateTime.UtcNow - exercise.ClockStartedAt.Value;
        finalElapsedTime = (exercise.ClockElapsedBeforePause ?? TimeSpan.Zero) + currentPeriod;
    }
    else
    {
        // If clock is paused, use accumulated time only
        finalElapsedTime = exercise.ClockElapsedBeforePause ?? TimeSpan.Zero;
    }

    // Update status
    exercise.Status = ExerciseStatus.Completed;
    exercise.CompletedAt = DateTime.UtcNow;
    exercise.CompletedBy = userId;

    // Stop clock permanently
    exercise.ClockState = ExerciseClockState.Stopped;
    exercise.ClockElapsedBeforePause = finalElapsedTime;
    exercise.ClockStartedAt = null;

    await _context.SaveChangesAsync();

    // Broadcast status change
    await _hubContext.NotifyExerciseStatusChanged(exerciseId, ExerciseStatus.Completed);
    await _hubContext.NotifyClockStopped(exerciseId, finalElapsedTime);

    return _mapper.Map<ExerciseDto>(exercise);
}
```

### Frontend Implementation

**Hook:** `src/frontend/src/features/exercises/hooks/useCompleteExercise.ts`

```typescript
export const useCompleteExercise = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (exerciseId: string) => {
      const response = await api.post(`/exercises/${exerciseId}/complete`);
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
const { mutate: completeExercise, isLoading: isCompleting } = useCompleteExercise();

const handleComplete = async () => {
  const confirmed = await showConfirmDialog({
    title: 'Complete Exercise',
    message: `Mark "${exercise.name}" as completed?`,
    details: [
      'The exercise clock will stop permanently',
      'Conduct will end (no more inject firing)',
      'Observations can still be added/edited',
      '',
      `Final elapsed time: ${formatDuration(exercise.elapsedTime)}`
    ],
    confirmText: 'Complete',
    cancelText: 'Cancel',
    severity: 'warning'
  });

  if (confirmed) {
    completeExercise(exercise.id, {
      onSuccess: () => {
        showSnackbar('Exercise completed successfully', 'success');
      },
      onError: () => {
        showSnackbar('Failed to complete exercise', 'error');
      }
    });
  }
};

// Render button conditionally
{(exercise.status === 'Active' || exercise.status === 'Paused') && canManageExercise && (
  <CobraPrimaryButton
    onClick={handleComplete}
    disabled={isCompleting}
    startIcon={<FontAwesomeIcon icon={faCheckCircle} />}
  >
    Complete Exercise
  </CobraPrimaryButton>
)}
```

### SignalR Events

**Event Name:** `ExerciseStatusChanged`

**Payload:**
```typescript
{
  exerciseId: string;
  newStatus: ExerciseStatus; // "Completed"
  completedAt: string; // ISO timestamp
  completedBy: string; // User ID
  finalElapsedTime: string; // ISO duration (e.g., "PT3H45M20S")
}
```

**Event Name:** `ClockStopped`

**Payload:**
```typescript
{
  exerciseId: string;
  finalElapsedTime: string; // ISO duration
  timestamp: string;
}
```

### Validation Rules

1. **Status Check:** Current status MUST be Active or Paused
2. **Permission Check:** User MUST be Administrator or Exercise Director
3. **No reversal:** Once completed, cannot return to Active/Paused

### Database Changes Required

**Migration:** Add CompletedAt and CompletedBy fields
```sql
ALTER TABLE Exercises ADD CompletedAt datetime2 NULL;
ALTER TABLE Exercises ADD CompletedBy uniqueidentifier NULL;

ALTER TABLE Exercises
ADD CONSTRAINT FK_Exercises_CompletedBy_Users
FOREIGN KEY (CompletedBy) REFERENCES Users(Id);
```

---

**Acceptance Criteria Checklist:** 22 criteria
**Estimated Effort:** 1 day (backend endpoint + frontend component + tests + confirmation dialog)
**Testing:** Unit tests for service logic with both Active→Completed and Paused→Completed paths, integration tests, verify clock finalization
