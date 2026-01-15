# S05: Sync on Reconnect

## Story

**As a** user who performed actions while offline
**I want to** have my changes automatically sync when connection restores
**So that** my work is saved and visible to other users

## Priority

P0 - MVP Required

## Acceptance Criteria

### AC1: Automatic Sync Trigger
- [x] Sync automatically starts when connection restored
- [x] Sync triggered by SignalR reconnection event
- [x] Sync triggered by browser online event
- [x] Brief delay (1-2 seconds) before sync to ensure stable connection

### AC2: Queue Processing
- [x] Pending actions processed in FIFO order
- [x] Each action sent to server via appropriate API endpoint
- [x] Successful actions removed from queue
- [x] Failed actions remain in queue with error status

### AC3: Data Refresh
- [x] Fresh data fetched from server after queue processed
- [x] Local cache updated with server data
- [x] Optimistic updates replaced with confirmed data
- [x] Pending sync flags cleared on success

### AC4: Progress Indication
- [x] Sync progress shown during processing
- [x] Example: "Syncing... (2/5)"
- [x] Spinner or progress indicator visible
- [x] Completion toast: "All changes synced successfully"

### AC5: Partial Success Handling
- [x] If some actions succeed and others fail, successes are committed
- [x] Failed actions remain in queue for retry
- [x] User notified of partial sync: "3 of 5 changes synced. 2 failed."
- [x] Failed items highlighted for user attention

### AC6: Retry Logic
- [x] Failed actions can be retried manually
- [x] Automatic retry with exponential backoff for server errors
- [x] Maximum retry attempts before marking permanently failed
- [x] User can discard failed actions

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
