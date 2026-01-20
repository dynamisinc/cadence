# Story CLK-07: Facilitator-Paced Conduct View

> **Story ID:** CLK-07
> **Feature:** Exercise Clock Modes
> **Phase:** D - Exercise Conduct
> **Status:** Ready for Development
> **Estimate:** Large (3-5 days)

---

## User Story

**As a** Facilitator in a discussion-based exercise,
**I want** a conduct view focused on the current inject with manual progression,
**So that** I can control the pace of discussion without a running clock.

---

## Background

Tabletop exercises (TTXs) and other discussion-based formats don't follow a real-time clock. Instead, a Facilitator guides participants through scenarios at whatever pace allows for adequate discussion. This view prioritizes:

1. **Current Inject** - Full content display for active discussion
2. **Up Next** - Preview of upcoming injects for planning
3. **Manual Progression** - Facilitator controls when to move forward

---

## Scope

### In Scope
- New conduct view layout for FacilitatorPaced mode
- "Current Inject" section with full content preview
- "Up Next" section showing next 2-3 injects by sequence
- Track current position in sequence
- Hide elapsed time clock
- Jump-to functionality with confirmation
- Story Time display per inject

### Out of Scope
- Clock-driven features (auto-Ready, countdown timers)
- Real-time clock display
- Session timer (optional future feature)

---

## Acceptance Criteria

- [ ] **Given** DeliveryMode = FacilitatorPaced, **when** I view conduct page, **then** no elapsed time clock is displayed
- [ ] **Given** I am in facilitator mode, **when** I view the page, **then** I see "Current Inject" section with full inject content
- [ ] **Given** current inject is #3, **when** I fire it, **then** #4 becomes the current inject automatically
- [ ] **Given** I am viewing current inject, **when** I look at "Up Next", **then** I see the next 2-3 injects by sequence order
- [ ] **Given** I want to skip ahead, **when** I click "Jump to" on an Up Next inject, **then** a confirmation dialog appears
- [ ] **Given** I confirm the jump, **when** processed, **then** skipped injects are marked Skipped and selected inject becomes current
- [ ] **Given** Story-only Timeline Mode, **when** I view an inject, **then** I see only Story Time (no Delivery Time)
- [ ] **Given** all injects are completed, **when** viewing, **then** a "Exercise Complete" message is displayed

---

## UI Design

### Facilitator-Paced Conduct View Layout

```
┌─────────────────────────────────────────────────────────────────────────┐
│  📖 Facilitator-Paced Mode                                              │
│     Progress: 3 of 12 injects                            [End Exercise] │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│ ▶ CURRENT INJECT                                                        │
│ ┌─────────────────────────────────────────────────────────────────────┐ │
│ │                                                                     │ │
│ │ #3  Evacuation Order                              Day 1 • 18:00     │ │
│ │ ═══════════════════════════════════════════════════════════════════ │ │
│ │                                                                     │ │
│ │ 📍 To: EOC Director                                                 │ │
│ │ 📤 Via: Verbal announcement                                         │ │
│ │                                                                     │ │
│ │ ─────────────────────────────────────────────────────────────────── │ │
│ │                                                                     │ │
│ │ "The Governor has issued a mandatory evacuation order for all       │ │
│ │ coastal zones within 50 miles of the projected landfall area.       │ │
│ │ Emergency Management Director, how do you proceed with notifying    │ │
│ │ the public and coordinating evacuation routes?"                     │ │
│ │                                                                     │ │
│ │ ─────────────────────────────────────────────────────────────────── │ │
│ │                                                                     │ │
│ │ 💡 Expected Action:                                                 │ │
│ │ Activate EAS, coordinate with transportation, open shelters         │ │
│ │                                                                     │ │
│ │ 📝 Controller Notes:                                                │ │
│ │ Allow 10-15 minutes discussion. Prompt if they miss shelter ops.    │ │
│ │                                                                     │ │
│ └─────────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│                              [Skip ▶]        [🔥 FIRE & CONTINUE]       │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│ UP NEXT                                                                 │
│                                                                         │
│ ┌───────────────────────────────────────────────────────────┐           │
│ │ #4 │ Shelter Operations Begin │ Day 2 • 06:00 │ [Jump →] │           │
│ │     "Red Cross has activated 3 shelters..."               │           │
│ └───────────────────────────────────────────────────────────┘           │
│                                                                         │
│ ┌───────────────────────────────────────────────────────────┐           │
│ │ #5 │ Medical Emergency      │ Day 2 • 10:00 │ [Jump →]   │           │
│ │     "Shelter B reports elderly patient..."                │           │
│ └───────────────────────────────────────────────────────────┘           │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│ ✓ COMPLETED (2)                                          [Show ▼]      │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Jump Confirmation Dialog

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│  Jump to Inject #5?                                              [X]    │
│                                                                         │
│  This will skip the following injects:                                  │
│                                                                         │
│    • #3 - Evacuation Order                                              │
│    • #4 - Shelter Operations Begin                                      │
│                                                                         │
│  Skipped injects will be marked as "Skipped" and can be                │
│  reviewed later if needed.                                              │
│                                                                         │
│                              [Cancel]        [Skip & Jump]              │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Design

### Component Structure

```
src/frontend/src/features/exercises/pages/
├── ExerciseConductPage.tsx              # Routes to appropriate view
├── FacilitatorPacedConductView.tsx      # New
└── FacilitatorPacedConductView.test.tsx

src/frontend/src/features/injects/components/
├── CurrentInjectPanel.tsx               # New - full inject display
├── CurrentInjectPanel.test.tsx
├── UpNextList.tsx                       # New
├── UpNextList.test.tsx
├── JumpConfirmationDialog.tsx           # New
└── JumpConfirmationDialog.test.tsx
```

### Current Inject Logic

```typescript
/**
 * Determines the current inject in facilitator-paced mode.
 * Current = first Pending inject in sequence order.
 */
function getCurrentInject(injects: InjectDto[]): InjectDto | null {
  const pending = injects
    .filter(i => i.status === InjectStatus.Pending)
    .sort((a, b) => a.sequence - b.sequence);

  return pending[0] || null;
}

/**
 * Gets the next N injects after the current one.
 */
function getUpNextInjects(
  injects: InjectDto[],
  currentSequence: number,
  count: number = 3
): InjectDto[] {
  return injects
    .filter(i => i.status === InjectStatus.Pending)
    .filter(i => i.sequence > currentSequence)
    .sort((a, b) => a.sequence - b.sequence)
    .slice(0, count);
}
```

### FacilitatorPacedConductView Component

```typescript
interface FacilitatorPacedConductViewProps {
  exercise: ExerciseDto;
  injects: InjectDto[];
  onFire: (injectId: string) => Promise<void>;
  onSkip: (injectId: string, reason?: string) => Promise<void>;
  onJumpTo: (injectId: string, skipInjectIds: string[]) => Promise<void>;
}

export const FacilitatorPacedConductView: React.FC<Props> = ({
  exercise,
  injects,
  onFire,
  onSkip,
  onJumpTo
}) => {
  const currentInject = useMemo(() => getCurrentInject(injects), [injects]);
  const upNextInjects = useMemo(
    () => getUpNextInjects(injects, currentInject?.sequence || 0),
    [injects, currentInject]
  );
  const completedInjects = useMemo(
    () => injects.filter(i =>
      i.status === InjectStatus.Fired || i.status === InjectStatus.Skipped
    ),
    [injects]
  );

  const [jumpTarget, setJumpTarget] = useState<InjectDto | null>(null);
  const [showCompleted, setShowCompleted] = useState(false);

  const handleFireAndContinue = async () => {
    if (currentInject) {
      await onFire(currentInject.id);
      // Next inject becomes current automatically via state update
    }
  };

  const handleJumpConfirm = async () => {
    if (!jumpTarget || !currentInject) return;

    // Find all injects between current and target
    const skipIds = injects
      .filter(i => i.status === InjectStatus.Pending)
      .filter(i => i.sequence >= currentInject.sequence && i.sequence < jumpTarget.sequence)
      .map(i => i.id);

    await onJumpTo(jumpTarget.id, skipIds);
    setJumpTarget(null);
  };

  if (!currentInject) {
    return <ExerciseCompletePanel exercise={exercise} />;
  }

  return (
    <Box>
      {/* Header - no clock */}
      <FacilitatorHeader
        exercise={exercise}
        progress={{ current: completedInjects.length + 1, total: injects.length }}
      />

      {/* Current Inject - Full Display */}
      <CurrentInjectPanel
        inject={currentInject}
        onFire={handleFireAndContinue}
        onSkip={(reason) => onSkip(currentInject.id, reason)}
      />

      {/* Up Next */}
      <UpNextList
        injects={upNextInjects}
        onJumpTo={setJumpTarget}
      />

      {/* Completed */}
      <CompletedSection
        injects={completedInjects}
        expanded={showCompleted}
        onToggle={() => setShowCompleted(!showCompleted)}
      />

      {/* Jump Confirmation Dialog */}
      <JumpConfirmationDialog
        open={!!jumpTarget}
        targetInject={jumpTarget}
        skippedInjects={jumpTarget ? injects
          .filter(i => i.status === InjectStatus.Pending)
          .filter(i => i.sequence >= (currentInject?.sequence || 0) && i.sequence < jumpTarget.sequence)
          : []
        }
        onConfirm={handleJumpConfirm}
        onCancel={() => setJumpTarget(null)}
      />
    </Box>
  );
};
```

### Jump-To Backend Support

Add endpoint or extend existing skip endpoint:

```csharp
// POST /api/exercises/{exerciseId}/injects/jump
public class JumpToInjectRequest
{
    public Guid TargetInjectId { get; set; }
    public List<Guid> SkipInjectIds { get; set; } = new();
    public string? SkipReason { get; set; }
}

[HttpPost("{exerciseId}/injects/jump")]
public async Task<IActionResult> JumpToInject(
    Guid exerciseId,
    JumpToInjectRequest request)
{
    // Skip all specified injects
    foreach (var injectId in request.SkipInjectIds)
    {
        await _injectService.SkipInjectAsync(
            exerciseId, injectId, UserId, "Jumped to later inject");
    }

    // Return updated inject list
    var injects = await _injectService.GetInjectsAsync(exerciseId);
    return Ok(injects);
}
```

### Mode Detection in Conduct Page

```typescript
// In ExerciseConductPage.tsx
if (exercise.deliveryMode === DeliveryMode.FacilitatorPaced) {
  return (
    <FacilitatorPacedConductView
      exercise={exercise}
      injects={injects}
      onFire={handleFire}
      onSkip={handleSkip}
      onJumpTo={handleJumpTo}
    />
  );
} else {
  return (
    <ClockDrivenConductView
      exercise={exercise}
      injects={injects}
      elapsedTimeMs={elapsedTimeMs}
      onFire={handleFire}
      onSkip={handleSkip}
    />
  );
}
```

---

## Test Cases

### Component Unit Tests

```typescript
describe('FacilitatorPacedConductView', () => {
  it('does not render elapsed time clock');
  it('shows current inject with full content');
  it('shows up next injects in sequence order');
  it('advances to next inject after firing');
  it('shows exercise complete when all injects done');
});

describe('getCurrentInject', () => {
  it('returns first pending inject by sequence');
  it('returns null when no pending injects');
  it('ignores fired and skipped injects');
});

describe('JumpConfirmationDialog', () => {
  it('lists injects that will be skipped');
  it('calls onConfirm with correct data');
  it('closes on cancel');
});
```

### Integration Tests

```typescript
describe('Facilitator-paced conduct flow', () => {
  it('renders facilitator view when DeliveryMode is FacilitatorPaced');
  it('fires current inject and advances to next');
  it('skips multiple injects when jumping');
  it('shows completion when last inject fired');
});
```

---

## Accessibility

- Current inject panel has `role="main"` or `aria-live="polite"`
- Progress announced to screen readers
- Jump confirmation dialog is focus-trapped
- Keyboard navigation through injects (arrow keys)
- Skip reason input is properly labeled

---

## Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| CLK-01: DeliveryMode field | 🔲 Required | Need mode check |
| Existing conduct infrastructure | ✅ Complete | Fire/Skip actions work |
| COBRA styled components | ✅ Complete | Use existing components |

---

## Blocked By

- CLK-01: Add timing configuration fields (need DeliveryMode)

---

## Blocks

None - this is a leaf story for facilitator-paced mode.

---

## Notes

- Consider adding optional session timer (total time spent, not exercise clock)
- Keyboard shortcuts: F = Fire, S = Skip, N = Next (same as fire), J = Jump
- Mobile-friendly: current inject should be scrollable
- Future: Add "Previous" button to review just-fired inject
- Future: Add notes/annotations during discussion
