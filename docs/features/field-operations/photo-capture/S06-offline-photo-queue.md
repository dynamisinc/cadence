# Story: Offline Photo Queue

## S06-offline-photo-queue

**As an** Evaluator working in an area with poor or no connectivity,
**I want** my captured photos to be saved locally and automatically uploaded when connectivity returns,
**So that** I never lose photo evidence due to network issues in the field.

### Context

Exercise venues frequently have unreliable connectivity — basements, parking structures, rural areas, large buildings with poor cell coverage. Evaluators and Controllers can't stop doing their job because the network is down. The existing Phase H offline sync architecture handles data operations (observations, inject status updates) through IndexedDB and a pending action queue. Photo capture must integrate with this same architecture, but photos present a unique challenge: they're much larger than data records (1-3 MB each vs. a few KB), requiring careful handling of storage limits, upload ordering, and progress visibility. The user should never have to think about whether they're online or offline — capture just works.

### Acceptance Criteria

- [ ] **Given** I am offline, **when** I capture a photo, **then** the compressed photo and thumbnail are stored in IndexedDB on my device
- [ ] **Given** I am offline, **when** I capture a photo and create/attach it to an observation, **then** both the photo blob and the observation data are queued as pending actions
- [ ] **Given** I have photos queued offline, **when** connectivity is restored, **then** queued photos begin uploading automatically without user intervention
- [ ] **Given** photos are uploading after reconnection, **when** I view the sync status indicator, **then** I see the number of pending photo uploads and upload progress (e.g., "Syncing 3 of 7 photos")
- [ ] **Given** a photo upload fails (network drops again mid-upload), **when** connectivity is restored again, **then** the failed upload resumes or retries from the beginning without creating duplicates
- [ ] **Given** I have photos queued offline, **when** I view those photos in the app, **then** I see the locally-stored version (not a "photo unavailable" placeholder)
- [ ] **Given** I have multiple photos queued, **when** they sync, **then** they upload in chronological order (oldest first) to preserve the exercise timeline
- [ ] **Given** my device storage is approaching capacity, **when** I try to capture another photo, **then** I see a warning with my current queue size and available storage before proceeding
- [ ] **Given** photos and observation data are both queued, **when** sync occurs, **then** observation data syncs first, then photos — so the observation record exists on the server before the photo blob references it
- [ ] **Given** I have synced photos, **when** the upload completes, **then** the local IndexedDB copy is retained as a cache (not immediately deleted) to enable offline viewing

### Out of Scope

- Background sync when the app is closed (requires service worker background sync API — consider for future)
- Peer-to-peer photo sharing between offline devices
- Automatic deletion of local photo cache based on age or storage pressure
- Photo upload over cellular vs WiFi preference settings
- Resume partial uploads (chunk-based upload) — retry full file on failure

### Dependencies

- S01-capture-photo (compression and thumbnail generation)
- Phase H: Offline sync architecture (IndexedDB, pending action queue, SignalR reconnection)
- Phase I: PWA (service worker context for potential background sync in future)

### Open Questions

- [ ] What is the IndexedDB storage limit we should target? (Recommendation: warn at 80% of available quota, block at 95%. Browser quotas vary — typically 50-100 MB minimum, often much more)
- [ ] Should photos sync over cellular data, or only on WiFi? (Recommendation: sync on any connection for simplicity. Users in the field often only have cellular.)
- [ ] Should there be a manual "Sync Now" button in addition to automatic sync? (Recommendation: yes — gives users confidence and control)
- [ ] After how long should locally cached photos be eligible for cleanup? (Recommendation: retain until exercise is archived or user explicitly clears cache)

### Domain Terms

| Term | Definition |
|------|------------|
| Photo Queue | The set of locally-stored photos that have been captured offline and are waiting to be uploaded to server storage |
| Pending Action | A data operation (observation save, photo upload, etc.) that was performed offline and is queued for server sync |
| Sync Status Indicator | A UI element showing the current state of offline-to-server synchronization, including pending photo upload count |

### UI/UX Notes

```
Sync status indicator (app header or status bar):

Online, all synced:
┌──────────────────────────────────┐
│ 🟢 Connected                     │
└──────────────────────────────────┘

Offline with queued items:
┌──────────────────────────────────┐
│ 🔴 Offline · 4 photos queued     │
└──────────────────────────────────┘

Syncing after reconnection:
┌──────────────────────────────────┐
│ 🟡 Syncing · 3 of 7 photos ████░░│
└──────────────────────────────────┘

Storage warning:
┌──────────────────────────────────┐
│ ⚠️ Device storage nearly full     │
│ 47 photos queued (142 MB)        │
│ [Sync Now]  [Continue Anyway]    │
└──────────────────────────────────┘
```

- Sync status should be visible but not intrusive — a small indicator in the app header
- During active sync, show a progress bar or fraction count
- Storage warning should be a modal that requires acknowledgment, not just a toast
- Queued photos should show a small sync indicator on their thumbnails (e.g., a cloud with an upload arrow)
- After successful sync, briefly show a "✓ All photos synced" confirmation

### Technical Notes

- Extend existing Phase H `pendingActionQueue` to support blob data alongside JSON actions
- Photo blobs stored in IndexedDB via Dexie.js: `db.photos.put({ id, exerciseId, blob, thumbnail, metadata, syncStatus })`
- Sync status enum: `Pending | Uploading | Synced | Failed`
- Upload endpoint: `POST /api/exercises/{id}/photos` with multipart/form-data
- Server responds with the permanent photo URL (Blob Storage) which replaces the local blob reference
- Implement retry with exponential backoff: 1s, 2s, 4s, 8s, max 30s between retries
- Monitor `navigator.storage.estimate()` for quota usage and warn proactively
- Sync order: data records first (observations), then blobs (photos), to ensure referential integrity
