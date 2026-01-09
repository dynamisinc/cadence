# Entity: User Roles

> **Type**: Core Domain Entity

## Overview

Cadence implements a simple Role-Based Access Control (RBAC) model with five HSEEP-aligned roles. Each user has exactly one role per exercise, and permissions are fixed (not configurable) in the MVP phase.

## The Five Roles

### Administrator

**HSEEP Alignment**: System management, not exercise-specific

**Description**: System-level user responsible for Cadence configuration, organization management, and user administration. Administrators manage the platform itself, not individual exercises.

**Typical User**: IT staff, emergency management coordinator, system owner

**Key Permissions**:
- Full system configuration access
- User account management
- Organization settings
- All exercise permissions (implied)

### Exercise Director

**HSEEP Alignment**: Senior Controller / Exercise Director

**Description**: Senior exercise leadership responsible for overall exercise management and real-time decision making during conduct. The Exercise Director has full authority over their exercises.

**Typical User**: Emergency manager, exercise program manager, lead planner

**Key Permissions**:
- Create, edit, delete exercises
- Manage exercise participants and roles
- Activate MSELs for conduct
- All Controller permissions (implied)

### Controller

**HSEEP Alignment**: Exercise Controller

**Description**: Exercise staff member responsible for delivering injects and guiding player actions during conduct. Controllers execute the MSEL during the exercise.

**Typical User**: Exercise controller, simulator, role player coordinator

**Key Permissions**:
- Fire and skip injects
- Update inject status
- View Controller notes
- Create/edit injects (authoring phase only)

### Evaluator

**HSEEP Alignment**: Evaluator

**Description**: Observer responsible for documenting player performance against exercise objectives for the After-Action Report. Evaluators record but do not influence the exercise.

**Typical User**: Evaluation team member, subject matter expert observer, AAR author

**Key Permissions**:
- Record observations
- Link observations to objectives
- View all injects and objectives
- Cannot fire or modify injects

### Observer

**HSEEP Alignment**: Observer / VIP

**Description**: Read-only participant who monitors exercise progress without active involvement. Observers see what happens but cannot interact with exercise data.

**Typical User**: VIP visitor, elected official, training observer, auditor

**Key Permissions**:
- View exercise timeline
- View fired injects
- No edit capabilities

## Permission Matrix

### Exercise Management

| Permission | Admin | Director | Controller | Evaluator | Observer |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|
| Create Exercise | ✅ | ✅ | ❌ | ❌ | ❌ |
| Edit Exercise | ✅ | ✅ | ❌ | ❌ | ❌ |
| Delete/Archive Exercise | ✅ | ✅ | ❌ | ❌ | ❌ |
| View Exercise List | ✅ | ✅ | ✅ | ✅ | ✅ |
| View Exercise Details | ✅ | ✅ | ✅ | ✅ | ✅ |
| Configure Practice Mode | ✅ | ✅ | ❌ | ❌ | ❌ |
| Set Time Zone | ✅ | ✅ | ❌ | ❌ | ❌ |

### Participant Management

| Permission | Admin | Director | Controller | Evaluator | Observer |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|
| Add Participants | ✅ | ✅ | ❌ | ❌ | ❌ |
| Remove Participants | ✅ | ✅ | ❌ | ❌ | ❌ |
| Change Participant Roles | ✅ | ✅ | ❌ | ❌ | ❌ |
| View Participant List | ✅ | ✅ | ✅ | ✅ | ❌ |

### MSEL Management

| Permission | Admin | Director | Controller | Evaluator | Observer |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|
| Create MSEL Version | ✅ | ✅ | ✅ | ❌ | ❌ |
| Activate MSEL | ✅ | ✅ | ❌ | ❌ | ❌ |
| Duplicate MSEL | ✅ | ✅ | ✅ | ❌ | ❌ |
| Import from Excel | ✅ | ✅ | ✅ | ❌ | ❌ |
| Export to Excel | ✅ | ✅ | ✅ | ✅ | ❌ |

### Inject Management (Authoring Phase)

| Permission | Admin | Director | Controller | Evaluator | Observer |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|
| Create Inject | ✅ | ✅ | ✅ | ❌ | ❌ |
| Edit Inject | ✅ | ✅ | ✅ | ❌ | ❌ |
| Delete Inject | ✅ | ✅ | ✅ | ❌ | ❌ |
| Reorder Injects | ✅ | ✅ | ✅ | ❌ | ❌ |
| View Inject List | ✅ | ✅ | ✅ | ✅ | ✅* |
| View Inject Details | ✅ | ✅ | ✅ | ✅ | ✅* |
| View Controller Notes | ✅ | ✅ | ✅ | ❌ | ❌ |

*\* Observers can only view fired injects during conduct*

### Inject Management (Conduct Phase)

| Permission | Admin | Director | Controller | Evaluator | Observer |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|
| Fire Inject | ✅ | ✅ | ✅ | ❌ | ❌ |
| Skip Inject | ✅ | ✅ | ✅ | ❌ | ❌ |
| View Pending Injects | ✅ | ✅ | ✅ | ✅ | ❌ |
| View Fired Injects | ✅ | ✅ | ✅ | ✅ | ✅ |

### Objective Management

| Permission | Admin | Director | Controller | Evaluator | Observer |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|
| Create Objective | ✅ | ✅ | ❌ | ❌ | ❌ |
| Edit Objective | ✅ | ✅ | ❌ | ❌ | ❌ |
| Delete Objective | ✅ | ✅ | ❌ | ❌ | ❌ |
| Link Inject to Objective | ✅ | ✅ | ✅ | ❌ | ❌ |
| View Objectives | ✅ | ✅ | ✅ | ✅ | ✅ |

### Phase Management

| Permission | Admin | Director | Controller | Evaluator | Observer |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|
| Create Phase | ✅ | ✅ | ❌ | ❌ | ❌ |
| Edit Phase | ✅ | ✅ | ❌ | ❌ | ❌ |
| Delete Phase | ✅ | ✅ | ❌ | ❌ | ❌ |
| Assign Inject to Phase | ✅ | ✅ | ✅ | ❌ | ❌ |
| View Phases | ✅ | ✅ | ✅ | ✅ | ✅ |

### Observation Management

| Permission | Admin | Director | Controller | Evaluator | Observer |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|
| Create Observation | ✅ | ✅ | ❌ | ✅ | ❌ |
| Edit Own Observation | ✅ | ✅ | ❌ | ✅ | ❌ |
| Delete Own Observation | ✅ | ✅ | ❌ | ✅ | ❌ |
| View All Observations | ✅ | ✅ | ❌ | ✅ | ❌ |

### System Administration

| Permission | Admin | Director | Controller | Evaluator | Observer |
|------------|:-----:|:--------:|:----------:|:---------:|:--------:|
| Manage Users | ✅ | ❌ | ❌ | ❌ | ❌ |
| Manage Organizations | ✅ | ❌ | ❌ | ❌ | ❌ |
| View Audit Logs | ✅ | ❌ | ❌ | ❌ | ❌ |
| System Configuration | ✅ | ❌ | ❌ | ❌ | ❌ |

## Role Assignment Rules

### Business Rules

1. **Single Role Per Exercise**: A user has exactly one role within an exercise
2. **Multiple Exercise Roles**: Same user can have different roles in different exercises
3. **Role Changes**: Only Administrator and Exercise Director can change role assignments
4. **Self-Demotion**: Directors cannot demote themselves (prevents lockout)
5. **Minimum Director**: Each exercise must have at least one Exercise Director

### Role Hierarchy

```
┌─────────────────┐
│  Administrator  │  ← System-wide access
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│Exercise Director│  ← Full exercise access
└────────┬────────┘
         │
    ┌────┴────┐
    ▼         ▼
┌────────┐ ┌─────────┐
│Controller│ │Evaluator│  ← Specialized access
└────┬───┘ └────┬────┘
     │          │
     └────┬─────┘
          ▼
    ┌──────────┐
    │ Observer │  ← Read-only access
    └──────────┘
```

### Inheritance Rules

| Higher Role | Inherits From |
|-------------|---------------|
| Administrator | Exercise Director + System permissions |
| Exercise Director | Controller + Evaluator + Management permissions |
| Controller | Base conduct permissions |
| Evaluator | Base observation permissions |
| Observer | Minimal read-only permissions |

## Entity Definition

### ExerciseParticipant (Join Table)

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Id` | GUID | Yes | Unique identifier |
| `ExerciseId` | GUID | Yes | FK to Exercise |
| `UserId` | GUID | Yes | FK to User |
| `Role` | enum | Yes | Assigned role |
| `AddedAt` | DateTime | Yes | When added |
| `AddedBy` | GUID | Yes | Who added them |

### User Entity (Reference)

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Id` | GUID | Yes | Unique identifier |
| `Email` | string | Yes | Login email |
| `DisplayName` | string | Yes | Shown in UI |
| `IsSystemAdmin` | bool | Yes | System-wide admin flag |
| `OrganizationId` | GUID | Yes | Primary organization |

## API Authorization

### Endpoint Security Pattern

```csharp
// Controller example
[Authorize(Roles = "Administrator,ExerciseDirector,Controller")]
[HttpPost("exercises/{exerciseId}/injects/{injectId}/fire")]
public async Task<IActionResult> FireInject(Guid exerciseId, Guid injectId)
{
    // Additional exercise-scoped authorization
    var participant = await _context.ExerciseParticipants
        .FirstOrDefaultAsync(p => p.ExerciseId == exerciseId 
                               && p.UserId == User.GetUserId());
    
    if (participant == null)
        return Forbid();
    
    if (!CanFireInject(participant.Role))
        return Forbid();
    
    // Proceed with action...
}
```

### Permission Check Helper

```csharp
public static class RolePermissions
{
    public static bool CanFireInject(Role role) =>
        role is Role.Administrator or Role.ExerciseDirector or Role.Controller;
    
    public static bool CanCreateObservation(Role role) =>
        role is Role.Administrator or Role.ExerciseDirector or Role.Evaluator;
    
    public static bool CanManageParticipants(Role role) =>
        role is Role.Administrator or Role.ExerciseDirector;
    
    // ... additional permission methods
}
```

## UI Considerations

### Role Display

| Role | Badge | Color | Icon |
|------|-------|-------|------|
| Administrator | Admin | Red | 🔧 |
| Exercise Director | Director | Blue | 👔 |
| Controller | Controller | Green | 🎮 |
| Evaluator | Evaluator | Purple | 📋 |
| Observer | Observer | Gray | 👁️ |

### Conditional UI Elements

Elements should be hidden (not just disabled) when user lacks permission:

```jsx
// React example
{canEdit && (
  <Button onClick={handleEdit}>Edit</Button>
)}

{canFireInject && (
  <Button onClick={handleFire} color="primary">
    Fire Inject
  </Button>
)}
```

### Role Selection UI

When assigning participants, show role descriptions:

```
┌─────────────────────────────────────────────────────────────┐
│ Assign Role to Jane Smith                                   │
├─────────────────────────────────────────────────────────────┤
│ ○ Exercise Director                                         │
│   Full exercise management and conduct authority            │
│                                                             │
│ ● Controller                                                │
│   Deliver injects and manage exercise conduct               │
│                                                             │
│ ○ Evaluator                                                 │
│   Record observations for After-Action Report               │
│                                                             │
│ ○ Observer                                                  │
│   View-only access to exercise progress                     │
└─────────────────────────────────────────────────────────────┘
```

## Migration Notes

### Role Enum

```sql
-- Roles stored as string for readability in database
-- Consider creating lookup table for referential integrity

CREATE TABLE Roles (
    Name NVARCHAR(20) PRIMARY KEY,
    DisplayName NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    SortOrder INT NOT NULL
);

INSERT INTO Roles (Name, DisplayName, Description, SortOrder) VALUES
('Administrator', 'Administrator', 'System-wide configuration and user management', 1),
('ExerciseDirector', 'Exercise Director', 'Full exercise management authority', 2),
('Controller', 'Controller', 'Inject delivery and conduct management', 3),
('Evaluator', 'Evaluator', 'Observation recording for AAR', 4),
('Observer', 'Observer', 'Read-only exercise monitoring', 5);
```

### Exercise Participant Table

```sql
CREATE TABLE ExerciseParticipants (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ExerciseId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    AddedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    AddedBy UNIQUEIDENTIFIER NOT NULL,
    
    CONSTRAINT FK_ExerciseParticipant_Exercise FOREIGN KEY (ExerciseId) 
        REFERENCES Exercises(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ExerciseParticipant_User FOREIGN KEY (UserId) 
        REFERENCES Users(Id),
    CONSTRAINT FK_ExerciseParticipant_Role FOREIGN KEY (Role) 
        REFERENCES Roles(Name),
    CONSTRAINT UQ_ExerciseParticipant_ExerciseUser UNIQUE (ExerciseId, UserId)
);

CREATE INDEX IX_ExerciseParticipant_UserId ON ExerciseParticipants(UserId);
```

## Future Considerations (Standard Phase)

1. **Custom Permissions**: Allow Exercise Directors to customize role permissions per exercise
2. **Role Templates**: Save custom permission sets as templates
3. **Delegation**: Allow Directors to delegate specific permissions to Controllers
4. **Audit Log**: Track all permission checks and denials

## Related Documentation

- [Exercise Entity](./exercise-entity.md) - Exercise structure
- [Inject Entity](./inject-entity.md) - Inject permissions
- [Domain Glossary](../DOMAIN_GLOSSARY.md) - Role definitions

---

*Last updated: 2025-01-08*
