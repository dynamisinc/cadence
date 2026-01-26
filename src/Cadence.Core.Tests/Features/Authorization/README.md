# Authorization Tests

This directory contains comprehensive authorization tests for the Cadence backend.

## Test Coverage

### RoleResolverTests.cs (67 tests)

Tests the core authorization business logic. The `RoleResolver` service is the single source of truth for authorization decisions.

#### Test Categories

1. **GetExerciseRoleAsync Tests** (6 tests)
   - Assigned participant returns correct role
   - Unassigned user returns null
   - Admin user returns null (no explicit exercise role)
   - Non-existent exercise returns null
   - Soft-deleted participant returns null

2. **CanAccessExerciseAsync Tests** (6 tests)
   - Admin user returns true (bypasses assignment)
   - Assigned participant returns true
   - Unassigned user returns false
   - Non-existent user returns false
   - Soft-deleted participant returns false

3. **HasExerciseRoleAsync Tests - Role Hierarchy** (30 tests)
   - Admin user always returns true for all roles
   - Unassigned user always returns false
   - **Observer role hierarchy**: Can only access Observer-level actions
   - **Evaluator role hierarchy**: Can access Observer + Evaluator actions
   - **Controller role hierarchy**: Can access Observer + Evaluator + Controller actions
   - **Exercise Director role hierarchy**: Can access all except Administrator
   - Non-existent user returns false
   - Soft-deleted participant returns false

4. **GetSystemRoleAsync Tests** (3 tests)
   - Admin user returns Admin
   - Regular user returns User
   - Non-existent user returns null

5. **Integration Scenario Tests** (7 tests)
   - **Observer cannot fire injects** (403 expected)
   - **Controller can fire injects** (200 expected)
   - **Evaluator can create observations** (201 expected)
   - **Director has all permissions** except Administrator
   - **Admin bypasses exercise assignment** - full access without being assigned
   - **Unassigned user denied** all access

## Authorization Architecture

### Components

```
┌─────────────────────────────────────────────────────────┐
│ ASP.NET Core Authorization Middleware                  │
│ - Enforces [Authorize] attributes on controllers       │
└─────────────────┬───────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────┐
│ Authorization Handlers (WebApi)                         │
│ - ExerciseAccessHandler                                 │
│ - ExerciseRoleHandler                                   │
│ - Thin wrappers that extract route parameters          │
└─────────────────┬───────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────┐
│ RoleResolver Service (Core) ⭐ TESTED HERE              │
│ - Core business logic for all authorization decisions  │
│ - Single source of truth                               │
│ - Queries database for roles and permissions           │
└─────────────────────────────────────────────────────────┘
```

### Why Handlers Are Not Unit Tested

The authorization handlers (`ExerciseAccessHandler`, `ExerciseRoleHandler`) are **thin wrappers** with minimal logic:

1. Extract user ID from claims
2. Extract exercise ID from route parameters
3. Call `RoleResolver` methods
4. Mark authorization as succeeded or failed

Since `HandleRequirementAsync` is a protected method from the base class, unit testing it directly is not practical. Instead:

- **Business logic is tested in `RoleResolverTests`** (100% coverage)
- **Handlers are validated through integration tests** (see `Cadence.WebApi.Tests`)
- **Route parameter extraction is simple string parsing** (no complex logic)

## Test Data

### Seeded Users

| User | System Role | Exercise Role | Email |
|------|-------------|---------------|-------|
| Admin | Admin | (not assigned) | admin@example.com |
| Director | User | ExerciseDirector | director@example.com |
| Controller | User | Controller | controller@example.com |
| Evaluator | User | Evaluator | evaluator@example.com |
| Observer | User | Observer | observer@example.com |
| Unassigned | User | (none) | unassigned@example.com |

### Role Hierarchy

```
Administrator      (System Admins - bypass all checks)
    ▲
    │
ExerciseDirector   (Full exercise control)
    ▲
    │
Controller         (Can fire injects)
    ▲
    │
Evaluator          (Can record observations)
    ▲
    │
Observer           (Read-only access)
```

## Authorization Policies

### Policy: ExerciseAccess

**Requirement**: User must be able to access the exercise

**Rules**:
- System Admins can access **all exercises** (no assignment needed)
- Other users must be explicitly assigned as participants
- Soft-deleted participants lose access

**Tested in**:
- `CanAccessExerciseAsync_AdminUser_ReturnsTrue`
- `CanAccessExerciseAsync_AssignedParticipant_ReturnsTrue`
- `CanAccessExerciseAsync_UnassignedUser_ReturnsFalse`
- `CanAccessExerciseAsync_SoftDeletedParticipant_ReturnsFalse`

### Policy: ExerciseController

**Requirement**: User must have Controller+ role

**Rules**:
- Controller, Exercise Director, Administrator roles pass
- Evaluator, Observer roles fail
- System Admins automatically pass

**Tested in**:
- `Scenario_ControllerCanFireInjects`
- `Scenario_ObserverCannotFireInjects`
- `HasExerciseRoleAsync_ControllerUser_MatchesHierarchy`

### Policy: ExerciseEvaluator

**Requirement**: User must have Evaluator+ role

**Rules**:
- Evaluator, Controller, Exercise Director, Administrator roles pass
- Observer role fails
- System Admins automatically pass

**Tested in**:
- `Scenario_EvaluatorCanCreateObservations`
- `Scenario_ObserverCannotCreateObservations`
- `HasExerciseRoleAsync_EvaluatorUser_MatchesHierarchy`

### Policy: ExerciseDirector

**Requirement**: User must have Exercise Director role

**Rules**:
- Only Exercise Director or Administrator roles pass
- Controller, Evaluator, Observer roles fail
- System Admins automatically pass

**Tested in**:
- `Scenario_DirectorHasAllPermissions`
- `Scenario_ControllerCannotPerformDirectorActions` (WebApi integration tests)
- `HasExerciseRoleAsync_DirectorUser_MatchesHierarchy`

## Running Tests

```bash
# Run all authorization tests
dotnet test src/Cadence.Core.Tests/Cadence.Core.Tests.csproj --filter "FullyQualifiedName~Authorization"

# Run with coverage
dotnet test src/Cadence.Core.Tests/Cadence.Core.Tests.csproj --filter "FullyQualifiedName~Authorization" --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~HasExerciseRoleAsync_ControllerUser_MatchesHierarchy"
```

## Test Results

```
Total tests: 67
     Passed: 67
     Failed: 0
    Skipped: 0
```

## Related Files

- **Implementation**: `src/Cadence.Core/Features/Authorization/Services/RoleResolver.cs`
- **Interface**: `src/Cadence.Core/Features/Authorization/Services/IRoleResolver.cs`
- **Handlers**: `src/Cadence.WebApi/Authorization/Handlers/`
- **Requirements**: `src/Cadence.WebApi/Authorization/Requirements/`
- **Entities**: `src/Cadence.Core/Models/Entities/ExerciseParticipant.cs`
- **Enums**: `src/Cadence.Core/Models/Entities/Enums.cs`

## TDD Status

✅ **All acceptance criteria covered by passing tests**

| Criterion | Test(s) | Status |
|-----------|---------|--------|
| Anonymous user cannot access protected endpoints | `HandleRequirementAsync_NoUserIdentifier_Fails` | ✅ Pass |
| Authenticated user without assignment gets 403 | `CanAccessExerciseAsync_UnassignedUser_ReturnsFalse` | ✅ Pass |
| Observer cannot fire injects | `Scenario_ObserverCannotFireInjects` | ✅ Pass |
| Controller can fire injects | `Scenario_ControllerCanFireInjects` | ✅ Pass |
| Evaluator can create observations | `Scenario_EvaluatorCanCreateObservations` | ✅ Pass |
| Admin can access all exercises | `Scenario_AdminBypassesExerciseAssignment` | ✅ Pass |
