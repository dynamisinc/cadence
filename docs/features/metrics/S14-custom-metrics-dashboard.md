# Story: Custom Metrics Dashboard

**Feature**: Metrics  
**Story ID**: S14  
**Priority**: P2 (Future)  
**Phase**: Future Enhancement

---

## User Story

**As a** Director or Administrator,  
**I want** to create custom dashboards with selected metrics and visualizations,  
**So that** I can focus on the metrics most relevant to my role and organizational priorities.

---

## Context

Different users care about different metrics:

- **Directors** may want inject timing and observation coverage
- **Emergency Managers** may want capability trends and improvement areas
- **Elected Officials** may want high-level summaries and year-over-year comparisons

Custom dashboards allow users to arrange and save their preferred metrics views.

---

## Acceptance Criteria

- [ ] **Given** I am viewing metrics, **when** I click "Customize Dashboard", **then** I enter edit mode
- [ ] **Given** edit mode, **when** I add a widget, **then** I can choose from available metric components
- [ ] **Given** edit mode, **when** I drag widgets, **then** I can arrange layout
- [ ] **Given** edit mode, **when** I resize widgets, **then** they adjust to my preferred size
- [ ] **Given** a configured dashboard, **when** I save, **then** my layout persists
- [ ] **Given** I have a saved dashboard, **when** I return to metrics, **then** my custom view loads
- [ ] **Given** I want multiple dashboards, **when** I create new, **then** I can have named dashboards
- [ ] **Given** I want to share, **when** I export dashboard config, **then** others can import it

---

## Out of Scope

- Dashboard templates marketplace
- Real-time dashboard sharing
- Dashboard scheduling/automation
- External data source widgets

---

## Dependencies

- All metrics stories implemented
- Widget component library

---

## Open Questions

- [ ] How many dashboards per user?
- [ ] Should dashboards be shareable within organization?
- [ ] Do we need role-based default dashboards?
- [ ] Mobile-responsive dashboard editing?

---

## UI/UX Notes

### Dashboard Edit Mode

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Custom Dashboard: My Exercise Overview           [+ Add Widget] [Save] │
├─────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────┐ ┌─────────────────────────┐              │
│  │  ╳ ⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮ ↔      │ │  ╳ ⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮ ↔      │              │
│  │  P/S/M/U Distribution   │ │  On-Time Rate Trend    │              │
│  │                         │ │                        │              │
│  │     [Chart]             │ │     [Chart]            │              │
│  │                         │ │                        │              │
│  └─────────────────────────┘ └─────────────────────────┘              │
│  ┌──────────────────────────────────────────────────────────────────┐ │
│  │  ╳ ⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮⋮ ↔            │ │
│  │  Recent Exercise Summary                                         │ │
│  │                                                                  │ │
│  │  Exercise        │ Date    │ Injects │ On-Time │ P/S %          │ │
│  │  ─────────────────────────────────────────────────────────────── │ │
│  │  Hurricane TTX   │ Jan 15  │   42    │   85%   │  78%           │ │
│  │  ...                                                             │ │
│  └──────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ╳ = Remove widget   ⋮⋮⋮ = Drag handle   ↔ = Resize                    │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Widget Picker

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Add Widget                                                        [X]  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Exercise Metrics                                                       │
│  ────────────────                                                       │
│  [+] Inject Summary          [+] Observation Summary                   │
│  [+] P/S/M/U Distribution    [+] Timeline Summary                      │
│  [+] Progress Indicator      [+] Controller Activity                   │
│                                                                         │
│  Organization Metrics                                                   │
│  ────────────────────                                                   │
│  [+] Exercise History        [+] Performance Trends                    │
│  [+] Capability Heatmap      [+] Recent Exercises Table                │
│                                                                         │
│  Comparison                                                             │
│  ──────────                                                             │
│  [+] Exercise Comparison     [+] Year-over-Year                        │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Technical Notes

- Store dashboard config as JSON in user preferences
- Use drag-and-drop library (react-grid-layout or similar)
- Widget components should be lazy-loaded
- Dashboard config: `{ widgets: [{ type, position, size, config }] }`
- Consider: responsive breakpoints for widget sizing

---

## Estimation

**T-Shirt Size**: XL  
**Story Points**: 13
