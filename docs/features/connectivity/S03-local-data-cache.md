# S03: Local Data Cache

## Story

**As a** user who may lose connectivity during an exercise
**I want to** have exercise data cached locally
**So that** I can continue viewing and working with the data while offline

## Priority

P0 - MVP Required

## Acceptance Criteria

### AC1: IndexedDB Setup
- [ ] IndexedDB database created on application load
- [ ] Database schema includes stores for: exercises, phases, injects, observations
- [ ] Database handles versioning for schema migrations
- [ ] Database errors are caught and logged gracefully

### AC2: Exercise Data Caching
- [ ] Current exercise data cached when Conduct page loads
- [ ] Exercise phases cached with exercise
- [ ] Cache updated when real-time events received
- [ ] Cache keyed by exercise ID for multi-exercise support

### AC3: Inject Data Caching
- [ ] All injects for current exercise cached on load
- [ ] Cache updated when inject status changes (fire/skip/reset)
- [ ] Cache includes all inject fields needed for display
- [ ] Cache indexed by exerciseId for efficient queries

### AC4: Observation Data Caching
- [ ] All observations for current exercise cached on load
- [ ] Cache updated when observations added/updated/deleted
- [ ] Cache includes all observation fields needed for display
- [ ] Cache indexed by exerciseId for efficient queries

### AC5: Offline Data Serving
- [ ] When offline, data served from IndexedDB cache
- [ ] Cached data used immediately, then updated from server when online
- [ ] Stale data indicator shown when serving cached data offline
- [ ] Cache timestamp tracked for freshness indication

### AC6: Cache Management
- [ ] Cache cleared on user logout
- [ ] Cache invalidated when newer data received from server
- [ ] Old exercise data pruned after configurable period (7 days)
- [ ] Cache size monitored to prevent storage issues

## Technical Notes

### IndexedDB Schema (Dexie.js)

```typescript
const db = new Dexie('CadenceDB');

db.version(1).stores({
  exercises: 'id, updatedAt',
  phases: 'id, exerciseId, updatedAt',
  injects: 'id, exerciseId, mselId, status, updatedAt',
  observations: 'id, exerciseId, injectId, updatedAt',
  syncMetadata: 'key'  // For tracking last sync times
});
```

### Cache Strategy

1. **Read-through cache**: Check cache first, then fetch from server
2. **Write-through cache**: Write to server first, then update cache
3. **Cache invalidation**: Server data always wins (updatedAt comparison)

### Stale Data Indicator

When offline and serving cached data:
```
┌─────────────────────────────────────────────────────────────────┐
│ 📋 Inject List                           ⏱️ Last updated: 5m ago │
└─────────────────────────────────────────────────────────────────┘
```

### Storage Limits

- IndexedDB typically allows 50MB+ per origin
- Monitor usage with `navigator.storage.estimate()`
- Warn user if approaching storage limits

## Dependencies

- S02 (Offline Detection) - Need to know when to serve cached data

## Out of Scope

- Offline write operations (S04)
- Sync on reconnect (S05)
- Conflict resolution (S06)

## Test Scenarios

1. Load Conduct page, verify data written to IndexedDB
2. Go offline, refresh page, verify data loads from cache
3. Fire inject while online, verify cache updated
4. Receive SignalR event, verify cache updated
5. Logout, verify cache cleared
6. Load different exercise, verify previous exercise data still cached
