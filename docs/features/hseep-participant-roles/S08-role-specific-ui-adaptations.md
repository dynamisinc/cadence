# Story: S08 - Role-Specific UI Adaptations

**Feature:** hseep-participant-roles
**Status:** 📋 Not Started

## User Story

**As a** Cadence user,
**I want** the UI to adapt based on my assigned role,
**So that** I see only relevant functionality and am not overwhelmed by features I cannot use.

## Context

After implementing all five new HSEEP roles (Player, Simulator, Facilitator, Safety Officer, Trusted Agent) with distinct permissions, the UI must adapt to show only role-appropriate features. This story consolidates UI adaptations across the application to provide role-optimized experiences.

## Acceptance Criteria

### Navigation & Layout

- [ ] **AC-01**: Given I am a Player, when I access the exercise, then I see only the Timeline view (no tabs)
  - Test: `ExercisePage.test.tsx::Player_SeesTimelineOnly`

- [ ] **AC-02**: Given I am a Simulator, when I access the exercise, then I see Conduct and Timeline tabs only
  - Test: `ExercisePage.test.tsx::Simulator_SeesLimitedTabs`

- [ ] **AC-03**: Given I am a Facilitator, when I access the exercise, then I see Conduct, Timeline, Objectives, and Participants tabs
  - Test: `ExercisePage.test.tsx::Facilitator_SeesFullConductTabs`

- [ ] **AC-04**: Given I am a Safety Officer, when I access the exercise, then I see Timeline, Participants, and Safety tabs
  - Test: `ExercisePage.test.tsx::SafetyOfficer_SeesSafetyFocusedTabs`

- [ ] **AC-05**: Given I am a Trusted Agent, when I access the exercise, then I see Timeline, Objectives, and Observations tabs
  - Test: `ExercisePage.test.tsx::TrustedAgent_SeesObservationFocusedTabs`

### Conduct View Adaptations

- [ ] **AC-06**: Given I am a Facilitator, when I view the conduct page, then I see pause/resume clock controls
  - Test: `ConductView.test.tsx::Facilitator_SeesClockControls`

- [ ] **AC-07**: Given I am a Safety Officer, when I view any exercise page, then I see a prominent Emergency Stop button
  - Test: `ConductView.test.tsx::SafetyOfficer_SeesEmergencyStopButton`

- [ ] **AC-08**: Given I am a Simulator, when I view pending injects, then I see a "Mark Sent" button instead of "Fire"
  - Test: `InjectRow.test.tsx::Simulator_SeesMarkSentButton`

### Timeline View Adaptations

- [ ] **AC-09**: Given I am a Player, when I view the timeline, then I see only delivered injects with public fields
  - Test: `InjectTimeline.test.tsx::Player_SeesPublicFieldsOnly`
  - Hidden: Controller Notes, Expected Actions, Objectives

- [ ] **AC-10**: Given I am an Observer, when I view the timeline, then I see delivered injects with full details
  - Test: `InjectTimeline.test.tsx::Observer_SeesFullDeliveredInjects`

- [ ] **AC-11**: Given I am a Trusted Agent, when I view the timeline, then I see expected actions but not controller notes
  - Test: `InjectTimeline.test.tsx::TrustedAgent_SeesExpectedActionsNotNotes`

### Observations Adaptations

- [ ] **AC-12**: Given I am a Trusted Agent, when I create an observation, then I see a "Coaching Provided" checkbox
  - Test: `CreateObservationForm.test.tsx::TrustedAgent_SeesCoachingCheckbox`

- [ ] **AC-13**: Given I am a Safety Officer, when I create an observation, then I see safety severity options
  - Test: `CreateObservationForm.test.tsx::SafetyOfficer_SeesSafetySeverityField`

- [ ] **AC-14**: Given I am an Evaluator, when I create an observation, then I do NOT see coaching or safety fields
  - Test: `CreateObservationForm.test.tsx::Evaluator_SeesStandardObservationForm`

### Dashboard/Home Adaptations

- [ ] **AC-15**: Given I am a Player, when I view my homepage, then I see only exercises where I am assigned as Player
  - Test: `HomePage.test.tsx::Player_SeesOnlyAssignedExercises`

- [ ] **AC-16**: Given I am a Safety Officer, when I view the exercise list, then exercises with upcoming start dates are highlighted
  - Test: `ExerciseList.test.tsx::SafetyOfficer_SeesUpcomingExerciseAlerts`

### Empty States & Guidance

- [ ] **AC-17**: Given I am a Player with no assigned exercises, when I view my homepage, then I see guidance explaining my role
  - Test: `HomePage.test.tsx::Player_SeesEmptyStateGuidance`
  - "Players are participants being tested/trained in exercises. You'll see exercises here when an Exercise Director assigns you."

- [ ] **AC-18**: Given I am a Simulator, when I view an exercise with no simulation injects, then I see guidance
  - Test: `ConductView.test.tsx::Simulator_SeesNoSimulationsGuidance`
  - "No simulation injects assigned. Contact the Exercise Director if you expect to see simulations."

## Out of Scope

- Role-specific dashboards (future enhancement)
- Customizable UI per role (future enhancement)
- Role-based color themes (future enhancement)

## Dependencies

- hseep-participant-roles/S01-S07: All role permission stories
- All existing UI components

## Technical Implementation

### Centralized Permission Hook

**File**: `src/frontend/src/features/exercises/hooks/useExercisePermissions.ts`

```typescript
export interface ExercisePermissions {
  // View permissions
  canViewPendingInjects: boolean;
  canViewControllerNotes: boolean;
  canViewExpectedActions: boolean;
  canViewObjectives: boolean;
  canViewParticipants: boolean;

  // Conduct permissions
  canFireInjects: boolean;
  canSkipInjects: boolean;
  canControlClock: boolean;
  canEmergencyStop: boolean;

  // Observation permissions
  canCreateObservations: boolean;
  canCreateSafetyObservations: boolean;
  canFlagCoaching: boolean;

  // Configuration permissions
  canModifyExercise: boolean;
  canManageParticipants: boolean;

  // Role identification
  role: ExerciseRole;
  isPlayer: boolean;
  isSimulator: boolean;
  isFacilitator: boolean;
  isSafetyOfficer: boolean;
  isTrustedAgent: boolean;
}

export const useExercisePermissions = (exerciseId: string): ExercisePermissions => {
  const { user } = useAuth();
  const { data: participant } = useQuery(
    ['exerciseParticipant', exerciseId],
    () => getExerciseParticipant(exerciseId, user.id)
  );

  const role = participant?.role ?? ExerciseRole.Observer;

  return {
    canViewPendingInjects: canViewPendingInjects(role),
    canViewControllerNotes: canViewControllerNotes(role),
    canViewExpectedActions: canViewExpectedActions(role),
    canViewObjectives: canViewObjectives(role),
    canViewParticipants: canViewParticipants(role),

    canFireInjects: canFireInjects(role),
    canSkipInjects: canSkipInjects(role),
    canControlClock: canControlClock(role),
    canEmergencyStop: canEmergencyStop(role),

    canCreateObservations: canCreateObservations(role),
    canCreateSafetyObservations: canCreateSafetyObservations(role),
    canFlagCoaching: canFlagCoaching(role),

    canModifyExercise: canModifyExercise(role),
    canManageParticipants: canManageParticipants(role),

    role,
    isPlayer: role === ExerciseRole.Player,
    isSimulator: role === ExerciseRole.Simulator,
    isFacilitator: role === ExerciseRole.Facilitator,
    isSafetyOfficer: role === ExerciseRole.SafetyOfficer,
    isTrustedAgent: role === ExerciseRole.TrustedAgent,
  };
};
```

### Role-Adaptive Exercise Page

**File**: `src/frontend/src/features/exercises/pages/ExercisePage.tsx`

```typescript
const ExercisePage: React.FC = () => {
  const { exerciseId } = useParams();
  const permissions = useExercisePermissions(exerciseId);

  // Player sees timeline only
  if (permissions.isPlayer) {
    return <PlayerTimelineView exerciseId={exerciseId} />;
  }

  // Safety Officer sees safety-focused view
  if (permissions.isSafetyOfficer) {
    return (
      <>
        <EmergencyStopButton exerciseId={exerciseId} />
        <Tabs>
          <Tab label="Timeline" component={<InjectTimeline exerciseId={exerciseId} />} />
          <Tab label="Participants" component={<ParticipantsList exerciseId={exerciseId} />} />
          <Tab label="Safety" component={<SafetyObservations exerciseId={exerciseId} />} />
        </Tabs>
      </>
    );
  }

  // Standard tabbed interface for other roles
  return (
    <Tabs>
      {permissions.canFireInjects && <Tab label="Conduct" component={<ConductView />} />}
      <Tab label="Timeline" component={<InjectTimeline />} />
      {permissions.canViewObjectives && <Tab label="Objectives" component={<ObjectivesList />} />}
      {permissions.canViewParticipants && <Tab label="Participants" component={<ParticipantsList />} />}
      {permissions.canCreateObservations && <Tab label="Observations" component={<ObservationsList />} />}
      {permissions.canModifyExercise && <Tab label="Configuration" component={<ExerciseConfig />} />}
    </Tabs>
  );
};
```

### Role-Specific Components

**File**: `src/frontend/src/features/exercises/components/PlayerTimelineView.tsx`

```typescript
/**
 * Simplified timeline view for Players - shows only delivered injects with public fields
 */
export const PlayerTimelineView: React.FC<{ exerciseId: string }> = ({ exerciseId }) => {
  const { data: injects } = useQuery(
    ['injects', exerciseId, 'delivered'],
    () => getDeliveredInjects(exerciseId)
  );

  return (
    <Box>
      <Typography variant="h5" gutterBottom>
        Exercise Timeline
      </Typography>
      <Alert severity="info" sx={{ mb: 2 }}>
        You are viewing this exercise as a Player. This timeline shows events that have been delivered during the exercise.
      </Alert>
      {injects?.map((inject) => (
        <InjectCard
          key={inject.id}
          inject={inject}
          showPublicFieldsOnly
          disableActions
        />
      ))}
    </Box>
  );
};
```

## Test Coverage

### Frontend Tests
- `src/frontend/src/features/exercises/pages/ExercisePage.test.tsx`
- `src/frontend/src/features/exercises/components/ConductView.test.tsx`
- `src/frontend/src/features/exercises/components/InjectTimeline.test.tsx`
- `src/frontend/src/features/exercises/components/PlayerTimelineView.test.tsx`
- `src/frontend/src/features/observations/components/CreateObservationForm.test.tsx`
- `src/frontend/src/features/exercises/hooks/useExercisePermissions.test.ts`

## Accessibility Considerations

- [ ] Hidden features are removed from DOM (not just CSS hidden) for screen readers
- [ ] Role-specific guidance uses appropriate ARIA labels
- [ ] Emergency Stop button has high-contrast styling for visibility

## Related Stories

- hseep-participant-roles/S01-S07: All role permission implementations

---

*Last updated: 2026-02-09*
