# Story CLK-10: Sequence Number Reordering via Drag-Drop

> **Story ID:** CLK-10
> **Feature:** Exercise Clock Modes
> **Phase:** D - Exercise Conduct
> **Status:** Ready for Development
> **Estimate:** Medium (1-2 days)

---

## User Story

**As a** Controller viewing the MSEL,
**I want** to reorder injects by dragging them,
**So that** sequence numbers update automatically without manual editing.

---

## Background

The MSEL (Master Scenario Events List) is an ordered list of injects. Controllers need to occasionally reorder injects during exercise preparation. Drag-and-drop provides an intuitive way to reorder without manually editing sequence numbers on each inject.

---

## Scope

### In Scope
- Add drag-drop functionality to inject list in MSEL view
- Update sequence numbers on drop
- Persist reordering to backend
- Broadcast changes via SignalR to other users
- Disable reordering when exercise is Active

### Out of Scope
- Reordering within conduct view (use MSEL view)
- Multi-select drag (single inject at a time)
- Undo/redo for reordering
- Drag between phases (reorder within same phase only initially)

---

## Acceptance Criteria

- [ ] **Given** I am viewing the MSEL inject list, **when** I drag inject #3 above inject #2, **then** former #3 becomes #2 and former #2 becomes #3
- [ ] **Given** I reorder injects, **when** I drop an inject, **then** the new order is saved to the backend
- [ ] **Given** another user is viewing the MSEL, **when** I reorder injects, **then** they see the update in real-time via SignalR
- [ ] **Given** the exercise is Active, **when** I view the MSEL, **then** drag handles are hidden and reordering is disabled
- [ ] **Given** the exercise is Draft or Paused, **when** I view the MSEL, **then** drag handles are visible
- [ ] **Given** I drag an inject, **when** dragging, **then** visual feedback shows where the inject will be placed
- [ ] **Given** the reorder fails (network error), **when** dropped, **then** the list reverts to original order and error message shown

---

## UI Design

### MSEL List with Drag Handles

```
┌─────────────────────────────────────────────────────────────────────────┐
│ MSEL: Hurricane Response Exercise                                       │
│ 12 injects                                        [+ Add Inject]        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│ ⋮⋮  #1  │ Exercise Kickoff        │ +00:00:00 │ Day 1 08:00 │  [Edit]  │
│ ⋮⋮  #2  │ Weather Update          │ +00:15:00 │ Day 1 08:15 │  [Edit]  │
│ ════════════════════════════════════════════════════════════════════════│
│ ⋮⋮  #3  │ Evacuation Order        │ +00:30:00 │ Day 1 08:30 │  [Edit]  │
│     ↑                                                                   │
│     └── Dragging here                                                   │
│ ════════════════════════════════════════════════════════════════════════│
│ ⋮⋮  #4  │ Shelter Operations      │ +00:45:00 │ Day 1 08:45 │  [Edit]  │
│ ⋮⋮  #5  │ Medical Emergency       │ +01:00:00 │ Day 1 09:00 │  [Edit]  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Drag Handle Icon

```
⋮⋮  (grip dots - visible on hover in desktop, always visible on touch)
```

### Active Exercise (Locked)

```
┌─────────────────────────────────────────────────────────────────────────┐
│ MSEL: Hurricane Response Exercise                        🔒 ACTIVE      │
│ 12 injects                                                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│     #1  │ Exercise Kickoff        │ +00:00:00 │ Day 1 08:00 │  [View]  │
│     #2  │ Weather Update          │ +00:15:00 │ Day 1 08:15 │  [View]  │
│                                                                         │
│  ⚠️ Reordering is disabled while exercise is active.                    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Design

### Library Choice

Use `@dnd-kit/core` and `@dnd-kit/sortable` for drag-and-drop:
- Accessible by default (keyboard support)
- Touch-friendly
- Small bundle size
- Active maintenance

### Component Structure

```
src/frontend/src/features/injects/components/
├── SortableInjectList.tsx           # DnD context wrapper
├── SortableInjectList.test.tsx
├── SortableInjectRow.tsx            # Draggable row
├── SortableInjectRow.test.tsx
├── DragHandle.tsx                   # Grip icon component
└── DragHandle.test.tsx
```

### SortableInjectList Component

```typescript
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  DragEndEvent,
} from '@dnd-kit/core';
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';

interface SortableInjectListProps {
  injects: InjectDto[];
  onReorder: (injectIds: string[]) => Promise<void>;
  disabled?: boolean;
}

export const SortableInjectList: React.FC<SortableInjectListProps> = ({
  injects,
  onReorder,
  disabled = false
}) => {
  const [items, setItems] = useState(injects);
  const [isReordering, setIsReordering] = useState(false);

  // Update local state when props change
  useEffect(() => {
    setItems(injects);
  }, [injects]);

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 8, // Prevent accidental drags
      },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;

    if (over && active.id !== over.id) {
      const oldIndex = items.findIndex(i => i.id === active.id);
      const newIndex = items.findIndex(i => i.id === over.id);

      // Optimistic update
      const newItems = arrayMove(items, oldIndex, newIndex);
      setItems(newItems);

      try {
        setIsReordering(true);
        await onReorder(newItems.map(i => i.id));
      } catch (error) {
        // Revert on failure
        setItems(injects);
        toast.error('Failed to reorder injects. Please try again.');
      } finally {
        setIsReordering(false);
      }
    }
  };

  if (disabled) {
    return (
      <Box>
        {items.map(inject => (
          <InjectRow key={inject.id} inject={inject} showDragHandle={false} />
        ))}
      </Box>
    );
  }

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCenter}
      onDragEnd={handleDragEnd}
    >
      <SortableContext
        items={items.map(i => i.id)}
        strategy={verticalListSortingStrategy}
      >
        {items.map(inject => (
          <SortableInjectRow
            key={inject.id}
            inject={inject}
            disabled={isReordering}
          />
        ))}
      </SortableContext>
    </DndContext>
  );
};
```

### SortableInjectRow Component

```typescript
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';

interface SortableInjectRowProps {
  inject: InjectDto;
  disabled?: boolean;
}

export const SortableInjectRow: React.FC<SortableInjectRowProps> = ({
  inject,
  disabled
}) => {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: inject.id, disabled });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
    zIndex: isDragging ? 1000 : 'auto',
  };

  return (
    <Box
      ref={setNodeRef}
      style={style}
      sx={{
        display: 'flex',
        alignItems: 'center',
        backgroundColor: isDragging ? 'action.selected' : 'background.paper',
        borderRadius: 1,
        mb: 0.5,
      }}
    >
      <DragHandle {...attributes} {...listeners} disabled={disabled} />
      <InjectRowContent inject={inject} />
    </Box>
  );
};
```

### Backend Reorder Endpoint

```csharp
// POST /api/msels/{mselId}/reorder
[HttpPost("{mselId}/reorder")]
public async Task<IActionResult> ReorderInjects(
    Guid mselId,
    ReorderInjectsRequest request)
{
    var msel = await _context.Msels
        .Include(m => m.Injects)
        .FirstOrDefaultAsync(m => m.Id == mselId);

    if (msel == null)
        return NotFound();

    // Validate exercise is not Active
    var exercise = await _context.Exercises.FindAsync(msel.ExerciseId);
    if (exercise?.Status == ExerciseStatus.Active)
        return BadRequest("Cannot reorder injects while exercise is active");

    // Update sequence numbers based on new order
    for (int i = 0; i < request.InjectIds.Count; i++)
    {
        var inject = msel.Injects.FirstOrDefault(j => j.Id == request.InjectIds[i]);
        if (inject != null)
        {
            inject.Sequence = i + 1;
        }
    }

    await _context.SaveChangesAsync();

    // Broadcast update
    await _hubContext.NotifyMselUpdated(msel.ExerciseId, msel.ToDto());

    return Ok(msel.Injects.OrderBy(i => i.Sequence).Select(i => i.ToDto()));
}

public class ReorderInjectsRequest
{
    public List<Guid> InjectIds { get; set; } = new();
}
```

### SignalR Event

Add to `IExerciseHubContext`:

```csharp
Task NotifyMselUpdated(Guid exerciseId, MselDto msel);
Task NotifyInjectsReordered(Guid exerciseId, List<InjectDto> injects);
```

---

## Test Cases

### Frontend Unit Tests

```typescript
describe('SortableInjectList', () => {
  it('renders all injects in order');
  it('shows drag handles when not disabled');
  it('hides drag handles when disabled');
  it('reorders on drag end');
  it('calls onReorder with new order');
  it('reverts on reorder failure');
});

describe('SortableInjectRow', () => {
  it('applies transform styles when dragging');
  it('reduces opacity when dragging');
  it('provides drag handle listeners');
});

describe('DragHandle', () => {
  it('renders grip icon');
  it('has proper aria-label');
  it('disables when disabled prop is true');
});
```

### Backend Unit Tests

```csharp
[Fact]
public async Task ReorderInjects_UpdatesSequenceNumbers()

[Fact]
public async Task ReorderInjects_ActiveExercise_ReturnsBadRequest()

[Fact]
public async Task ReorderInjects_BroadcastsSignalREvent()

[Fact]
public async Task ReorderInjects_InvalidInjectId_IgnoresIt()
```

### Integration Tests

```typescript
describe('MSEL reordering', () => {
  it('persists new order to backend');
  it('updates other clients via SignalR');
  it('disables when exercise is Active');
});
```

---

## Accessibility

- Keyboard support: Tab to drag handle, Space/Enter to pick up, Arrow keys to move, Space/Enter to drop
- Screen reader announcements for drag operations
- Focus visible on drag handle
- `aria-describedby` with instructions
- `role="listbox"` for the list, `role="option"` for rows

---

## Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| @dnd-kit/core | 🔲 To install | `npm install @dnd-kit/core @dnd-kit/sortable` |
| Inject list component | ✅ Complete | Wrapping existing component |
| SignalR infrastructure | ✅ Complete | Add new event type |

---

## Blocked By

None - can be implemented independently.

---

## Blocks

None - this is an enhancement story.

---

## Notes

- Drag-drop is disabled during Active exercises to prevent accidental reordering
- Consider adding "Reorder Mode" toggle for cleaner UX on mobile
- Future: Cross-phase drag-drop (move inject from Phase 1 to Phase 2)
- Future: Multi-select reorder
- The Sequence field is 1-based for user display (not 0-based)

---

## Installation

```bash
cd src/frontend
npm install @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities
```
