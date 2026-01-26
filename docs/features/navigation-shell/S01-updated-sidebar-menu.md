# S01: Updated Sidebar Menu Structure

## Story

**As a** Cadence user,
**I want** the sidebar to display organized menu sections,
**So that** I can quickly find and access the features I need.

## Context

The current sidebar may have a flat menu structure. This story introduces organized sections (CONDUCT, ANALYSIS, SYSTEM) with clear visual hierarchy to improve navigation efficiency as the application grows.

## Acceptance Criteria

### Menu Sections
- [ ] **Given** I view the sidebar, **when** it renders, **then** I see three sections: CONDUCT, ANALYSIS, SYSTEM
- [ ] **Given** the menu sections, **when** displayed, **then** each section has a subtle header label
- [ ] **Given** the menu sections, **when** viewed, **then** items are grouped logically under their section

### Menu Items
- [ ] **Given** the CONDUCT section, **when** displayed, **then** it contains: My Assignments, Exercises, Control Room, Inject Queue
- [ ] **Given** the ANALYSIS section, **when** displayed, **then** it contains: Observations, Reports
- [ ] **Given** the SYSTEM section, **when** displayed, **then** it contains: Templates, Users, Settings

### Visual Design
- [ ] **Given** a menu item, **when** it is the current route, **then** it shows an active/selected state
- [ ] **Given** a menu item, **when** I hover over it, **then** it shows a hover state
- [ ] **Given** a menu item, **when** rendered, **then** it displays an icon and label
- [ ] **Given** the sidebar, **when** viewed on mobile, **then** it functions as a drawer (existing behavior maintained)

### Icons
- [ ] **Given** each menu item, **when** displayed, **then** it has a relevant FontAwesome icon
- [ ] **Given** icons, **when** rendered, **then** they use consistent sizing and spacing

## Out of Scope

- Role-based visibility (see S02)
- In-exercise context transformation (see S03)
- Collapsible/expandable sections
- Customizable menu order

## Dependencies

- Existing sidebar component
- FontAwesome icon library
- React Router for active state detection
- COBRA styling system

## Domain Terms

| Term | Definition |
|------|------------|
| Sidebar | Primary navigation panel, typically on the left side |
| Menu Section | A grouping of related menu items under a header |
| Active State | Visual indication that a menu item matches the current route |

## UI/UX Notes

### Menu Structure
```
CONDUCT ─────────────────────
  📋 My Assignments
  📁 Exercises
  🎮 Control Room
  📥 Inject Queue

ANALYSIS ────────────────────
  👁️ Observations
  📊 Reports

SYSTEM ──────────────────────
  📄 Templates
  👥 Users
  ⚙️ Settings
```

### Suggested Icons (FontAwesome)
| Item | Icon |
|------|------|
| My Assignments | `faClipboardList` |
| Exercises | `faFolderOpen` |
| Control Room | `faDesktop` |
| Inject Queue | `faListCheck` |
| Observations | `faEye` |
| Reports | `faChartBar` |
| Templates | `faFileAlt` |
| Users | `faUsers` |
| Settings | `faCog` |

## Technical Notes

- Create menuConfig.ts to centralize menu structure
- Use consistent spacing from theme (theme.spacing())
- Section headers should be subtle (smaller font, muted color)
- Maintain existing mobile drawer behavior

---

*Story created: 2026-01-23*
