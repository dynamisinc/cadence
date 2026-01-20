# Story CLK-06: Clock-Driven Conduct View Sections

> **Story ID:** CLK-06
> **Feature:** Exercise Clock Modes
> **Phase:** D - Exercise Conduct
> **Status:** Ready for Development
> **Estimate:** Medium (1-2 days)

---

## User Story

**As a** Controller in a clock-driven exercise,
**I want** the conduct view to show "Ready to Fire" and "Upcoming" sections,
**So that** I can focus on injects that need immediate attention.

---

## Background

The current conduct view shows all injects in a flat list. For clock-driven exercises, Controllers need a prioritized view that highlights:
1. **Ready to Fire** - Injects that need immediate action
2. **Upcoming** - Injects arriving soon (with countdowns)
3. **Completed** - Fired/skipped injects (collapsed by default)

---

## Scope

### In Scope
- Refactor conduct page layout for clock-driven mode
- "Ready to Fire" section with highlighted injects
- "Upcoming" section with countdown timers
- "Completed" section (collapsible)
- Real-time section updates via SignalR
- Visual emphasis on Ready injects

### Out of Scope
- Facilitator-paced view (CLK-07)
- Auto-Ready logic (CLK-05 - prerequisite)
- Story Time display (CLK-08)

---

## Acceptance Criteria

- [ ] **Given** DeliveryMode = ClockDriven, **when** I view the conduct page, **then** injects are grouped into "Ready to Fire", "Upcoming", and "Completed" sections
- [ ] **Given** injects with status = Ready, **when** displayed, **then** they appear in "Ready to Fire" section with visual emphasis (highlight, larger text, alert icon)
- [ ] **Given** injects with status = Pending and DeliveryTime within next 30 minutes, **when** displayed, **then** they appear in "Upcoming" section
- [ ] **Given** an upcoming inject, **when** displayed, **then** it shows countdown text (e.g., "in 12:45")
- [ ] **Given** injects with status = Fired or Skipped, **when** displayed, **then** they appear in "Completed" section
- [ ] **Given** the Completed section, **when** page loads, **then** it is collapsed by default
- [ ] **Given** an inject becomes Ready via SignalR, **when** event received, **then** inject moves to "Ready to Fire" section in real-time
- [ ] **Given** 0 Ready injects, **when** displayed, **then** "Ready to Fire" section shows "No injects ready" message

---

## UI Design

### Clock-Driven Conduct View Layout

```
┌─────────────────────────────────────────────────────────────────────────┐
│  ▶ Exercise Clock: 00:32:15                    [⏸ Pause] [⏹ Stop]      │
│    Story Time: Day 1 18:32                                              │
├─────────────────────────────────────────────────────────────────────────┤
│ ⚠️ READY TO FIRE (1)                                          [Expand] │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │ 🔔 #3 │ Evacuation Order │ +00:30:00 │ Day 1 18:00 │   [🔥 FIRE]   │ │
│ │       │ Target: EOC Director                                        │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────────────┤
│ 📋 UPCOMING (2)                                                         │
│   #4 │ Shelter Opens       │ +00:45:00 │ in 12:45                       │
│   #5 │ Medical Emergency   │ +01:00:00 │ in 27:45                       │
│   #6 │ Resource Request    │ +01:15:00 │ in 42:45                       │
├─────────────────────────────────────────────────────────────────────────┤
│ ✓ COMPLETED (5)                                              [Expand ▼] │
│   (collapsed - click to expand)                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Ready to Fire Card (Expanded)

```
┌─────────────────────────────────────────────────────────────────────────┐
│ 🔔 READY                                                                │
│ ─────────────────────────────────────────────────────────────────────── │
│ #3  Evacuation Order                                                    │
│                                                                         │
│ 📍 Target: EOC Director                                                 │
│ 📤 Method: Radio                                                        │
│ ⏱️ Delivery: +00:30:00                                                  │
│ 📖 Story: Day 1 18:00                                                   │
│                                                                         │
│ "The Governor has issued a mandatory evacuation order..."               │
│                                                                         │
│                                    [Skip ▶]  [🔥 FIRE INJECT]           │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Design

### Component Structure

```
src/frontend/src/features/exercises/pages/
├── ExerciseConductPage.tsx           # Main page - delegates to mode-specific view
├── ClockDrivenConductView.tsx        # New - clock-driven layout
└── ClockDrivenConductView.test.tsx

src/frontend/src/features/injects/components/
├── ReadyToFireSection.tsx            # New
├── ReadyToFireSection.test.tsx
├── UpcomingSection.tsx               # New
├── UpcomingSection.test.tsx
├── CompletedSection.tsx              # New
├── CompletedSection.test.tsx
├── ReadyInjectCard.tsx               # New - expanded Ready inject
└── ReadyInjectCard.test.tsx
```

### Inject Grouping Logic

```typescript
interface GroupedInjects {
  ready: InjectDto[];
  upcoming: InjectDto[];
  completed: InjectDto[];
}

function groupInjectsForClockDriven(
  injects: InjectDto[],
  elapsedTimeMs: number
): GroupedInjects {
  const UPCOMING_WINDOW_MS = 30 * 60 * 1000; // 30 minutes

  return {
    ready: injects.filter(i => i.status === InjectStatus.Ready),

    upcoming: injects
      .filter(i => i.status === InjectStatus.Pending)
      .filter(i => {
        if (!i.deliveryTime) return false;
        const deliveryMs = parseDeliveryTime(i.deliveryTime);
        const timeUntil = deliveryMs - elapsedTimeMs;
        return timeUntil > 0 && timeUntil <= UPCOMING_WINDOW_MS;
      })
      .sort((a, b) =>
        parseDeliveryTime(a.deliveryTime!) - parseDeliveryTime(b.deliveryTime!)
      ),

    completed: injects.filter(
      i => i.status === InjectStatus.Fired || i.status === InjectStatus.Skipped
    )
  };
}
```

### Countdown Display

```typescript
function formatCountdown(targetMs: number, currentMs: number): string {
  const diff = targetMs - currentMs;
  if (diff <= 0) return 'now';

  const minutes = Math.floor(diff / 60000);
  const seconds = Math.floor((diff % 60000) / 1000);

  if (minutes >= 60) {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return `in ${hours}h ${mins}m`;
  }

  return `in ${minutes}:${String(seconds).padStart(2, '0')}`;
}
```

### ClockDrivenConductView Component

```typescript
interface ClockDrivenConductViewProps {
  exercise: ExerciseDto;
  injects: InjectDto[];
  elapsedTimeMs: number;
  onFire: (injectId: string) => void;
  onSkip: (injectId: string, reason?: string) => void;
}

export const ClockDrivenConductView: React.FC<ClockDrivenConductViewProps> = ({
  exercise,
  injects,
  elapsedTimeMs,
  onFire,
  onSkip
}) => {
  const grouped = useMemo(
    () => groupInjectsForClockDriven(injects, elapsedTimeMs),
    [injects, elapsedTimeMs]
  );

  const [completedExpanded, setCompletedExpanded] = useState(false);

  return (
    <Box>
      <ReadyToFireSection
        injects={grouped.ready}
        onFire={onFire}
        onSkip={onSkip}
      />

      <UpcomingSection
        injects={grouped.upcoming}
        elapsedTimeMs={elapsedTimeMs}
      />

      <CompletedSection
        injects={grouped.completed}
        expanded={completedExpanded}
        onToggle={() => setCompletedExpanded(!completedExpanded)}
      />
    </Box>
  );
};
```

### Real-Time Updates

The conduct page already subscribes to SignalR events. Add handling for section updates:

```typescript
// In ExerciseConductPage
connection.on('InjectReadyToFire', (inject: InjectDto) => {
  // Query invalidation will re-group injects
  queryClient.invalidateQueries(['injects', exerciseId]);

  // Optional: scroll Ready section into view
  readySectionRef.current?.scrollIntoView({ behavior: 'smooth' });
});
```

---

## Styling

### Ready Section Emphasis

```typescript
const ReadySection = styled(Box)(({ theme }) => ({
  backgroundColor: alpha(theme.palette.warning.light, 0.1),
  border: `2px solid ${theme.palette.warning.main}`,
  borderRadius: theme.shape.borderRadius,
  padding: theme.spacing(2),
  marginBottom: theme.spacing(2),

  '& .section-header': {
    color: theme.palette.warning.dark,
    fontWeight: 600,
    display: 'flex',
    alignItems: 'center',
    gap: theme.spacing(1),
  },
}));
```

### Countdown Timer Animation

```typescript
const CountdownChip = styled(Chip)(({ theme }) => ({
  fontFamily: 'monospace',
  fontWeight: 500,

  '&.imminent': {
    animation: 'pulse 1s infinite',
    backgroundColor: theme.palette.warning.light,
  },

  '@keyframes pulse': {
    '0%, 100%': { opacity: 1 },
    '50%': { opacity: 0.6 },
  },
}));
```

---

## Test Cases

### Component Unit Tests

```typescript
describe('ClockDrivenConductView', () => {
  it('groups injects into ready, upcoming, and completed sections');
  it('shows Ready injects in Ready to Fire section');
  it('shows countdown for upcoming injects');
  it('collapses Completed section by default');
  it('expands Completed section on click');
});

describe('groupInjectsForClockDriven', () => {
  it('puts Ready status injects in ready array');
  it('puts Pending injects within 30 min in upcoming array');
  it('excludes Pending injects beyond 30 min from upcoming');
  it('puts Fired and Skipped injects in completed array');
  it('sorts upcoming by DeliveryTime ascending');
});

describe('formatCountdown', () => {
  it('formats minutes and seconds correctly');
  it('shows hours for times over 60 minutes');
  it('returns "now" for zero or negative difference');
});
```

### Integration Tests

```typescript
describe('Clock-driven conduct page', () => {
  it('renders sections when DeliveryMode is ClockDriven');
  it('updates sections in real-time via SignalR');
  it('moves inject to Ready section when InjectReadyToFire received');
  it('moves inject to Completed when fired');
});
```

---

## Accessibility

- Section headers are `<h2>` or use `role="heading"`
- Collapsed sections announce state to screen readers
- Countdown timers have `aria-live="polite"` for updates
- Fire/Skip buttons have clear labels
- Ready section has `role="alert"` for new items

---

## Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| CLK-04: Ready status | 🔲 Required | Need Ready enum value |
| CLK-05: Auto-Ready logic | 🔲 Required | Injects must become Ready |
| CLK-01: DeliveryMode | 🔲 Required | Need mode check |
| Existing conduct page | ✅ Complete | Refactoring existing page |

---

## Blocked By

- CLK-05: Auto-Ready injects (need Ready injects to display)
- CLK-01: Add timing configuration fields (need DeliveryMode check)

---

## Blocks

None - this is a leaf story for clock-driven mode.

---

## Notes

- Consider adding sound effect when inject becomes Ready
- The 30-minute upcoming window is configurable - could be exercise setting
- Completed section helps Controllers track progress
- Future: Add search/filter within sections for large MSELs
