# S04: Exercise Header with Clock

## Story

**As a** user working within an active exercise,
**I want** to see the exercise clock prominently in the sidebar,
**So that** I always know the current exercise time without looking at the main content area.

## Context

During exercise conduct, time awareness is critical. The clock is already implemented in the conduct page (CLK-06), but having it visible in the sidebar header ensures users always see it regardless of which exercise page they're viewing. This is a compact display that complements the full clock in the conduct view.

## Acceptance Criteria

### Clock Display
- [ ] **Given** I am in an exercise context, **when** the sidebar header renders, **then** I see the exercise clock
- [ ] **Given** the clock is running, **when** displayed, **then** it shows elapsed time in HH:MM:SS format
- [ ] **Given** the clock is running, **when** time passes, **then** the display updates every second
- [ ] **Given** the exercise hasn't started, **when** displayed, **then** it shows "00:00:00"

### Clock Styling
- [ ] **Given** the clock display, **when** rendered, **then** it uses a monospace font for consistent width
- [ ] **Given** the clock display, **when** rendered, **then** it is sized appropriately for the sidebar (not too large)

### Status Badge
- [ ] **Given** the clock is running, **when** displayed, **then** a green "Active" badge appears
- [ ] **Given** the clock is paused, **when** displayed, **then** a yellow "Paused" badge appears
- [ ] **Given** the clock hasn't started, **when** displayed, **then** a gray "Not Started" badge appears
- [ ] **Given** the exercise is completed, **when** displayed, **then** a blue "Completed" badge appears

### Real-Time Updates
- [ ] **Given** another user starts the clock, **when** SignalR event received, **then** sidebar clock starts
- [ ] **Given** another user pauses the clock, **when** SignalR event received, **then** sidebar clock pauses and badge updates
- [ ] **Given** I am on a non-conduct page within the exercise, **when** clock changes, **then** sidebar clock still updates

### Integration with Existing Clock
- [ ] **Given** CLK-06 clock components exist, **when** implementing, **then** reuse the same hooks/utilities
- [ ] **Given** useExerciseClock hook exists, **when** building sidebar clock, **then** use it for consistency

## Out of Scope

- Clock controls in sidebar (use conduct page for controls)
- Story time display in sidebar (keep it simple)
- Sound/notification on clock events

## Dependencies

- S03 (In-Exercise Context Navigation)
- CLK-06 implementation (existing clock display/hooks)
- useExerciseClock hook
- SignalR subscription infrastructure

## Domain Terms

| Term | Definition |
|------|------------|
| Elapsed Time | Time since exercise clock started (pauses during pause) |
| Clock State | Running, Paused, Stopped, or NotStarted |
| Status Badge | Visual indicator of current clock state |

## UI/UX Notes

### Sidebar Header with Clock
```
┌─────────────────────────────────────┐
│  ← Back                             │
│                                     │
│  Hurricane Response 2025            │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│                                     │
│   00:32:15   ● Active               │
│   ↑          ↑                      │
│   │          └── Status badge       │
│   └── Monospace, centered           │
│                                     │
├─────────────────────────────────────┤
│  Menu items...                      │
```

### Status Badge Colors
| State | Color | Label |
|-------|-------|-------|
| Running | Green (`success.main`) | Active |
| Paused | Yellow (`warning.main`) | Paused |
| Stopped/Not Started | Gray (`grey.500`) | Not Started |
| Completed | Blue (`info.main`) | Completed |

### Clock Format Examples
- `00:00:00` - Not started
- `00:32:15` - 32 minutes, 15 seconds elapsed
- `01:45:30` - 1 hour, 45 minutes elapsed
- `12:00:00` - 12 hours elapsed (long exercises)

## Technical Notes

### Reuse Existing Components
The CLK-06 implementation includes:
- `ClockDisplay.tsx` - May need compact variant
- `useExerciseClock.ts` - Hook for clock state and real-time updates
- SignalR events: `ClockStarted`, `ClockPaused`, `ClockStopped`

### Compact Clock Component
```typescript
interface ExerciseClockCompactProps {
  exerciseId: string;
  className?: string;
}

// Uses existing useExerciseClock hook
// Renders compact HH:MM:SS + badge
```

### Styling Guidelines
- Font: `fontFamily: 'monospace'`
- Size: ~16-18px (readable but not dominant)
- Badge: Small chip/pill, ~12px font
- Spacing: Consistent with theme.spacing()

---

*Story created: 2026-01-23*

## Implementation Notes

**Existing Code to Leverage:**
- `src/frontend/src/features/exercise-clock/hooks/useExerciseClock.ts`
- `src/frontend/src/features/exercise-clock/components/ClockDisplay.tsx`
- SignalR events already broadcast clock state changes

**New Code Needed:**
- Compact clock variant for sidebar
- Integration into ExerciseSidebar header
