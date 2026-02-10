# Story: S04 - Define Simulator Role Permissions

**Feature:** hseep-participant-roles
**Status:** 📋 Not Started

## User Story

**As a** Simulator (SimCell staff member),
**I want** to send simulated communications and track my assigned simulations,
**So that** I can role-play external entities during exercise conduct.

## Context

**HSEEP Simulator Definition**: Exercise staff who role-play individuals or organizations outside the scope of the exercise, providing realism through simulated interactions (media, higher headquarters, partner agencies, victims, etc.).

Simulators operate in the **SimCell (Simulation Cell)** - a dedicated area where staff generate realistic external inputs. They need to:
- View injects assigned to their simulation role
- Mark when simulated communications are sent
- Add ad-hoc simulated events (within Controller approval)
- Track what they've delivered

**Permission Level**: Limited write access - can update simulation-related fields, but cannot modify exercise structure or fire standard injects.

## Acceptance Criteria

### View Permissions

- [ ] **AC-01**: Given I am a Simulator, when I access an exercise, then I see all injects where I am designated as the simulator
  - Test: `SimulatorAccessTests.cs::Simulator_SeesAssignedInjects`
  - Requires inject.AssignedSimulator field (future enhancement noted in Out of Scope)

- [ ] **AC-02**: Given I am a Simulator, when I view an inject, then I see the description, expected action, and controller notes
  - Test: `SimulatorAccessTests.cs::Simulator_CanSeeInjectDetails`
  - Need full context to perform simulation realistically

- [ ] **AC-03**: Given I am a Simulator, when I access the exercise, then I can view the full inject timeline (pending and delivered)
  - Test: `SimulatorAccessTests.cs::Simulator_CanSeePendingInjects`
  - Need to prepare upcoming simulations

- [ ] **AC-04**: Given I am a Simulator, when I access objectives, then I can view them
  - Test: `SimulatorAccessTests.cs::Simulator_CanSeeObjectives`
  - Understanding objectives helps provide realistic simulation

### Write Permissions

- [ ] **AC-05**: Given I am a Simulator, when I mark a simulation as "sent", then the inject status updates to "Delivered"
  - Test: `SimulatorAccessTests.cs::Simulator_CanMarkSimulationSent`
  - Equivalent to firing an inject for simulation-type injects

- [ ] **AC-06**: Given I am a Simulator, when I attempt to fire a standard inject, then I receive a 403 Forbidden response
  - Test: `SimulatorAccessTests.cs::Simulator_CannotFireStandardInjects`
  - Can only deliver simulations, not standard scenario injects

- [ ] **AC-07**: Given I am a Simulator, when I add notes to a simulation inject, then they are saved
  - Test: `SimulatorAccessTests.cs::Simulator_CanAddSimulationNotes`
  - "Simulation Notes" field separate from "Controller Notes"

- [ ] **AC-08**: Given I am a Simulator, when I attempt to create or edit injects, then I receive a 403 Forbidden response
  - Test: `SimulatorAccessTests.cs::Simulator_CannotModifyInjects`
  - Cannot change MSEL structure

### Restrictions

- [ ] **AC-09**: Given I am a Simulator, when I attempt to create observations, then I receive a 403 Forbidden response
  - Test: `SimulatorAccessTests.cs::Simulator_CannotCreateObservations`
  - Simulators maintain realism, not evaluate

- [ ] **AC-10**: Given I am a Simulator, when I attempt to modify exercise settings, then I receive a 403 Forbidden response
  - Test: `SimulatorAccessTests.cs::Simulator_CannotModifyExercise`

- [ ] **AC-11**: Given I am a Simulator, when I attempt to assign participants, then I receive a 403 Forbidden response
  - Test: `SimulatorAccessTests.cs::Simulator_CannotManageParticipants`

## Out of Scope

- Inject.AssignedSimulator field (future enhancement - requires schema change)
- Simulation-specific inject type (future enhancement)
- SimCell communication templates (future enhancement)
- Multi-simulator coordination features (future enhancement)

**Note**: For MVP, Simulators will have similar permissions to Controllers but will be assigned by Exercise Directors to specific simulation responsibilities outside the platform.

## Dependencies

- hseep-participant-roles/S01: Extend ExerciseRole Enum
- hseep-participant-roles/S02: Update Role Assignment UI

## Permission Matrix Update

### Inject Management (Conduct Phase)

| Permission | Admin | Director | Controller | Evaluator | Observer | Player | **Simulator** |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|:------:|:-------------:|
| Fire Inject | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ⚠️ Simulations only |
| View Pending Injects | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| View Fired Injects | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| View Controller Notes | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ |
| Add Simulation Notes | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ |

### Objective Management

| Permission | Admin | Director | Controller | Evaluator | Observer | Player | **Simulator** |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|:------:|:-------------:|
| View Objectives | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |

## Technical Implementation

### Backend Authorization

**File**: `src/Cadence.Core/Infrastructure/Authorization/RolePermissions.cs`

```csharp
public static bool CanViewPendingInjects(ExerciseRole role) =>
    role is ExerciseRole.Administrator
        or ExerciseRole.ExerciseDirector
        or ExerciseRole.Controller
        or ExerciseRole.Evaluator
        or ExerciseRole.Simulator;

public static bool CanViewControllerNotes(ExerciseRole role) =>
    role is ExerciseRole.Administrator
        or ExerciseRole.ExerciseDirector
        or ExerciseRole.Controller
        or ExerciseRole.Simulator;

public static bool CanAddSimulationNotes(ExerciseRole role) =>
    role is ExerciseRole.Administrator
        or ExerciseRole.ExerciseDirector
        or ExerciseRole.Controller
        or ExerciseRole.Simulator;

public static bool CanFireInject(ExerciseRole role) =>
    role is ExerciseRole.Administrator
        or ExerciseRole.ExerciseDirector
        or ExerciseRole.Controller
        or ExerciseRole.Simulator; // Subject to inject type check
```

### API Logic

**File**: `src/Cadence.Core/Features/Injects/Services/InjectService.cs`

```csharp
public async Task<InjectDto> FireInjectAsync(Guid injectId, Guid userId)
{
    var inject = await _context.Injects.FindAsync(injectId);
    var participant = await GetExerciseParticipantAsync(inject.Msel.ExerciseId, userId);

    // Simulators can only fire simulation-type injects (future enhancement)
    // For MVP, Simulators have same fire permissions as Controllers
    if (!RolePermissions.CanFireInject(participant.Role))
    {
        throw new ForbiddenException("User does not have permission to fire injects");
    }

    inject.Status = InjectStatus.Delivered;
    inject.ActualTime = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return _mapper.Map<InjectDto>(inject);
}
```

## UI Adaptations

**File**: `src/frontend/src/features/exercises/pages/ExercisePage.tsx`

```typescript
// Simulators see Conduct view similar to Controllers
const tabs = [];

if (permissions.canViewPendingInjects) {
  tabs.push(<Tab label="Conduct" key="conduct" />);
}

tabs.push(<Tab label="Timeline" key="timeline" />);

if (permissions.canViewObjectives) {
  tabs.push(<Tab label="Objectives" key="objectives" />);
}

// Simulators do NOT see Participants or Configuration
```

## Test Coverage

### Backend Tests
- `src/Cadence.Core.Tests/Features/Authorization/SimulatorAccessTests.cs`
- `src/Cadence.Core.Tests/Features/Injects/InjectServiceTests.cs`

### Frontend Tests
- `src/frontend/src/features/exercises/hooks/useExercisePermissions.test.ts`

## Future Enhancements

1. **Inject.AssignedSimulator**: FK to track which Simulator is responsible for each simulation inject
2. **InjectType.Simulation**: Dedicated inject type for simulated communications
3. **Simulation Notes**: Separate field from Controller Notes for simulation delivery details
4. **SimCell Dashboard**: Dedicated view for Simulators showing only their assigned simulations

## Related Stories

- hseep-participant-roles/S08: Role-Specific UI Adaptations
- inject-crud/S01-S04: Inject CRUD operations

---

*Last updated: 2026-02-09*
