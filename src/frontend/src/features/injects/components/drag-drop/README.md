# Inject Drag-and-Drop Reordering

This module provides drag-and-drop functionality for reordering injects in the MSEL view.

## Components

### DragHandle

A visual grip handle that indicates draggability.

**Props:**
- `attributes` - Draggable attributes from @dnd-kit
- `listeners` - Draggable listeners from @dnd-kit
- `disabled` - Disable dragging (hides the handle)

**Usage:**
```tsx
import { DragHandle } from './DragHandle'

<DragHandle attributes={attributes} listeners={listeners} disabled={false} />
```

### SortableInjectRow

Wrapper for table rows that adds drag-and-drop capability.

**Props:**
- `inject` - The inject to display
- `disabled` - Disable dragging
- `children` - Table cells to display

**Usage:**
```tsx
import { SortableInjectRow } from './SortableInjectRow'

<SortableInjectRow inject={inject} disabled={false}>
  <TableCell>{inject.title}</TableCell>
  <TableCell>{inject.status}</TableCell>
</SortableInjectRow>
```

### SortableInjectList

Provides DndContext for drag-and-drop reordering with optimistic updates.

**Props:**
- `injects` - Array of injects (should be ordered by sequence)
- `onReorder` - Callback when reorder completes - receives new order of inject IDs
- `disabled` - Disable reordering (e.g., during Active exercise)
- `children` - Render function that receives ordered inject list

**Usage:**
```tsx
import { SortableInjectList } from './SortableInjectList'
import { SortableInjectRow } from './SortableInjectRow'

const handleReorder = async (injectIds: string[]) => {
  await injectService.reorderInjects(mselId, { injectIds })
}

<SortableInjectList
  injects={injects}
  onReorder={handleReorder}
  disabled={exerciseStatus === 'Active'}
>
  {(orderedInjects) => (
    <Table>
      <TableBody>
        {orderedInjects.map(inject => (
          <SortableInjectRow key={inject.id} inject={inject}>
            <TableCell>{inject.injectNumber}</TableCell>
            <TableCell>{inject.title}</TableCell>
            <TableCell>{inject.status}</TableCell>
          </SortableInjectRow>
        ))}
      </TableBody>
    </Table>
  )}
</SortableInjectList>
```

## Features

### Optimistic Updates

The list updates immediately when dragging, providing instant feedback. If the API call fails, the list reverts to the original order and shows an error toast.

### Keyboard Support

Users can:
- Tab to a drag handle
- Press Space or Enter to pick up an item
- Use arrow keys to move it
- Press Space or Enter to drop it
- Press Escape to cancel

### Disabled State

When `disabled={true}`:
- Drag handles are hidden
- Dragging is disabled
- The list renders normally (read-only)

This is used when the exercise status is "Active" to prevent accidental reordering during conduct.

### Error Handling

If the `onReorder` callback throws an error:
- The list reverts to the original order (rollback)
- An error toast is displayed
- The error message is shown to the user

## Backend Integration

The `onReorder` callback should call a backend endpoint that:
1. Updates the sequence numbers of all injects
2. Returns the updated inject list
3. Broadcasts the change via SignalR to other users

Example backend endpoint:
```csharp
[HttpPost("{mselId}/reorder")]
public async Task<IActionResult> ReorderInjects(
    Guid mselId,
    ReorderInjectsRequest request)
{
    // Validate exercise is not Active
    var exercise = await _context.Exercises
        .Include(e => e.Msel)
        .FirstOrDefaultAsync(e => e.Msel.Id == mselId);

    if (exercise?.Status == ExerciseStatus.Active)
        return BadRequest("Cannot reorder injects while exercise is active");

    // Update sequence numbers
    for (int i = 0; i < request.InjectIds.Count; i++)
    {
        var inject = await _context.Injects.FindAsync(request.InjectIds[i]);
        if (inject != null)
        {
            inject.Sequence = i + 1;
        }
    }

    await _context.SaveChangesAsync();

    // Broadcast update
    await _hubContext.NotifyMselUpdated(exercise.Id, msel.ToDto());

    return Ok();
}
```

## Testing

The components include comprehensive tests:
- `DragHandle.test.tsx` - Visual rendering and disabled state
- `SortableInjectRow.test.tsx` - Row rendering and drag handle integration
- `SortableInjectList.test.tsx` - List rendering and prop updates

Note: Full drag-and-drop interaction testing with @dnd-kit is complex and requires additional setup. The current tests verify rendering and prop handling.

## Accessibility

- Keyboard navigation fully supported
- Screen reader announcements for drag operations (provided by @dnd-kit)
- Focus visible on drag handles
- Proper ARIA attributes

## Dependencies

- `@dnd-kit/core` - Core drag-and-drop functionality
- `@dnd-kit/sortable` - Sortable list utilities
- `@dnd-kit/utilities` - CSS transform utilities
- `react-toastify` - Error notifications

## Story

Implements Story CLK-10: Sequence Number Reordering via Drag-Drop

See: `docs/features/exercise-config/S10-sequence-drag-drop-reorder.md`
