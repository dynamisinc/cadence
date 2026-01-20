# Clock-Driven Conduct View Sections

Components for displaying injects in clock-driven delivery mode exercises.

## Overview

These components implement CLK-06: Clock-Driven Conduct View Sections. They provide a prioritized view for Controllers during exercise conduct, automatically grouping injects based on their status and delivery time.

## Components

### ReadyToFireSection

Displays injects with `status = Ready` that need immediate attention.

**Features:**
- Warning-colored border and background for visual prominence
- Pulsing bell icon animation
- Expanded inject cards showing full details (title, description, target, source, method)
- Large "FIRE INJECT" buttons
- Overdue indicator for injects past their delivery time
- Skip dialog with reason capture

**Props:**
- `injects: InjectDto[]` - Injects with status = Ready
- `elapsedTimeMs: number` - Current elapsed time for overdue calculation
- `canControl: boolean` - Whether user can fire/skip
- `isSubmitting: boolean` - Loading state
- `onFire: (id) => void` - Fire handler
- `onSkip: (id, request) => void` - Skip handler

### UpcomingSection

Displays pending injects that will become ready within the next 30 minutes.

**Features:**
- Countdown timers showing time until each inject becomes ready
- Imminent indicator (pulsing) for injects < 5 minutes away
- Table format showing inject number, title, delivery time, and countdown
- Sorted by delivery time (soonest first)

**Props:**
- `injects: InjectDto[]` - Pending injects within 30-minute window
- `elapsedTimeMs: number` - Current elapsed time for countdown calculation

### CompletedSection

Displays fired and skipped injects in a collapsible section.

**Features:**
- Collapsed by default to maintain focus on upcoming work
- Shows separate counts for fired vs. skipped
- Table format with inject number, title, delivery time, status, action time, and action by
- Skip reason displayed for skipped injects

**Props:**
- `injects: InjectDto[]` - Fired and skipped injects
- `expanded: boolean` - Collapse state
- `onToggle: () => void` - Toggle collapse

## Usage

```tsx
import { ClockDrivenConductView } from '@/features/exercises'

<ClockDrivenConductView
  exercise={exercise}
  injects={injects}
  elapsedTimeMs={elapsedTimeMs}
  canControl={canControl}
  onFire={handleFire}
  onSkip={handleSkip}
/>
```

## Integration

The `ClockDrivenConductView` is automatically used in `ExerciseConductPage` when:
```typescript
exercise.deliveryMode === DeliveryMode.ClockDriven
```

Otherwise, the traditional `InjectListByStatus` view is displayed.

## Related Stories

- **CLK-01**: Add timing configuration fields (deliveryMode)
- **CLK-04**: Add Ready status to InjectStatus enum
- **CLK-05**: Auto-ready injects (backend service)
- **CLK-06**: Clock-driven conduct view sections (this feature)
- **CLK-08**: Story time display

## Testing

Tests are located in:
- `ClockDrivenConductView.test.tsx`
- `../utils/clockDrivenGrouping.test.ts`

Run tests:
```bash
npm test -- ClockDrivenConductView
npm test -- clockDrivenGrouping
```
