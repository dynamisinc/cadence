# S02: Offline Detection & Indicators

## Story

**As a** user working in a location with unreliable connectivity
**I want to** clearly see when I'm offline
**So that** I understand why updates may not be appearing and know my changes will sync later

## Priority

P0 - MVP Required

## Acceptance Criteria

### AC1: Connection State Detection
- [x] System detects when SignalR connection is lost
- [x] System detects when browser goes offline (Navigator.onLine)
- [x] System detects when connection is restored
- [x] Connection state combines both SignalR and browser online status

### AC2: Visual Connection Indicator
- [x] Persistent indicator visible in application header
- [x] Connected state: Green dot or no indicator (normal operation)
- [x] Connecting/Reconnecting state: Yellow/orange indicator with "Connecting..." text
- [x] Disconnected state: Red indicator with "Offline" text
- [x] Indicator is non-intrusive but noticeable

### AC3: Toast Notifications
- [x] Toast notification appears when connection is lost
- [x] Toast message: "You are offline. Changes will sync when connection restores."
- [x] Toast notification appears when connection is restored
- [x] Toast message: "Connection restored. Syncing changes..."
- [x] Toast for successful sync: "All changes synced successfully"

### AC4: Graceful UI Degradation
- [x] UI remains functional when offline (no error screens)
- [x] Read operations use cached data when offline
- [x] Write operations queue locally when offline
- [x] Loading spinners don't appear indefinitely when offline

### AC5: Reconnection Handling
- [x] Automatic reconnection attempts with exponential backoff
- [x] Maximum reconnection delay capped at 30 seconds
- [x] Manual "Retry Connection" option available after multiple failures
- [x] Successful reconnection triggers data refresh

## Technical Notes

### Connection States

```typescript
type ConnectionState =
  | 'connected'      // SignalR connected, browser online
  | 'connecting'     // Initial connection in progress
  | 'reconnecting'   // Lost connection, attempting to reconnect
  | 'disconnected';  // No connection, not attempting to reconnect
```

### State Combination Logic

```
Browser Online + SignalR Connected = 'connected'
Browser Online + SignalR Connecting = 'connecting'
Browser Online + SignalR Reconnecting = 'reconnecting'
Browser Offline (any SignalR state) = 'disconnected'
SignalR Disconnected + not reconnecting = 'disconnected'
```

### UI Indicator Placement

```
┌─────────────────────────────────────────────────────────────────┐
│ Cadence                              🔴 Offline     [User Menu] │
└─────────────────────────────────────────────────────────────────┘
```

Or as a banner when disconnected:
```
┌─────────────────────────────────────────────────────────────────┐
│ ⚠️ You are offline. Changes will sync when connection restores. │
└─────────────────────────────────────────────────────────────────┘
```

### Existing Infrastructure

- `useSignalR` hook already tracks `connectionState`
- `useExerciseSignalR` exposes `connectionState`
- React Query can be configured for offline behavior

## Out of Scope

- Actual data caching (S03)
- Action queue (S04)
- Sync logic (S05)

## Test Scenarios

1. Open DevTools → Network → Offline, verify indicator changes to red
2. Restore network, verify indicator changes to green
3. Kill backend server, verify SignalR disconnection detected
4. Restart backend, verify automatic reconnection
5. Verify toast appears on disconnect and reconnect
6. Verify UI doesn't show error screens when offline
