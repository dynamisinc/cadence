# Optimistic Updates Implementation Plan

> **Purpose:** Improve perceived performance during exercise conduct by showing instant UI feedback while API calls happen in the background.

## Current State Analysis

### Already Implemented (useInjects.ts)

The `useInjects` hook already has **full optimistic update support** for:

| Action | Optimistic Update | Rollback on Error | Online/Offline |
|--------|-------------------|-------------------|----------------|
| Fire inject | Yes | Yes | Yes |
| Skip inject | Yes | Yes | Yes |
| Reset inject | Yes | Yes | Yes |
| Create inject | No (not frequent) | N/A | N/A |
| Update inject | Yes | Yes | N/A |
| Delete inject | Yes | Yes | N/A |

**Pattern used:**
```typescript
const fireMutation = useMutation({
  mutationFn: ...,
  onMutate: async ({ id }) => {
    // 1. Cancel pending queries to avoid race conditions
    await queryClient.cancelQueries({ queryKey })

    // 2. Snapshot current state for rollback
    const previousInjects = queryClient.getQueryData<InjectDto[]>(queryKey)

    // 3. Optimistic update - immediately show new state
    queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
      old.map(inject =>
        inject.id === id
          ? { ...inject, status: InjectStatus.Fired, firedAt: new Date().toISOString() }
          : inject
      )
    )

    // 4. Return context for rollback
    return { previousInjects }
  },
  onSuccess: firedInject => {
    // Replace optimistic data with real server data
    queryClient.setQueryData<InjectDto[]>(queryKey, (old = []) =>
      old.map(inject => inject.id === firedInject.id ? firedInject : inject)
    )
  },
  onError: (err, _variables, context) => {
    // Rollback to previous state on error
    if (context?.previousInjects) {
      queryClient.setQueryData(queryKey, context.previousInjects)
    }
    toast.error(message)
  },
})
```

### Needs Optimistic Updates

| Hook | Action | Priority | Impact | Notes |
|------|--------|----------|--------|-------|
| `useExerciseClock` | Start/Pause clock | High | Immediate feedback for clock controls | Frequently toggled during conduct |
| `useObservations` | Create observation | Medium | Quick capture during evaluation | Already has offline support |
| `useExerciseStatus` | Revert to Draft | Low | Infrequent operation | Keep invalidation (discussed) |

---

## Implementation Plan

### Phase 1: Exercise Clock Optimistic Updates (High Priority)

**File:** `src/frontend/src/features/exercise-clock/hooks/useExerciseClock.ts`

**Why:** Clock controls are toggled frequently during active exercise conduct. Users expect instant visual feedback when clicking Start/Pause.

**Changes needed:**

#### 1.1 Start Clock - Optimistic Update

```typescript
const startMutation = useMutation({
  mutationFn: () => clockService.startClock(exerciseId),
  onMutate: async () => {
    await queryClient.cancelQueries({ queryKey: clockQueryKey(exerciseId) })
    const previousState = queryClient.getQueryData<ClockStateDto>(clockQueryKey(exerciseId))

    // Optimistic: Show clock as running immediately
    const now = new Date().toISOString()
    const optimisticState: ClockStateDto = {
      state: ExerciseClockState.Running,
      elapsedTime: previousState?.elapsedTime ?? '00:00:00',
      startedAt: now,
      startedBy: 'current-user', // Will be replaced by server
      startedByName: 'You',
      exerciseStartTime: previousState?.exerciseStartTime ?? null,
      capturedAt: now,
    }

    queryClient.setQueryData(clockQueryKey(exerciseId), optimisticState)
    return { previousState }
  },
  onSuccess: newState => {
    updateClockState(newState) // Replace with real server data
    toast.success('Exercise clock started')
  },
  onError: (err, _variables, context) => {
    if (context?.previousState) {
      queryClient.setQueryData(clockQueryKey(exerciseId), context.previousState)
    }
    toast.error(err instanceof Error ? err.message : 'Failed to start clock')
  },
})
```

#### 1.2 Pause Clock - Optimistic Update

```typescript
const pauseMutation = useMutation({
  mutationFn: () => clockService.pauseClock(exerciseId),
  onMutate: async () => {
    await queryClient.cancelQueries({ queryKey: clockQueryKey(exerciseId) })
    const previousState = queryClient.getQueryData<ClockStateDto>(clockQueryKey(exerciseId))

    // Calculate elapsed time at pause moment
    const now = Date.now()
    let totalElapsedMs = parseElapsedTime(previousState?.elapsedTime ?? '00:00:00')
    if (previousState?.state === ExerciseClockState.Running && previousState.startedAt) {
      const startedAt = new Date(previousState.capturedAt).getTime()
      totalElapsedMs += (now - startedAt)
    }

    const optimisticState: ClockStateDto = {
      state: ExerciseClockState.Paused,
      elapsedTime: formatElapsedTimeForDto(totalElapsedMs),
      startedAt: null,
      startedBy: previousState?.startedBy ?? null,
      startedByName: previousState?.startedByName ?? null,
      exerciseStartTime: previousState?.exerciseStartTime ?? null,
      capturedAt: new Date().toISOString(),
    }

    queryClient.setQueryData(clockQueryKey(exerciseId), optimisticState)
    return { previousState }
  },
  onSuccess: newState => {
    updateClockState(newState)
    toast.success('Exercise clock paused')
  },
  onError: (err, _variables, context) => {
    if (context?.previousState) {
      queryClient.setQueryData(clockQueryKey(exerciseId), context.previousState)
    }
    toast.error(err instanceof Error ? err.message : 'Failed to pause clock')
  },
})
```

#### 1.3 Helper Function Needed

```typescript
// Convert milliseconds to elapsed time format for DTO
const formatElapsedTimeForDto = (ms: number): string => {
  const totalSeconds = Math.floor(ms / 1000)
  const hours = Math.floor(totalSeconds / 3600)
  const minutes = Math.floor((totalSeconds % 3600) / 60)
  const seconds = totalSeconds % 60
  return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`
}
```

---

### Phase 2: Observation Create Optimistic Updates (Medium Priority)

**File:** `src/frontend/src/features/observations/hooks/useObservations.ts`

**Why:** Evaluators capture observations rapidly during exercise conduct. Waiting for server round-trip breaks the flow.

**Current state:** Already has offline optimistic support, but the online path uses `onSuccess` only.

**Changes needed:**

#### 2.1 Update createMutation for online optimistic updates

```typescript
const createMutation = useMutation({
  mutationFn: (request: CreateObservationRequest) =>
    observationService.createObservation(exerciseId, request),
  onMutate: async (request) => {
    await queryClient.cancelQueries({ queryKey: observationsQueryKey(exerciseId) })
    const previousObservations = queryClient.getQueryData<ObservationDto[]>(
      observationsQueryKey(exerciseId)
    )

    // Optimistic observation with temp ID
    const tempId = `temp-${Date.now()}-${Math.random().toString(36).slice(2)}`
    const optimisticObservation: ObservationDto = {
      id: tempId,
      exerciseId,
      injectId: request.injectId ?? null,
      objectiveId: request.objectiveId ?? null,
      content: request.content,
      rating: request.rating ?? null,
      recommendation: request.recommendation ?? null,
      observedAt: request.observedAt ?? new Date().toISOString(),
      location: request.location ?? null,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      createdBy: 'pending',
      createdByName: 'You',
      injectTitle: null, // Will be populated by server
      injectNumber: null,
    }

    queryClient.setQueryData<ObservationDto[]>(
      observationsQueryKey(exerciseId),
      (old = []) => [optimisticObservation, ...old]
    )

    return { previousObservations, tempId }
  },
  onSuccess: (newObservation, _request, context) => {
    // Replace temp observation with real one
    queryClient.setQueryData<ObservationDto[]>(
      observationsQueryKey(exerciseId),
      (old = []) => old.map(obs =>
        obs.id === context?.tempId ? newObservation : obs
      )
    )
    if (newObservation.injectId) {
      queryClient.invalidateQueries({
        queryKey: observationsByInjectQueryKey(newObservation.injectId),
      })
    }
    toast.success('Observation recorded')
  },
  onError: (err, _request, context) => {
    if (context?.previousObservations) {
      queryClient.setQueryData(
        observationsQueryKey(exerciseId),
        context.previousObservations
      )
    }
    toast.error(err instanceof Error ? err.message : 'Failed to create observation')
  },
})
```

#### 2.2 Simplify createObservation wrapper

```typescript
const createObservation = async (request: CreateObservationRequest): Promise<ObservationDto> => {
  if (isEffectivelyOnline) {
    return createMutation.mutateAsync(request)
  }

  // Offline path remains unchanged (already has optimistic updates)
  // ... existing offline code ...
}
```

---

### Phase 3: Update/Delete Observation Optimistic (Optional)

Lower priority since updates/deletes are less frequent than creates during conduct.

**If implemented:**
- `updateMutation`: Add `onMutate` with snapshot + optimistic update
- `deleteMutation`: Add `onMutate` to immediately remove from list

---

## Actions NOT Requiring Optimistic Updates

| Action | Reason |
|--------|--------|
| Exercise CRUD | Infrequent, not time-sensitive |
| Inject CRUD | Already has optimistic updates |
| Phase management | Infrequent, setup-time only |
| Objective management | Infrequent, setup-time only |
| Revert to Draft | Infrequent, invalidation acceptable |
| Archive/Complete | Infrequent, invalidation acceptable |

---

## Testing Checklist

### Clock Optimistic Updates
- [ ] Start clock shows Running state immediately
- [ ] Pause clock shows Paused state immediately
- [ ] Clock display continues updating during optimistic Running state
- [ ] Error during start reverts to previous state
- [ ] Error during pause reverts to previous state (clock keeps running)
- [ ] Server response replaces optimistic data correctly

### Observation Optimistic Updates
- [ ] New observation appears immediately in list
- [ ] Temp ID observation gets replaced with real observation on success
- [ ] Error reverts to previous observation list
- [ ] Inject-specific query gets invalidated on success

### General
- [ ] No duplicate entries during optimistic + server response
- [ ] Offline mode still works correctly
- [ ] SignalR updates don't conflict with optimistic updates

---

## Implementation Order

1. **Phase 1.1-1.3**: Clock start/pause optimistic updates (~30 min)
2. **Phase 2.1-2.2**: Observation create optimistic updates (~20 min)
3. **Testing**: Manual testing of all paths (~20 min)
4. **Phase 3** (optional): Observation update/delete (~20 min)

Total estimated work: ~1-1.5 hours

---

## Code Quality Notes

- All optimistic updates follow the same pattern (snapshot → optimistic → replace/rollback)
- Use `cancelQueries` before optimistic updates to prevent race conditions
- Always return previous state in `onMutate` for rollback capability
- Replace optimistic data with server response in `onSuccess`
- Rollback in `onError` using context from `onMutate`
