# S02: Role-Based Menu Visibility

## Story

**As a** user with a specific role,
**I want** to see only the menu items I have permission to access,
**So that** I'm not confused by options I cannot use.

## Context

Cadence has five HSEEP roles with different permission levels. The sidebar should adapt to show only relevant options, reducing clutter and preventing unauthorized access attempts. Some items are disabled (shown but not clickable) when context is missing, while others are completely hidden based on role.

## Acceptance Criteria

### Role-Based Filtering
- [ ] **Given** I am an Administrator, **when** I view the sidebar, **then** I see all menu items
- [ ] **Given** I am an Exercise Director, **when** I view the sidebar, **then** I see all items except Templates and Users
- [ ] **Given** I am a Controller, **when** I view the sidebar, **then** I see: My Assignments, Exercises, Control Room, Inject Queue, Settings
- [ ] **Given** I am an Evaluator, **when** I view the sidebar, **then** I see: My Assignments, Exercises, Observations, Settings
- [ ] **Given** I am an Observer, **when** I view the sidebar, **then** I see: My Assignments, Exercises, Settings

### Disabled State (Context Required)
- [ ] **Given** I am NOT in an exercise context, **when** I view Control Room, **then** it appears disabled with tooltip "Enter an exercise first"
- [ ] **Given** I am NOT in an exercise context, **when** I view Inject Queue, **then** it appears disabled with tooltip
- [ ] **Given** I am NOT in an exercise context, **when** I view Observations, **then** it appears disabled with tooltip
- [ ] **Given** a disabled item, **when** I click it, **then** nothing happens (no navigation)

### Hidden vs Disabled
- [ ] **Given** a user lacks role permission, **when** rendering, **then** the item is hidden (not rendered)
- [ ] **Given** a user has role permission but no exercise context, **when** rendering, **then** the item is visible but disabled

### Empty Sections
- [ ] **Given** all items in a section are hidden, **when** rendering, **then** the section header is also hidden
- [ ] **Given** an Observer user, **when** viewing ANALYSIS section, **then** the entire section is hidden (no visible items)

## Out of Scope

- Per-exercise role overrides (handled by in-exercise navigation)
- Permission denied error pages
- Role switching UI

## Dependencies

- S01 (Updated Sidebar Menu Structure)
- Authentication context (provides user role)
- Exercise navigation context (provides exercise-scoped role)

## Domain Terms

| Term | Definition |
|------|------------|
| System Role | User's overall application role (Admin, Director, etc.) |
| Exercise Role | User's role within a specific exercise (may differ from system role) |
| Hidden | Item not rendered in DOM |
| Disabled | Item rendered but not interactive, with tooltip explaining why |

## Role Permission Matrix

| Menu Item | Admin | Director | Controller | Evaluator | Observer |
|-----------|:-----:|:--------:|:----------:|:---------:|:--------:|
| My Assignments | ✓ | ✓ | ✓ | ✓ | ✓ |
| Exercises | ✓ | ✓ | ✓ | ✓ | ✓ |
| Control Room | ✓ | ✓ | ✓ | - | - |
| Inject Queue | ✓ | ✓ | ✓ | - | - |
| Observations | ✓ | ✓ | - | ✓ | - |
| Reports | ✓ | ✓ | - | - | - |
| Templates | ✓ | - | - | - | - |
| Users | ✓ | - | - | - | - |
| Settings | ✓ | ✓ | ✓ | ✓ | ✓ |

## UI/UX Notes

### Disabled Item Appearance
```
┌─────────────────────────────────────┐
│  📥 Inject Queue  [grayed out]      │
│      ↑                              │
│      └── Tooltip: "Enter an        │
│          exercise first"            │
└─────────────────────────────────────┘
```

### Controller View Example
```
CONDUCT ─────────────────────
  📋 My Assignments
  📁 Exercises
  🎮 Control Room        (disabled)
  📥 Inject Queue        (disabled)

SYSTEM ──────────────────────
  ⚙️ Settings

(ANALYSIS section hidden - no permitted items)
```

## Technical Notes

- Create useFilteredMenu hook to handle filtering logic
- Menu items should declare required roles in configuration
- Disabled state requires both visual styling and pointer-events: none
- Use MUI Tooltip for disabled state explanations

---

*Story created: 2026-01-23*
