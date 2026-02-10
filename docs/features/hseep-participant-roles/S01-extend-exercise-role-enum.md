# Story: S01 - Extend ExerciseRole Enum with HSEEP Roles

**Feature:** hseep-participant-roles
**Status:** 📋 Not Started

## User Story

**As a** Platform Developer,
**I want** the ExerciseRole enum to include all HSEEP-defined participant types,
**So that** the system can support complete exercise staff role assignments per HSEEP standards.

## Context

The current `ExerciseRole` enum in `Cadence.Core/Models/Entities/Enums.cs` defines five roles:
- Administrator (1)
- ExerciseDirector (2)
- Controller (3)
- Evaluator (4)
- Observer (5)

HSEEP 2020 defines additional exercise participant types that are essential for full exercise conduct:
- Player - Exercise participants being tested/trained
- Simulator - SimCell staff role-playing external entities
- Facilitator - Discussion guides for tabletop exercises
- SafetyOfficer - Safety oversight with pause/stop authority
- TrustedAgent - Subject matter experts embedded with players

This story extends the enum and updates all dependent code to recognize the new roles.

## Acceptance Criteria

### Backend Changes

- [ ] **AC-01**: Given the ExerciseRole enum, when I add the five new roles, then they have sequential values 6-10
  - Test: `EnumTests.cs::ExerciseRole_HasExpectedValues`
  - Values: Player=6, Simulator=7, Facilitator=8, SafetyOfficer=9, TrustedAgent=10

- [ ] **AC-02**: Given each new enum value, when I inspect its XML documentation, then it includes a HSEEP-aligned description
  - Test: Manual code review
  - Player: "Exercise participants being tested or trained"
  - Simulator: "SimCell staff role-playing external entities"
  - Facilitator: "Exercise staff guiding discussions and managing pace"
  - SafetyOfficer: "Safety oversight with authority to pause or terminate exercise"
  - TrustedAgent: "Subject matter experts embedded with players for observation and guidance"

- [ ] **AC-03**: Given the ExerciseUser entity, when I create a participant with a new role, then the role is persisted correctly
  - Test: `ExerciseUserTests.cs::CreateExerciseUser_WithNewRoles_PersistsSuccessfully`

- [ ] **AC-04**: Given existing seed data for roles, when I run migrations, then the new roles are seeded with appropriate display metadata
  - Test: Migration validation
  - If using a Roles lookup table, seed new role records

- [ ] **AC-05**: Given API endpoints that filter by role, when I query with new role values, then they are recognized and processed correctly
  - Test: `ExerciseParticipantServiceTests.cs::GetParticipantsByRole_WithNewRoles_ReturnsCorrectly`

### Frontend Changes

- [ ] **AC-06**: Given the ExerciseRole TypeScript enum, when I update it, then it matches the backend enum exactly
  - Test: `exerciseRole.test.ts::ExerciseRole_MatchesBackendValues`
  - File: `src/frontend/src/features/exercises/types/index.ts`

- [ ] **AC-07**: Given role display utilities, when I get the display name for a new role, then it returns the correct HSEEP-aligned text
  - Test: `roleUtils.test.ts::getRoleDisplayName_WithNewRoles_ReturnsCorrectNames`
  - Expected values match AC-02 descriptions

- [ ] **AC-08**: Given role color utilities, when I get the color for a new role, then it returns an appropriate color per design system
  - Test: `roleUtils.test.ts::getRoleColor_WithNewRoles_ReturnsValidColors`
  - Suggested colors (from COBRA system):
    - Player: theme.palette.info.main (blue)
    - Simulator: theme.palette.warning.main (orange/yellow)
    - Facilitator: theme.palette.success.main (green)
    - SafetyOfficer: theme.palette.error.main (red)
    - TrustedAgent: theme.palette.secondary.main (purple)

### Database Migration

- [ ] **AC-09**: Given existing ExerciseUser records, when migration runs, then no existing data is affected
  - Test: Migration rollback test
  - Enum values 1-5 remain unchanged

- [ ] **AC-10**: Given a fresh database, when migrations run, then all ten enum values are supported
  - Test: Fresh database creation test

## Out of Scope

- Role-specific permissions (covered in S03-S07)
- UI updates for role selection (covered in S02)
- Bulk import support (covered in S09)
- Exercise configuration for enabling/disabling roles (already exists in exercise-config/S01)

## Dependencies

- Database schema must support enum values as integers or strings
- TypeScript frontend must be regenerated if using code generation

## Technical Notes

### Backend Implementation

**File**: `src/Cadence.Core/Models/Entities/Enums.cs`

```csharp
/// <summary>
/// HSEEP-aligned roles for exercise participation.
/// Values start at 1 to support EF Core seeding (0 is not allowed for seed data PKs).
/// </summary>
public enum ExerciseRole
{
    /// <summary>System-wide configuration and user management.</summary>
    Administrator = 1,

    /// <summary>Full exercise management authority.</summary>
    ExerciseDirector = 2,

    /// <summary>Inject delivery and conduct management.</summary>
    Controller = 3,

    /// <summary>Observation recording for AAR.</summary>
    Evaluator = 4,

    /// <summary>Read-only exercise monitoring.</summary>
    Observer = 5,

    /// <summary>Exercise participants being tested or trained.</summary>
    Player = 6,

    /// <summary>SimCell staff role-playing external entities.</summary>
    Simulator = 7,

    /// <summary>Exercise staff guiding discussions and managing pace.</summary>
    Facilitator = 8,

    /// <summary>Safety oversight with authority to pause or terminate exercise.</summary>
    SafetyOfficer = 9,

    /// <summary>Subject matter experts embedded with players for observation and guidance.</summary>
    TrustedAgent = 10
}
```

### Frontend Implementation

**File**: `src/frontend/src/features/exercises/types/index.ts`

```typescript
export enum ExerciseRole {
  Administrator = 1,
  ExerciseDirector = 2,
  Controller = 3,
  Evaluator = 4,
  Observer = 5,
  Player = 6,
  Simulator = 7,
  Facilitator = 8,
  SafetyOfficer = 9,
  TrustedAgent = 10,
}

export const ExerciseRoleDisplayNames: Record<ExerciseRole, string> = {
  [ExerciseRole.Administrator]: 'Administrator',
  [ExerciseRole.ExerciseDirector]: 'Exercise Director',
  [ExerciseRole.Controller]: 'Controller',
  [ExerciseRole.Evaluator]: 'Evaluator',
  [ExerciseRole.Observer]: 'Observer',
  [ExerciseRole.Player]: 'Player',
  [ExerciseRole.Simulator]: 'Simulator',
  [ExerciseRole.Facilitator]: 'Facilitator',
  [ExerciseRole.SafetyOfficer]: 'Safety Officer',
  [ExerciseRole.TrustedAgent]: 'Trusted Agent',
};

export const ExerciseRoleDescriptions: Record<ExerciseRole, string> = {
  [ExerciseRole.Administrator]: 'System-wide configuration and user management',
  [ExerciseRole.ExerciseDirector]: 'Full exercise management authority',
  [ExerciseRole.Controller]: 'Inject delivery and conduct management',
  [ExerciseRole.Evaluator]: 'Observation recording for AAR',
  [ExerciseRole.Observer]: 'Read-only exercise monitoring',
  [ExerciseRole.Player]: 'Exercise participants being tested or trained',
  [ExerciseRole.Simulator]: 'SimCell staff role-playing external entities',
  [ExerciseRole.Facilitator]: 'Exercise staff guiding discussions and managing pace',
  [ExerciseRole.SafetyOfficer]: 'Safety oversight with authority to pause or terminate exercise',
  [ExerciseRole.TrustedAgent]: 'Subject matter experts embedded with players for observation and guidance',
};
```

### Migration Strategy

If using a Roles lookup table (per `_core/user-roles.md` migration notes):

```sql
-- Add new roles to lookup table
INSERT INTO Roles (Name, DisplayName, Description, SortOrder) VALUES
('Player', 'Player', 'Exercise participants being tested or trained', 6),
('Simulator', 'Simulator', 'SimCell staff role-playing external entities', 7),
('Facilitator', 'Facilitator', 'Exercise staff guiding discussions and managing pace', 8),
('SafetyOfficer', 'Safety Officer', 'Safety oversight with authority to pause or terminate exercise', 9),
('TrustedAgent', 'Trusted Agent', 'Subject matter experts embedded with players for observation and guidance', 10);
```

If storing enum directly in ExerciseUser table, no migration needed (just code changes).

## Implementation Sequence

1. **Backend**: Update `Enums.cs` with new enum values and XML docs
2. **Backend**: Update any role-based switch statements to handle new values (add default case to prevent errors)
3. **Backend**: Write unit tests for enum values and persistence
4. **Database**: Create and test migration script
5. **Frontend**: Update TypeScript enum definition
6. **Frontend**: Update role display utilities
7. **Frontend**: Write unit tests for role utilities
8. **Integration**: Verify end-to-end role assignment with new roles

## Test Coverage

### Backend Tests
- `src/Cadence.Core.Tests/Models/EnumTests.cs`
- `src/Cadence.Core.Tests/Features/Exercises/ExerciseUserTests.cs`
- `src/Cadence.Core.Tests/Features/Exercises/ExerciseParticipantServiceTests.cs`

### Frontend Tests
- `src/frontend/src/features/exercises/types/exerciseRole.test.ts`
- `src/frontend/src/features/exercises/utils/roleUtils.test.ts`

### Integration Tests
- `src/Cadence.Core.Tests/Integration/ExerciseParticipantIntegrationTests.cs`

## Related Stories

- hseep-participant-roles/S02: Update Role Assignment UI
- hseep-participant-roles/S03-S07: Define role-specific permissions
- exercise-config/S01: Configure Exercise Roles (already supports enabling/disabling roles)

---

*Last updated: 2026-02-09*
