# Story: S06 - Define Safety Officer Role Permissions

**Feature:** hseep-participant-roles
**Status:** 📋 Not Started

## User Story

**As a** Safety Officer,
**I want** to monitor exercise safety and have authority to pause or terminate the exercise,
**So that** I can ensure participant safety during operations-based exercises.

## Context

**HSEEP Safety Officer Definition**: Exercise staff responsible for safety oversight, with authority to pause or terminate the exercise if safety hazards arise.

Safety Officers are **critical for operations-based exercises** (FSE, FE) where:
- Real resources are deployed (vehicles, aircraft, boats)
- Responders are in field environments
- Physical activities occur (search & rescue, firefighting simulations)
- Weather or environmental hazards may develop

Safety Officers need:
- Real-time visibility of exercise status
- Authority to pause or stop the exercise immediately
- Ability to add safety observations/incidents
- Minimal interference with conduct otherwise (observe-only for most operations)

**Permission Level**: Hybrid role - mostly Observer permissions + emergency stop authority + safety logging.

## Acceptance Criteria

### View Permissions

- [ ] **AC-01**: Given I am a Safety Officer, when I access an exercise, then I see all pending and delivered injects
  - Test: `SafetyOfficerAccessTests.cs::SafetyOfficer_CanSeeAllInjects`
  - Need full visibility to anticipate safety issues

- [ ] **AC-02**: Given I am a Safety Officer, when I view an inject, then I see description, expected action, and timing
  - Test: `SafetyOfficerAccessTests.cs::SafetyOfficer_CanSeeInjectDetails`
  - Controller notes are hidden (not relevant to safety)

- [ ] **AC-03**: Given I am a Safety Officer, when I access the exercise, then I can view participants and their roles
  - Test: `SafetyOfficerAccessTests.cs::SafetyOfficer_CanSeeParticipants`
  - Need to know who's in the field

- [ ] **AC-04**: Given I am a Safety Officer, when I access the exercise, then I can view exercise objectives
  - Test: `SafetyOfficerAccessTests.cs::SafetyOfficer_CanSeeObjectives`
  - Understanding objectives helps assess safety vs. realism trade-offs

### Safety Authority

- [ ] **AC-05**: Given I am a Safety Officer, when I click "Emergency Stop", then the exercise clock stops immediately
  - Test: `SafetyOfficerAccessTests.cs::SafetyOfficer_CanEmergencyStopExercise`
  - Broadcast to all participants via SignalR

- [ ] **AC-06**: Given I am a Safety Officer, when I emergency stop the exercise, then I am prompted for a safety reason
  - Test: `SafetyOfficerAccessTests.cs::SafetyOfficer_MustProvideStopReason`
  - Logged for incident report

- [ ] **AC-07**: Given I am a Safety Officer, when I pause the exercise, then the exercise clock pauses
  - Test: `SafetyOfficerAccessTests.cs::SafetyOfficer_CanPauseExercise`
  - Temporary pause for minor safety adjustments

- [ ] **AC-08**: Given I am a Safety Officer, when I create a safety observation, then it is saved with "Safety" flag
  - Test: `SafetyOfficerAccessTests.cs::SafetyOfficer_CanCreateSafetyObservations`
  - Different from Evaluator observations (safety vs. performance)

### Restrictions

- [ ] **AC-09**: Given I am a Safety Officer, when I attempt to fire or skip injects, then I receive a 403 Forbidden response
  - Test: `SafetyOfficerAccessTests.cs::SafetyOfficer_CannotFireInjects`
  - Safety Officers observe, not conduct

- [ ] **AC-10**: Given I am a Safety Officer, when I attempt to create or edit injects, then I receive a 403 Forbidden response
  - Test: `SafetyOfficerAccessTests.cs::SafetyOfficer_CannotModifyInjects`

- [ ] **AC-11**: Given I am a Safety Officer, when I attempt to modify exercise configuration, then I receive a 403 Forbidden response
  - Test: `SafetyOfficerAccessTests.cs::SafetyOfficer_CannotModifyExerciseConfig`

- [ ] **AC-12**: Given I am a Safety Officer, when I attempt to assign participants, then I receive a 403 Forbidden response
  - Test: `SafetyOfficerAccessTests.cs::SafetyOfficer_CannotManageParticipants`

### UI Indicators

- [ ] **AC-13**: Given I am any role, when the Safety Officer emergency stops the exercise, then I see a prominent "EXERCISE STOPPED - SAFETY" banner
  - Test: `ExercisePage.test.tsx::displays_safety_stop_banner`
  - Red banner across all views

- [ ] **AC-14**: Given I am a Safety Officer, when I view the conduct page, then I see a prominent "EMERGENCY STOP" button
  - Test: `ConductView.test.tsx::SafetyOfficer_SeesEmergencyStopButton`
  - Red, always visible, requires confirmation

## Out of Scope

- Safety incident reporting system (future enhancement)
- Safety checklist integration (future enhancement)
- Weather/environmental monitoring integration (future enhancement)
- Safety equipment tracking (future enhancement)
- Multi-Safety Officer coordination (future enhancement)

## Dependencies

- hseep-participant-roles/S01: Extend ExerciseRole Enum
- hseep-participant-roles/S02: Update Role Assignment UI
- exercise-clock features: Exercise clock control
- Observations feature: Safety observation logging

## Permission Matrix Update

### Inject Management (Conduct Phase)

| Permission | Admin | Director | Controller | Evaluator | Observer | Player | Simulator | Facilitator | **Safety Officer** |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|:------:|:---------:|:-----------:|:------------------:|
| Fire Inject | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ⚠️ | ✅ | ❌ |
| View Pending Injects | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ | ✅ | ✅ |
| View Expected Actions | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ |

### Exercise Clock Control

| Permission | Admin | Director | Controller | Evaluator | Observer | Player | Simulator | Facilitator | **Safety Officer** |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|:------:|:---------:|:-----------:|:------------------:|
| Pause Exercise Clock | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Emergency Stop | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |

### Observation Management

| Permission | Admin | Director | Controller | Evaluator | Observer | Player | Simulator | Facilitator | **Safety Officer** |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|:------:|:---------:|:-----------:|:------------------:|
| Create Safety Observation | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| View All Observations | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |

## Technical Implementation

### Backend Authorization

**File**: `src/Cadence.Core/Infrastructure/Authorization/RolePermissions.cs`

```csharp
public static bool CanEmergencyStopExercise(ExerciseRole role) =>
    role is ExerciseRole.Administrator
        or ExerciseRole.ExerciseDirector
        or ExerciseRole.SafetyOfficer;

public static bool CanPauseExerciseClock(ExerciseRole role) =>
    role is ExerciseRole.Administrator
        or ExerciseRole.ExerciseDirector
        or ExerciseRole.Facilitator
        or ExerciseRole.SafetyOfficer;

public static bool CanCreateSafetyObservation(ExerciseRole role) =>
    role is ExerciseRole.Administrator
        or ExerciseRole.ExerciseDirector
        or ExerciseRole.SafetyOfficer;
```

### Emergency Stop Logic

**File**: `src/Cadence.Core/Features/ExerciseClock/Services/ExerciseClockService.cs`

```csharp
public async Task EmergencyStopAsync(Guid exerciseId, Guid userId, string reason)
{
    var participant = await GetExerciseParticipantAsync(exerciseId, userId);

    if (!RolePermissions.CanEmergencyStopExercise(participant.Role))
    {
        throw new ForbiddenException("User does not have authority to emergency stop exercise");
    }

    if (string.IsNullOrWhiteSpace(reason))
    {
        throw new ValidationException("Safety reason is required for emergency stop");
    }

    var clock = await _context.ExerciseClocks
        .FirstOrDefaultAsync(c => c.ExerciseId == exerciseId);

    if (clock == null)
        throw new NotFoundException("Exercise clock not found");

    clock.State = ClockState.EmergencyStopped;
    clock.EmergencyStopReason = reason;
    clock.EmergencyStoppedAt = DateTime.UtcNow;
    clock.EmergencyStoppedBy = userId;

    await _context.SaveChangesAsync();

    // Broadcast emergency stop to ALL participants
    await _hubContext.NotifyEmergencyStop(exerciseId, new EmergencyStopDto
    {
        Reason = reason,
        StoppedBy = participant.DisplayName,
        StoppedAt = DateTime.UtcNow
    });

    // Log safety incident
    _logger.LogWarning(
        "Exercise {ExerciseId} EMERGENCY STOPPED by Safety Officer {UserId}. Reason: {Reason}",
        exerciseId, userId, reason);
}
```

### Safety Observation

**File**: `src/Cadence.Core/Models/Entities/Observation.cs`

```csharp
public class Observation : BaseEntity, IOrganizationScoped
{
    // ... existing properties ...

    /// <summary>
    /// Indicates this is a safety-related observation (vs. performance evaluation)
    /// </summary>
    public bool IsSafetyObservation { get; set; }

    /// <summary>
    /// If safety observation, severity level
    /// </summary>
    public SafetySeverity? SafetySeverity { get; set; }
}

public enum SafetySeverity
{
    Information = 1,
    Concern = 2,
    Hazard = 3,
    Critical = 4
}
```

## UI Implementation

### Emergency Stop Button

**File**: `src/frontend/src/features/exercises/components/EmergencyStopButton.tsx`

```typescript
export const EmergencyStopButton: React.FC<{ exerciseId: string }> = ({ exerciseId }) => {
  const [dialogOpen, setDialogOpen] = useState(false);
  const [reason, setReason] = useState('');
  const emergencyStopMutation = useEmergencyStop(exerciseId);

  const handleConfirm = () => {
    emergencyStopMutation.mutate({ exerciseId, reason });
    setDialogOpen(false);
  };

  return (
    <>
      <CobraDeleteButton
        size="large"
        onClick={() => setDialogOpen(true)}
        startIcon={<FontAwesomeIcon icon={faOctagonExclamation} />}
        sx={{
          backgroundColor: 'error.main',
          fontWeight: 'bold',
          fontSize: '1.1rem'
        }}
      >
        EMERGENCY STOP
      </CobraDeleteButton>

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)}>
        <DialogTitle>Emergency Stop Exercise</DialogTitle>
        <DialogContent>
          <Alert severity="error">
            This will immediately stop the exercise for ALL participants.
          </Alert>
          <CobraTextField
            label="Safety Reason (Required)"
            multiline
            rows={3}
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            required
          />
        </DialogContent>
        <DialogActions>
          <CobraSecondaryButton onClick={() => setDialogOpen(false)}>
            Cancel
          </CobraSecondaryButton>
          <CobraDeleteButton
            onClick={handleConfirm}
            disabled={!reason.trim()}
          >
            EMERGENCY STOP
          </CobraDeleteButton>
        </DialogActions>
      </Dialog>
    </>
  );
};
```

### Emergency Stop Banner

**File**: `src/frontend/src/features/exercises/components/EmergencyStopBanner.tsx`

```typescript
export const EmergencyStopBanner: React.FC<{ stopInfo: EmergencyStopDto }> = ({ stopInfo }) => {
  return (
    <Alert
      severity="error"
      sx={{
        position: 'sticky',
        top: 0,
        zIndex: 1100,
        fontSize: '1.2rem',
        fontWeight: 'bold',
        borderRadius: 0,
      }}
      icon={<FontAwesomeIcon icon={faOctagonExclamation} size="2x" />}
    >
      EXERCISE STOPPED - SAFETY ISSUE
      <Typography variant="body2">
        Stopped by {stopInfo.stoppedBy} at {formatTime(stopInfo.stoppedAt)}
      </Typography>
      <Typography variant="body2">
        Reason: {stopInfo.reason}
      </Typography>
    </Alert>
  );
};
```

## Test Coverage

### Backend Tests
- `src/Cadence.Core.Tests/Features/Authorization/SafetyOfficerAccessTests.cs`
- `src/Cadence.Core.Tests/Features/ExerciseClock/ExerciseClockServiceTests.cs::EmergencyStop_*`
- `src/Cadence.Core.Tests/Features/Observations/SafetyObservationTests.cs`

### Frontend Tests
- `src/frontend/src/features/exercises/components/EmergencyStopButton.test.tsx`
- `src/frontend/src/features/exercises/components/EmergencyStopBanner.test.tsx`

## Related Stories

- hseep-participant-roles/S08: Role-Specific UI Adaptations
- exercise-clock features: Clock control functionality
- observations features: Safety observation logging

---

*Last updated: 2026-02-09*
