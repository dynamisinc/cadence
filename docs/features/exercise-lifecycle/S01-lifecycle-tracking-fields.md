# Story: Lifecycle Tracking Fields

## S01-lifecycle-tracking-fields.md

**As the** system,
**I want** to track exercise lifecycle metadata,
**So that** archive, restore, and delete operations work correctly.

### Context

The Exercise entity needs additional fields to support archive/restore operations and track whether an exercise has ever been published. This data enables:
- Determining who can delete an exercise (creator check)
- Knowing if an exercise is safe to delete (never published = no conduct data)
- Restoring to the correct status after un-archiving
- Audit trail for archive operations

### Acceptance Criteria

- [ ] **Given** an exercise is created, **when** saved, **then** `CreatedById` is set to the current user
- [ ] **Given** an exercise status changes from Draft to any other status, **when** saved, **then** `HasBeenPublished` is set to true
- [ ] **Given** `HasBeenPublished` is true, **when** status changes back to Draft, **then** `HasBeenPublished` remains true (never resets)
- [ ] **Given** an exercise is archived, **when** the operation runs, **then** `PreviousStatus`, `ArchivedAt`, and `ArchivedById` are set
- [ ] **Given** an exercise is restored, **when** the operation completes, **then** `ArchivedAt`, `ArchivedById`, and `PreviousStatus` are cleared
- [ ] **Given** the migration runs, **when** existing exercises are updated, **then** `CreatedById` defaults to system admin for exercises without a creator
- [ ] **Given** the migration runs, **when** existing exercises not in Draft are updated, **then** `HasBeenPublished` is set to true

### Out of Scope

- Archive/restore API endpoints (S02, S04)
- Delete functionality (S05, S06)
- UI changes

### Technical Notes

**Update ExerciseStatus Enum:**

```csharp
public enum ExerciseStatus
{
    Draft = 0,
    Published = 1,
    Active = 2,
    Completed = 3,
    Archived = 4  // NEW
}
```

**New Exercise Entity Fields:**

```csharp
/// <summary>
/// User who created the exercise. Used for delete permission on draft exercises.
/// </summary>
public Guid CreatedById { get; set; }
public User CreatedBy { get; set; }

/// <summary>
/// True if the exercise has ever been published (left Draft status).
/// Once true, never set back to false. Used to determine delete eligibility.
/// </summary>
public bool HasBeenPublished { get; set; } = false;

/// <summary>
/// Status before archiving. Used to restore exercise to correct state.
/// </summary>
public ExerciseStatus? PreviousStatus { get; set; }

/// <summary>
/// When the exercise was archived. Null if not archived.
/// </summary>
public DateTime? ArchivedAt { get; set; }

/// <summary>
/// User who archived the exercise.
/// </summary>
public Guid? ArchivedById { get; set; }
public User? ArchivedBy { get; set; }
```

**Migration Notes:**
- Set `CreatedById` to a default admin user for existing exercises
- Set `HasBeenPublished = true` for any exercise not in Draft status
- All new fields except `CreatedById` should be nullable or have defaults

### Domain Terms

| Term | Definition |
|------|------------|
| HasBeenPublished | Boolean flag set true when exercise first leaves Draft; never resets to false |
| PreviousStatus | Stores the status before archiving for proper restoration |
| CreatedById | Foreign key to User who created the exercise |

### Dependencies

- User entity must exist with proper relationships

### Deliverables

1. Update `ExerciseStatus` enum with `Archived = 4`
2. Add new fields to `Exercise` entity
3. Update `ExerciseConfiguration` with new field settings and relationships
4. Create and run migration
5. Update `ExerciseDto` with new fields
6. Update AutoMapper profile
7. Update `ExerciseService.CreateAsync` to set `CreatedById`
8. Update status change logic to set `HasBeenPublished = true` when leaving Draft
9. Unit tests for entity changes and status transition logic
