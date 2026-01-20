# Integration Example: Drag-Drop MSEL Reordering

This document shows a complete example of integrating the drag-drop components into an MSEL list view.

## Complete Page Example

```tsx
/**
 * MselListPage - MSEL view with drag-drop reordering
 */

import { useState, useCallback } from 'react'
import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
  Alert,
  Chip,
} from '@mui/material'
import { toast } from 'react-toastify'
import { useParams } from 'react-router-dom'

import {
  SortableInjectList,
  SortableInjectRow,
  InjectStatusChip,
  InjectTypeChip,
} from '@/features/injects'
import { useExercise } from '@/features/exercises'
import { useInjects } from '@/features/injects/hooks/useInjects'
import { injectService } from '@/features/injects/services/injectService'
import { ExerciseStatus } from '@/types'
import CobraStyles from '@/theme/CobraStyles'

export const MselListPage = () => {
  const { exerciseId } = useParams<{ exerciseId: string }>()
  const { exercise, loading: exerciseLoading } = useExercise(exerciseId!)
  const { injects, loading: injectsLoading, refetch } = useInjects(exerciseId!)
  const [reordering, setReordering] = useState(false)

  // Disable reordering when exercise is Active
  const isActive = exercise?.status === ExerciseStatus.Active
  const canReorder = !isActive && !reordering

  /**
   * Handle inject reorder
   * Calls backend to update sequence numbers and broadcast change
   */
  const handleReorder = useCallback(
    async (injectIds: string[]) => {
      if (!exercise?.mselId) return

      setReordering(true)
      try {
        await injectService.reorderInjects(exercise.mselId, { injectIds })
        toast.success('Inject order updated')
        // Refetch to get updated sequence numbers from backend
        await refetch()
      } catch (error) {
        // Error toast shown by SortableInjectList
        throw error // Re-throw to trigger rollback
      } finally {
        setReordering(false)
      }
    },
    [exercise?.mselId, refetch]
  )

  // Loading state
  if (exerciseLoading || injectsLoading) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Typography>Loading...</Typography>
      </Box>
    )
  }

  // Error state
  if (!exercise) {
    return (
      <Box padding={CobraStyles.Padding.MainWindow}>
        <Alert severity="error">Exercise not found</Alert>
      </Box>
    )
  }

  return (
    <Box padding={CobraStyles.Padding.MainWindow}>
      {/* Page Header */}
      <Box marginBottom={3}>
        <Typography variant="h5" gutterBottom>
          MSEL: {exercise.name}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {injects.length} injects
        </Typography>

        {/* Active Exercise Warning */}
        {isActive && (
          <Alert severity="info" sx={{ mt: 2 }}>
            🔒 Reordering is disabled while the exercise is active.
          </Alert>
        )}
      </Box>

      {/* Inject List with Drag-Drop */}
      <TableContainer component={Paper} variant="outlined">
        <SortableInjectList
          injects={injects}
          onReorder={handleReorder}
          disabled={!canReorder}
        >
          {(orderedInjects) => (
            <Table size="small">
              <TableHead>
                <TableRow>
                  {/* Empty cell for drag handle */}
                  {canReorder && <TableCell sx={{ width: 40 }} />}

                  <TableCell sx={{ width: 50 }}>#</TableCell>
                  <TableCell sx={{ width: 100 }}>Time</TableCell>
                  <TableCell>Title</TableCell>
                  <TableCell sx={{ width: 90 }}>Type</TableCell>
                  <TableCell sx={{ width: 100 }}>Status</TableCell>
                </TableRow>
              </TableHead>

              <TableBody>
                {orderedInjects.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={canReorder ? 6 : 5} align="center">
                      <Typography variant="body2" color="text.secondary">
                        No injects in this MSEL
                      </Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  orderedInjects.map((inject) =>
                    canReorder ? (
                      // Draggable row when reordering is enabled
                      <SortableInjectRow key={inject.id} inject={inject} disabled={!canReorder}>
                        <TableCell>
                          <Typography variant="body2" fontWeight={600}>
                            #{inject.injectNumber}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" fontFamily="monospace">
                            {inject.scenarioTime || '--:--'}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">{inject.title}</Typography>
                        </TableCell>
                        <TableCell>
                          <InjectTypeChip type={inject.injectType} />
                        </TableCell>
                        <TableCell>
                          <InjectStatusChip status={inject.status} />
                        </TableCell>
                      </SortableInjectRow>
                    ) : (
                      // Static row when reordering is disabled
                      <TableRow key={inject.id}>
                        <TableCell>
                          <Typography variant="body2" fontWeight={600}>
                            #{inject.injectNumber}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2" fontFamily="monospace">
                            {inject.scenarioTime || '--:--'}
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Typography variant="body2">{inject.title}</Typography>
                        </TableCell>
                        <TableCell>
                          <InjectTypeChip type={inject.injectType} />
                        </TableCell>
                        <TableCell>
                          <InjectStatusChip status={inject.status} />
                        </TableCell>
                      </TableRow>
                    )
                  )
                )}
              </TableBody>
            </Table>
          )}
        </SortableInjectList>
      </TableContainer>
    </Box>
  )
}
```

## Backend Service Method

Add this method to `injectService.ts`:

```typescript
/**
 * Reorder injects in the MSEL
 * Updates sequence numbers based on new order
 */
reorderInjects: async (
  mselId: string,
  request: { injectIds: string[] }
): Promise<void> => {
  await apiClient.post(`/api/msels/${mselId}/reorder`, request)
}
```

## Backend Endpoint (C#)

```csharp
[HttpPost("{mselId}/reorder")]
public async Task<IActionResult> ReorderInjects(
    Guid mselId,
    [FromBody] ReorderInjectsRequest request)
{
    // Get MSEL with injects and exercise
    var msel = await _context.Msels
        .Include(m => m.Injects)
        .Include(m => m.Exercise)
        .FirstOrDefaultAsync(m => m.Id == mselId);

    if (msel == null)
        return NotFound("MSEL not found");

    // Validate exercise is not Active
    if (msel.Exercise.Status == ExerciseStatus.Active)
        return BadRequest("Cannot reorder injects while exercise is active");

    // Validate all inject IDs belong to this MSEL
    var mselInjectIds = msel.Injects.Select(i => i.Id).ToHashSet();
    var invalidIds = request.InjectIds.Where(id => !mselInjectIds.Contains(id)).ToList();

    if (invalidIds.Any())
        return BadRequest($"Invalid inject IDs: {string.Join(", ", invalidIds)}");

    // Update sequence numbers (1-based)
    for (int i = 0; i < request.InjectIds.Count; i++)
    {
        var inject = msel.Injects.FirstOrDefault(j => j.Id == request.InjectIds[i]);
        if (inject != null)
        {
            inject.Sequence = i + 1;
            inject.InjectNumber = i + 1; // Update inject number to match sequence
        }
    }

    await _context.SaveChangesAsync();
    _logger.LogInformation(
        "Reordered {Count} injects in MSEL {MselId}",
        request.InjectIds.Count,
        mselId
    );

    // Broadcast update via SignalR
    var updatedInjects = msel.Injects
        .OrderBy(i => i.Sequence)
        .Select(i => i.ToDto())
        .ToList();

    await _hubContext.NotifyInjectsReordered(msel.Exercise.Id, updatedInjects);

    return Ok();
}

public class ReorderInjectsRequest
{
    public List<Guid> InjectIds { get; set; } = new();
}
```

## SignalR Event

Add to `IExerciseHubContext.cs`:

```csharp
/// <summary>
/// Notifies clients that injects have been reordered
/// </summary>
Task NotifyInjectsReordered(Guid exerciseId, List<InjectDto> injects);
```

Implement in `ExerciseHubContext.cs`:

```csharp
public async Task NotifyInjectsReordered(Guid exerciseId, List<InjectDto> injects)
{
    await Clients
        .Group(exerciseId.ToString())
        .SendAsync("InjectsReordered", injects);
}
```

## Frontend SignalR Listener

In your `useInjects` hook or component:

```typescript
useEffect(() => {
  if (!connection) return

  const handleInjectsReordered = (updatedInjects: InjectDto[]) => {
    // Update local state with new order
    queryClient.setQueryData(['injects', exerciseId], updatedInjects)
    toast.info('Inject order updated by another user')
  }

  connection.on('InjectsReordered', handleInjectsReordered)

  return () => {
    connection.off('InjectsReordered', handleInjectsReordered)
  }
}, [connection, exerciseId, queryClient])
```

## Key Points

1. **Optimistic Updates**: The list reorders immediately for responsive UX
2. **Error Handling**: Failed reorders automatically rollback with error toast
3. **Active Exercise Lock**: Drag handles hidden when exercise is Active
4. **SignalR Sync**: Other users see reorder changes in real-time
5. **Keyboard Support**: Full keyboard navigation for accessibility
6. **Mobile Friendly**: Touch events supported via @dnd-kit

## Testing

```typescript
describe('MselListPage drag-drop integration', () => {
  it('allows reordering when exercise is Draft', async () => {
    // Test implementation
  })

  it('disables reordering when exercise is Active', () => {
    // Test implementation
  })

  it('shows error toast on reorder failure', async () => {
    // Test implementation
  })

  it('updates other clients via SignalR', async () => {
    // Test implementation
  })
})
```
