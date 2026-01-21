# Story CLK-02: Add DeliveryTime Field to Inject Entity

> **Story ID:** CLK-02
> **Feature:** Exercise Clock Modes
> **Phase:** D - Exercise Conduct
> **Status:** Ready for Development
> **Estimate:** Small (< 1 day)

---

## User Story

**As a** developer,
**I want** the Inject entity to have a DeliveryTime (TimeSpan) field,
**So that** injects can be scheduled relative to exercise start rather than wall clock time.

---

## Background

The current `ScheduledTime` field is a `TimeOnly` representing wall clock time (e.g., "10:30 AM"). The requirements call for `DeliveryTime` as a `TimeSpan` representing elapsed time from exercise start (e.g., "+00:30:00" meaning 30 minutes into the exercise).

Both fields serve different use cases:
- **ScheduledTime**: Useful when the exercise has a fixed start time and you want wall-clock delivery
- **DeliveryTime**: Useful when timing is relative to whenever the exercise actually starts

---

## Scope

### In Scope
- Add `DeliveryTime` (TimeSpan?) to Inject entity
- Create EF Core migration
- Update InjectDto and InjectMapper
- Update TypeScript types in frontend
- Keep existing `ScheduledTime` for backward compatibility

### Out of Scope
- Migration of existing ScheduledTime data to DeliveryTime
- UI changes to inject forms (separate story)
- Auto-Ready logic using DeliveryTime (CLK-05)

---

## Acceptance Criteria

- [ ] **Given** the Inject entity, **when** inspected, **then** it includes `DeliveryTime` as `TimeSpan?`
- [ ] **Given** an inject with DeliveryTime = 00:30:00, **when** serialized to JSON, **then** it appears as "00:30:00" string
- [ ] **Given** an inject without DeliveryTime, **when** retrieved, **then** DeliveryTime is null
- [ ] **Given** existing injects in database, **when** migration runs, **then** DeliveryTime defaults to null (existing data preserved)
- [ ] **Given** frontend types, **when** updated, **then** InjectDto includes `deliveryTime: string | null`
- [ ] **Given** both ScheduledTime and DeliveryTime are set, **when** retrieved, **then** both values are returned

---

## Technical Design

### Inject Entity Update

Add to `src/Cadence.Core/Models/Entities/Inject.cs`:

```csharp
/// <summary>
/// Elapsed time from exercise start when inject should be delivered.
/// Used in ClockDriven mode for auto-Ready functionality.
/// Format: TimeSpan from 00:00:00 (e.g., 00:30:00 = 30 minutes into exercise).
/// </summary>
public TimeSpan? DeliveryTime { get; set; }
```

### Migration

```csharp
migrationBuilder.AddColumn<TimeSpan>(
    name: "DeliveryTime",
    table: "Injects",
    type: "time",
    nullable: true);
```

**Note:** SQL Server stores TimeSpan as `time(7)` which supports up to 23:59:59.9999999. For exercises longer than 24 hours, consider using `bigint` (ticks) instead.

### Alternative for Long Exercises

If exercises may exceed 24 hours:

```csharp
// Store as ticks (long) for unlimited duration
public long? DeliveryTimeTicks { get; set; }

[NotMapped]
public TimeSpan? DeliveryTime
{
    get => DeliveryTimeTicks.HasValue ? TimeSpan.FromTicks(DeliveryTimeTicks.Value) : null;
    set => DeliveryTimeTicks = value?.Ticks;
}
```

### DTO Updates

Update `InjectDto`:

```csharp
/// <summary>
/// Elapsed time from exercise start for delivery. Format: "HH:MM:SS"
/// </summary>
public TimeSpan? DeliveryTime { get; init; }
```

### Mapper Updates

Update `InjectMapper`:

```csharp
DeliveryTime = inject.DeliveryTime,
```

### Frontend Types

Update `src/frontend/src/features/injects/types/index.ts`:

```typescript
export interface InjectDto {
  // ... existing fields ...

  /** Elapsed time from exercise start. Format: "HH:MM:SS" or "d.HH:MM:SS" */
  deliveryTime: string | null;

  // Keep existing
  scheduledTime: string;  // Wall clock time "HH:MM:SS"
}
```

### Helper Function

Add to frontend utils:

```typescript
/**
 * Parse a TimeSpan string to milliseconds
 * Handles both "HH:MM:SS" and "d.HH:MM:SS" formats
 */
export function parseDeliveryTime(deliveryTime: string | null): number | null {
  if (!deliveryTime) return null;

  // Handle day component if present
  const dayMatch = deliveryTime.match(/^(\d+)\.(.+)$/);
  let days = 0;
  let timeStr = deliveryTime;

  if (dayMatch) {
    days = parseInt(dayMatch[1], 10);
    timeStr = dayMatch[2];
  }

  const parts = timeStr.split(':').map(Number);
  const hours = parts[0] || 0;
  const minutes = parts[1] || 0;
  const seconds = parts[2] || 0;

  return ((days * 24 + hours) * 60 + minutes) * 60 * 1000 + seconds * 1000;
}

/**
 * Format milliseconds as delivery time string
 */
export function formatDeliveryTime(ms: number): string {
  const totalSeconds = Math.floor(ms / 1000);
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;

  return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
}
```

---

## Test Cases

### Backend Unit Tests

```csharp
[Fact]
public async Task CreateInject_WithDeliveryTime_PersistsValue()

[Fact]
public async Task CreateInject_WithoutDeliveryTime_IsNull()

[Fact]
public async Task GetInject_ReturnsDeliveryTimeInDto()

[Fact]
public async Task UpdateInject_CanSetDeliveryTime()
```

### Frontend Unit Tests

```typescript
describe('parseDeliveryTime', () => {
  it('parses HH:MM:SS format');
  it('parses d.HH:MM:SS format with days');
  it('returns null for null input');
});

describe('formatDeliveryTime', () => {
  it('formats milliseconds as HH:MM:SS');
  it('pads single digits with zeros');
});
```

---

## Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| Inject entity exists | ✅ Complete | Adding field to existing entity |
| EF Core configured | ✅ Complete | Standard migration process |

---

## Blocked By

None - this is a foundational story.

---

## Blocks

- CLK-05: Auto-Ready injects (needs DeliveryTime to compare against clock)
- CLK-06: Clock-driven conduct view (shows DeliveryTime)

---

## Notes

- `ScheduledTime` (TimeOnly) = wall clock time "deliver at 10:30 AM"
- `DeliveryTime` (TimeSpan) = offset from exercise start "deliver at +00:30:00"
- For MVP, keep both fields; future work may derive one from the other
- Consider: Should UI allow entering either format and auto-calculate the other?

---

## Future Considerations

1. **Auto-calculate DeliveryTime**: If exercise has StartTime and inject has ScheduledTime, calculate DeliveryTime automatically
2. **Deprecate ScheduledTime**: Eventually move to DeliveryTime-only for simplicity
3. **Import/Export**: Excel import should support both formats
