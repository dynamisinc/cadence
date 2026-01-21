# Exercise Clock Feature

Manages the exercise clock and story time during exercise conduct.

## Components

### ClockDisplay
Displays the exercise clock with elapsed time and state indicator (Running, Paused, Stopped).

### ClockControls
Provides Start, Pause, Stop, and Reset controls for the exercise clock.

### ExerciseProgress
Shows exercise progress with completion percentage and visual indicators.

### StoryTimeDisplay
**Added in CLK-08**

Displays the current story time alongside the exercise clock. Story time represents the fictional timeline within the scenario narrative.

#### Features:
- **RealTime mode**: Story time matches elapsed time 1:1
- **Compressed mode**: Story time advances faster (e.g., 4x) with compression indicator chip
- **StoryOnly mode**: Displays inject's scenario time, hides elapsed clock

#### Usage:
```tsx
import { StoryTimeDisplay, useStoryTime } from '@/features/exercise-clock'

const { storyTime, formattedStoryTime, isStoryOnly } = useStoryTime({
  exercise,
  elapsedTimeMs,
  currentInject, // For StoryOnly mode
})

<StoryTimeDisplay
  storyTime={storyTime}
  formattedStoryTime={formattedStoryTime}
  isStoryOnly={isStoryOnly}
  timelineMode={exercise.timelineMode}
  timeScale={exercise.timeScale}
/>
```

## Hooks

### useExerciseClock
Main hook for managing exercise clock state and operations. Includes real-time elapsed time calculation for running clocks.

Returns:
- `clockState` - Current clock state from API
- `displayTime` - Formatted elapsed time (HH:MM:SS)
- `elapsedTimeMs` - Elapsed time in milliseconds
- `isRunning`, `isPaused`, `isStopped` - Clock state booleans
- `startClock`, `pauseClock`, `stopClock`, `resetClock` - Control functions

### useStoryTime
**Added in CLK-08**

Calculates and formats story time based on exercise timeline mode and elapsed time.

Returns:
- `storyTime` - Story time object { day, hours, minutes }
- `formattedStoryTime` - Formatted string like "Day 1 • 08:32"
- `isStoryOnly` - Whether exercise is in StoryOnly mode

## Utilities

### calculateStoryTime
Calculates story time from elapsed time based on timeline mode and compression scale.

```typescript
const storyTime = calculateStoryTime(
  elapsedMs,
  TimelineMode.Compressed,
  4, // timeScale
  { startDay: 1, startHours: 0, startMinutes: 0 }
)
// => { day: 1, hours: 4, minutes: 0 } for 1 hour elapsed at 4x compression
```

### formatStoryTime
Formats story time as "Day N • HH:MM".

```typescript
formatStoryTime({ day: 1, hours: 8, minutes: 32 })
// => "Day 1 • 08:32"
```

### parseInjectScenarioTime
Parses inject scenario day and time into StoryTime object.

## Timeline Modes

The exercise clock supports three timeline modes (set on Exercise entity):

| Mode | Description | Story Time Calculation |
|------|-------------|------------------------|
| **RealTime** | 1:1 ratio | Story time = Elapsed time |
| **Compressed** | Time compression | Story time = Elapsed × TimeScale |
| **StoryOnly** | No real-time clock | Story time from inject's ScenarioDay/ScenarioTime |

### Examples

**RealTime (1:1):**
- Exercise runs for 2:30:00 → Story time is Day 1 • 02:30

**Compressed (4x):**
- Exercise runs for 0:15:00 (15 min) → Story time is Day 1 • 01:00 (1 hour)
- Exercise runs for 6:00:00 (6 hours) → Story time is Day 2 • 00:00 (24 hours)

**StoryOnly:**
- No elapsed clock displayed
- Story time comes from current inject's ScenarioDay/ScenarioTime
- Controllers advance story by firing next inject

## Integration Points

### StickyClockHeader
The story time display is integrated into the `StickyClockHeader` component, which appears at the top of the conduct page when using sticky layout mode.

The header now shows:
1. Elapsed clock (hidden in StoryOnly mode)
2. Clock controls (hidden in StoryOnly mode)
3. Story time display (always visible)
4. Current phase and progress
5. Ready-to-fire badge

## Testing

All components, hooks, and utilities include comprehensive test coverage:

```bash
# Run all exercise-clock tests
npm run test:run src/features/exercise-clock

# Run specific test files
npm run test:run src/features/exercise-clock/utils/storyTime.test.ts
npm run test:run src/features/exercise-clock/hooks/useStoryTime.test.ts
npm run test:run src/features/exercise-clock/components/StoryTimeDisplay.test.tsx
```

Test coverage includes:
- Story time calculation for all timeline modes
- Day rollover handling
- Time compression scenarios
- Story-only mode with inject scenario times
- Edge cases (null values, fractional minutes, etc.)
- Component rendering and styling
- Compression indicator display

## See Also

- [CLK-08 Story Requirements](../../../../../docs/features/exercise-config/S08-story-time-display.md)
- [Exercise Clock Modes](../../../../../docs/features/exercise-config/exercise-clock-modes-requirements.md)
- [COBRA Styling Guide](../../../../../docs/COBRA_STYLING.md)
