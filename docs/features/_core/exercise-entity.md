# Entity: Exercise

> **Type**: Core Domain Entity

## Overview

The Exercise entity is the top-level container in Cadence, representing a single emergency management exercise event. All other data (MSELs, participants, objectives, observations) belongs to an exercise.

## Entity Definition

### Properties

| Property | Type | Required | Description | Constraints |
|----------|------|----------|-------------|-------------|
| `Id` | GUID | Yes | Unique identifier | System-generated |
| `Name` | string | Yes | Exercise name | 1-200 characters |
| `Description` | string | No | Detailed description | Max 4000 characters |
| `ExerciseType` | enum | Yes | Type of exercise | See Exercise Types |
| `Status` | enum | Yes | Current lifecycle status | See Status Values |
| `IsPracticeMode` | bool | Yes | Training/test flag | Default: false |
| `ScheduledDate` | date | Yes | Planned exercise date | Cannot be null |
| `StartTime` | time | No | Planned start time | HH:MM format |
| `EndTime` | time | No | Planned end time | Must be after StartTime |
| `TimeZoneId` | string | Yes | IANA time zone | Default: user's TZ |
| `Location` | string | No | Exercise location | Max 500 characters |
| `OrganizationId` | GUID | Yes | Owning organization | FK to Organization |
| `ActiveMselId` | GUID? | No | Current active MSEL | FK to MSEL, nullable |

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

### Exercise Types

| Value | Display Name | Description |
|-------|--------------|-------------|
| `TTX` | Table Top Exercise | Discussion-based scenario walkthrough |
| `FE` | Functional Exercise | Simulated operations in controlled environment |
| `FSE` | Full-Scale Exercise | Actual deployment of resources |
| `CAX` | Computer-Aided Exercise | Technology-driven simulation |
| `Hybrid` | Hybrid Exercise | Combination of multiple types |

### Exercise Status

| Value | Display Name | Transitions To | Description |
|-------|--------------|----------------|-------------|
| `Draft` | Draft | Active, Archived | Initial creation state |
| `Active` | Active | Completed, Archived | Currently in conduct |
| `Completed` | Completed | Archived | Conduct finished |
| `Archived` | Archived | *(terminal)* | Read-only historical |

```
┌─────────┐     ┌─────────┐     ┌───────────┐     ┌──────────┐
│  Draft  │────▶│  Active │────▶│ Completed │────▶│ Archived │
└────┬────┘     └────┬────┘     └─────┬─────┘     └──────────┘
     │               │                │                 ▲
     │               │                │                 │
     └───────────────┴────────────────┴─────────────────┘
              (Can archive from any state)
```

## Relationships

### Has Many

| Related Entity | Relationship | Cascade Behavior |
|----------------|--------------|------------------|
| MSEL | One-to-Many | Cascade delete |
| Participant | One-to-Many | Cascade delete |
| Objective | One-to-Many | Cascade delete |
| Phase | One-to-Many | Cascade delete |
| Observation | One-to-Many | Cascade delete |

### Belongs To

| Related Entity | Relationship | Required |
|----------------|--------------|----------|
| Organization | Many-to-One | Yes |

## Business Rules

### Creation Rules

1. **Name Required**: Exercise name cannot be empty
2. **Type Required**: Must select valid exercise type
3. **Date Required**: Scheduled date is mandatory
4. **Default Status**: New exercises start in "Draft" status
5. **Default Practice Mode**: Practice mode defaults to false
6. **Time Zone Default**: Uses creator's browser time zone if not specified

### Status Transition Rules

1. **Draft → Active**: Requires at least one MSEL with at least one inject
2. **Active → Completed**: Can be set manually by Exercise Director
3. **Any → Archived**: Can archive from any state; makes read-only
4. **Archived is Terminal**: Cannot un-archive exercises

### Practice Mode Rules

1. Practice exercises are visually distinguished in lists (badge/icon)
2. Practice exercises are excluded from:
   - Production reports
   - Organization-wide metrics
   - Export totals
3. Practice mode cannot be changed once exercise has injects
4. Practice exercises can be deleted (not just archived)

### Time Zone Rules

1. All internal timestamps stored in UTC
2. Display times converted to exercise TimeZoneId
3. Time zone cannot be changed once exercise is Active
4. DST transitions handled automatically by IANA database

## Validation Rules

```csharp
// Pseudocode validation rules
public class ExerciseValidator
{
    public ValidationResult Validate(Exercise exercise)
    {
        // Required fields
        Require(exercise.Name).NotEmpty().MaxLength(200);
        Require(exercise.ExerciseType).IsValidEnum();
        Require(exercise.ScheduledDate).NotNull();
        Require(exercise.TimeZoneId).IsValidIanaTimeZone();
        
        // Optional field constraints
        Optional(exercise.Description).MaxLength(4000);
        Optional(exercise.Location).MaxLength(500);
        
        // Time validation
        When(exercise.StartTime).And(exercise.EndTime)
            .Then(exercise.EndTime > exercise.StartTime);
        
        // Business rules
        When(exercise.Status == Status.Active)
            .Then(exercise.ActiveMselId != null);
    }
}
```

## Query Patterns

### Common Queries

| Query | Use Case | Index Recommendation |
|-------|----------|---------------------|
| By Organization | Exercise list page | `IX_Exercise_OrgId_Status` |
| Active exercises | Dashboard | `IX_Exercise_Status` filtered |
| By date range | Reporting | `IX_Exercise_ScheduledDate` |
| Non-practice only | Production reports | Include `IsPracticeMode` in compound |

### Example Queries

```sql
-- Active exercises for organization
SELECT * FROM Exercises 
WHERE OrganizationId = @OrgId 
  AND Status = 'Active' 
  AND IsDeleted = 0;

-- Upcoming exercises (next 30 days)
SELECT * FROM Exercises 
WHERE ScheduledDate BETWEEN GETUTCDATE() AND DATEADD(day, 30, GETUTCDATE())
  AND Status IN ('Draft', 'Active')
  AND IsDeleted = 0
ORDER BY ScheduledDate;

-- Production exercises only (exclude practice)
SELECT * FROM Exercises 
WHERE IsPracticeMode = 0 
  AND IsDeleted = 0;
```

## API Representation

### JSON Schema

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Hurricane Response TTX 2025",
  "description": "Annual hurricane preparedness exercise",
  "exerciseType": "TTX",
  "status": "Draft",
  "isPracticeMode": false,
  "scheduledDate": "2025-06-15",
  "startTime": "09:00",
  "endTime": "12:00",
  "timeZoneId": "America/Chicago",
  "location": "EOC Conference Room A",
  "organizationId": "org-guid-here",
  "activeMselId": null,
  "createdAt": "2025-01-08T14:30:00Z",
  "createdBy": "user-guid-here",
  "modifiedAt": "2025-01-08T14:30:00Z",
  "modifiedBy": "user-guid-here"
}
```

## UI Considerations

### Display Formatting

| Field | Display Format | Example |
|-------|----------------|---------|
| ScheduledDate | Localized date | "June 15, 2025" |
| StartTime/EndTime | 12-hour with AM/PM | "9:00 AM - 12:00 PM" |
| Status | Colored badge | 🟡 Draft, 🟢 Active, ✅ Completed, 📦 Archived |
| IsPracticeMode | Badge when true | "🔧 Practice" |

### List View Columns

1. Name (with practice badge if applicable)
2. Type
3. Date
4. Status
5. Inject Count (from active MSEL)
6. Actions

## Migration Notes

### Initial Migration

```sql
CREATE TABLE Exercises (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(4000) NULL,
    ExerciseType NVARCHAR(20) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'Draft',
    IsPracticeMode BIT NOT NULL DEFAULT 0,
    ScheduledDate DATE NOT NULL,
    StartTime TIME NULL,
    EndTime TIME NULL,
    TimeZoneId NVARCHAR(100) NOT NULL,
    Location NVARCHAR(500) NULL,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    ActiveMselId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy UNIQUEIDENTIFIER NOT NULL,
    ModifiedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ModifiedBy UNIQUEIDENTIFIER NOT NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2 NULL,
    
    CONSTRAINT FK_Exercise_Organization FOREIGN KEY (OrganizationId) 
        REFERENCES Organizations(Id),
    CONSTRAINT FK_Exercise_ActiveMsel FOREIGN KEY (ActiveMselId) 
        REFERENCES Msels(Id),
    CONSTRAINT CK_Exercise_EndTimeAfterStart 
        CHECK (EndTime IS NULL OR StartTime IS NULL OR EndTime > StartTime)
);

CREATE INDEX IX_Exercise_OrgId_Status ON Exercises(OrganizationId, Status) 
    WHERE IsDeleted = 0;
CREATE INDEX IX_Exercise_ScheduledDate ON Exercises(ScheduledDate) 
    WHERE IsDeleted = 0;
```

## Related Documentation

- [MSEL Entity](./inject-entity.md) - Child entity containing injects
- [User Roles](./user-roles.md) - Role permissions for exercise access
- [Domain Glossary](../DOMAIN_GLOSSARY.md) - Term definitions

---

*Last updated: 2025-01-08*
