# S06: Conflict Resolution

## Story

**As a** user who made changes while offline
**I want to** be informed when my changes conflict with others' changes
**So that** I understand what happened and no data is silently lost

## Priority

P1 - Important but not blocking MVP

## Acceptance Criteria

### AC1: Conflict Detection
- [x] System detects when offline action conflicts with server state
- [x] Conflict detected by comparing timestamps or checking preconditions
- [x] Conflict types identified: already-fired, already-deleted, concurrent-edit

### AC2: Inject Conflict Handling
- [x] If inject already fired by someone else, user's fire action is discarded
- [x] User notified: "Inject #4 was already fired by [Name] at [Time]"
- [x] If inject already skipped, user's action is discarded with notification
- [x] First-write-wins policy for inject status changes

### AC3: Observation Conflict Handling
- [x] If observation deleted by someone else, user's edit is discarded
- [x] User notified: "This observation was deleted while you were offline"
- [x] If observation edited by someone else, last-write-wins applies
- [x] User notified: "Your changes were applied over [Name]'s edits"

### AC4: Conflict Notification UI
- [x] Conflict dialog/modal shown when conflicts detected during sync
- [x] Dialog lists all conflicts with clear explanations
- [x] User must acknowledge conflicts before continuing
- [x] Option to view conflict details

### AC5: Conflict Log (Nice to Have)
- [ ] Conflicts logged to IndexedDB for review
- [ ] User can view recent conflicts in settings/profile
- [ ] Conflict log shows: action attempted, reason for conflict, resolution
- [ ] Log auto-prunes after 30 days

### AC6: No Silent Data Loss
- [x] User ALWAYS notified when their action is discarded
- [x] Notification includes enough context to understand what happened
- [x] User can take follow-up action if needed (e.g., re-fire different inject)

## Technical Notes

### Conflict Scenarios & Resolution

| Scenario | Detection | Resolution |
|----------|-----------|------------|
| Inject fired by two users | Server returns 409 or "already fired" | First write wins, notify second user |
| Inject: one fired, one skipped | Server returns conflict error | First write wins, notify second user |
| Observation edited by two users | Compare `updatedAt` timestamps | Last write wins, notify both users |
| Observation deleted while editing | Server returns 404 | Discard edit, notify user |
| Inject edited while offline | Compare `updatedAt` timestamps | Last write wins |

### Conflict Response from Server

```typescript
// API returns 409 Conflict with details
interface ConflictResponse {
  code: 'INJECT_ALREADY_FIRED' | 'INJECT_ALREADY_SKIPPED' |
        'OBSERVATION_DELETED' | 'VERSION_CONFLICT';
  message: string;
  conflictingUser?: {
    id: string;
    name: string;
  };
  conflictingTimestamp?: Date;
  currentState?: unknown;  // Current server state
}
```

### Conflict Notification UI

```tsx
// Conflict Dialog
<Dialog open={hasConflicts}>
  <DialogTitle>
    <FontAwesomeIcon icon={faExclamationTriangle} /> Sync Conflicts
  </DialogTitle>
  <DialogContent>
    <Typography>
      Some of your offline changes couldn't be applied:
    </Typography>
    <List>
      {conflicts.map(conflict => (
        <ListItem key={conflict.id}>
          <ListItemIcon>
            <FontAwesomeIcon icon={faTimesCircle} color="error" />
          </ListItemIcon>
          <ListItemText
            primary={conflict.actionDescription}
            secondary={conflict.reason}
          />
        </ListItem>
      ))}
    </List>
  </DialogContent>
  <DialogActions>
    <CobraPrimaryButton onClick={acknowledgeConflicts}>
      OK, I understand
    </CobraPrimaryButton>
  </DialogActions>
</Dialog>
```

### Conflict Messages

| Conflict Type | User Message |
|---------------|--------------|
| Inject already fired | "Inject #{number} '{title}' was already fired by {name} at {time}. Your fire action was discarded." |
| Inject already skipped | "Inject #{number} '{title}' was already skipped by {name}. Your action was discarded." |
| Observation deleted | "The observation you edited was deleted by {name} while you were offline." |
| Observation overwritten | "Your changes to observation were saved. Note: {name} also edited this observation." |

### Backend Support Needed

Controllers should return 409 Conflict with structured response:

```csharp
[HttpPost("{injectId}/fire")]
public async Task<IActionResult> FireInject(Guid exerciseId, Guid injectId, ...)
{
    var inject = await _injectService.GetByIdAsync(injectId);

    if (inject.Status == InjectStatus.Delivered)
    {
        return Conflict(new {
            code = "INJECT_ALREADY_FIRED",
            message = "This inject has already been fired",
            conflictingUser = new { id = inject.FiredById, name = inject.FiredByName },
            conflictingTimestamp = inject.FiredAt
        });
    }

    // ... proceed with fire
}
```

## Dependencies

- S05 (Sync on Reconnect) - Conflicts detected during sync process

## Out of Scope

- Complex merge resolution (showing diff of changes)
- User choice for resolution (always automatic per rules)
- Real-time conflict prevention (locking)

## Test Scenarios

1. User A fires inject, User B goes offline and tries to fire same inject, B reconnects → conflict shown
2. User A deletes observation, User B edits same observation offline, B reconnects → conflict shown
3. User A and B both edit same observation offline, both reconnect → last-write-wins, both notified
4. Multiple conflicts in one sync → all shown in single dialog
5. User acknowledges conflicts, verify they're removed from pending
6. Verify conflict log populated (if implemented)
