# Story: User Keyboard Shortcuts

**Feature**: Settings  
**Story ID**: S07  
**Priority**: P1 (Standard)  
**Phase**: Standard Implementation

---

## User Story

**As a** Controller or Director,  
**I want** to use keyboard shortcuts for common actions,  
**So that** I can operate more efficiently during fast-paced exercises without excessive mouse clicking.

---

## Context

SME feedback on EXIS highlighted excessive mouse clicking as a major pain point. During high-tempo exercises, Controllers may need to fire multiple injects in rapid succession. Keyboard shortcuts enable:

- Faster operations
- Reduced physical strain
- Better accessibility
- Professional power-user workflows

This addresses the EXIS complaint: "way too much clicking."

---

## Acceptance Criteria

- [ ] **Given** I am in user settings, **when** I view keyboard shortcuts, **then** I see a list of available shortcuts
- [ ] **Given** shortcuts are enabled (default), **when** I press Space on a focused inject row, **then** the Fire action is triggered
- [ ] **Given** shortcuts are enabled, **when** I press S on a focused inject row, **then** the Skip action is triggered
- [ ] **Given** shortcuts are enabled, **when** I press P anywhere in conduct view, **then** the clock toggles pause/play
- [ ] **Given** shortcuts are enabled, **when** I press ? anywhere, **then** a shortcuts help overlay appears
- [ ] **Given** I am typing in an input field, **when** I press shortcut keys, **then** shortcuts are suppressed (type normally)
- [ ] **Given** I want to disable shortcuts, **when** I toggle them off in settings, **then** shortcuts no longer function
- [ ] **Given** shortcuts help overlay is shown, **when** I press Escape or click outside, **then** it closes
- [ ] **Given** I press a shortcut, **when** confirmation is required (per exercise setting), **then** the confirmation dialog opens with keyboard focus

---

## Out of Scope

- Custom shortcut key assignment
- Macro recording (sequence of shortcuts)
- Touch gestures (tablet equivalent)
- Per-exercise shortcut configurations

---

## Dependencies

- S05: Exercise Confirmation Dialogs (shortcuts may trigger confirmations)
- MSEL view with focusable inject rows
- Exercise clock controls

---

## Open Questions

- [ ] Should shortcuts work when confirmation dialogs are open (Enter to confirm, Escape to cancel)?
- [ ] Do we need vim-style navigation (j/k for up/down in list)?
- [ ] Should there be a "command palette" (Cmd+K / Ctrl+K) for discoverability?
- [ ] What about users who need specific accessibility accommodations?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Keyboard Shortcut | Key combination that triggers an action without clicking |
| Focus | The currently selected element that receives keyboard input |

---

## UI/UX Notes

### Default Keyboard Shortcuts

| Shortcut | Action | Context |
|----------|--------|---------|
| `Space` | Fire inject | Inject row focused |
| `S` | Skip inject | Inject row focused |
| `E` | Edit inject | Inject row focused |
| `P` | Toggle pause/play clock | Conduct view |
| `N` | New observation | Conduct view (Evaluator) |
| `↑` / `↓` | Navigate inject list | MSEL view |
| `Enter` | Open inject detail | Inject row focused |
| `?` | Show shortcuts help | Anywhere |
| `Escape` | Close modal/overlay | Anywhere |

### Keyboard Settings

```
┌─────────────────────────────────────────────────────────────┐
│  User Settings                                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Keyboard                                                   │
│  ─────────────────────────────────────────────              │
│                                                             │
│  Enable Keyboard Shortcuts                                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                                          [  ON  ]   │   │
│  │  Use keyboard shortcuts for common actions          │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Press ? at any time to see available shortcuts            │
│                                                             │
│                           [View All Shortcuts]              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Shortcuts Help Overlay

```
┌─────────────────────────────────────────────────────────────┐
│  Keyboard Shortcuts                                    [X]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Navigation                                                 │
│  ───────────                                                │
│  ↑ / ↓         Move between injects                        │
│  Enter         Open inject detail                           │
│  Escape        Close modal / cancel                         │
│                                                             │
│  Inject Actions (when inject selected)                      │
│  ────────────────────────────────────                       │
│  Space         Fire inject                                  │
│  S             Skip inject                                  │
│  E             Edit inject                                  │
│                                                             │
│  Exercise Control                                           │
│  ────────────────                                           │
│  P             Pause / Play clock                           │
│  N             New observation                              │
│                                                             │
│  General                                                    │
│  ───────                                                    │
│  ?             Show this help                               │
│                                                             │
│                                              [Got it]       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- Use `useHotkeys` hook or similar library (react-hotkeys-hook)
- Shortcuts should be globally registered but context-aware
- Disable shortcuts when input/textarea is focused
- Store enabled/disabled preference in user profile
- Ensure shortcuts don't conflict with browser defaults
- ARIA: Announce action results to screen readers

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 5
