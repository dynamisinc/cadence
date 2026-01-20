# Story CLK-01: Add Timing Configuration Fields to Exercise Entity

> **Story ID:** CLK-01
> **Feature:** Exercise Clock Modes
> **Phase:** D - Exercise Conduct
> **Status:** Ready for Development
> **Estimate:** Medium (1-2 days)

---

## User Story

**As a** developer,
**I want** the Exercise entity to have DeliveryMode, TimelineMode, and TimeScale fields,
**So that** timing configuration can be persisted and used throughout the application.

---

## Scope

### In Scope
- Add `DeliveryMode` enum (ClockDriven, FacilitatorPaced) to Enums.cs
- Add `TimelineMode` enum (RealTime, Compressed, StoryOnly) to Enums.cs
- Add `DeliveryMode`, `TimelineMode`, and `TimeScale` properties to Exercise entity
- Create EF Core migration
- Update ExerciseDto and ExerciseMapper
- Add validation rules
- Update TypeScript types in frontend

### Out of Scope
- Configuration UI (see CLK-03)
- Auto-Ready logic (see CLK-05)
- Conduct view changes

---

## Acceptance Criteria

- [ ] **Given** the Exercise entity, **when** inspected, **then** it includes `DeliveryMode`, `TimelineMode`, and `TimeScale` properties
- [ ] **Given** DeliveryMode is not specified, **when** an exercise is created, **then** it defaults to `ClockDriven`
- [ ] **Given** TimelineMode is not specified, **when** an exercise is created, **then** it defaults to `RealTime`
- [ ] **Given** TimelineMode is `Compressed`, **when** TimeScale is null or ≤ 0, **then** validation fails with appropriate error message
- [ ] **Given** TimelineMode is `Compressed`, **when** TimeScale > 60, **then** validation fails (max 60x compression)
- [ ] **Given** TimelineMode is NOT `Compressed`, **when** TimeScale is provided, **then** it is accepted but ignored
- [ ] **Given** existing exercises in database, **when** migration runs, **then** they default to ClockDriven + RealTime
- [ ] **Given** frontend types, **when** updated, **then** they include the new enum types and fields

---

## Technical Design

### Backend Enums

Add to `src/Cadence.Core/Models/Entities/Enums.cs`:

```csharp
/// <summary>
/// Delivery mode determines how injects transition to Ready status.
/// </summary>
public enum DeliveryMode
{
    /// <summary>
    /// Injects become Ready when exercise clock reaches DeliveryTime.
    /// </summary>
    ClockDriven = 0,

    /// <summary>
    /// Injects are fired manually by Controller in Sequence order.
    /// </summary>
    FacilitatorPaced = 1
}

/// <summary>
/// Timeline mode determines how exercise time relates to story time.
/// </summary>
public enum TimelineMode
{
    /// <summary>
    /// 1:1 ratio - exercise time matches wall clock.
    /// </summary>
    RealTime = 0,

    /// <summary>
    /// Story time advances faster than real time per TimeScale.
    /// </summary>
    Compressed = 1,

    /// <summary>
    /// No real-time clock; only Story Time is used.
    /// </summary>
    StoryOnly = 2
}
```

### Exercise Entity Updates

Add to `src/Cadence.Core/Models/Entities/Exercise.cs`:

```csharp
/// <summary>
/// How injects transition to Ready status during conduct.
/// </summary>
public DeliveryMode DeliveryMode { get; set; } = DeliveryMode.ClockDriven;

/// <summary>
/// How exercise time relates to story/scenario time.
/// </summary>
public TimelineMode TimelineMode { get; set; } = TimelineMode.RealTime;

/// <summary>
/// Time compression ratio. Only used when TimelineMode = Compressed.
/// Example: 4.0 means 1 real minute = 4 story minutes.
/// Valid range: 0.1 to 60.0
/// </summary>
public decimal? TimeScale { get; set; }
```

### Migration

```csharp
migrationBuilder.AddColumn<int>(
    name: "DeliveryMode",
    table: "Exercises",
    type: "int",
    nullable: false,
    defaultValue: 0);

migrationBuilder.AddColumn<int>(
    name: "TimelineMode",
    table: "Exercises",
    type: "int",
    nullable: false,
    defaultValue: 0);

migrationBuilder.AddColumn<decimal>(
    name: "TimeScale",
    table: "Exercises",
    type: "decimal(5,2)",
    nullable: true);
```

### Validation

Add to Exercise validator or service:

```csharp
if (exercise.TimelineMode == TimelineMode.Compressed)
{
    if (!exercise.TimeScale.HasValue || exercise.TimeScale <= 0)
        throw new ValidationException("TimeScale is required and must be > 0 when TimelineMode is Compressed");

    if (exercise.TimeScale > 60)
        throw new ValidationException("TimeScale cannot exceed 60x compression");
}
```

### DTO Updates

Update `ExerciseDto`:

```csharp
public DeliveryMode DeliveryMode { get; init; }
public TimelineMode TimelineMode { get; init; }
public decimal? TimeScale { get; init; }
```

### Frontend Types

Add to `src/frontend/src/types/index.ts`:

```typescript
export enum DeliveryMode {
  ClockDriven = 'ClockDriven',
  FacilitatorPaced = 'FacilitatorPaced'
}

export enum TimelineMode {
  RealTime = 'RealTime',
  Compressed = 'Compressed',
  StoryOnly = 'StoryOnly'
}

// Update ExerciseDto interface
export interface ExerciseDto {
  // ... existing fields ...
  deliveryMode: DeliveryMode;
  timelineMode: TimelineMode;
  timeScale: number | null;
}
```

---

## Test Cases

### Backend Unit Tests

```csharp
[Fact]
public async Task CreateExercise_DefaultsToClockDrivenRealTime()

[Fact]
public async Task CreateExercise_CompressedMode_RequiresTimeScale()

[Fact]
public async Task CreateExercise_CompressedMode_TimeScaleExceeds60_Fails()

[Fact]
public async Task CreateExercise_RealTimeMode_IgnoresTimeScale()

[Fact]
public async Task UpdateExercise_CanChangeDeliveryMode_WhenDraft()
```

### Frontend Unit Tests

```typescript
describe('Exercise timing configuration', () => {
  it('includes deliveryMode in exercise DTO');
  it('includes timelineMode in exercise DTO');
  it('includes timeScale in exercise DTO');
});
```

---

## Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| Exercise entity exists | ✅ Complete | Adding fields to existing entity |
| EF Core configured | ✅ Complete | Standard migration process |

---

## Blocked By

None - this is a foundational story.

---

## Blocks

- CLK-03: Exercise timing configuration UI
- CLK-05: Auto-Ready injects logic
- CLK-07: Facilitator-paced conduct view
- CLK-08: Story Time display

---

## Notes

- Timing configuration should be locked when exercise is Active (validation in CLK-03)
- Consider adding `ExerciseStartStoryTime` field in future story for compressed mode baseline
