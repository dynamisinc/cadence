# Story CLK-08: Display Story Time in Clock Area

> **Story ID:** CLK-08
> **Feature:** Exercise Clock Modes
> **Phase:** D - Exercise Conduct
> **Status:** Ready for Development
> **Estimate:** Medium (1-2 days)

---

## User Story

**As an** Evaluator,
**I want** to see the current Story Time alongside the exercise clock,
**So that** I understand the narrative timeline for my observations.

---

## Background

Exercises often simulate scenarios that span longer periods than the actual exercise duration. Story Time represents the fictional timeline in the scenario narrative. For example:

- **Real-time**: Exercise runs 4 hours, story spans 4 hours (1:1)
- **Compressed 4x**: Exercise runs 4 hours, story spans 16 hours (Day 1 08:00 to Day 2 00:00)
- **Story-only**: No real-time clock, just static Story Time per inject

---

## Scope

### In Scope
- Add Story Time display to clock header area
- Calculate Story Time based on TimelineMode and TimeScale
- Real-time update when clock is running (for Compressed mode)
- Format as "Day N • HH:MM"
- Handle Story-only mode (no elapsed clock, just inject Story Times)

### Out of Scope
- Exercise starting Story Time configuration (use first inject's Story Time or hardcode Day 1 00:00)
- Per-inject Story Time editing
- Story Time in observations

---

## Acceptance Criteria

- [ ] **Given** TimelineMode = RealTime, **when** clock is running, **then** Story Time matches elapsed time (displayed as "Day 1 • HH:MM")
- [ ] **Given** TimelineMode = Compressed with TimeScale = 4, **when** 15 real minutes pass, **then** Story Time advances 60 minutes
- [ ] **Given** TimelineMode = Compressed, **when** Story Time crosses midnight, **then** Day increments (Day 1 → Day 2)
- [ ] **Given** TimelineMode = StoryOnly, **when** viewing conduct page, **then** only Story Time header is displayed (no elapsed clock)
- [ ] **Given** TimelineMode = StoryOnly, **when** viewing current inject, **then** that inject's Story Time is prominently displayed
- [ ] **Given** clock is paused, **when** viewing Story Time, **then** it shows the paused Story Time (not advancing)
- [ ] **Given** exercise starts, **when** elapsed = 0, **then** Story Time shows configured starting time (default: Day 1 00:00)

---

## UI Design

### Clock Header with Story Time

**Real-time Mode:**
```
┌─────────────────────────────────────────────────────────────────────────┐
│  ▶ Exercise Clock: 00:32:15                    [⏸ Pause] [⏹ Stop]      │
│    📖 Story Time: Day 1 • 00:32                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

**Compressed Mode (4x):**
```
┌─────────────────────────────────────────────────────────────────────────┐
│  ▶ Exercise Clock: 00:32:15                    [⏸ Pause] [⏹ Stop]      │
│    📖 Story Time: Day 1 • 02:08                          (4x compressed) │
└─────────────────────────────────────────────────────────────────────────┘
```

**Story-only Mode (No Clock):**
```
┌─────────────────────────────────────────────────────────────────────────┐
│  📖 Current Story Time: Day 1 • 18:00                                   │
│     (from current inject)                                               │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Design

### Story Time Calculation

```typescript
interface StoryTime {
  day: number;
  hours: number;
  minutes: number;
}

interface StoryTimeConfig {
  startDay: number;
  startHours: number;
  startMinutes: number;
}

/**
 * Calculate current Story Time based on elapsed time and timeline mode.
 */
function calculateStoryTime(
  elapsedMs: number,
  timelineMode: TimelineMode,
  timeScale: number | null,
  startConfig: StoryTimeConfig = { startDay: 1, startHours: 0, startMinutes: 0 }
): StoryTime {
  // Convert elapsed to story minutes based on mode
  let storyMinutes: number;

  switch (timelineMode) {
    case TimelineMode.RealTime:
      storyMinutes = Math.floor(elapsedMs / 60000);
      break;

    case TimelineMode.Compressed:
      const scale = timeScale || 1;
      storyMinutes = Math.floor((elapsedMs / 60000) * scale);
      break;

    case TimelineMode.StoryOnly:
      // In Story-only mode, return null/undefined - use inject's Story Time
      return { day: 0, hours: 0, minutes: 0 }; // Placeholder
  }

  // Calculate from starting point
  const startTotalMinutes =
    (startConfig.startDay - 1) * 24 * 60 +
    startConfig.startHours * 60 +
    startConfig.startMinutes;

  const currentTotalMinutes = startTotalMinutes + storyMinutes;

  const day = Math.floor(currentTotalMinutes / (24 * 60)) + 1;
  const remainingMinutes = currentTotalMinutes % (24 * 60);
  const hours = Math.floor(remainingMinutes / 60);
  const minutes = remainingMinutes % 60;

  return { day, hours, minutes };
}

/**
 * Format Story Time for display.
 */
function formatStoryTime(storyTime: StoryTime): string {
  const { day, hours, minutes } = storyTime;
  const timeStr = `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`;
  return `Day ${day} • ${timeStr}`;
}
```

### useStoryTime Hook

```typescript
interface UseStoryTimeOptions {
  exercise: ExerciseDto;
  elapsedTimeMs: number;
  currentInject?: InjectDto | null;
}

interface UseStoryTimeResult {
  storyTime: StoryTime | null;
  formattedStoryTime: string;
  isStoryOnly: boolean;
}

function useStoryTime({
  exercise,
  elapsedTimeMs,
  currentInject
}: UseStoryTimeOptions): UseStoryTimeResult {
  const isStoryOnly = exercise.timelineMode === TimelineMode.StoryOnly;

  const storyTime = useMemo(() => {
    if (isStoryOnly) {
      // In Story-only mode, derive from current inject
      if (currentInject?.scenarioDay && currentInject?.scenarioTime) {
        const [hours, minutes] = currentInject.scenarioTime.split(':').map(Number);
        return {
          day: currentInject.scenarioDay,
          hours,
          minutes
        };
      }
      return null;
    }

    return calculateStoryTime(
      elapsedTimeMs,
      exercise.timelineMode,
      exercise.timeScale
    );
  }, [elapsedTimeMs, exercise.timelineMode, exercise.timeScale, isStoryOnly, currentInject]);

  const formattedStoryTime = storyTime ? formatStoryTime(storyTime) : '—';

  return { storyTime, formattedStoryTime, isStoryOnly };
}
```

### StoryTimeDisplay Component

```typescript
interface StoryTimeDisplayProps {
  storyTime: StoryTime | null;
  formattedStoryTime: string;
  isStoryOnly: boolean;
  timelineMode: TimelineMode;
  timeScale: number | null;
}

export const StoryTimeDisplay: React.FC<StoryTimeDisplayProps> = ({
  storyTime,
  formattedStoryTime,
  isStoryOnly,
  timelineMode,
  timeScale
}) => {
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
      <FontAwesomeIcon icon={faBook} />
      <Typography variant="body1" sx={{ fontFamily: 'monospace' }}>
        {isStoryOnly ? 'Current Story Time:' : 'Story Time:'} {formattedStoryTime}
      </Typography>
      {timelineMode === TimelineMode.Compressed && timeScale && (
        <Chip
          size="small"
          label={`${timeScale}x compressed`}
          variant="outlined"
        />
      )}
    </Box>
  );
};
```

### Integration in Clock Header

Update `ClockDisplay.tsx` or create `ExerciseClockHeader.tsx`:

```typescript
interface ExerciseClockHeaderProps {
  exercise: ExerciseDto;
  clockState: ClockStateDto | undefined;
  displayTime: string;
  elapsedTimeMs: number;
  currentInject?: InjectDto | null;
}

export const ExerciseClockHeader: React.FC<ExerciseClockHeaderProps> = ({
  exercise,
  clockState,
  displayTime,
  elapsedTimeMs,
  currentInject
}) => {
  const { formattedStoryTime, isStoryOnly } = useStoryTime({
    exercise,
    elapsedTimeMs,
    currentInject
  });

  return (
    <Paper elevation={2} sx={{ p: 2, mb: 2 }}>
      {/* Elapsed Clock - hidden in Story-only mode */}
      {!isStoryOnly && (
        <ClockDisplay
          clockState={clockState}
          displayTime={displayTime}
        />
      )}

      {/* Story Time - always shown */}
      <StoryTimeDisplay
        storyTime={storyTime}
        formattedStoryTime={formattedStoryTime}
        isStoryOnly={isStoryOnly}
        timelineMode={exercise.timelineMode}
        timeScale={exercise.timeScale}
      />

      {/* Clock Controls */}
      {!isStoryOnly && (
        <ClockControls
          state={clockState?.state}
          onStart={onStart}
          onPause={onPause}
          onStop={onStop}
        />
      )}
    </Paper>
  );
};
```

---

## Test Cases

### Unit Tests

```typescript
describe('calculateStoryTime', () => {
  it('returns same time as elapsed for RealTime mode');
  it('multiplies elapsed by timeScale for Compressed mode');
  it('handles day rollover at midnight');
  it('uses start config for non-zero starting time');
  it('returns placeholder for StoryOnly mode');
});

describe('formatStoryTime', () => {
  it('formats as "Day N • HH:MM"');
  it('pads single-digit hours and minutes');
});

describe('useStoryTime', () => {
  it('calculates story time from elapsed in RealTime mode');
  it('applies compression in Compressed mode');
  it('derives from current inject in StoryOnly mode');
  it('returns null when no inject in StoryOnly mode');
});

describe('StoryTimeDisplay', () => {
  it('shows compression indicator for Compressed mode');
  it('shows "Current Story Time" label for StoryOnly mode');
  it('shows "Story Time" label for other modes');
});
```

### Integration Tests

```typescript
describe('Clock header with Story Time', () => {
  it('updates Story Time in real-time when clock running');
  it('pauses Story Time when clock paused');
  it('hides elapsed clock in StoryOnly mode');
  it('shows inject Story Time in StoryOnly mode');
});
```

---

## Edge Cases

| Scenario | Expected Behavior |
|----------|-------------------|
| Elapsed = 0, Compressed 4x | Story Time = Day 1 00:00 |
| Elapsed = 6 hours, Compressed 4x | Story Time = Day 2 00:00 (24 story hours) |
| Story-only, no current inject | Show "—" or placeholder |
| TimeScale = 0.5 (slow motion) | Story Time advances slower than real time |
| Exercise spans multiple days | Day counter continues (Day 3, Day 4, etc.) |

---

## Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| CLK-01: TimelineMode, TimeScale | 🔲 Required | Need mode and scale values |
| Existing clock display | ✅ Complete | Extending with Story Time |
| Inject ScenarioDay/ScenarioTime | ✅ Complete | Fields exist on Inject entity |

---

## Blocked By

- CLK-01: Add timing configuration fields (need TimelineMode, TimeScale)

---

## Blocks

None - this is a leaf story for display.

---

## Notes

- Consider adding exercise-level `StartStoryDay` and `StartStoryTime` fields for flexibility
- For Story-only mode, the "current" Story Time changes as Facilitator moves through injects
- Evaluators use Story Time to contextualize their observations
- Future: Allow observations to be tagged with Story Time, not just wall clock time

---

## Future Enhancements

1. **Configurable Start Time**: Let Exercise Directors set the starting Story Time (e.g., "Day 3 14:00" for a continuation exercise)
2. **Story Time in Observations**: Record Story Time when observations are created
3. **Story Time Jump**: In facilitator mode, allow jumping to a specific Story Time
4. **Multi-day Display**: For very long exercises, show full date format
