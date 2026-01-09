# Story: S03 Auto-save

> **Status**: 📋 Ready for Development  
> **Priority**: P0 (Critical)  
> **Epic**: E4 - MSEL Authoring  
> **Sprint Points**: 5

## User Story

**As a** user editing exercise data,  
**I want** my changes to be automatically saved as I work,  
**So that** I don't lose work due to browser crashes, accidental navigation, or forgetting to save.

## Context

The EXIS analysis identified data loss from unsaved work as a critical pain point. Users working on large MSELs for extended periods expect modern auto-save behavior. Combined with offline capability, auto-save ensures no work is ever lost.

### User Impact

| Scenario | Without Auto-save | With Auto-save |
|----------|-------------------|----------------|
| Browser crash | All unsaved work lost | < 30 seconds of work lost |
| Accidental navigation | Entire form lost | Changes preserved |
| Tab closure | Work lost if not saved | Auto-saved content recoverable |
| Network interruption | Save fails, user must retry | Queued for offline sync |

## Acceptance Criteria

### Trigger Conditions

- [ ] **Given** I am editing any form field, **when** I move focus away from the field (blur), **then** changes are auto-saved within 2 seconds

- [ ] **Given** I am editing a form, **when** 30 seconds pass with unsaved changes, **then** changes are auto-saved automatically

- [ ] **Given** I am editing a form, **when** I press `Ctrl+S` / `Cmd+S`, **then** changes are saved immediately

- [ ] **Given** I have unsaved changes, **when** I attempt to navigate away or close the tab, **then** I see a confirmation dialog "You have unsaved changes. Leave anyway?"

### Save Indicators

- [ ] **Given** auto-save is triggered, **when** save completes successfully, **then** I see a subtle indicator "Saved" or ✓ with timestamp

- [ ] **Given** auto-save is in progress, **when** waiting for server response, **then** I see "Saving..." indicator

- [ ] **Given** auto-save fails, **when** the failure is network-related, **then** I see "Saved locally, will sync when online"

- [ ] **Given** auto-save fails, **when** the failure is a validation error, **then** I see the specific error and field is highlighted

### Offline Behavior

- [ ] **Given** I am offline, **when** auto-save triggers, **then** changes are saved to local storage (IndexedDB)

- [ ] **Given** I have locally saved changes, **when** I come back online, **then** changes are synced to the server automatically

- [ ] **Given** multiple offline changes exist, **when** syncing, **then** changes are applied in chronological order

### Conflict Handling

- [ ] **Given** another user modified the same record, **when** my auto-save attempts to save, **then** I see a conflict notification with options

- [ ] **Given** a conflict notification, **when** I choose "Keep mine", **then** my changes overwrite the server version

- [ ] **Given** a conflict notification, **when** I choose "Keep theirs", **then** my changes are discarded and form reloads

- [ ] **Given** a conflict notification, **when** I choose "Review", **then** I see a diff view of both versions

### Recovery

- [ ] **Given** I closed a form without saving, **when** I return to the same form within 24 hours, **then** I am prompted "Restore unsaved changes?"

- [ ] **Given** a recovery prompt, **when** I click "Restore", **then** my previous unsaved changes are loaded into the form

- [ ] **Given** a recovery prompt, **when** I click "Discard", **then** the form loads with server data and local draft is deleted

## Out of Scope

- Real-time collaborative editing (Google Docs style) - Standard phase
- Version history / undo stack - Standard phase
- Auto-save configuration per user - not planned
- Merge conflict resolution (3-way merge) - not planned

## Dependencies

- Offline storage infrastructure (IndexedDB)
- Sync queue management
- API support for optimistic updates

## Open Questions

- [ ] Should auto-save interval be configurable? Recommend: No, fixed at 30 seconds
- [ ] Should there be a "Disable auto-save" option? Recommend: No, always enabled
- [ ] How long to retain recovery drafts? Recommend: 24 hours, then purge

## Domain Terms

| Term | Definition |
|------|------------|
| Auto-save | Automatic preservation of form data without explicit user action |
| Draft | Unsaved or locally-saved version of a record |
| Optimistic update | UI updates immediately, server sync happens in background |
| Conflict | Two versions of same record modified independently |

## UI/UX Notes

### Save Status Indicator

Position in top-right of form, subtle but visible:

```
Editing Inject #15
                                              [Saved at 2:34 PM ✓]

┌─────────────────────────────────────────────────────────────┐
│ Title: Emergency Broadcast Alert                            │
│ ...                                                         │
└─────────────────────────────────────────────────────────────┘
```

Status states:
- `✓ Saved at 2:34 PM` - Green text, normal state
- `○ Saving...` - Gray text with spinner
- `⚠ Saved locally` - Orange text (offline)
- `✗ Error saving` - Red text with retry option

### Navigation Warning

```
┌─────────────────────────────────────────────────────────────┐
│              Unsaved Changes                                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  You have changes that haven't been saved to the server.    │
│                                                             │
│  They will be saved locally and synced when possible.       │
│                                                             │
│           ┌──────────────┐  ┌────────────────┐             │
│           │ Stay on Page │  │ Leave Anyway   │             │
│           └──────────────┘  └────────────────┘             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Recovery Prompt

```
┌─────────────────────────────────────────────────────────────┐
│              Unsaved Draft Found                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  You have an unsaved draft of this inject from              │
│  January 8, 2025 at 2:30 PM.                                │
│                                                             │
│  Would you like to restore it?                              │
│                                                             │
│           ┌──────────────┐  ┌────────────────┐             │
│           │   Restore    │  │    Discard     │             │
│           └──────────────┘  └────────────────┘             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Conflict Resolution

```
┌─────────────────────────────────────────────────────────────┐
│              Save Conflict                                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  This record was modified by Jane Smith while you were      │
│  editing. What would you like to do?                        │
│                                                             │
│  Your version:    "Emergency Broadcast Alert"               │
│  Their version:   "Emergency Alert Notification"            │
│                                                             │
│    ┌─────────────┐  ┌─────────────┐  ┌────────────┐        │
│    │  Keep Mine  │  │ Keep Theirs │  │   Review   │        │
│    └─────────────┘  └─────────────┘  └────────────┘        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Technical Notes

### Auto-save Architecture

```typescript
// Hook for auto-save behavior
function useAutoSave<T>(
  data: T,
  saveFunction: (data: T) => Promise<void>,
  options?: { interval?: number; onError?: (error: Error) => void }
) {
  const [status, setStatus] = useState<'idle' | 'saving' | 'saved' | 'error'>('idle');
  const [lastSaved, setLastSaved] = useState<Date | null>(null);
  const dirtyRef = useRef(false);
  const timerRef = useRef<NodeJS.Timeout>();

  // Save function with debounce
  const save = useCallback(async () => {
    if (!dirtyRef.current) return;
    
    setStatus('saving');
    try {
      await saveFunction(data);
      setStatus('saved');
      setLastSaved(new Date());
      dirtyRef.current = false;
    } catch (error) {
      setStatus('error');
      options?.onError?.(error);
    }
  }, [data, saveFunction, options]);

  // Blur handler
  const onBlur = useCallback(() => {
    save();
  }, [save]);

  // Interval auto-save
  useEffect(() => {
    timerRef.current = setInterval(() => {
      if (dirtyRef.current) save();
    }, options?.interval ?? 30000);
    
    return () => clearInterval(timerRef.current);
  }, [save, options?.interval]);

  return { status, lastSaved, onBlur, markDirty: () => dirtyRef.current = true };
}
```

### Offline Storage Schema

```typescript
// IndexedDB schema for drafts
interface DraftRecord {
  id: string;              // Record ID being edited
  entityType: string;      // 'inject', 'exercise', 'objective'
  data: unknown;           // Form data
  savedAt: Date;           // When draft was saved
  serverVersion: number;   // Last known server version (for conflict detection)
}

// Dexie.js example
class CadenceDB extends Dexie {
  drafts: Dexie.Table<DraftRecord, string>;
  
  constructor() {
    super('CadenceDB');
    this.version(1).stores({
      drafts: 'id, entityType, savedAt'
    });
  }
}
```

### Conflict Detection

```typescript
// Include version in all entities
interface VersionedEntity {
  id: string;
  version: number;  // Incremented on each save
  modifiedAt: Date;
  modifiedBy: string;
}

// API returns 409 Conflict if version mismatch
// Response includes current server version for comparison
```

### Navigation Guard

```typescript
// React Router navigation guard
function useNavigationGuard(isDirty: boolean) {
  const blocker = useBlocker(isDirty);
  
  useEffect(() => {
    if (blocker.state === 'blocked') {
      // Show confirmation modal
      if (window.confirm('You have unsaved changes. Leave anyway?')) {
        blocker.proceed();
      } else {
        blocker.reset();
      }
    }
  }, [blocker]);
}

// Also handle browser close/refresh
useEffect(() => {
  const handler = (e: BeforeUnloadEvent) => {
    if (isDirty) {
      e.preventDefault();
      e.returnValue = '';
    }
  };
  window.addEventListener('beforeunload', handler);
  return () => window.removeEventListener('beforeunload', handler);
}, [isDirty]);
```

---

## INVEST Checklist

- [x] **I**ndependent - Can be implemented with any form component
- [x] **N**egotiable - Timing and UI details flexible
- [x] **V**aluable - Prevents data loss, major UX improvement
- [x] **E**stimable - Clear scope, ~5 points
- [x] **S**mall - Focused on save behavior only
- [x] **T**estable - Each trigger condition testable

## Test Scenarios

### Unit Tests
- Debounce timing logic
- Dirty flag management
- Version conflict detection

### Integration Tests
- Save on blur triggers API call
- Interval save with dirty data
- Offline queue persistence

### E2E Tests
- Edit → blur → verify saved
- Edit → wait 30s → verify saved
- Edit → offline → online → verify synced
- Edit → close tab → reopen → verify recovery prompt

---

*Related Stories*: [S01 Session Management](./S01-session-management.md), [inject-crud/S02 Edit Inject](../inject-crud/S02-edit-inject.md)

*Last updated: 2025-01-08*
