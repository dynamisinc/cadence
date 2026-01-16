# exercise-status/S02: Activate Exercise (Draft → Active)

## Story

**As an** Exercise Director or Administrator,
**I want** to activate a draft exercise when it's ready for conduct,
**So that** Controllers can begin firing injects and the exercise officially starts.

## Context

Activation is the transition from planning phase to conduct phase. This is a critical moment in the exercise lifecycle where:
- The MSEL transitions from editable to locked (except for Controller notes)
- The exercise clock starts running
- Controllers gain the ability to fire injects
- Evaluators can begin recording observations

Activation requires at least one inject in the MSEL to prevent accidental activation of empty exercises. Once activated, the exercise cannot return to Draft status without data loss (only via Paused → Revert to Draft).

This transition must be deliberate and confirmed to prevent accidental activation during planning.

## Acceptance Criteria

### Activation Action Availability

- [ ] **Given** I am an Exercise Director or Administrator viewing a Draft exercise, **when** I look at the exercise actions, **then** I see an "Activate Exercise" button
- [ ] **Given** I am a Controller, Evaluator, or Observer viewing a Draft exercise, **when** I look at the exercise actions, **then** I do NOT see the "Activate Exercise" button
- [ ] **Given** an exercise is in Active, Paused, Completed, or Archived status, **when** I view the exercise, **then** the "Activate Exercise" button is not visible

### Validation: Minimum Inject Requirement

- [ ] **Given** I click "Activate Exercise" on an exercise with zero injects, **when** validation runs, **then** I see an error message "Cannot activate exercise. Add at least one inject to the MSEL before activating."
- [ ] **Given** I click "Activate Exercise" on an exercise with at least one inject, **when** validation runs, **then** the confirmation dialog appears

### Activation Confirmation Dialog

- [ ] **Given** validation passes, **when** the confirmation dialog appears, **then** I see the title "Activate Exercise"
- [ ] **Given** the confirmation dialog is open, **when** I read the message, **then** I see "Start exercise conduct for '[Exercise Name]'? The exercise clock will start and the MSEL will be locked for editing."
- [ ] **Given** the confirmation dialog is open, **when** I look at the actions, **then** I see two buttons: "Cancel" and "Activate"
- [ ] **Given** I click "Cancel", **when** the dialog closes, **then** the exercise remains in Draft status and no changes are made
- [ ] **Given** I click "Activate", **when** the action processes, **then** the dialog shows a loading indicator

### Successful Activation

- [ ] **Given** I confirm activation, **when** the action completes successfully, **then** the exercise status changes to "Active"
- [ ] **Given** the exercise is activated, **when** the action completes, **then** the exercise clock starts (ClockState changes to Running)
- [ ] **Given** the exercise is activated, **when** the action completes, **then** the `ActivatedAt` timestamp is set to current UTC time
- [ ] **Given** the exercise is activated, **when** the action completes, **then** the `ActivatedBy` field is set to my user ID
- [ ] **Given** the exercise is activated, **when** the action completes, **then** I see a success message "Exercise activated successfully"
- [ ] **Given** the exercise is activated, **when** I view the exercise detail page, **then** the status badge shows "Active" with green color

### Real-Time Notification

- [ ] **Given** another user is viewing the same exercise, **when** I activate it, **then** they receive a real-time update and see the status change to Active without refreshing
- [ ] **Given** a Controller is viewing the MSEL, **when** the exercise is activated, **then** they see the "Fire" buttons appear on pending injects

### Post-Activation Behavior

- [ ] **Given** an exercise is Active, **when** I attempt to edit basic exercise details (name, description), **then** editing is disabled with message "Exercise is active. Pause or complete the exercise to make changes."
- [ ] **Given** an exercise is Active, **when** I attempt to add/edit/delete injects, **then** inject CRUD is disabled (except for Controller notes/status updates)
- [ ] **Given** an exercise is Active, **when** Controllers view the MSEL, **then** they can fire pending injects

## Out of Scope

- Scheduled activation (activate at future time)
- Pre-activation checklist (verify MSEL completeness)
- Multi-step activation wizard
- Email notifications on activation
- Auto-activation based on scheduled start time

## Dependencies

- exercise-crud/S01: Create Exercise (must have exercise to activate)
- inject-crud/S01: Create Inject (must have ≥1 inject to activate)
- exercise-status/S01: View Status (status badge must display Active state)
- exercise-clock: Exercise clock start logic (coupled with activation)

## Open Questions

- [ ] Should we validate that at least one Controller is assigned before activation? (Recommendation: No - can activate with zero Controllers)
- [ ] Should activation require a start time to be set? (Recommendation: No - optional field)
- [ ] Should we show a "ready to activate" indicator when all prerequisites are met? (Recommendation: Future enhancement)

## Domain Terms

| Term | Definition |
|------|------------|
| Activate | Transition from Draft to Active status, starting exercise conduct |
| Exercise Conduct | The active phase of an exercise where injects are delivered and observations are recorded |
| MSEL Lock | Restriction on editing injects once exercise is activated |
| Exercise Clock | Real-time timer tracking elapsed time during conduct |

## UI/UX Notes

### Activation Button Placement

```
┌────────────────────────────────────────────────────────────┐
│  Exercise: Hurricane Response 2025              [⋮ Menu]   │
│  [Draft]  📍 Houston, TX                                   │
│  Jan 15, 2026 | 9:00 AM - 5:00 PM                          │
│                                                            │
│  [Activate Exercise]  [Edit Details]  [View MSEL]         │
└────────────────────────────────────────────────────────────┘
```

### Activation Confirmation Dialog

```
┌─────────────────────────────────────────────────────┐
│  Activate Exercise                                  │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Start exercise conduct for                         │
│  "Hurricane Response 2025"?                         │
│                                                     │
│  • The exercise clock will start                    │
│  • The MSEL will be locked for editing              │
│  • Controllers can begin firing injects             │
│                                                     │
│                       [Cancel]  [Activate]          │
└─────────────────────────────────────────────────────┘
```

### Validation Error Message

```
┌─────────────────────────────────────────────────────┐
│  ⚠ Cannot Activate Exercise                         │
├─────────────────────────────────────────────────────┤
│                                                     │
│  Add at least one inject to the MSEL before         │
│  activating this exercise.                          │
│                                                     │
│                                 [OK]                │
└─────────────────────────────────────────────────────┘
```

### Success Message (Toast/Snackbar)

```
✓ Exercise activated successfully
```

## Technical Notes

### Backend Implementation

**Endpoint:** `POST /api/exercises/{exerciseId}/activate`

**Service Logic:**
```csharp
public async Task<ExerciseDto> ActivateExerciseAsync(Guid exerciseId, Guid userId)
{
    var exercise = await _context.Exercises
        .Include(e => e.ActiveMsel)
            .ThenInclude(m => m.Injects)
        .FirstOrDefaultAsync(e => e.Id == exerciseId);

    if (exercise == null)
        throw new NotFoundException("Exercise not found");

    if (exercise.Status != ExerciseStatus.Draft)
        throw new InvalidOperationException($"Cannot activate exercise in {exercise.Status} status");

    // Validation: Must have at least 1 inject
    var injectCount = exercise.ActiveMsel?.Injects?.Count ?? 0;
    if (injectCount == 0)
        throw new ValidationException("Cannot activate exercise with zero injects");

    // Update status
    exercise.Status = ExerciseStatus.Active;
    exercise.ActivatedAt = DateTime.UtcNow;
    exercise.ActivatedBy = userId;

    // Start clock
    exercise.ClockState = ExerciseClockState.Running;
    exercise.ClockStartedAt = DateTime.UtcNow;
    exercise.ClockStartedBy = userId;
    exercise.ClockElapsedBeforePause = TimeSpan.Zero;

    await _context.SaveChangesAsync();

    // Broadcast status change
    await _hubContext.NotifyExerciseStatusChanged(exerciseId, ExerciseStatus.Active);
    await _hubContext.NotifyClockStarted(exerciseId, DateTime.UtcNow);

    return _mapper.Map<ExerciseDto>(exercise);
}
```

### Frontend Implementation

**Hook:** `src/frontend/src/features/exercises/hooks/useActivateExercise.ts`

```typescript
export const useActivateExercise = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (exerciseId: string) => {
      const response = await api.post(`/exercises/${exerciseId}/activate`);
      return response.data;
    },
    onSuccess: (data, exerciseId) => {
      queryClient.invalidateQueries(['exercises', exerciseId]);
      queryClient.invalidateQueries(['exercises']);
    },
  });
};
```

**Component:** Update ExerciseActions component to include activation button

```typescript
const { mutate: activateExercise, isLoading } = useActivateExercise();

const handleActivate = async () => {
  // Show confirmation dialog
  const confirmed = await showConfirmDialog({
    title: 'Activate Exercise',
    message: `Start exercise conduct for "${exercise.name}"?`,
    details: [
      'The exercise clock will start',
      'The MSEL will be locked for editing',
      'Controllers can begin firing injects'
    ],
    confirmText: 'Activate',
    cancelText: 'Cancel'
  });

  if (confirmed) {
    activateExercise(exercise.id, {
      onSuccess: () => {
        showSnackbar('Exercise activated successfully', 'success');
      },
      onError: (error) => {
        if (error.message.includes('zero injects')) {
          showSnackbar('Cannot activate exercise. Add at least one inject first.', 'error');
        } else {
          showSnackbar('Failed to activate exercise', 'error');
        }
      }
    });
  }
};
```

### SignalR Events

**Event Name:** `ExerciseStatusChanged`

**Payload:**
```typescript
{
  exerciseId: string;
  newStatus: ExerciseStatus;
  activatedAt: string; // ISO timestamp
  activatedBy: string; // User ID
}
```

### Validation Rules

1. **Status Check:** Current status MUST be Draft
2. **Inject Count:** MUST have at least 1 inject in active MSEL
3. **Permission Check:** User MUST be Administrator or Exercise Director

### Database Changes Required

**Migration:** Add new fields to Exercise table
```sql
ALTER TABLE Exercises ADD ActivatedAt datetime2 NULL;
ALTER TABLE Exercises ADD ActivatedBy uniqueidentifier NULL;

ALTER TABLE Exercises
ADD CONSTRAINT FK_Exercises_ActivatedBy_Users
FOREIGN KEY (ActivatedBy) REFERENCES Users(Id);
```

---

**Acceptance Criteria Checklist:** 17 criteria
**Estimated Effort:** 1 day (backend endpoint + frontend component + tests)
**Testing:** Unit tests for service logic, integration tests for API endpoint, E2E test for activation workflow
