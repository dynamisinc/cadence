# Entity: Inject

> **Type**: Core Domain Entity

## Overview

An Inject is a single event, message, or piece of information introduced into an exercise to drive player actions. Injects are the fundamental building blocks of a MSEL (Master Scenario Events List) and represent what happens during exercise conduct.

Cadence supports **dual time tracking**: each inject has both a Scheduled Time (when to deliver) and an optional Scenario Time (the fictional in-story time).

## Entity Definition

### Core Properties

| Property | Type | Required | Description | Constraints |
|----------|------|----------|-------------|-------------|
| `Id` | GUID | Yes | Unique identifier | System-generated |
| `MselId` | GUID | Yes | Parent MSEL | FK to MSEL |
| `InjectNumber` | int | Yes | Sequential number | Unique within MSEL, auto-generated |
| `Title` | string | Yes | Brief descriptive name | 1-200 characters |
| `Description` | string | Yes | Full inject content | 1-4000 characters |

### Time Properties (Dual Time Tracking)

| Property | Type | Required | Description | Constraints |
|----------|------|----------|-------------|-------------|
| `ScheduledTime` | time | Yes | Planned delivery time (wall clock) | HH:MM format |
| `ScenarioDay` | int? | No | In-story day number | 1-99, null if not used |
| `ScenarioTime` | time? | No | In-story time | HH:MM format |

### Targeting Properties

| Property | Type | Required | Description | Constraints |
|----------|------|----------|-------------|-------------|
| `Target` | string | Yes | Player/role receiving inject | Max 200 characters |
| `Source` | string | No | Simulated origin | Max 200 characters |
| `DeliveryMethod` | enum | No | How inject is delivered | See Delivery Methods |

### Organization Properties

| Property | Type | Required | Description | Constraints |
|----------|------|----------|-------------|-------------|
| `PhaseId` | GUID? | No | Exercise phase | FK to Phase |
| `InjectType` | enum | Yes | Type of inject | Default: Standard |
| `Status` | enum | Yes | Current status | See Status Values |

### Branching Properties

| Property | Type | Required | Description | Constraints |
|----------|------|----------|-------------|-------------|
| `ParentInjectId` | GUID? | No | Parent inject for branching | FK to Inject |
| `TriggerCondition` | string | No | When to fire this branch | Max 500 characters |

### Supplemental Properties

| Property | Type | Required | Description | Constraints |
|----------|------|----------|-------------|-------------|
| `ExpectedAction` | string | No | Anticipated player response | Max 2000 characters |
| `ControllerNotes` | string | No | Private guidance for Controller | Max 2000 characters |
| `Sequence` | int | Yes | Display order | Auto-managed |

### Conduct Properties (Updated During Exercise)

| Property | Type | Required | Description | Constraints |
|----------|------|----------|-------------|-------------|
| `FiredAt` | DateTime? | No | Actual UTC delivery time | Set when fired |
| `FiredBy` | GUID? | No | Controller who fired | FK to User |
| `SkippedAt` | DateTime? | No | When skipped | Set when skipped |
| `SkippedBy` | GUID? | No | Who skipped | FK to User |
| `SkipReason` | string | No | Why skipped | Max 500 characters |

### Audit Fields

| Property | Type | Description |
|----------|------|-------------|
| `CreatedAt` | DateTime | UTC creation timestamp |
| `CreatedBy` | GUID | User who created |
| `ModifiedAt` | DateTime | UTC last modified |
| `ModifiedBy` | GUID | User who last modified |
| `IsDeleted` | bool | Soft delete flag |
| `DeletedAt` | DateTime? | UTC deletion timestamp |

## Enumerations

### Inject Types

| Value | Display Name | Description | Use Case |
|-------|--------------|-------------|----------|
| `Standard` | Standard | Delivered at scheduled time | Most common |
| `Contingency` | Contingency | Used if players deviate | Guide back on track |
| `Adaptive` | Adaptive | Branch based on player decision | Realistic consequences |
| `Complexity` | Complexity | Increase difficulty | Challenge advanced players |

### Inject Status

| Value | Display Name | Description | Set By |
|-------|--------------|-------------|--------|
| `Pending` | Pending | Not yet delivered | Default |
| `Fired` | Fired | Delivered to players | Controller action |
| `Skipped` | Skipped | Intentionally not delivered | Controller action |

```
┌─────────┐
│ Pending │──────┬──────▶ Fired
└─────────┘      │
                 └──────▶ Skipped
```

### Delivery Methods

| Value | Display Name | Description |
|-------|--------------|-------------|
| `Verbal` | Verbal | Spoken directly to player |
| `Phone` | Phone Call | Simulated phone call |
| `Email` | Email | Simulated email |
| `Radio` | Radio | Radio communication |
| `Written` | Written Document | Paper document |
| `Simulation` | Simulation System | CAX/simulation input |
| `Other` | Other | Custom method |

## Dual Time Tracking Explained

### The Problem

Real exercises often simulate multi-day scenarios within a few hours. For example:
- **Real time**: Exercise runs from 9:00 AM to 12:00 PM (3 hours)
- **Story time**: Scenario spans Day 1 morning through Day 3 afternoon (60+ hours)

Without dual time tracking, Controllers must mentally calculate the scenario timeline, leading to confusion and errors.

### The Solution

Cadence maintains two separate time values:

| Time Type | Purpose | Example |
|-----------|---------|---------|
| **Scheduled Time** | When Controller delivers inject | 09:30 |
| **Scenario Time** | When event "happens" in story | Day 2, 14:30 |

### Display Format

```
┌────────────────────────────────────────────────────────────────┐
│ Inject #15: Emergency Broadcast Alert                          │
├────────────────────────────────────────────────────────────────┤
│ Deliver at: 10:15 AM          │  Scenario: Day 2 @ 18:00       │
│ (45 minutes into exercise)    │  (Second day, 6:00 PM)         │
├────────────────────────────────────────────────────────────────┤
│ Target: Public Information Officer                             │
│ Method: Email                                                  │
├────────────────────────────────────────────────────────────────┤
│ Content: "Governor has declared State of Emergency..."         │
└────────────────────────────────────────────────────────────────┘
```

### Scenario Time Configuration

Scenario time is configured at the Exercise level:
- `ScenarioStartDay`: What day number the scenario begins (default: 1)
- `ScenarioStartTime`: What time the scenario begins (e.g., "06:00")

Individual injects specify their scenario day and time relative to this baseline.

## Relationships

### Belongs To

| Related Entity | Relationship | Required |
|----------------|--------------|----------|
| MSEL | Many-to-One | Yes |
| Phase | Many-to-One | No |
| Parent Inject | Many-to-One (self-ref) | No |
| Fired By User | Many-to-One | No (set on fire) |

### Has Many

| Related Entity | Relationship | Cascade Behavior |
|----------------|--------------|------------------|
| Child Injects | One-to-Many (self-ref) | Orphan (set ParentId to null) |
| Objective Links | Many-to-Many | Cascade delete links |
| Observations | One-to-Many | Cascade delete |

## Business Rules

### Creation Rules

1. **Title Required**: Cannot be empty, max 200 characters
2. **Description Required**: Full inject content required
3. **Scheduled Time Required**: Must specify delivery time
4. **Scenario Time Optional**: Only required if exercise uses scenario time
5. **Auto-Number**: Inject number auto-assigned as next in sequence
6. **Default Type**: New injects are "Standard" type
7. **Default Status**: New injects are "Pending" status

### Sequence Rules

1. Sequence determines display order (separate from inject number)
2. Sequence can be manually reordered via drag-and-drop
3. New injects added at end of sequence by default
4. Importing preserves original sequence

### Branching Rules

1. Branching injects must have a parent inject
2. Parent must be in same MSEL
3. Trigger condition describes when to use branch
4. Branching limited to 2 levels in MVP (parent → child)
5. Orphaned children (parent deleted) retain ParentInjectId but show warning

### Status Transition Rules

1. **Pending → Fired**: Records timestamp, user, cannot be undone
2. **Pending → Skipped**: Records timestamp, user, reason; cannot be undone
3. **Fired/Skipped**: Terminal states in MVP (no undo)

### Deletion Rules

1. Soft delete only (IsDeleted = true)
2. Child injects are orphaned, not deleted
3. Objective links are removed
4. Observations remain linked for audit trail

## Validation Rules

```csharp
// Pseudocode validation rules
public class InjectValidator
{
    public ValidationResult Validate(Inject inject)
    {
        // Required fields
        Require(inject.Title).NotEmpty().MaxLength(200);
        Require(inject.Description).NotEmpty().MaxLength(4000);
        Require(inject.ScheduledTime).NotNull();
        Require(inject.Target).NotEmpty().MaxLength(200);
        
        // Optional field constraints
        Optional(inject.Source).MaxLength(200);
        Optional(inject.ExpectedAction).MaxLength(2000);
        Optional(inject.ControllerNotes).MaxLength(2000);
        Optional(inject.TriggerCondition).MaxLength(500);
        Optional(inject.SkipReason).MaxLength(500);
        
        // Scenario time validation
        When(inject.ScenarioTime).IsNotNull()
            .Then(inject.ScenarioDay).IsNotNull();
        When(inject.ScenarioDay).IsNotNull()
            .Then(inject.ScenarioDay >= 1 && inject.ScenarioDay <= 99);
        
        // Branching validation
        When(inject.ParentInjectId).IsNotNull()
            .Then(inject.InjectType != InjectType.Standard);
        When(inject.InjectType).In(Contingency, Adaptive, Complexity)
            .And(inject.ParentInjectId).IsNotNull()
            .Then(inject.TriggerCondition).IsNotEmpty();
    }
}
```

## Query Patterns

### Common Queries

| Query | Use Case | Index Recommendation |
|-------|----------|---------------------|
| By MSEL | Inject list | `IX_Inject_MselId_Sequence` |
| By Phase | Phase filtering | `IX_Inject_PhaseId` |
| Pending only | Conduct view | `IX_Inject_MselId_Status` |
| By scheduled time | Timeline view | `IX_Inject_MselId_ScheduledTime` |

### Example Queries

```sql
-- Injects for MSEL in sequence order
SELECT * FROM Injects 
WHERE MselId = @MselId 
  AND IsDeleted = 0
ORDER BY Sequence;

-- Pending injects for conduct
SELECT * FROM Injects 
WHERE MselId = @MselId 
  AND Status = 'Pending' 
  AND IsDeleted = 0
ORDER BY ScheduledTime;

-- Injects with scenario time in Day 2
SELECT * FROM Injects 
WHERE MselId = @MselId 
  AND ScenarioDay = 2
  AND IsDeleted = 0
ORDER BY ScenarioTime;

-- Child injects of a parent
SELECT * FROM Injects 
WHERE ParentInjectId = @ParentId 
  AND IsDeleted = 0;
```

## API Representation

### JSON Schema

```json
{
  "id": "inject-guid-here",
  "mselId": "msel-guid-here",
  "injectNumber": 15,
  "title": "Emergency Broadcast Alert",
  "description": "Governor has declared State of Emergency for coastal counties...",
  "scheduledTime": "10:15:00",
  "scenarioDay": 2,
  "scenarioTime": "18:00:00",
  "target": "Public Information Officer",
  "source": "Governor's Office",
  "deliveryMethod": "Email",
  "phaseId": "phase-guid-here",
  "injectType": "Standard",
  "status": "Pending",
  "parentInjectId": null,
  "triggerCondition": null,
  "expectedAction": "PIO drafts press release and coordinates with media",
  "controllerNotes": "Allow 10 minutes for response before follow-up inject",
  "sequence": 15,
  "firedAt": null,
  "firedBy": null,
  "createdAt": "2025-01-08T14:30:00Z",
  "createdBy": "user-guid-here",
  "modifiedAt": "2025-01-08T14:30:00Z",
  "modifiedBy": "user-guid-here"
}
```

## UI Considerations

### List View Columns

1. Inject # (with type badge for non-Standard)
2. Scheduled Time
3. Scenario Time (if exercise uses it)
4. Title
5. Target
6. Status (badge)
7. Actions

### Status Badges

| Status | Badge | Color |
|--------|-------|-------|
| Pending | ⏳ Pending | Gray |
| Fired | ✅ Fired | Green |
| Skipped | ⏭️ Skipped | Orange |

### Type Badges

| Type | Badge | Color |
|------|-------|-------|
| Standard | *(none)* | - |
| Contingency | 🔀 | Yellow |
| Adaptive | 🌿 | Blue |
| Complexity | ⬆️ | Purple |

## Migration Notes

### Initial Migration

```sql
CREATE TABLE Injects (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    MselId UNIQUEIDENTIFIER NOT NULL,
    InjectNumber INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(4000) NOT NULL,
    
    -- Dual time tracking
    ScheduledTime TIME NOT NULL,
    ScenarioDay INT NULL,
    ScenarioTime TIME NULL,
    
    -- Targeting
    Target NVARCHAR(200) NOT NULL,
    Source NVARCHAR(200) NULL,
    DeliveryMethod NVARCHAR(20) NULL,
    
    -- Organization
    PhaseId UNIQUEIDENTIFIER NULL,
    InjectType NVARCHAR(20) NOT NULL DEFAULT 'Standard',
    Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    
    -- Branching
    ParentInjectId UNIQUEIDENTIFIER NULL,
    TriggerCondition NVARCHAR(500) NULL,
    
    -- Supplemental
    ExpectedAction NVARCHAR(2000) NULL,
    ControllerNotes NVARCHAR(2000) NULL,
    Sequence INT NOT NULL,
    
    -- Conduct tracking
    FiredAt DATETIME2 NULL,
    FiredBy UNIQUEIDENTIFIER NULL,
    SkippedAt DATETIME2 NULL,
    SkippedBy UNIQUEIDENTIFIER NULL,
    SkipReason NVARCHAR(500) NULL,
    
    -- Audit
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    ModifiedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ModifiedBy UNIQUEIDENTIFIER NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2 NULL,
    
    CONSTRAINT FK_Inject_Msel FOREIGN KEY (MselId) 
        REFERENCES Msels(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Inject_Phase FOREIGN KEY (PhaseId) 
        REFERENCES Phases(Id),
    CONSTRAINT FK_Inject_Parent FOREIGN KEY (ParentInjectId) 
        REFERENCES Injects(Id),
    CONSTRAINT UQ_Inject_MselId_InjectNumber UNIQUE (MselId, InjectNumber),
    CONSTRAINT CK_Inject_ScenarioDay CHECK (ScenarioDay IS NULL OR ScenarioDay BETWEEN 1 AND 99)
);

CREATE INDEX IX_Inject_MselId_Sequence ON Injects(MselId, Sequence) WHERE IsDeleted = 0;
CREATE INDEX IX_Inject_MselId_Status ON Injects(MselId, Status) WHERE IsDeleted = 0;
CREATE INDEX IX_Inject_PhaseId ON Injects(PhaseId) WHERE IsDeleted = 0;
CREATE INDEX IX_Inject_ParentId ON Injects(ParentInjectId) WHERE IsDeleted = 0;
```

## Related Documentation

- [Exercise Entity](./exercise-entity.md) - Parent exercise container
- [User Roles](./user-roles.md) - Who can fire injects
- [Domain Glossary](../DOMAIN_GLOSSARY.md) - Term definitions

---

*Last updated: 2025-01-08*
