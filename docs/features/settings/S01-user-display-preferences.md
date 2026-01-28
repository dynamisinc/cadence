# Story: User Display Preferences

**Feature**: Settings  
**Story ID**: S01  
**Priority**: P0 (MVP)  
**Phase**: MVP

---

## User Story

**As a** Cadence user (any role),  
**I want** to customize my display preferences (theme and density),  
**So that** I can work comfortably during long exercise sessions without eye strain.

---

## Context

Exercise conduct can span multiple hours, often in challenging lighting conditions (EOCs with varied lighting, field locations, night operations). Users need control over their visual experience to maintain focus and reduce fatigue. Display preferences follow the user across all exercises and devices.

Emergency management professionals often work in environments where screen brightness and contrast matter—dim EOCs, bright outdoor locations, or extended overnight shifts.

---

## Acceptance Criteria

- [ ] **Given** I am logged in as any role, **when** I access my profile menu, **then** I see a "Settings" or "Preferences" option
- [ ] **Given** I am in user settings, **when** I view display options, **then** I can select Light, Dark, or System theme
- [ ] **Given** I select a theme, **when** I change the selection, **then** the UI updates immediately without page reload
- [ ] **Given** I am in user settings, **when** I view density options, **then** I can select Comfortable (default) or Compact
- [ ] **Given** I select Compact density, **when** applied, **then** table rows, buttons, and spacing reduce to show more content
- [ ] **Given** I change any display preference, **when** I close settings, **then** my preference persists across sessions
- [ ] **Given** I have saved preferences, **when** I log in on a different device, **then** my preferences are applied
- [ ] **Given** I want to reset preferences, **when** I click "Reset to Default", **then** theme reverts to System and density to Comfortable

---

## Out of Scope

- Custom color themes beyond Light/Dark/System
- Font size adjustment (may be added in accessibility story)
- Per-exercise display overrides
- High contrast mode (separate accessibility story)

---

## Dependencies

- User authentication (user must be logged in)
- User profile/preferences storage in database

---

## Open Questions

- [ ] Should we support a "High Contrast" option for accessibility? (Recommend separate story)
- [ ] Do we auto-detect system preference changes while app is open?
- [ ] Should Compact density also affect font size or just spacing?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Theme | Visual color scheme: Light (white background), Dark (dark background), System (follows OS setting) |
| Density | UI spacing: Comfortable (spacious, touch-friendly) vs Compact (condensed, more data visible) |
| EOC | Emergency Operations Center - often has controlled lighting |

---

## UI/UX Notes

### Settings Panel Layout

```
┌─────────────────────────────────────────────────────────────┐
│  User Settings                                              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Display                                                    │
│  ─────────────────────────────────────────────              │
│                                                             │
│  Theme                                                      │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐                    │
│  │  ☀ Light │ │  ☾ Dark  │ │ ⚙ System │                    │
│  └──────────┘ └──────────┘ └──────────┘                    │
│       ○            ○            ●                           │
│                                                             │
│  Display Density                                            │
│  ┌──────────────────┐ ┌──────────────────┐                 │
│  │   Comfortable    │ │     Compact      │                 │
│  │   (Default)      │ │   More content   │                 │
│  └──────────────────┘ └──────────────────┘                 │
│           ●                    ○                            │
│                                                             │
│                               [Reset to Default]            │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Implementation Notes

- Use MUI's theme provider for Light/Dark switching
- System theme should use `prefers-color-scheme` media query
- Density affects MUI's `dense` prop on components
- Store preferences in user profile (database), not just localStorage
- Apply preferences at app initialization, before first render to avoid flash

---

## Technical Notes

- Store in `UserPreferences` table or as JSON column on `User` entity
- Consider caching preferences in localStorage for faster initial load
- Theme change should not require API call (immediate local update, background sync)

---

## Estimation

**T-Shirt Size**: S  
**Story Points**: 2-3
