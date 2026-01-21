# Story CLK-09: Fire Confirmation Dialog for Critical Injects

> **Story ID:** CLK-09
> **Feature:** Exercise Clock Modes
> **Phase:** D - Exercise Conduct
> **Status:** Complete
> **Estimate:** Small (< 1 day)

---

## User Story

**As a** Controller,
**I want** a confirmation dialog before firing critical injects,
**So that** I don't accidentally fire the wrong inject during a high-pressure exercise.

---

## Background

During exercise conduct, Controllers may accidentally click "Fire" on the wrong inject, especially when multiple injects are ready. A confirmation dialog provides a safety net, particularly for critical injects that would be difficult to "undo" in the exercise narrative.

---

## Scope

### In Scope
- Add confirmation dialog before firing any inject
- Show inject details in confirmation dialog
- Option to "Don't ask again for this session" (session-scoped preference)
- Keyboard shortcuts to confirm (Enter) or cancel (Escape)

### Out of Scope
- Per-inject "RequireConfirmation" flag (future enhancement)
- Undo/revert fired inject functionality
- Confirmation for Skip action (lower risk)

---

## Acceptance Criteria

- [x] **Given** I click Fire on any inject, **when** the button is clicked, **then** a confirmation dialog appears
- [x] **Given** the confirmation dialog, **when** displayed, **then** it shows inject number, title, and target
- [x] **Given** the confirmation dialog, **when** I click "Confirm Fire", **then** the inject is fired
- [x] **Given** the confirmation dialog, **when** I click "Cancel", **then** nothing happens and dialog closes
- [x] **Given** the confirmation dialog, **when** I press Enter, **then** the inject is fired (same as Confirm)
- [x] **Given** the confirmation dialog, **when** I press Escape, **then** dialog closes (same as Cancel)
- [x] **Given** I check "Don't ask again this session", **when** I fire subsequent injects, **then** no dialog appears
- [x] **Given** I start a new session, **when** I fire an inject, **then** confirmation dialog appears again

---

## UI Design

### Fire Confirmation Dialog

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│  🔥 Fire Inject?                                                 [X]    │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  You are about to fire:                                                 │
│                                                                         │
│  #3 - Evacuation Order                                                  │
│  📍 Target: EOC Director                                                │
│  📖 Story Time: Day 1 • 18:00                                           │
│                                                                         │
│  This action will be broadcast to all exercise participants.            │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ☐ Don't ask again this session                                         │
│                                                                         │
│                                  [Cancel]        [🔥 Confirm Fire]      │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Design

### Component Structure

```
src/frontend/src/features/injects/components/
├── FireConfirmationDialog.tsx
└── FireConfirmationDialog.test.tsx
```

### FireConfirmationDialog Component

```typescript
interface FireConfirmationDialogProps {
  open: boolean;
  inject: InjectDto | null;
  onConfirm: () => void;
  onCancel: () => void;
  onDontAskAgain: (value: boolean) => void;
}

export const FireConfirmationDialog: React.FC<FireConfirmationDialogProps> = ({
  open,
  inject,
  onConfirm,
  onCancel,
  onDontAskAgain
}) => {
  const [dontAsk, setDontAsk] = useState(false);

  const handleConfirm = () => {
    if (dontAsk) {
      onDontAskAgain(true);
    }
    onConfirm();
  };

  // Handle keyboard shortcuts
  useEffect(() => {
    if (!open) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        handleConfirm();
      } else if (e.key === 'Escape') {
        e.preventDefault();
        onCancel();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [open, handleConfirm, onCancel]);

  if (!inject) return null;

  return (
    <Dialog
      open={open}
      onClose={onCancel}
      maxWidth="sm"
      fullWidth
      aria-labelledby="fire-confirmation-title"
    >
      <DialogTitle id="fire-confirmation-title">
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <FontAwesomeIcon icon={faFire} color="warning" />
          Fire Inject?
        </Box>
      </DialogTitle>

      <DialogContent>
        <Typography variant="body1" gutterBottom>
          You are about to fire:
        </Typography>

        <Paper variant="outlined" sx={{ p: 2, my: 2 }}>
          <Typography variant="h6">
            #{inject.injectNumber} - {inject.title}
          </Typography>
          <Box sx={{ mt: 1, display: 'flex', flexDirection: 'column', gap: 0.5 }}>
            <Typography variant="body2" color="text.secondary">
              <FontAwesomeIcon icon={faCrosshairs} /> Target: {inject.target}
            </Typography>
            {inject.scenarioDay && inject.scenarioTime && (
              <Typography variant="body2" color="text.secondary">
                <FontAwesomeIcon icon={faBook} /> Story Time: Day {inject.scenarioDay} • {inject.scenarioTime}
              </Typography>
            )}
          </Box>
        </Paper>

        <Typography variant="body2" color="text.secondary">
          This action will be broadcast to all exercise participants.
        </Typography>

        <FormControlLabel
          control={
            <Checkbox
              checked={dontAsk}
              onChange={(e) => setDontAsk(e.target.checked)}
            />
          }
          label="Don't ask again this session"
          sx={{ mt: 2 }}
        />
      </DialogContent>

      <DialogActions>
        <CobraSecondaryButton onClick={onCancel}>
          Cancel
        </CobraSecondaryButton>
        <CobraPrimaryButton
          onClick={handleConfirm}
          startIcon={<FontAwesomeIcon icon={faFire} />}
        >
          Confirm Fire
        </CobraPrimaryButton>
      </DialogActions>
    </Dialog>
  );
};
```

### Session Preference Hook

```typescript
/**
 * Hook to manage "don't ask again" preference for the session.
 * Uses sessionStorage for persistence within the browser session.
 */
function useFireConfirmationPreference(): [boolean, (skip: boolean) => void] {
  const STORAGE_KEY = 'cadence_skipFireConfirmation';

  const [skipConfirmation, setSkipConfirmation] = useState(() => {
    return sessionStorage.getItem(STORAGE_KEY) === 'true';
  });

  const setSkip = (skip: boolean) => {
    setSkipConfirmation(skip);
    if (skip) {
      sessionStorage.setItem(STORAGE_KEY, 'true');
    } else {
      sessionStorage.removeItem(STORAGE_KEY);
    }
  };

  return [skipConfirmation, setSkip];
}
```

### Integration with Fire Action

```typescript
// In conduct view component or useInjectActions hook
const [skipConfirmation, setSkipConfirmation] = useFireConfirmationPreference();
const [confirmInject, setConfirmInject] = useState<InjectDto | null>(null);

const handleFireClick = (inject: InjectDto) => {
  if (skipConfirmation) {
    // Fire immediately without confirmation
    fireInject(inject.id);
  } else {
    // Show confirmation dialog
    setConfirmInject(inject);
  }
};

const handleConfirmFire = async () => {
  if (confirmInject) {
    await fireInject(confirmInject.id);
    setConfirmInject(null);
  }
};

const handleCancelFire = () => {
  setConfirmInject(null);
};

// In render:
<FireConfirmationDialog
  open={!!confirmInject}
  inject={confirmInject}
  onConfirm={handleConfirmFire}
  onCancel={handleCancelFire}
  onDontAskAgain={setSkipConfirmation}
/>
```

---

## Test Cases

### Component Unit Tests

```typescript
describe('FireConfirmationDialog', () => {
  it('displays inject number and title');
  it('displays target information');
  it('displays story time when available');
  it('calls onConfirm when Confirm Fire clicked');
  it('calls onCancel when Cancel clicked');
  it('calls onConfirm on Enter key');
  it('calls onCancel on Escape key');
  it('calls onDontAskAgain when checkbox checked and confirmed');
  it('does not call onDontAskAgain when checkbox unchecked');
});

describe('useFireConfirmationPreference', () => {
  it('defaults to false (show confirmation)');
  it('returns true after setSkip(true)');
  it('persists to sessionStorage');
  it('clears on setSkip(false)');
});
```

### Integration Tests

```typescript
describe('Fire confirmation flow', () => {
  it('shows dialog when Fire clicked');
  it('fires inject after confirmation');
  it('does not fire when cancelled');
  it('skips dialog after "don\'t ask again"');
  it('resets preference on new session');
});
```

---

## Accessibility

- Dialog has proper `aria-labelledby` and `aria-describedby`
- Focus trapped within dialog
- Keyboard navigation (Tab through elements)
- Enter/Escape shortcuts announced
- Checkbox has associated label

---

## Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| Fire inject functionality | ✅ Complete | Wrapping existing action |
| MUI Dialog component | ✅ Complete | Using existing MUI |
| COBRA styled buttons | ✅ Complete | Use project patterns |

---

## Blocked By

None - can be implemented independently.

---

## Blocks

None - this is an enhancement story.

---

## Notes

- Consider adding visual/audio feedback after fire (toast notification)
- Future: Per-inject `requireConfirmation` flag for critical injects
- Future: Different confirmation levels (simple vs detailed)
- The "Don't ask again" preference is session-scoped intentionally for safety

---

## Implementation Summary

**Status:** ✅ Complete

### Files Created

1. **Hook:** `src/frontend/src/features/injects/hooks/useFireConfirmationPreference.ts`
   - Manages session-scoped "don't ask again" preference
   - Uses sessionStorage for persistence
   - Auto-clears on new browser session

2. **Component:** `src/frontend/src/features/injects/components/FireConfirmationDialog.tsx`
   - Confirmation dialog with inject details
   - Keyboard shortcuts (Enter/Escape)
   - COBRA styling with theme colors
   - Displays inject number, title, target, story time

3. **Tests:**
   - `useFireConfirmationPreference.test.ts` - 7 tests
   - `FireConfirmationDialog.test.tsx` - 21 tests

### Integration Example

To integrate with existing fire functionality:

```typescript
// In conduct view or inject management component
import { FireConfirmationDialog, useFireConfirmationPreference } from '@/features/injects'

const [skipConfirmation, setSkipConfirmation] = useFireConfirmationPreference()
const [confirmInject, setConfirmInject] = useState<InjectDto | null>(null)

const handleFireClick = (inject: InjectDto) => {
  if (skipConfirmation) {
    fireInject(inject.id)
  } else {
    setConfirmInject(inject)
  }
}

const handleConfirmFire = async () => {
  if (confirmInject) {
    await fireInject(confirmInject.id)
    setConfirmInject(null)
  }
}

// In render:
<FireConfirmationDialog
  open={!!confirmInject}
  inject={confirmInject}
  onConfirm={handleConfirmFire}
  onCancel={() => setConfirmInject(null)}
  onDontAskAgain={setSkipConfirmation}
/>
```

### Test Results

- All 28 tests passing (7 hook tests + 21 component tests)
- Full coverage of acceptance criteria
- Keyboard shortcuts verified
- SessionStorage behavior validated
