# Story: S03 - Define Player Role Permissions

**Feature:** hseep-participant-roles
**Status:** 📋 Not Started

## User Story

**As an** Exercise Director,
**I want** Players to have limited read-only access to injects they've received,
**So that** they can review exercise events in virtual/hybrid exercises or during after-action review.

## Context

**HSEEP Player Definition**: Personnel who have an active role in responding to the simulated emergency.

In traditional exercises, Players interact through real-world systems (phones, radios, incident command posts) and do NOT need Cadence access. However, modern exercise formats increasingly require player platform access:

1. **Virtual/Hybrid Exercises**: Players receive injects through the platform
2. **Self-Paced Training**: Players advance at their own pace
3. **After-Action Review**: Players review what they received for lessons learned
4. **Computer-Aided Exercises (CAX)**: Players interact through simulation interfaces

The Player role provides **minimal read access** - players can see what was delivered to them but cannot see pending injects, controller notes, or modify anything.

## Acceptance Criteria

### Core Permissions

- [ ] **AC-01**: Given I am a Player, when I access an exercise, then I see ONLY the injects marked as "delivered" or "fired"
  - Test: `PlayerAccessTests.cs::Player_CannotSeePendingInjects`
  - Cannot see injects with status "Pending" or "Ready"

- [ ] **AC-02**: Given I am a Player, when I view an inject, then I do NOT see controller notes
  - Test: `PlayerAccessTests.cs::Player_CannotSeeControllerNotes`
  - Controller notes field hidden in API response for Players

- [ ] **AC-03**: Given I am a Player, when I view an inject, then I do NOT see expected actions
  - Test: `PlayerAccessTests.cs::Player_CannotSeeExpectedActions`
  - Expected actions would reveal "correct" response

- [ ] **AC-04**: Given I am a Player, when I access the exercise, then I cannot see other participants or their roles
  - Test: `PlayerAccessTests.cs::Player_CannotSeeParticipantList`
  - Participants endpoint returns 403 for Players

- [ ] **AC-05**: Given I am a Player, when I access the exercise, then I cannot see exercise objectives
  - Test: `PlayerAccessTests.cs::Player_CannotSeeObjectives`
  - Objectives would reveal evaluation criteria

- [ ] **AC-06**: Given I am a Player, when I access the exercise, then I can see the exercise timeline of delivered injects
  - Test: `PlayerAccessTests.cs::Player_CanSeeDeliveredInjectsTimeline`
  - Read-only timeline view

### Write Restrictions

- [ ] **AC-07**: Given I am a Player, when I attempt to fire an inject, then I receive a 403 Forbidden response
  - Test: `PlayerAccessTests.cs::Player_CannotFireInjects`

- [ ] **AC-08**: Given I am a Player, when I attempt to create or edit an inject, then I receive a 403 Forbidden response
  - Test: `PlayerAccessTests.cs::Player_CannotModifyInjects`

- [ ] **AC-09**: Given I am a Player, when I attempt to create an observation, then I receive a 403 Forbidden response
  - Test: `PlayerAccessTests.cs::Player_CannotCreateObservations`
  - Players are observed, not observers

- [ ] **AC-10**: Given I am a Player, when I attempt to modify exercise settings, then I receive a 403 Forbidden response
  - Test: `PlayerAccessTests.cs::Player_CannotModifyExercise`

### UI Adaptations

- [ ] **AC-11**: Given I am a Player, when I view the exercise page, then I see ONLY the "Timeline" tab
  - Test: `ExercisePage.test.tsx::Player_SeesOnlyTimelineTab`
  - Hide: Conduct, Configuration, Participants, Objectives tabs

- [ ] **AC-12**: Given I am a Player viewing the timeline, when I see an inject, then fire/skip/edit buttons are hidden
  - Test: `InjectTimeline.test.tsx::Player_CannotSeeActionButtons`

- [ ] **AC-13**: Given I am a Player, when I view an inject detail, then only public fields are shown
  - Test: `InjectDetail.test.tsx::Player_SeesOnlyPublicFields`
  - Show: Number, Time, Description, Source, Method
  - Hide: Controller Notes, Expected Action, Objectives, Status (if pending)

## Out of Scope

- Player acknowledgment/confirmation of injects (future enhancement)
- Player-to-SimCell messaging (future enhancement)
- Player-specific mobile app (separate epic)
- Restricting injects to specific players (future enhancement - requires target field)

## Dependencies

- hseep-participant-roles/S01: Extend ExerciseRole Enum
- hseep-participant-roles/S02: Update Role Assignment UI

## Permission Matrix Update

Add to `_core/user-roles.md` permission matrix:

### Exercise Management

| Permission | Admin | Director | Controller | Evaluator | Observer | **Player** |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|:----------:|
| View Exercise Details | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| View Exercise List | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |

### Inject Management (Conduct Phase)

| Permission | Admin | Director | Controller | Evaluator | Observer | **Player** |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|:----------:|
| Fire Inject | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| View Pending Injects | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| View Fired Injects | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| View Controller Notes | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ |
| View Expected Actions | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |

### Participant Management

| Permission | Admin | Director | Controller | Evaluator | Observer | **Player** |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|:----------:|
| View Participant List | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |

### Objective Management

| Permission | Admin | Director | Controller | Evaluator | Observer | **Player** |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|:----------:|
| View Objectives | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |

## Technical Implementation

### Backend Authorization

**File**: `src/Cadence.Core/Infrastructure/Authorization/RolePermissions.cs`

```csharp
public static class RolePermissions
{
    public static bool CanViewPendingInjects(ExerciseRole role) =>
        role is ExerciseRole.Administrator
            or ExerciseRole.ExerciseDirector
            or ExerciseRole.Controller
            or ExerciseRole.Evaluator;

    public static bool CanViewControllerNotes(ExerciseRole role) =>
        role is ExerciseRole.Administrator
            or ExerciseRole.ExerciseDirector
            or ExerciseRole.Controller;

    public static bool CanViewExpectedActions(ExerciseRole role) =>
        role is ExerciseRole.Administrator
            or ExerciseRole.ExerciseDirector
            or ExerciseRole.Controller
            or ExerciseRole.Evaluator
            or ExerciseRole.Observer;

    public static bool CanViewObjectives(ExerciseRole role) =>
        role is ExerciseRole.Administrator
            or ExerciseRole.ExerciseDirector
            or ExerciseRole.Controller
            or ExerciseRole.Evaluator
            or ExerciseRole.Observer;

    public static bool CanViewParticipants(ExerciseRole role) =>
        role is ExerciseRole.Administrator
            or ExerciseRole.ExerciseDirector
            or ExerciseRole.Controller
            or ExerciseRole.Evaluator;
}
```

### API Filtering

**File**: `src/Cadence.Core/Features/Injects/Services/InjectService.cs`

```csharp
public async Task<IEnumerable<InjectDto>> GetInjectsForExerciseAsync(
    Guid exerciseId,
    Guid userId)
{
    var participant = await GetExerciseParticipantAsync(exerciseId, userId);

    var query = _context.Injects
        .Where(i => i.Msel.ExerciseId == exerciseId);

    // Filter by status for Players
    if (participant.Role == ExerciseRole.Player)
    {
        query = query.Where(i => i.Status == InjectStatus.Delivered);
    }

    var injects = await query.ToListAsync();

    // Map to DTOs with field filtering
    return injects.Select(i => MapToDto(i, participant.Role));
}

private InjectDto MapToDto(Inject inject, ExerciseRole role)
{
    var dto = _mapper.Map<InjectDto>(inject);

    // Filter sensitive fields for Players
    if (role == ExerciseRole.Player)
    {
        dto.ControllerNotes = null;
        dto.ExpectedAction = null;
        dto.Objectives = null;
    }

    if (!RolePermissions.CanViewControllerNotes(role))
    {
        dto.ControllerNotes = null;
    }

    return dto;
}
```

### Frontend Permission Hooks

**File**: `src/frontend/src/features/exercises/hooks/useExercisePermissions.ts`

```typescript
export const useExercisePermissions = (exerciseId: string) => {
  const { user } = useAuth();
  const { data: participant } = useQuery(['exerciseParticipant', exerciseId], () =>
    getExerciseParticipant(exerciseId, user.id)
  );

  return {
    canViewPendingInjects: canViewPendingInjects(participant?.role),
    canViewControllerNotes: canViewControllerNotes(participant?.role),
    canViewExpectedActions: canViewExpectedActions(participant?.role),
    canViewObjectives: canViewObjectives(participant?.role),
    canViewParticipants: canViewParticipants(participant?.role),
    canFireInjects: canFireInjects(participant?.role),
    isPlayer: participant?.role === ExerciseRole.Player,
  };
};
```

### UI Component Example

**File**: `src/frontend/src/features/exercises/pages/ExercisePage.tsx`

```typescript
const ExercisePage: React.FC = () => {
  const { exerciseId } = useParams();
  const permissions = useExercisePermissions(exerciseId);

  // Players only see Timeline tab
  if (permissions.isPlayer) {
    return <InjectTimeline exerciseId={exerciseId} readonly />;
  }

  return (
    <Tabs>
      <Tab label="Conduct" />
      <Tab label="Timeline" />
      {permissions.canViewObjectives && <Tab label="Objectives" />}
      {permissions.canViewParticipants && <Tab label="Participants" />}
      {/* ... other tabs */}
    </Tabs>
  );
};
```

## Test Coverage

### Backend Tests
- `src/Cadence.Core.Tests/Features/Authorization/PlayerAccessTests.cs`
- `src/Cadence.Core.Tests/Features/Injects/InjectServiceTests.cs::GetInjects_AsPlayer_FiltersCorrectly`

### Frontend Tests
- `src/frontend/src/features/exercises/pages/ExercisePage.test.tsx`
- `src/frontend/src/features/exercises/components/InjectTimeline.test.tsx`
- `src/frontend/src/features/exercises/components/InjectDetail.test.tsx`
- `src/frontend/src/features/exercises/hooks/useExercisePermissions.test.ts`

## Security Considerations

- [ ] Backend enforces ALL permission checks (never rely on frontend alone)
- [ ] API returns filtered DTOs, not full entities
- [ ] 403 responses logged for security audit
- [ ] Players cannot bypass filters through direct API calls

## Related Stories

- hseep-participant-roles/S08: Role-Specific UI Adaptations
- _core/user-roles.md: Update permission matrix documentation

---

*Last updated: 2026-02-09*
