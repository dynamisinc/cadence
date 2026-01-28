# Story: User Notification Preferences

**Feature**: Settings  
**Story ID**: S06  
**Priority**: P1 (Standard)  
**Phase**: Standard Implementation

---

## User Story

**As a** Cadence user (any role),  
**I want** to control which notifications I receive and how they're displayed,  
**So that** I can stay informed about relevant events without being overwhelmed during exercise conduct.

---

## Context

During exercise conduct, multiple events occur that users may want to know about: injects firing, clock state changes, new observations, sync status. Different roles care about different events:

- **Directors/Controllers**: Need to know about clock changes, inject fires, issues
- **Evaluators**: Care about inject fires (to observe responses) and observation submissions
- **Observers**: Minimal notifications, mostly read-only

Users should control their notification experience to match their role and preferences.

---

## Acceptance Criteria

- [ ] **Given** I am in user settings, **when** I view notification options, **then** I see toggles for each notification type
- [ ] **Given** "Inject Fired" notifications enabled, **when** any inject fires, **then** I see a toast notification
- [ ] **Given** "Clock State Changes" enabled, **when** clock starts/pauses/stops, **then** I see a toast notification
- [ ] **Given** "Sound Alerts" enabled, **when** a notification appears, **then** an audio chime plays
- [ ] **Given** "Sound Alerts" disabled, **when** a notification appears, **then** no audio plays
- [ ] **Given** any notification, **when** displayed, **then** it auto-dismisses after 5 seconds (configurable)
- [ ] **Given** I disable all notifications, **when** events occur, **then** no toasts appear (but events still logged)
- [ ] **Given** I am a new user, **when** I first log in, **then** sensible defaults are set based on typical role needs

---

## Out of Scope

- Push notifications (browser notifications when app not focused)
- Email/SMS notifications
- Notification history/log view
- Role-specific notification defaults

---

## Dependencies

- S01: User Display Preferences (establishes settings pattern)
- Real-time sync (Phase H) - events to notify about

---

## Open Questions

- [ ] Should notification duration be configurable (3s, 5s, 10s)?
- [ ] Do we need notification grouping if many fire at once?
- [ ] Should we support browser push notifications for when tab is not active?
- [ ] Different sound for different event types?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Toast Notification | Transient popup message that auto-dismisses |
| Sound Alert | Audio chime accompanying visual notification |

---

## UI/UX Notes

### Notification Settings

```
┌─────────────────────────────────────────────────────────────┐
│  User Settings                                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Notifications                                              │
│  ─────────────────────────────────────────────              │
│                                                             │
│  Show notifications for:                                    │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  [✓]  Inject fired                                  │   │
│  │  [✓]  Clock started/paused/stopped                  │   │
│  │  [ ]  Observation submitted (by others)             │   │
│  │  [✓]  Connection status changes                     │   │
│  │  [ ]  User joined/left exercise                     │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Sound Alerts                                               │
│  ┌─────────────────────────────────────────────────────┐   │
│  │                                          [  OFF ]   │   │
│  │  Play audio chime with notifications                │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Notification Duration                                      │
│  [ 5 seconds ▼ ]                                           │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- Store notification preferences in user profile
- Use MUI Snackbar or similar for toasts
- Audio requires user gesture before playing (browser policy)
- Consider notification queue to prevent stacking
- Notification types enum for type safety

---

## Estimation

**T-Shirt Size**: S  
**Story Points**: 3
