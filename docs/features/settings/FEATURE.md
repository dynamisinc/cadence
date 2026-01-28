# Feature: Settings

**Parent Epic**: E7 - Configuration & Personalization

## Description

Cadence supports a three-tier settings model that mirrors how emergency management organizations operate: user-level preferences that follow individuals across exercises, exercise-level settings configured by Directors for specific events, and organization-level defaults managed by Administrators. This layered approach ensures flexibility while maintaining consistency.

## Business Value

- **Personalization**: Users can customize their experience (time format, display density, notifications) without affecting others
- **Exercise Control**: Directors can configure exercise-specific behaviors (clock mode, auto-fire, confirmation requirements)
- **Organizational Consistency**: Administrators establish defaults and guardrails for all exercises
- **Reduced Friction**: Settings persist across sessions, eliminating repetitive configuration

## User Personas

| Persona | Settings Access | Key Needs |
|---------|-----------------|-----------|
| **Administrator** | All tiers | Org defaults, session policies, branding |
| **Exercise Director** | User + Exercise | Clock behavior, auto-fire, confirmation dialogs |
| **Controller** | User only | Time format, display density, notifications |
| **Evaluator** | User only | Observation form defaults, rating display |
| **Observer** | User only | Read-only display preferences |

## Features by Phase

### MVP (P0) — 5 Stories

Essential settings for basic exercise conduct. Must be complete before initial deployment.

| Story | File | Description | Est. Points |
|-------|------|-------------|-------------|
| S01 | `S01-user-display-preferences.md` | Theme (Light/Dark/System) and density settings | 2-3 |
| S02 | `S02-user-time-format.md` | 12-hour vs 24-hour (military) time display | 2 |
| S03 | `S03-exercise-clock-mode.md` | Real-time vs accelerated clock (1x, 2x, 5x, 10x) | 3-5 |
| S04 | `S04-exercise-auto-fire.md` | Enable/disable automatic inject firing | 5 |
| S05 | `S05-exercise-confirmation-dialogs.md` | Configure confirmations for fire/skip/clock actions | 3 |

**MVP Total: ~15-18 story points**

### Standard Implementation (P1) — 7 Stories

Enhanced settings for production use. Addresses SME feedback (EXIS pain points).

| Story | File | Description | Est. Points |
|-------|------|-------------|-------------|
| S06 | `S06-user-notification-preferences.md` | Control which toast notifications appear | 3 |
| S07 | `S07-user-keyboard-shortcuts.md` | Enable shortcuts for fast operations (addresses "too many clicks") | 5 |
| S08 | `S08-exercise-skip-reason-requirement.md` | Require explanation when skipping injects | 3 |
| S09 | `S09-exercise-observation-required-fields.md` | Configure which observation fields are mandatory | 3 |
| S10 | `S10-org-default-exercise-template.md` | Organization-level defaults for new exercises | 5 |
| S11 | `S11-org-session-timeout.md` | Configurable session timeout (addresses EXIS timeout complaints) | 5 |
| S12 | `S12-org-auto-save-interval.md` | Configurable auto-save frequency | 5 |

**P1 Total: ~29 story points**

### Future Enhancement (P2) — 3 Stories

Nice-to-have features for mature deployments.

| Story | File | Description | Est. Points |
|-------|------|-------------|-------------|
| S13 | `S13-org-branding.md` | Organization logo and colors | 3 |
| S14 | `S14-org-core-capability-list.md` | Customize available Core Capabilities | 5 |
| S15 | `S15-user-default-msel-view.md` | Set preferred MSEL organization/grouping | 3 |

**P2 Total: ~11 story points**

## Settings Inheritance Model

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

## UI/UX Notes

- **User Settings**: Accessed via profile menu (avatar dropdown), always available
- **Exercise Settings**: Gear icon in exercise header, visible to Director+ only
- **Organization Settings**: Admin panel, Admin role only
- Settings should auto-save on change (no explicit save button)
- Include reset-to-default option for each setting

## Dependencies

- Authentication system (to identify user role)
- Exercise permissions (to control who can modify exercise settings)
- Organization entity (if not already exists)

## Out of Scope

- Multi-organization support (single org for MVP)
- Settings import/export
- Settings audit log

## Open Questions

- [ ] Should exercise settings be locked once exercise starts?
- [ ] Do we need a "copy settings from previous exercise" feature?
- [ ] Should some settings require exercise pause to change?
