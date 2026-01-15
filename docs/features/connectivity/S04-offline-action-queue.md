# S04: Offline Action Queue

## Story

**As a** Controller or Evaluator working offline
**I want to** perform actions that queue locally
**So that** I can continue working and have my changes sync when connection restores

## Priority

P0 - MVP Required

## Acceptance Criteria

### AC1: Action Queue Storage
- [ ] Pending actions stored in IndexedDB
- [ ] Queue persists across page refresh and app restart
- [ ] Each queued action has unique ID, timestamp, type, and payload
- [ ] Queue maintains FIFO order for processing

### AC2: Queueable Actions
- [ ] Fire inject action queues when offline
- [ ] Skip inject action queues when offline
- [ ] Create observation action queues when offline
- [ ] Update observation action queues when offline
- [ ] Delete observation action queues when offline

### AC3: Optimistic UI Updates
- [ ] UI updates immediately when action queued (optimistic)
- [ ] Queued items show visual indicator (e.g., ⏳ pending sync icon)
- [ ] User can see their changes reflected in UI while offline
- [ ] Optimistic updates distinguishable from confirmed updates

### AC4: Queue Status Display
- [ ] User can see number of pending actions
- [ ] Pending count visible in connection indicator area
- [ ] Example: "🔴 Offline (3 pending)"
- [ ] Clicking shows list of pending actions

### AC5: Queue Management
- [ ] Failed actions can be retried individually
- [ ] Failed actions can be discarded by user
- [ ] Queue cleared after successful sync
- [ ] Maximum queue size enforced (e.g., 100 actions)

### AC6: Error Handling
- [ ] Network errors don't clear the queue
- [ ] Validation errors (4xx) mark action as failed, not retried
- [ ] Server errors (5xx) trigger retry with backoff
- [ ] User notified of permanently failed actions

## Technical Notes

### Queue Structure

```typescript
interface PendingAction {
  id: string;           // UUID
  type: 'FIRE_INJECT' | 'SKIP_INJECT' | 'RESET_INJECT' |
        'CREATE_OBSERVATION' | 'UPDATE_OBSERVATION' | 'DELETE_OBSERVATION';
  exerciseId: string;
  payload: unknown;     // Type depends on action type
  timestamp: Date;      // When action was taken
  retryCount: number;   // Number of sync attempts
  status: 'pending' | 'syncing' | 'failed';
  error?: string;       // Error message if failed
}
```

### Action Payloads

```typescript
// Fire inject
{ injectId: string; firedAt: Date; }

// Skip inject
{ injectId: string; reason?: string; skippedAt: Date; }

// Create observation
{ observation: CreateObservationRequest; tempId: string; }

// Update observation
{ observationId: string; changes: UpdateObservationRequest; }

// Delete observation
{ observationId: string; }
```

### Optimistic UI Pattern

```typescript
const fireInjectOffline = async (injectId: string) => {
  // 1. Create pending action
  const action = createPendingAction('FIRE_INJECT', { injectId, firedAt: new Date() });
  await db.pendingActions.add(action);

  // 2. Optimistic update to cache
  await db.injects.update(injectId, {
    status: 'Delivered',
    _pendingSync: true  // Flag for UI
  });

  // 3. Update React Query cache
  queryClient.setQueryData(['injects', exerciseId], (old) =>
    old.map(i => i.id === injectId ? { ...i, status: 'Delivered', _pendingSync: true } : i)
  );
};
```

### Pending Indicator UI

```tsx
// In inject row
{inject._pendingSync && (
  <Tooltip title="Pending sync">
    <FontAwesomeIcon icon={faClock} className="pending-indicator" />
  </Tooltip>
)}
```

## Dependencies

- S02 (Offline Detection) - Need to know when to queue vs send directly
- S03 (Local Data Cache) - Queue uses same IndexedDB instance

## Out of Scope

- Queue processing logic (S05)
- Conflict resolution (S06)

## Test Scenarios

1. Go offline, fire inject, verify action added to queue
2. Go offline, fire inject, verify UI shows "Delivered" with pending indicator
3. Refresh page while offline, verify queued action persists
4. Go offline, queue 5 actions, verify all appear in pending list
5. Verify validation error (e.g., inject already fired) marks action as failed
6. Verify pending count updates in connection indicator
