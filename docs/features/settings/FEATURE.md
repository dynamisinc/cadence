# Feature: Settings

**Phase:** MVP (P0-P1), Standard (P1-P2)
**Status:** Not Started

## Overview

Cadence supports a three-tier settings model that mirrors how emergency management organizations operate: user-level preferences that follow individuals across exercises, exercise-level settings configured by Directors for specific events, and organization-level defaults managed by Administrators.

## Problem Statement

Emergency management professionals work across multiple exercises with different teams, configurations, and operational tempos. Users need personal preferences (time format, display density) that persist across exercises. Exercise Directors need exercise-specific controls (clock mode, auto-fire behavior) that don't affect other exercises. Administrators need organization-wide defaults to ensure consistency while allowing flexibility. Without this layered approach, users face repetitive configuration and inconsistent experiences.

## User Stories

### MVP (P0) — 5 Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-user-display-preferences.md) | User Display Preferences | P0 | 📋 Ready |
| [S02](./S02-user-time-format.md) | User Time Format | P0 | 📋 Ready |
| [S03](./S03-exercise-clock-mode.md) | Exercise Clock Mode | P0 | 📋 Ready |
| [S04](./S04-exercise-auto-fire.md) | Exercise Auto-Fire | P0 | 📋 Ready |
| [S05](./S05-exercise-confirmation-dialogs.md) | Exercise Confirmation Dialogs | P0 | 📋 Ready |

### Standard Implementation (P1) — 7 Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S06](./S06-user-notification-preferences.md) | User Notification Preferences | P1 | 📋 Ready |
| [S07](./S07-user-keyboard-shortcuts.md) | User Keyboard Shortcuts | P1 | 📋 Ready |
| [S08](./S08-exercise-skip-reason-requirement.md) | Exercise Skip Reason Requirement | P1 | 📋 Ready |
| [S09](./S09-exercise-observation-required-fields.md) | Exercise Observation Required Fields | P1 | 📋 Ready |
| [S10](./S10-org-default-exercise-template.md) | Org Default Exercise Template | P1 | 📋 Ready |
| [S11](./S11-org-session-timeout.md) | Org Session Timeout | P1 | 📋 Ready |
| [S12](./S12-org-auto-save-interval.md) | Org Auto-Save Interval | P1 | 📋 Ready |

### Future Enhancement (P2) — 3 Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S13](./S13-org-branding.md) | Org Branding | P2 | 📋 Ready |
| [S14](./S14-org-core-capability-list.md) | Org Core Capability List | P2 | 📋 Ready |
| [S15](./S15-user-default-msel-view.md) | User Default MSEL View | P2 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|-------------|
| **Administrator** | Manages organization-level defaults, session policies, branding |
| **Exercise Director** | Configures exercise-specific clock behavior, auto-fire, confirmation dialogs |
| **Controller** | Sets personal time format, display density, notification preferences |
| **Evaluator** | Customizes observation form defaults, rating display preferences |
| **Observer** | Adjusts read-only display preferences |

## Key Concepts

### Settings Inheritance Model

```
┌─────────────────────────────────────────────────────────────┐
│  Organization Settings (Admin)                              │
│  └── Defaults for all exercises                             │
│      └── Can be overridden at Exercise level                │
├─────────────────────────────────────────────────────────────┤
│  Exercise Settings (Director)                               │
│  └── Specific to this exercise                              │
│      └── Inherits org defaults, can override                │
├─────────────────────────────────────────────────────────────┤
│  User Settings (All Users)                                  │
│  └── Personal preferences                                   │
│      └── Follow user across all exercises                   │
└─────────────────────────────────────────────────────────────┘
```

## Dependencies

- User authentication and authorization
- Exercise permissions (to control who can modify exercise settings)
- Organization management (multi-tenant architecture)

## Acceptance Criteria (Feature-Level)

- [ ] Users can set personal preferences that persist across exercises
- [ ] Exercise Directors can configure exercise-specific settings
- [ ] Administrators can set organization-wide defaults
- [ ] Settings auto-save on change (no explicit save button)
- [ ] Each setting includes a reset-to-default option
- [ ] Exercise settings inherit from organization defaults when appropriate

## Out of Scope

- Settings import/export
- Settings audit log (beyond standard entity audit fields)
- Multi-organization comparison of settings

## Notes

### UI/UX Notes

- **User Settings**: Accessed via profile menu (avatar dropdown), always available
- **Exercise Settings**: Gear icon in exercise header, visible to Director+ only
- **Organization Settings**: Admin panel, Admin role only
- Settings should auto-save on change (no explicit save button)

### Open Questions

- [ ] Should exercise settings be locked once exercise starts?
- [ ] Do we need a "copy settings from previous exercise" feature?
- [ ] Should some settings require exercise pause to change?

### EXIS Pain Points Addressed

| EXIS Pain Point | Cadence Solution |
|-----------------|------------------|
| Short session timeout (30 min) causing data loss | Configurable timeout with org defaults (S11) |
| No keyboard shortcuts, excessive clicking | Comprehensive keyboard navigation (S07) |
| Inconsistent time format across pages | User-level time format preference (S02) |
| No control over notification noise | User notification preferences (S06) |
