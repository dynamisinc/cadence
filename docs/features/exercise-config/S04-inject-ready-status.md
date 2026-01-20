# Story CLK-04: Add "Ready" Status to Inject Workflow

> **Story ID:** CLK-04
> **Feature:** Exercise Clock Modes
> **Phase:** D - Exercise Conduct
> **Status:** Ready for Development
> **Estimate:** Small (< 1 day)

---

## User Story

**As a** system,
**I want** injects to have a "Ready" status between "Pending" and "Fired",
**So that** auto-transitioned injects can be distinguished from those still waiting.

---

## Background

Currently, injects have three statuses: `Pending`, `Fired`, `Skipped`. The requirements introduce a `Ready` status that indicates an inject has reached its delivery time and is awaiting Controller action.

**Status Flow:**
```
Pending ──[clock reaches DeliveryTime]──► Ready ──[Controller fires]──► Fired
    │                                       │
    └──────[Controller skips]───────────────┴──[Controller skips]──► Skipped
```

---

## Scope

### In Scope
- Add `Ready` to InjectStatus enum (backend)
- Update frontend InjectStatus enum
- Update status chip styling for Ready state
- Update inject service to allow firing from Ready status
- Update validation rules

### Out of Scope
- Auto-Ready logic (CLK-05)
- UI sections based on Ready status (CLK-06)
- Manual transition to Ready (always automatic)

---

## Acceptance Criteria

- [ ] **Given** InjectStatus enum, **when** inspected, **then** it includes: Pending, Ready, Fired, Skipped
- [ ] **Given** an inject with status = Ready, **when** displayed, **then** it shows a distinct "Ready" chip with appropriate color
- [ ] **Given** an inject with status = Ready, **when** fire action is invoked, **then** status transitions to Fired
- [ ] **Given** an inject with status = Ready, **when** skip action is invoked, **then** status transitions to Skipped
- [ ] **Given** an inject with status = Pending, **when** fire action is invoked, **then** validation fails (must be Ready first in clock-driven mode) OR succeeds (in facilitator-paced mode)
- [ ] **Given** existing Pending injects in database, **when** upgrade runs, **then** they remain Pending

---

## Technical Design

### Backend Enum Update

Update `src/Cadence.Core/Models/Entities/Enums.cs`:

```csharp
/// <summary>
/// Status of an inject during exercise conduct.
/// </summary>
public enum InjectStatus
{
    /// <summary>
    /// Inject is waiting; delivery time not yet reached.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Inject is ready to fire; delivery time reached (clock-driven mode).
    /// </summary>
    Ready = 1,

    /// <summary>
    /// Inject has been delivered to players.
    /// </summary>
    Fired = 2,

    /// <summary>
    /// Inject was intentionally not delivered.
    /// </summary>
    Skipped = 3
}
```

### Inject Entity Updates

Add to `src/Cadence.Core/Models/Entities/Inject.cs`:

```csharp
/// <summary>
/// When the inject transitioned to Ready status. Null if not yet ready.
/// </summary>
public DateTime? ReadyAt { get; set; }
```

### Migration

```csharp
// No migration needed for enum change (stored as int, 1 is new value)
// Add ReadyAt column
migrationBuilder.AddColumn<DateTime>(
    name: "ReadyAt",
    table: "Injects",
    type: "datetime2",
    nullable: true);
```

### Service Updates

Update `InjectService.FireInjectAsync`:

```csharp
public async Task<InjectDto> FireInjectAsync(Guid exerciseId, Guid injectId, Guid userId)
{
    var exercise = await GetExerciseAsync(exerciseId);
    var inject = await GetInjectAsync(injectId);

    // Validation
    if (exercise.Status != ExerciseStatus.Active)
        throw new ValidationException("Exercise must be Active to fire injects");

    // In clock-driven mode, inject must be Ready
    // In facilitator-paced mode, inject can be Pending or Ready
    if (exercise.DeliveryMode == DeliveryMode.ClockDriven)
    {
        if (inject.Status != InjectStatus.Ready)
            throw new ValidationException("Inject must be Ready to fire in clock-driven mode");
    }
    else // FacilitatorPaced
    {
        if (inject.Status != InjectStatus.Pending && inject.Status != InjectStatus.Ready)
            throw new ValidationException("Inject must be Pending or Ready to fire");
    }

    inject.Status = InjectStatus.Fired;
    inject.FiredAt = DateTime.UtcNow;
    inject.FiredBy = userId;

    await _context.SaveChangesAsync();
    await _hubContext.NotifyInjectFired(exerciseId, inject.ToDto());

    return inject.ToDto();
}
```

### Frontend Enum Update

Update `src/frontend/src/types/index.ts`:

```typescript
export enum InjectStatus {
  Pending = 'Pending',
  Ready = 'Ready',
  Fired = 'Fired',
  Skipped = 'Skipped'
}
```

### Status Chip Styling

Update inject status chip component:

```typescript
const getStatusChipProps = (status: InjectStatus) => {
  switch (status) {
    case InjectStatus.Pending:
      return { color: 'default', icon: faClock, label: 'Pending' };
    case InjectStatus.Ready:
      return { color: 'warning', icon: faBell, label: 'Ready' };
    case InjectStatus.Fired:
      return { color: 'success', icon: faCheck, label: 'Fired' };
    case InjectStatus.Skipped:
      return { color: 'default', icon: faForward, label: 'Skipped' };
  }
};
```

### SignalR Event

Add new event to `IExerciseHubContext`:

```csharp
/// <summary>
/// Notifies clients that an inject has transitioned to Ready status.
/// </summary>
Task NotifyInjectReadyToFire(Guid exerciseId, InjectDto inject);
```

---

## Test Cases

### Backend Unit Tests

```csharp
[Fact]
public void InjectStatus_IncludesReadyValue()
{
    Assert.True(Enum.IsDefined(typeof(InjectStatus), InjectStatus.Ready));
    Assert.Equal(1, (int)InjectStatus.Ready);
}

[Fact]
public async Task FireInject_ClockDriven_RequiresReadyStatus()

[Fact]
public async Task FireInject_FacilitatorPaced_AllowsPendingStatus()

[Fact]
public async Task FireInject_FromReadyStatus_TransitionsToFired()

[Fact]
public async Task SkipInject_FromReadyStatus_TransitionsToSkipped()
```

### Frontend Unit Tests

```typescript
describe('InjectStatus chip', () => {
  it('renders Ready status with warning color');
  it('renders Ready status with bell icon');
  it('displays "Ready" label');
});

describe('Inject actions', () => {
  it('enables Fire button when status is Ready');
  it('enables Skip button when status is Ready');
});
```

---

## Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| Inject entity exists | ✅ Complete | Adding enum value |
| CLK-01: Timing fields | 🔲 Recommended | Needed for mode-based validation |

---

## Blocked By

None - can be implemented independently.

---

## Blocks

- CLK-05: Auto-Ready injects (sets Ready status)
- CLK-06: Clock-driven conduct view (filters by Ready status)

---

## Notes

- Ready status is only set automatically by the system (CLK-05)
- Controllers cannot manually set an inject to Ready
- Consider: Should Ready injects revert to Pending if clock is reset? (Probably yes)
- The `ReadyAt` timestamp helps with audit trail and debugging

---

## Status Transition Rules

| From | To | Trigger | Valid |
|------|------|---------|-------|
| Pending | Ready | Clock reaches DeliveryTime | ✅ Auto only |
| Pending | Fired | Controller fires (facilitator mode) | ✅ |
| Pending | Skipped | Controller skips | ✅ |
| Ready | Fired | Controller fires | ✅ |
| Ready | Skipped | Controller skips | ✅ |
| Ready | Pending | Clock reset | ✅ System only |
| Fired | * | - | ❌ Terminal |
| Skipped | Pending | Controller resets | ✅ |
