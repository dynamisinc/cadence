# Story: S02 Keyboard Navigation

> **Status**: 📋 Ready for Development  
> **Priority**: P1 (Important)  
> **Epic**: E4 - MSEL Authoring  
> **Sprint Points**: 5

## User Story

**As a** Controller or power user,  
**I want** to navigate and perform common actions using keyboard shortcuts,  
**So that** I can work efficiently during time-sensitive exercise conduct without excessive mouse clicking.

## Context

The EXIS analysis identified excessive clicking as a major UX pain point. Controllers during live exercises need to fire injects quickly and navigate between injects efficiently. Keyboard shortcuts are essential for power users and accessibility compliance.

### User Impact

| User Type | Benefit |
|-----------|---------|
| Controllers | Rapid inject firing during conduct |
| Power users | Faster MSEL authoring workflow |
| Accessibility users | Screen reader and keyboard-only navigation |
| All users | Reduced repetitive strain |

## Acceptance Criteria

### Global Shortcuts

- [ ] **Given** I am anywhere in the application, **when** I press `?`, **then** I see a keyboard shortcuts help modal

- [ ] **Given** I am anywhere in the application, **when** I press `Esc`, **then** any open modal or dropdown closes

- [ ] **Given** I am in a text input field, **when** I press shortcut keys, **then** shortcuts are disabled (type normally)

### Navigation Shortcuts

- [ ] **Given** I am on any page, **when** I press `g` then `h`, **then** I navigate to the home/dashboard

- [ ] **Given** I am viewing an exercise, **when** I press `g` then `i`, **then** I navigate to the inject list

- [ ] **Given** I am viewing an exercise, **when** I press `g` then `o`, **then** I navigate to objectives

### Inject List Shortcuts

- [ ] **Given** I am viewing the inject list, **when** I press `j`, **then** focus moves to the next inject in the list

- [ ] **Given** I am viewing the inject list, **when** I press `k`, **then** focus moves to the previous inject in the list

- [ ] **Given** an inject is focused, **when** I press `Enter`, **then** the inject detail view opens

- [ ] **Given** I am viewing the inject list, **when** I press `n`, **then** the create new inject form opens

- [ ] **Given** an inject is focused, **when** I press `e`, **then** the inject opens in edit mode

- [ ] **Given** an inject is focused, **when** I press `Delete`, **then** a delete confirmation modal appears

### Conduct Shortcuts (During Active Exercise)

- [ ] **Given** an inject is focused during conduct, **when** I press `f`, **then** the inject is fired (with confirmation if configured)

- [ ] **Given** an inject is focused during conduct, **when** I press `s`, **then** the skip inject dialog opens

- [ ] **Given** a fired inject confirmation appears, **when** I press `Enter`, **then** the inject is confirmed fired

### Form Shortcuts

- [ ] **Given** I am editing a form, **when** I press `Ctrl+S` (or `Cmd+S` on Mac), **then** the form saves

- [ ] **Given** I am editing a form, **when** I press `Ctrl+Enter`, **then** the form saves and closes

- [ ] **Given** I am in a form, **when** I press `Tab`, **then** focus moves to the next field

- [ ] **Given** I am in a form, **when** I press `Shift+Tab`, **then** focus moves to the previous field

### Search and Filter

- [ ] **Given** I am on a page with search, **when** I press `/`, **then** focus moves to the search input

- [ ] **Given** I am in search results, **when** I press `Esc`, **then** search is cleared and focus returns to list

### Accessibility Requirements

- [ ] **Given** any focusable element, **when** it receives focus, **then** there is a visible focus indicator

- [ ] **Given** keyboard navigation is used, **when** focus moves, **then** the focused element scrolls into view

- [ ] **Given** a screen reader is active, **when** shortcuts are used, **then** appropriate announcements are made

## Out of Scope

- Customizable keyboard shortcuts (Standard phase)
- Vim-style modal commands (not planned)
- Gamepad/controller input (not planned)
- Voice commands (not planned)

## Dependencies

- Component library focus management
- ARIA implementation across components
- Global keyboard event handling setup

## Open Questions

- [ ] Should there be a "vim mode" toggle for j/k navigation everywhere?
- [ ] Should `Ctrl+S` work in offline mode (queue save)?
- [ ] Should shortcuts require modifier keys to avoid accidental triggers?

## Domain Terms

| Term | Definition |
|------|------------|
| Focus | The currently active element that receives keyboard input |
| Focus trap | Modal behavior that keeps focus within the modal |
| Skip link | Hidden link to skip to main content (accessibility) |
| Shortcut chord | Two-key sequence like `g` then `h` |

## UI/UX Notes

### Keyboard Shortcuts Modal

```
┌─────────────────────────────────────────────────────────────┐
│                  Keyboard Shortcuts                    [×]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  NAVIGATION                      INJECT LIST                │
│  ──────────                      ───────────                │
│  g h    Go to home               j / k    Next / Previous   │
│  g i    Go to injects            Enter    Open detail       │
│  g o    Go to objectives         n        New inject        │
│  ?      Show this help           e        Edit inject       │
│  Esc    Close modal              Delete   Delete inject     │
│                                                             │
│  CONDUCT                         FORMS                      │
│  ───────                         ─────                      │
│  f      Fire inject              Ctrl+S   Save              │
│  s      Skip inject              Ctrl+↵   Save and close    │
│                                  Tab      Next field        │
│  SEARCH                          Shift+Tab Previous field   │
│  ──────                                                     │
│  /      Focus search                                        │
│  Esc    Clear search                                        │
│                                                             │
│                          [Got it]                           │
└─────────────────────────────────────────────────────────────┘
```

### Focus Indicators

- Use visible outline (not just color change)
- High contrast (minimum 3:1 ratio)
- Consistent style across all components

### Shortcut Hints

Show keyboard hints in tooltips and buttons:
```
[Fire Inject (F)]  [Skip (S)]  [Edit (E)]
```

## Technical Notes

### Global Keyboard Handler

```typescript
// Use a library like react-hotkeys-hook or custom implementation
import { useHotkeys } from 'react-hotkeys-hook';

// Global shortcuts
useHotkeys('?', () => setShowShortcutsModal(true));
useHotkeys('esc', () => closeActiveModal());

// Context-aware shortcuts (only when not in input)
useHotkeys('n', () => openNewInjectForm(), {
  enableOnFormTags: false
});
```

### Two-Key Sequences

```typescript
// Implement chord detection
const [pendingKey, setPendingKey] = useState<string | null>(null);

useHotkeys('g', () => {
  setPendingKey('g');
  setTimeout(() => setPendingKey(null), 1000); // Timeout after 1s
});

useHotkeys('h', () => {
  if (pendingKey === 'g') {
    navigate('/home');
    setPendingKey(null);
  }
});
```

### Focus Management

```typescript
// Custom hook for list navigation
function useListNavigation<T>(items: T[], onSelect: (item: T) => void) {
  const [focusIndex, setFocusIndex] = useState(0);
  
  useHotkeys('j', () => setFocusIndex(i => Math.min(i + 1, items.length - 1)));
  useHotkeys('k', () => setFocusIndex(i => Math.max(i - 1, 0)));
  useHotkeys('enter', () => onSelect(items[focusIndex]));
  
  return { focusIndex, setFocusIndex };
}
```

### Platform Detection

```typescript
// Show correct modifier key
const isMac = navigator.platform.includes('Mac');
const modifierKey = isMac ? '⌘' : 'Ctrl';
// Display: "⌘S" on Mac, "Ctrl+S" on Windows/Linux
```

---

## INVEST Checklist

- [x] **I**ndependent - Can be implemented with existing UI components
- [x] **N**egotiable - Specific shortcuts can be adjusted based on user feedback
- [x] **V**aluable - Significant efficiency gain for power users
- [x] **E**stimable - Well-defined scope, ~5 points
- [x] **S**mall - Focused on keyboard interaction only
- [x] **T**estable - Each shortcut has clear expected behavior

## Test Scenarios

### Unit Tests
- Key detection logic
- Chord sequence handling
- Input field shortcut disabling

### Integration Tests
- Navigation shortcuts change routes
- Form shortcuts trigger save
- Focus management in lists

### E2E Tests
- Full workflow using only keyboard
- Tab order through forms
- Modal focus trapping

### Accessibility Tests
- Screen reader announcements
- Focus visibility
- ARIA attributes

---

*Related Stories*: [S03 Auto-save](./S03-auto-save.md), [inject-crud/S01 Create Inject](../inject-crud/S01-create-inject.md)

*Last updated: 2025-01-08*
