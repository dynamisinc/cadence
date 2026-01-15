# S05: Sync on Reconnect

## Story

**As a** user who performed actions while offline
**I want to** have my changes automatically sync when connection restores
**So that** my work is saved and visible to other users

## Priority

P0 - MVP Required

## Acceptance Criteria

### AC1: Automatic Sync Trigger
- [ ] Sync automatically starts when connection restored
- [ ] Sync triggered by SignalR reconnection event
- [ ] Sync triggered by browser online event
- [ ] Brief delay (1-2 seconds) before sync to ensure stable connection

### AC2: Queue Processing
- [ ] Pending actions processed in FIFO order
- [ ] Each action sent to server via appropriate API endpoint
- [ ] Successful actions removed from queue
- [ ] Failed actions remain in queue with error status

### AC3: Data Refresh
- [ ] Fresh data fetched from server after queue processed
- [ ] Local cache updated with server data
- [ ] Optimistic updates replaced with confirmed data
- [ ] Pending sync flags cleared on success

### AC4: Progress Indication
- [ ] Sync progress shown during processing
- [ ] Example: "Syncing... (2/5)"
- [ ] Spinner or progress indicator visible
- [ ] Completion toast: "All changes synced successfully"

### AC5: Partial Success Handling
- [ ] If some actions succeed and others fail, successes are committed
- [ ] Failed actions remain in queue for retry
- [ ] User notified of partial sync: "3 of 5 changes synced. 2 failed."
- [ ] Failed items highlighted for user attention

### AC6: Retry Logic
- [ ] Failed actions can be retried manually
- [ ] Automatic retry with exponential backoff for server errors
- [ ] Maximum retry attempts before marking permanently failed
- [ ] User can discard failed actions

## Technical Notes

### Sync Flow

```
Connection Restored
       ↓
Wait 1-2 seconds (debounce)
       ↓
Fetch latest server data
       ↓
Process pending action queue (FIFO)
       ↓
For each action:
   ┌───────────────────┐
   │ Send to server    │
   └─────────┬─────────┘
             │
   ┌─────────┴─────────┐
   │                   │
Success             Failure
   │                   │
   ↓                   ↓
Remove from       Check error type
queue                  │
   │         ┌─────────┴─────────┐
   │         │                   │
   │     4xx (client)       5xx (server)
   │         │                   │
   │         ↓                   ↓
   │    Mark failed          Retry with
   │    (user error)         backoff
   │
   ↓
Update local cache
       ↓
Clear pending flags
       ↓
Show completion toast
```

### Sync Service

```typescript
interface SyncService {
  // Start sync process
  sync(): Promise<SyncResult>;

  // Get current sync status
  getStatus(): SyncStatus;

  // Retry specific failed action
  retryAction(actionId: string): Promise<boolean>;

  // Discard failed action
  discardAction(actionId: string): Promise<void>;

  // Cancel ongoing sync
  cancel(): void;
}

interface SyncResult {
  totalActions: number;
  succeeded: number;
  failed: number;
  failedActions: PendingAction[];
}

type SyncStatus = 'idle' | 'syncing' | 'completed' | 'partial' | 'failed';
```

### API Endpoint Mapping

```typescript
const actionEndpoints = {
  FIRE_INJECT: (a) => api.post(`/exercises/${a.exerciseId}/injects/${a.payload.injectId}/fire`),
  SKIP_INJECT: (a) => api.post(`/exercises/${a.exerciseId}/injects/${a.payload.injectId}/skip`),
  RESET_INJECT: (a) => api.post(`/exercises/${a.exerciseId}/injects/${a.payload.injectId}/reset`),
  CREATE_OBSERVATION: (a) => api.post(`/exercises/${a.exerciseId}/observations`, a.payload.observation),
  UPDATE_OBSERVATION: (a) => api.put(`/observations/${a.payload.observationId}`, a.payload.changes),
  DELETE_OBSERVATION: (a) => api.delete(`/observations/${a.payload.observationId}`),
};
```

### Progress UI

```tsx
// During sync
<Box className="sync-progress">
  <CircularProgress size={16} />
  <Typography variant="body2">
    Syncing... ({current}/{total})
  </Typography>
</Box>

// Partial failure
<Alert severity="warning">
  3 of 5 changes synced. <Link onClick={showFailedActions}>2 failed</Link>
</Alert>
```

## Dependencies

- S02 (Offline Detection) - Reconnection event triggers sync
- S04 (Offline Action Queue) - Queue to process

## Out of Scope

- Conflict resolution logic (S06) - handled separately
- This story covers basic sync; conflicts are detected but resolved in S06

## Test Scenarios

1. Queue 3 actions offline, reconnect, verify all sync successfully
2. Queue actions, reconnect, verify FIFO order maintained
3. Queue action that will fail (e.g., inject already fired), verify error handling
4. Queue 5 actions, have 2 fail, verify partial success messaging
5. Verify progress indicator shows during sync
6. Verify "All changes synced" toast on complete success
7. Disconnect during sync, verify queue preserved for retry
