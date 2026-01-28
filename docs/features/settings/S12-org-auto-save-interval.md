# Story: Organization Auto-Save Interval

**Feature**: Settings  
**Story ID**: S12  
**Priority**: P1 (Standard)  
**Phase**: Standard Implementation

---

## User Story

**As an** Administrator,  
**I want** to configure how frequently user work is auto-saved,  
**So that** users don't lose significant work if their session ends unexpectedly.

---

## Context

SME feedback emphasized data loss as a major frustration with existing tools. Auto-save mitigates this by periodically saving in-progress work without explicit user action.

Key considerations:
- Too frequent: May cause performance issues or confusing intermediate saves
- Too infrequent: Risk of losing significant work
- Organization may have different needs based on network reliability

---

## Acceptance Criteria

- [ ] **Given** I am an Administrator, **when** I access organization settings, **then** I see auto-save interval configuration
- [ ] **Given** I am configuring auto-save, **when** I view options, **then** I can select: 30 seconds, 1 minute, 2 minutes, 5 minutes, or Disabled
- [ ] **Given** auto-save is set to 1 minute, **when** a user is editing an observation, **then** a draft saves every 60 seconds
- [ ] **Given** auto-save occurs, **when** the user continues editing, **then** there is no visual disruption (background operation)
- [ ] **Given** auto-save fails (network issue), **when** the save fails, **then** the system retries and data is queued offline
- [ ] **Given** auto-save is Disabled, **when** a user edits content, **then** no automatic saves occur (manual save only)
- [ ] **Given** auto-save is active, **when** a save completes, **then** a subtle indicator shows "Saved" briefly
- [ ] **Given** defaults, **when** a new organization is created, **then** auto-save interval is 1 minute

---

## Out of Scope

- Per-user auto-save preferences
- Per-form auto-save settings (e.g., different for observations vs injects)
- Version history / undo to previous auto-save

---

## Dependencies

- Offline capability (Phase H) - auto-save should work offline
- Form state management
- Organization entity

---

## Open Questions

- [ ] What data types support auto-save (observations, injects, exercise metadata)?
- [ ] Should auto-save create visible "draft" status, or save seamlessly?
- [ ] How does auto-save interact with validation (save invalid data as draft)?
- [ ] Should there be visual feedback on each auto-save, or only on failure?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Auto-Save | Automatic periodic saving of in-progress work without user action |
| Draft | Unsaved or partially saved work that may not be complete |

---

## UI/UX Notes

### Organization Settings - Auto-Save

```
┌─────────────────────────────────────────────────────────────┐
│  Organization Settings                        [Admin Only]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Data Safety                                                │
│  ─────────────────────────────────────────────              │
│                                                             │
│  Auto-Save Interval                                         │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  [ 1 minute ▼ ]                                     │   │
│  │                                                     │   │
│  │  In-progress observations and edits will be saved   │   │
│  │  automatically at this interval.                    │   │
│  │                                                     │   │
│  │  Options: 30 sec, 1 min, 2 min, 5 min, Disabled     │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ⓘ Shorter intervals provide better protection against    │
│     data loss but may use more bandwidth.                  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Auto-Save Indicator (Subtle)

```
Observation Form Header:
┌─────────────────────────────────────────────────────────────┐
│  New Observation                           ☁ Saved 10s ago │
└─────────────────────────────────────────────────────────────┘

When saving:
┌─────────────────────────────────────────────────────────────┐
│  New Observation                              ↻ Saving...  │
└─────────────────────────────────────────────────────────────┘

When offline:
┌─────────────────────────────────────────────────────────────┐
│  New Observation                    📴 Saved locally       │
└─────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- Store interval on Organization entity (in seconds, 0 = disabled)
- Frontend: use debounced save with configurable interval
- Auto-save should:
  1. Save to local storage (IndexedDB) immediately
  2. Queue server sync at interval
- Track `lastAutoSave` timestamp per form
- Auto-save should NOT trigger validation errors (save current state)
- Consider: auto-save triggers only if content changed since last save

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 5
