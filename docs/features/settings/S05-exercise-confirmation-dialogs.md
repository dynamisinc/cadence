# Story: Exercise Confirmation Dialogs Setting

**Feature**: Settings  
**Story ID**: S05  
**Priority**: P0 (MVP)  
**Phase**: MVP

---

## User Story

**As an** Exercise Director,  
**I want** to configure whether confirmation dialogs appear before critical actions,  
**So that** I can balance between preventing accidental actions and enabling quick operations during fast-paced exercises.

---

## Context

During exercise conduct, Controllers perform critical actions: firing injects, skipping injects, pausing the clock. Confirmation dialogs can prevent accidents but slow down operations. The right balance depends on:

- **Exercise pace**: Fast-paced exercises with many injects benefit from fewer dialogs
- **User experience**: New users may want confirmations; experienced Controllers may find them annoying (EXIS feedback: "too many clicks")
- **Action reversibility**: Some actions are easily reversed (pause), others are not (fire inject)

This setting allows Directors to configure confirmation behavior for their exercise.

---

## Acceptance Criteria

- [ ] **Given** I am a Director viewing exercise settings, **when** I access confirmation settings, **then** I see toggles for each confirmation type
- [ ] **Given** "Confirm Fire Inject" is enabled, **when** a Controller clicks Fire, **then** a confirmation dialog appears before firing
- [ ] **Given** "Confirm Fire Inject" is disabled, **when** a Controller clicks Fire, **then** the inject fires immediately (no dialog)
- [ ] **Given** "Confirm Skip Inject" is enabled, **when** a Controller clicks Skip, **then** a confirmation dialog appears
- [ ] **Given** "Confirm Skip Inject" is disabled, **when** a Controller clicks Skip, **then** the inject is skipped immediately
- [ ] **Given** "Confirm Pause/Stop Clock" is enabled, **when** a user clicks Pause or Stop, **then** a confirmation dialog appears
- [ ] **Given** any confirmation is shown, **when** I check "Don't ask again this session", **then** confirmations are suppressed for that session only
- [ ] **Given** I am a Controller, **when** viewing exercise settings, **then** I can see but not modify confirmation settings
- [ ] **Given** defaults, **when** a new exercise is created, **then** all confirmations are enabled by default

---

## Out of Scope

- Per-user confirmation preferences (exercise-level only)
- Undo functionality (alternative to confirmations)
- Custom confirmation messages
- Bulk action confirmations (fire multiple injects)

---

## Dependencies

- Inject firing mechanism (Phase D)
- Exercise clock controls (Phase D)
- Exercise settings panel

---

## Open Questions

- [ ] Should "Don't ask again" persist across sessions or just the current session?
- [ ] Do we need keyboard shortcuts that bypass confirmation (for power users)?
- [ ] Should Auto-Fire have a separate confirmation setting (e.g., show countdown)?
- [ ] What about confirming destructive actions like delete inject?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Confirmation Dialog | Modal popup requiring user to confirm before action executes |
| Fire | Deliver/release an inject to exercise players |
| Skip | Mark inject as intentionally not delivered |

---

## UI/UX Notes

### Exercise Settings - Confirmations

```
┌─────────────────────────────────────────────────────────────┐
│  Exercise Settings                          [Director Only] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Confirmation Dialogs                                       │
│  ─────────────────────────────────────────────              │
│                                                             │
│  Require confirmation before:                               │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  [✓]  Firing an inject                              │   │
│  │       Shows confirm dialog before inject fires       │   │
│  │                                                     │   │
│  │  [✓]  Skipping an inject                            │   │
│  │       Shows confirm dialog before marking skipped    │   │
│  │                                                     │   │
│  │  [✓]  Pausing or stopping the exercise clock        │   │
│  │       Shows confirm dialog before clock control      │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ⓘ Tip: Disable confirmations for experienced teams        │
│     running fast-paced exercises.                          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Confirmation Dialog Example

```
┌─────────────────────────────────────────────────────────────┐
│  Fire Inject?                                          [X]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  You are about to fire:                                     │
│                                                             │
│  INJ-015: Building evacuation notification                  │
│  Scheduled: 14:30 | Current: 14:28                          │
│                                                             │
│  This action cannot be undone.                              │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  [ ] Don't ask again this session                    │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│                          [Cancel]   [🔥 Fire Inject]       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Quick Action Without Confirmation

When confirmations are disabled:

```
Controller clicks [🔥 Fire] 
    ↓
Inject immediately fires
    ↓
Toast notification: "✓ INJ-015 fired at 14:28"
```

---

## Technical Notes

- Store as three booleans on Exercise entity:
  - `ConfirmFireInject`
  - `ConfirmSkipInject`
  - `ConfirmClockControl`
- "Don't ask again this session" stored in React state (not persisted)
- Consider: keyboard shortcut + modifier (e.g., Ctrl+Enter) to fire without confirmation regardless of setting
- All confirmations should be accessible (focus trap, keyboard navigable)
- Default: all confirmations enabled (conservative approach)

---

## Estimation

**T-Shirt Size**: S  
**Story Points**: 3
