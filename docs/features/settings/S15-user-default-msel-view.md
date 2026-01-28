# Story: User Default MSEL View

**Feature**: Settings  
**Story ID**: S15  
**Priority**: P2 (Future)  
**Phase**: Future Enhancement

---

## User Story

**As a** Cadence user,  
**I want** to set my preferred default view for the MSEL,  
**So that** when I open an exercise, I see injects organized the way I prefer to work.

---

## Context

Different users prefer different MSEL organizations:

- **Controllers** may prefer chronological (by scheduled time)
- **Evaluators** may prefer by phase or capability
- **Directors** may prefer grouped by status (pending, fired, skipped)

Saving a default view eliminates repetitive filter/sort adjustments.

---

## Acceptance Criteria

- [ ] **Given** I am in user settings, **when** I view MSEL preferences, **then** I see default view options
- [ ] **Given** default view options, **when** displayed, **then** I can select: Chronological, By Phase, By Status, By Controller
- [ ] **Given** I select "By Phase", **when** I open any MSEL, **then** injects are grouped by phase by default
- [ ] **Given** I have a saved default, **when** I manually change view in MSEL, **then** my session view changes (default unchanged)
- [ ] **Given** I want to update default from MSEL, **when** I click "Save as Default", **then** current view becomes my default
- [ ] **Given** I am a new user, **when** I first access MSEL, **then** default is Chronological (standard order)
- [ ] **Given** default view includes sort direction, **when** I save ascending, **then** ascending is applied by default

---

## Out of Scope

- Per-exercise view preferences
- Complex saved views (multiple filters)
- Shared team views
- View templates

---

## Dependencies

- Inject organization (Phase F) - sort, filter, group functionality
- User settings infrastructure

---

## Open Questions

- [ ] Should default include filter settings (e.g., hide skipped)?
- [ ] Can we save column visibility preferences?
- [ ] Should there be quick-switch between common views?
- [ ] What about default for mobile vs desktop?

---

## Domain Terms

| Term | Definition |
|------|------------|
| MSEL View | How injects are organized/displayed in the inject list |
| Grouping | Visual organization of injects by category (phase, status, etc.) |

---

## UI/UX Notes

### User Settings - MSEL Preferences

```
┌─────────────────────────────────────────────────────────────────────────┐
│  User Settings                                                          │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  MSEL Display                                                           │
│  ─────────────────────────────────────────────                          │
│                                                                         │
│  Default View                                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  ● Chronological (by scheduled time)                            │   │
│  │  ○ Grouped by Phase                                             │   │
│  │  ○ Grouped by Status (Pending/Fired/Skipped)                    │   │
│  │  ○ Grouped by Controller                                        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Default Sort Direction                                                 │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  ● Ascending (earliest first)                                   │   │
│  │  ○ Descending (latest first)                                    │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Show by Default                                                        │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  [✓] Pending injects                                            │   │
│  │  [✓] Fired injects                                              │   │
│  │  [ ] Skipped injects                                            │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ⓘ You can change the view anytime in the MSEL. Use "Save as          │
│     Default" to update this setting from within the MSEL.              │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### MSEL View - Save as Default

```
┌─────────────────────────────────────────────────────────────────────────┐
│  MSEL: Hurricane Response TTX                                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  View: [By Phase ▼]  Sort: [Scheduled ▼]  Filter: [All ▼]              │
│                                                                         │
│                                            [⭐ Save as Default View]   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- Store in user preferences:
  - `DefaultMselViewMode` (enum: Chronological, ByPhase, ByStatus, ByController)
  - `DefaultMselSortDirection` (enum: Asc, Desc)
  - `DefaultMselShowPending`, `ShowFired`, `ShowSkipped` (booleans)
- Apply defaults when loading MSEL component
- Session state overrides default (don't persist every change)
- "Save as Default" updates user preferences via API

---

## Estimation

**T-Shirt Size**: S  
**Story Points**: 3
