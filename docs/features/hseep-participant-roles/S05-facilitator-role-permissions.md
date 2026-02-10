# Story: S05 - Define Facilitator Role Permissions

**Feature:** hseep-participant-roles
**Status:** 📋 Not Started

## User Story

**As a** Facilitator,
**I want** to control exercise pacing and manage discussion flow,
**So that** I can guide participants through tabletop exercises effectively.

## Context

**HSEEP Facilitator Definition**: Exercise staff who guides discussions, manages pace, and ensures objectives are addressed - especially critical in discussion-based exercises (TTX).

Facilitators differ from Controllers:
- **Controllers** deliver specific injects on a timeline
- **Facilitators** guide group discussions, decide when to move forward, and adapt pacing to participant engagement

Facilitators need:
- Full visibility of pending injects and objectives
- Ability to fire injects when discussion is ready (not time-based)
- Ability to skip injects that become irrelevant to discussion
- Cannot modify MSEL structure

**Permission Level**: Similar to Controller for conduct operations, but focused on discussion management rather than time-based inject delivery.

## Acceptance Criteria

### View Permissions

- [ ] **AC-01**: Given I am a Facilitator, when I access an exercise, then I see all pending and delivered injects
  - Test: `FacilitatorAccessTests.cs::Facilitator_CanSeeAllInjects`

- [ ] **AC-02**: Given I am a Facilitator, when I view an inject, then I see all fields including controller notes and expected actions
  - Test: `FacilitatorAccessTests.cs::Facilitator_CanSeeInjectDetails`
  - Need full context to guide discussion

- [ ] **AC-03**: Given I am a Facilitator, when I access objectives, then I can view them
  - Test: `FacilitatorAccessTests.cs::Facilitator_CanSeeObjectives`
  - Facilitators ensure objectives are addressed in discussion

- [ ] **AC-04**: Given I am a Facilitator, when I access the exercise, then I can view participants and their roles
  - Test: `FacilitatorAccessTests.cs::Facilitator_CanSeeParticipants`
  - Need to know who's in the discussion

### Conduct Permissions

- [ ] **AC-05**: Given I am a Facilitator, when I fire an inject, then it is marked as delivered
  - Test: `FacilitatorAccessTests.cs::Facilitator_CanFireInjects`
  - Facilitators control when to introduce new discussion points

- [ ] **AC-06**: Given I am a Facilitator, when I skip an inject, then it is marked as skipped
  - Test: `FacilitatorAccessTests.cs::Facilitator_CanSkipInjects`
  - Discussion may make certain injects irrelevant

- [ ] **AC-07**: Given I am a Facilitator, when I pause the exercise clock, then it pauses
  - Test: `FacilitatorAccessTests.cs::Facilitator_CanPauseExerciseClock`
  - Facilitators control pacing

- [ ] **AC-08**: Given I am a Facilitator, when I resume the exercise clock, then it resumes
  - Test: `FacilitatorAccessTests.cs::Facilitator_CanResumeExerciseClock`

### Restrictions

- [ ] **AC-09**: Given I am a Facilitator, when I attempt to create or edit injects, then I receive a 403 Forbidden response
  - Test: `FacilitatorAccessTests.cs::Facilitator_CannotModifyInjects`
  - Cannot change MSEL structure during conduct

- [ ] **AC-10**: Given I am a Facilitator, when I attempt to create observations, then I receive a 403 Forbidden response
  - Test: `FacilitatorAccessTests.cs::Facilitator_CannotCreateObservations`
  - Facilitators guide, not evaluate (Evaluators observe)

- [ ] **AC-11**: Given I am a Facilitator, when I attempt to modify exercise configuration, then I receive a 403 Forbidden response
  - Test: `FacilitatorAccessTests.cs::Facilitator_CannotModifyExerciseConfig`

- [ ] **AC-12**: Given I am a Facilitator, when I attempt to assign participants, then I receive a 403 Forbidden response
  - Test: `FacilitatorAccessTests.cs::Facilitator_CannotManageParticipants`

## Out of Scope

- Facilitator-specific discussion notes (future enhancement)
- Discussion timer/pacing tools (future enhancement)
- TTX-specific UI adaptations (future enhancement)
- Participant engagement tracking (future enhancement)

## Dependencies

- hseep-participant-roles/S01: Extend ExerciseRole Enum
- hseep-participant-roles/S02: Update Role Assignment UI
- exercise-clock features: Exercise clock control

## Permission Matrix Update

### Inject Management (Conduct Phase)

| Permission | Admin | Director | Controller | Evaluator | Observer | Player | Simulator | **Facilitator** |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|:------:|:---------:|:---------------:|
| Fire Inject | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ⚠️ | ✅ |
| Skip Inject | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ |
| View Pending Injects | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ | ✅ |
| View Controller Notes | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ | ✅ |
| View Expected Actions | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |

### Exercise Clock Control

| Permission | Admin | Director | Controller | Evaluator | Observer | Player | Simulator | **Facilitator** |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|:------:|:---------:|:---------------:|
| Pause Exercise Clock | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |
| Resume Exercise Clock | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |

### Participant Management

| Permission | Admin | Director | Controller | Evaluator | Observer | Player | Simulator | **Facilitator** |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|:------:|:---------:|:---------------:|
| View Participant List | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ |

### Objective Management

| Permission | Admin | Director | Controller | Evaluator | Observer | Player | Simulator | **Facilitator** |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|:------:|:---------:|:---------------:|
| View Objectives | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |

## Technical Implementation

### Backend Authorization

**File**: `src/Cadence.Core/Infrastructure/Authorization/RolePermissions.cs`

```csharp
public static bool CanFireInject(ExerciseRole role) =>
    role is ExerciseRole.Administrator
        or ExerciseRole.ExerciseDirector
        or ExerciseRole.Controller
        or ExerciseRole.Simulator
        or ExerciseRole.Facilitator;

public static bool CanSkipInject(ExerciseRole role) =>
    role is ExerciseRole.Administrator
        or ExerciseRole.ExerciseDirector
        or ExerciseRole.Controller
        or ExerciseRole.Facilitator;

public static bool CanControlExerciseClock(ExerciseRole role) =>
    role is ExerciseRole.Administrator
        or ExerciseRole.ExerciseDirector
        or ExerciseRole.Facilitator;

public static bool CanViewParticipants(ExerciseRole role) =>
    role is ExerciseRole.Administrator
        or ExerciseRole.ExerciseDirector
        or ExerciseRole.Controller
        or ExerciseRole.Evaluator
        or ExerciseRole.Facilitator;
```

### Exercise Clock Control

**File**: `src/Cadence.Core/Features/ExerciseClock/Services/ExerciseClockService.cs`

```csharp
public async Task PauseClockAsync(Guid exerciseId, Guid userId)
{
    var participant = await GetExerciseParticipantAsync(exerciseId, userId);

    if (!RolePermissions.CanControlExerciseClock(participant.Role))
    {
        throw new ForbiddenException("User does not have permission to control exercise clock");
    }

    var clock = await _context.ExerciseClocks
        .FirstOrDefaultAsync(c => c.ExerciseId == exerciseId);

    if (clock == null)
        throw new NotFoundException("Exercise clock not found");

    clock.State = ClockState.Paused;
    await _context.SaveChangesAsync();

    await _hubContext.NotifyClockPaused(exerciseId, clock);
}
```

## UI Adaptations

**File**: `src/frontend/src/features/exercises/pages/ExercisePage.tsx`

```typescript
// Facilitators see full conduct UI
const ExercisePage: React.FC = () => {
  const permissions = useExercisePermissions(exerciseId);

  return (
    <Tabs>
      {permissions.canFireInjects && <Tab label="Conduct" />}
      <Tab label="Timeline" />
      {permissions.canViewObjectives && <Tab label="Objectives" />}
      {permissions.canViewParticipants && <Tab label="Participants" />}
      {/* No Configuration tab for Facilitators */}
    </Tabs>
  );
};
```

**File**: `src/frontend/src/features/exercises/components/ConductView.tsx`

```typescript
// Show clock controls for Facilitators
{permissions.canControlExerciseClock && (
  <Box>
    <CobraPrimaryButton onClick={handlePauseClock}>
      <FontAwesomeIcon icon={faPause} /> Pause
    </CobraPrimaryButton>
    <CobraPrimaryButton onClick={handleResumeClock}>
      <FontAwesomeIcon icon={faPlay} /> Resume
    </CobraPrimaryButton>
  </Box>
)}
```

## Test Coverage

### Backend Tests
- `src/Cadence.Core.Tests/Features/Authorization/FacilitatorAccessTests.cs`
- `src/Cadence.Core.Tests/Features/ExerciseClock/ExerciseClockServiceTests.cs`

### Frontend Tests
- `src/frontend/src/features/exercises/pages/ExercisePage.test.tsx`
- `src/frontend/src/features/exercises/components/ConductView.test.tsx`

## Use Cases

### Tabletop Exercise (TTX)
1. Facilitator introduces inject to group
2. Group discusses response
3. When discussion slows, Facilitator fires next inject
4. If discussion diverges productively, Facilitator may skip less-relevant injects

### Facilitator vs Controller

| Aspect | Controller | Facilitator |
|--------|-----------|-------------|
| Primary Use | Time-based exercises (FE, FSE) | Discussion-based exercises (TTX) |
| Inject Timing | Scheduled, clock-driven | Discussion-driven, manual |
| Pacing Control | Limited (follows timeline) | Full (pause/resume clock) |
| Focus | Deliver injects on time | Guide discussion, ensure objectives |

## Related Stories

- hseep-participant-roles/S08: Role-Specific UI Adaptations
- exercise-clock features: Clock control functionality

---

*Last updated: 2026-02-09*
