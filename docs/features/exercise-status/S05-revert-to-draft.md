# exercise-status/S05: Revert to Draft (Paused → Draft)

## Story

**As an** Exercise Director or Administrator,
**I want** to revert a paused exercise back to draft status,
**So that** I can undo an exercise that was activated prematurely and return to planning mode.

## Context

Sometimes exercises are activated before they're truly ready:
- Exercise activated accidentally (wrong exercise, wrong date)
- Major MSEL errors discovered during conduct requiring significant rework
- Exercise needs to be postponed and reconvened at a later date with different setup
- Technical issues prevent proper conduct, requiring return to planning

Reverting to Draft is a **destructive operation** that:
- Resets all inject statuses to Pending (clears fired/skipped states)
- Deletes all observations recorded during the aborted conduct
- Resets the exercise clock to zero elapsed time
- Returns the exercise to fully editable mode

This operation is **only available from Paused status** (not Active) to prevent accidental data loss during live conduct. Exercise Directors must pause the exercise first, then deliberately choose to revert.

This requires strong confirmation with explicit data loss warning.

## Acceptance Criteria

### Revert Action Availability

- [ ] **Given** I am an Exercise Director or Administrator viewing a Paused exercise, **when** I look at the exercise actions menu, **then** I see a "Revert to Draft" option
- [ ] **Given** I am a Controller, Evaluator, or Observer viewing a Paused exercise, **when** I look at the exercise actions, **then** I do NOT see the "Revert to Draft" option
- [ ] **Given** an exercise is in Draft, Active, Completed, or Archived status, **when** I view the exercise, **then** the "Revert to Draft" option is not available
- [ ] **Given** an exercise is in Active status, **when** I look for "Revert to Draft", **then** it is not visible (must pause first)

### Revert Confirmation Dialog

- [ ] **Given** I click "Revert to Draft", **when** the confirmation dialog appears, **then** I see the title "Revert to Draft - Data Loss Warning"
- [ ] **Given** the confirmation dialog is open, **when** I read the message, **then** I see "This will reset '[Exercise Name]' back to draft status and delete all conduct data."
- [ ] **Given** the confirmation dialog is open, **when** I look at the warning details, **then** I see a bulleted list of what will be deleted:
  - "All inject statuses will reset to Pending"
  - "All fired/skipped inject records will be cleared"
  - "All observations will be permanently deleted"
  - "Exercise clock will reset to 00:00:00"
- [ ] **Given** the confirmation dialog is open, **when** I look at the severity, **then** the dialog displays a red/error color scheme indicating danger
- [ ] **Given** the confirmation dialog is open, **when** I look at the actions, **then** I see two buttons: "Cancel" and "Revert to Draft"
- [ ] **Given** the confirmation dialog is open, **when** I look at the confirm button, **then** it is styled as a destructive action (red/danger color)
- [ ] **Given** I click "Cancel", **when** the dialog closes, **then** the exercise remains in Paused status and no data is deleted

### Data Deletion Confirmation Checkbox

- [ ] **Given** the confirmation dialog is open, **when** I look for confirmation controls, **then** I see a checkbox labeled "I understand this will permanently delete all conduct data"
- [ ] **Given** the confirmation checkbox is unchecked, **when** I look at the "Revert to Draft" button, **then** it is disabled
- [ ] **Given** I check the confirmation checkbox, **when** the button state updates, **then** the "Revert to Draft" button becomes enabled
- [ ] **Given** I check the confirmation checkbox and click "Revert to Draft", **when** the action processes, **then** the dialog shows a loading indicator

### Successful Revert

- [ ] **Given** I confirm the revert action, **when** the action completes successfully, **then** the exercise status changes to "Draft"
- [ ] **Given** the exercise is reverted, **when** the action completes, **then** the exercise clock state changes to "Stopped"
- [ ] **Given** the exercise is reverted, **when** the action completes, **then** the `ClockElapsedBeforePause` is reset to null or zero
- [ ] **Given** the exercise is reverted, **when** the action completes, **then** the `ClockStartedAt` is cleared
- [ ] **Given** the exercise is reverted, **when** the action completes, **then** the `ActivatedAt` and `ActivatedBy` fields are cleared
- [ ] **Given** the exercise is reverted, **when** the action completes, **then** I see a success message "Exercise reverted to draft. All conduct data has been deleted."

### Inject Status Reset

- [ ] **Given** the exercise had fired injects, **when** revert completes, **then** all inject statuses are set to "Pending"
- [ ] **Given** the exercise had skipped injects, **when** revert completes, **then** all inject statuses are set to "Pending"
- [ ] **Given** injects had `FiredAt` timestamps, **when** revert completes, **then** all `FiredAt` timestamps are cleared
- [ ] **Given** injects had `FiredBy` user references, **when** revert completes, **then** all `FiredBy` fields are cleared

### Observation Deletion

- [ ] **Given** the exercise had observations, **when** revert completes, **then** all observations are permanently deleted from the database
- [ ] **Given** Evaluators recorded 10 observations during conduct, **when** revert completes, **then** the observation count is 0

### Real-Time Notification

- [ ] **Given** another user is viewing the same exercise, **when** I revert it to draft, **then** they receive a real-time update and see the status change to Draft without refreshing
- [ ] **Given** a Controller is viewing the MSEL, **when** the exercise is reverted, **then** they see all inject statuses reset to Pending

### Post-Revert Behavior

- [ ] **Given** an exercise is reverted to Draft, **when** I view the exercise detail page, **then** the status badge shows "Draft" with blue color
- [ ] **Given** an exercise is reverted to Draft, **when** I view the MSEL, **then** all injects show "Pending" status
- [ ] **Given** an exercise is reverted to Draft, **when** I view observations, **then** no observations exist
- [ ] **Given** an exercise is reverted to Draft, **when** I look at exercise actions, **then** I see "Activate Exercise" button again
- [ ] **Given** an exercise is reverted to Draft, **when** I attempt to edit exercise details, **then** full editing is enabled

## Out of Scope

- Revert from Active status (must pause first - safety measure)
- Revert from Completed status (permanent state)
- Undo revert (recover deleted data) - data loss is permanent
- Archive conduct data before revert (future enhancement)
- Revert reason tracking (audit log only)

## Dependencies

- exercise-status/S01: View Status (status badge must display Draft state after revert)
- exercise-status/S03: Pause Exercise (must be Paused to revert)
- inject-crud: Inject status management (reset inject statuses)
- observations: Observation deletion (delete all observations)

## Open Questions

- [ ] Should we offer to export conduct data before reverting? (Recommendation: Future enhancement - not MVP)
- [ ] Should revert create an audit log entry with reason field? (Recommendation: Yes - audit only, no user input)
- [ ] Should we soft-delete observations instead of hard-delete? (Recommendation: Hard delete for MVP - no recovery)

## Domain Terms

| Term | Definition |
|------|------------|
| Revert | Return exercise from Paused status back to Draft, deleting all conduct data |
| Conduct Data | All data generated during exercise execution (fired injects, observations, clock time) |
| Destructive Operation | Action that permanently deletes data with no undo capability |
| Data Loss Warning | Explicit notification that data will be permanently deleted |

## UI/UX Notes

### Revert Action Menu Item

```
┌────────────────────────────────────────────────────────────┐
│  Exercise: Hurricane Response 2025              [⋮ Menu]   │
│  [Paused]  📍 Houston, TX                                  │
│  Clock: 0:15:30 elapsed (paused)                           │
│                                                            │
│  [Resume Exercise]  [Complete Exercise]  [▼ More Actions] │
│                                          │                 │
│                                          ├─ Revert to Draft│
│                                          └─ View Settings  │
└────────────────────────────────────────────────────────────┘
```

### Revert Confirmation Dialog

```
┌──────────────────────────────────────────────────────┐
│  ⚠ Revert to Draft - Data Loss Warning              │
├──────────────────────────────────────────────────────┤
│                                                      │
│  This will reset "Hurricane Response 2025" back     │
│  to draft status and delete all conduct data.       │
│                                                      │
│  The following will be permanently deleted:          │
│                                                      │
│  • All inject statuses will reset to Pending        │
│  • All fired/skipped inject records will be cleared │
│  • All observations will be permanently deleted     │
│  • Exercise clock will reset to 00:00:00            │
│                                                      │
│  ⚠ This action cannot be undone.                    │
│                                                      │
│  [✓] I understand this will permanently delete      │
│      all conduct data                               │
│                                                      │
│                  [Cancel]  [Revert to Draft]        │
│                            (Red/Danger Button)       │
└──────────────────────────────────────────────────────┘
```

### Success Message (Toast/Snackbar)

```
⚠ Exercise reverted to draft. All conduct data has been deleted.
```

### After Revert (Status Changes)

```
┌────────────────────────────────────────────────────────────┐
│  Exercise: Hurricane Response 2025              [⋮ Menu]   │
│  [Draft]  📍 Houston, TX                                   │
│  Jan 15, 2026 | 9:00 AM - 5:00 PM                          │
│                                                            │
│  [Activate Exercise]  [Edit Details]  [View MSEL]         │
└────────────────────────────────────────────────────────────┘
```

## Technical Notes

### Backend Implementation

**Endpoint:** `POST /api/exercises/{exerciseId}/revert-to-draft`

**Service Logic:**
```csharp
public async Task<ExerciseDto> RevertToDraftAsync(Guid exerciseId, Guid userId)
{
    var exercise = await _context.Exercises
        .Include(e => e.ActiveMsel)
            .ThenInclude(m => m.Injects)
        .Include(e => e.Observations)
        .FirstOrDefaultAsync(e => e.Id == exerciseId);

    if (exercise == null)
        throw new NotFoundException("Exercise not found");

    if (exercise.Status != ExerciseStatus.Paused)
        throw new InvalidOperationException($"Can only revert from Paused status. Current status: {exercise.Status}");

    // Begin transaction for data consistency
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // 1. Reset exercise status
        exercise.Status = ExerciseStatus.Draft;
        exercise.ActivatedAt = null;
        exercise.ActivatedBy = null;

        // 2. Reset clock
        exercise.ClockState = ExerciseClockState.Stopped;
        exercise.ClockStartedAt = null;
        exercise.ClockStartedBy = null;
        exercise.ClockElapsedBeforePause = null;

        // 3. Reset all inject statuses to Pending
        if (exercise.ActiveMsel?.Injects != null)
        {
            foreach (var inject in exercise.ActiveMsel.Injects)
            {
                inject.Status = InjectStatus.Pending;
                inject.FiredAt = null;
                inject.FiredBy = null;
            }
        }

        // 4. Delete all observations (hard delete)
        if (exercise.Observations.Any())
        {
            _context.Observations.RemoveRange(exercise.Observations);
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // Broadcast status change
        await _hubContext.NotifyExerciseStatusChanged(exerciseId, ExerciseStatus.Draft);
        await _hubContext.NotifyExerciseReverted(exerciseId);

        return _mapper.Map<ExerciseDto>(exercise);
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### Frontend Implementation

**Hook:** `src/frontend/src/features/exercises/hooks/useRevertToDraft.ts`

```typescript
export const useRevertToDraft = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (exerciseId: string) => {
      const response = await api.post(`/exercises/${exerciseId}/revert-to-draft`);
      return response.data;
    },
    onSuccess: (data, exerciseId) => {
      queryClient.invalidateQueries(['exercises', exerciseId]);
      queryClient.invalidateQueries(['exercises']);
      queryClient.invalidateQueries(['injects', exerciseId]);
      queryClient.invalidateQueries(['observations', exerciseId]);
    },
  });
};
```

**Component:** Create RevertToDraftDialog component

```typescript
const [confirmChecked, setConfirmChecked] = useState(false);
const { mutate: revertToDraft, isLoading } = useRevertToDraft();

const handleRevert = () => {
  revertToDraft(exercise.id, {
    onSuccess: () => {
      showSnackbar('Exercise reverted to draft. All conduct data has been deleted.', 'warning');
      onClose();
    },
    onError: () => {
      showSnackbar('Failed to revert exercise', 'error');
    }
  });
};

return (
  <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
    <DialogTitle sx={{ color: 'error.main' }}>
      <FontAwesomeIcon icon={faExclamationTriangle} />
      Revert to Draft - Data Loss Warning
    </DialogTitle>
    <DialogContent>
      <Typography variant="body1" gutterBottom>
        This will reset <strong>{exercise.name}</strong> back to draft status
        and delete all conduct data.
      </Typography>

      <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
        The following will be permanently deleted:
      </Typography>

      <ul>
        <li>All inject statuses will reset to Pending</li>
        <li>All fired/skipped inject records will be cleared</li>
        <li>All observations will be permanently deleted</li>
        <li>Exercise clock will reset to 00:00:00</li>
      </ul>

      <Alert severity="error" sx={{ mt: 2 }}>
        This action cannot be undone.
      </Alert>

      <FormControlLabel
        control={
          <Checkbox
            checked={confirmChecked}
            onChange={(e) => setConfirmChecked(e.target.checked)}
          />
        }
        label="I understand this will permanently delete all conduct data"
        sx={{ mt: 2 }}
      />
    </DialogContent>
    <DialogActions>
      <CobraSecondaryButton onClick={onClose}>Cancel</CobraSecondaryButton>
      <CobraDeleteButton
        onClick={handleRevert}
        disabled={!confirmChecked || isLoading}
        startIcon={<FontAwesomeIcon icon={faUndo} />}
      >
        Revert to Draft
      </CobraDeleteButton>
    </DialogActions>
  </Dialog>
);
```

### SignalR Events

**Event Name:** `ExerciseStatusChanged`

**Payload:**
```typescript
{
  exerciseId: string;
  newStatus: ExerciseStatus; // "Draft"
  timestamp: string;
}
```

**Event Name:** `ExerciseReverted` (optional - for audit/notification)

**Payload:**
```typescript
{
  exerciseId: string;
  revertedBy: string; // User ID
  timestamp: string;
  deletedObservationCount: number;
  resetInjectCount: number;
}
```

### Validation Rules

1. **Status Check:** Current status MUST be Paused (cannot revert from Active/Completed/Archived)
2. **Permission Check:** User MUST be Administrator or Exercise Director
3. **Transaction:** All data deletions must occur in a single database transaction (all-or-nothing)

### Database Operations

**Operations in transaction:**
1. UPDATE Exercises SET Status = 'Draft', ActivatedAt = NULL, ActivatedBy = NULL, ClockState = 'Stopped', ...
2. UPDATE Injects SET Status = 'Pending', FiredAt = NULL, FiredBy = NULL WHERE MselId IN (...)
3. DELETE FROM Observations WHERE ExerciseId = @exerciseId
4. Commit transaction

### Audit Logging

Create audit log entry:
```json
{
  "action": "ExerciseRevertedToDraft",
  "exerciseId": "guid",
  "userId": "guid",
  "timestamp": "2026-01-15T14:30:00Z",
  "metadata": {
    "deletedObservations": 15,
    "resetInjects": 42,
    "previousElapsedTime": "PT0H15M30S"
  }
}
```

---

**Acceptance Criteria Checklist:** 32 criteria
**Estimated Effort:** 1.5 days (backend transaction logic + frontend confirmation dialog + extensive testing)
**Testing:** Unit tests for data deletion, integration tests for transaction rollback, E2E test for full revert workflow with data verification
