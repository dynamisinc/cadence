# Story: S07 - Define Trusted Agent Role Permissions

**Feature:** hseep-participant-roles
**Status:** 📋 Not Started

## User Story

**As a** Trusted Agent,
**I want** to observe player teams and provide subtle guidance while recording observations,
**So that** I can support players without disrupting exercise realism.

## Context

**HSEEP Trusted Agent Definition**: Subject matter experts embedded with player teams to observe performance and provide subtle guidance without disrupting realism.

Trusted Agents are a **hybrid observer-advisor role**:
- Embedded with specific player teams (e.g., hazmat expert with incident command)
- Observe and document performance (like Evaluators)
- Can provide subtle guidance when players are stuck (limited coaching)
- Cannot fire injects or modify exercise structure
- Observations may include both performance notes and coaching provided

**Permission Level**: Between Evaluator and Observer - can create observations, view full exercise, but cannot control conduct.

## Acceptance Criteria

### View Permissions

- [ ] **AC-01**: Given I am a Trusted Agent, when I access an exercise, then I see all delivered injects and objectives
  - Test: `TrustedAgentAccessTests.cs::TrustedAgent_CanSeeDeliveredInjects`

- [ ] **AC-02**: Given I am a Trusted Agent, when I access pending injects, then I see them (to anticipate what players will receive)
  - Test: `TrustedAgentAccessTests.cs::TrustedAgent_CanSeePendingInjects`

- [ ] **AC-03**: Given I am a Trusted Agent, when I view an inject, then I see expected actions
  - Test: `TrustedAgentAccessTests.cs::TrustedAgent_CanSeeExpectedActions`
  - Need to know correct responses to provide guidance

- [ ] **AC-04**: Given I am a Trusted Agent, when I view an inject, then I do NOT see controller notes
  - Test: `TrustedAgentAccessTests.cs::TrustedAgent_CannotSeeControllerNotes`
  - Controller notes are conduct logistics, not relevant to embedded role

### Write Permissions

- [ ] **AC-05**: Given I am a Trusted Agent, when I create an observation, then it is saved
  - Test: `TrustedAgentAccessTests.cs::TrustedAgent_CanCreateObservations`

- [ ] **AC-06**: Given I am a Trusted Agent, when I create an observation, then I can flag it as "Coaching Provided"
  - Test: `TrustedAgentAccessTests.cs::TrustedAgent_CanFlagCoachingObservations`
  - Distinguishes between passive observation and active guidance

- [ ] **AC-07**: Given I am a Trusted Agent, when I edit my own observation, then it is updated
  - Test: `TrustedAgentAccessTests.cs::TrustedAgent_CanEditOwnObservations`

- [ ] **AC-08**: Given I am a Trusted Agent, when I attempt to edit another user's observation, then I receive a 403 Forbidden response
  - Test: `TrustedAgentAccessTests.cs::TrustedAgent_CannotEditOthersObservations`

### Restrictions

- [ ] **AC-09**: Given I am a Trusted Agent, when I attempt to fire or skip injects, then I receive a 403 Forbidden response
  - Test: `TrustedAgentAccessTests.cs::TrustedAgent_CannotFireInjects`

- [ ] **AC-10**: Given I am a Trusted Agent, when I attempt to modify injects, then I receive a 403 Forbidden response
  - Test: `TrustedAgentAccessTests.cs::TrustedAgent_CannotModifyInjects`

- [ ] **AC-11**: Given I am a Trusted Agent, when I attempt to control the exercise clock, then I receive a 403 Forbidden response
  - Test: `TrustedAgentAccessTests.cs::TrustedAgent_CannotControlClock`

## Permission Matrix Update

### Inject Management (Conduct Phase)

| Permission | Admin | Director | Controller | Evaluator | Observer | Player | Simulator | Facilitator | Safety Officer | **Trusted Agent** |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|:------:|:---------:|:-----------:|:--------------:|:----------------:|
| View Pending Injects | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ |
| View Expected Actions | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ |
| View Controller Notes | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ |

### Observation Management

| Permission | Admin | Director | Controller | Evaluator | Observer | Player | Simulator | Facilitator | Safety Officer | **Trusted Agent** |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|:------:|:---------:|:-----------:|:--------------:|:----------------:|
| Create Observation | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ⚠️ Safety | ✅ |
| Edit Own Observation | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ |
| Flag Coaching Provided | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |

## Technical Implementation

### Backend Authorization

**File**: `src/Cadence.Core/Infrastructure/Authorization/RolePermissions.cs`

```csharp
public static bool CanViewPendingInjects(ExerciseRole role) =>
    role is ExerciseRole.Administrator
        or ExerciseRole.ExerciseDirector
        or ExerciseRole.Controller
        or ExerciseRole.Evaluator
        or ExerciseRole.Simulator
        or ExerciseRole.Facilitator
        or ExerciseRole.SafetyOfficer
        or ExerciseRole.TrustedAgent;

public static bool CanViewExpectedActions(ExerciseRole role) =>
    role is ExerciseRole.Administrator
        or ExerciseRole.ExerciseDirector
        or ExerciseRole.Controller
        or ExerciseRole.Evaluator
        or ExerciseRole.Observer
        or ExerciseRole.Simulator
        or ExerciseRole.Facilitator
        or ExerciseRole.SafetyOfficer
        or ExerciseRole.TrustedAgent;

public static bool CanCreateObservation(ExerciseRole role) =>
    role is ExerciseRole.Administrator
        or ExerciseRole.ExerciseDirector
        or ExerciseRole.Evaluator
        or ExerciseRole.TrustedAgent;

public static bool CanFlagCoaching(ExerciseRole role) =>
    role is ExerciseRole.Administrator
        or ExerciseRole.ExerciseDirector
        or ExerciseRole.TrustedAgent;
```

### Observation Entity Update

**File**: `src/Cadence.Core/Models/Entities/Observation.cs`

```csharp
public class Observation : BaseEntity, IOrganizationScoped
{
    // ... existing properties ...

    /// <summary>
    /// Indicates the observer provided coaching/guidance to players
    /// (primarily for Trusted Agents documenting their interventions)
    /// </summary>
    public bool CoachingProvided { get; set; }

    /// <summary>
    /// Details of coaching provided (if applicable)
    /// </summary>
    public string? CoachingDetails { get; set; }
}
```

## UI Implementation

**File**: `src/frontend/src/features/observations/components/CreateObservationForm.tsx`

```typescript
{permissions.canFlagCoaching && (
  <>
    <FormControlLabel
      control={
        <Checkbox
          checked={coachingProvided}
          onChange={(e) => setCoachingProvided(e.target.checked)}
        />
      }
      label="Coaching/Guidance Provided"
    />
    {coachingProvided && (
      <CobraTextField
        label="Coaching Details"
        multiline
        rows={2}
        value={coachingDetails}
        onChange={(e) => setCoachingDetails(e.target.value)}
        placeholder="Describe the guidance you provided to players..."
      />
    )}
  </>
)}
```

## Test Coverage

### Backend Tests
- `src/Cadence.Core.Tests/Features/Authorization/TrustedAgentAccessTests.cs`
- `src/Cadence.Core.Tests/Features/Observations/ObservationServiceTests.cs`

### Frontend Tests
- `src/frontend/src/features/observations/components/CreateObservationForm.test.tsx`

## Use Cases

1. **Hazmat Expert Embedded with IC**: Observes hazmat response, provides subtle guidance when IC misses critical procedure
2. **Legal Advisor with EOC**: Watches legal decision-making, coaches on compliance issues
3. **Technical Expert with Operations**: Provides equipment-specific guidance when players encounter unfamiliar systems

## Trusted Agent vs Evaluator

| Aspect | Evaluator | Trusted Agent |
|--------|-----------|---------------|
| Position | External observer | Embedded with players |
| Interaction | No player interaction | Can provide subtle guidance |
| Knowledge | Scenario + Objectives | Scenario + Objectives + Subject Matter Expertise |
| Coaching | Never | Allowed (sparingly) |
| Documentation | Performance only | Performance + Coaching provided |

## Related Stories

- hseep-participant-roles/S08: Role-Specific UI Adaptations
- observations features: Observation creation and management

---

*Last updated: 2026-02-09*
