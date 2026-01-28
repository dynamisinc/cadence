# Story: P/S/M/U Distribution Chart

**Feature**: Metrics  
**Story ID**: S05  
**Priority**: P1 (Standard)  
**Phase**: Standard Implementation

---

## User Story

**As a** Director or Evaluator Lead,  
**I want** to see P/S/M/U ratings visualized as interactive charts,  
**So that** I can quickly communicate performance distribution to stakeholders in AAR presentations.

---

## Context

While S03 provides rating counts, visual charts are more effective for:

- AAR presentations to leadership
- Quick identification of performance patterns
- Comparing performance across phases or capabilities
- Exported reports and documentation

This story adds interactive, presentation-ready visualizations.

---

## Acceptance Criteria

- [ ] **Given** I am viewing observation metrics, **when** ratings exist, **then** I see a donut/pie chart of P/S/M/U distribution
- [ ] **Given** the chart, **when** I hover over a segment, **then** I see count and percentage tooltip
- [ ] **Given** the chart, **when** I click a segment, **then** I see a filtered list of observations with that rating
- [ ] **Given** the chart, **when** displayed, **then** ratings use consistent colors (P=green, S=blue, M=yellow, U=red)
- [ ] **Given** multiple phases exist, **when** viewing distribution, **then** I can see a stacked bar chart comparing phases
- [ ] **Given** chart options, **when** I select "By Capability", **then** I see distribution grouped by core capability
- [ ] **Given** I want to export, **when** I click export, **then** I can download the chart as PNG or include in PDF report
- [ ] **Given** no observations exist, **when** viewing chart area, **then** I see appropriate empty state

---

## Out of Scope

- Trend analysis across exercises (org-level metrics)
- Custom color schemes
- 3D charts or animations
- Real-time chart updates during conduct

---

## Dependencies

- S03: Exercise Observation Summary Metrics
- Observation data with ratings
- Chart library (Recharts or similar)

---

## Open Questions

- [ ] Should charts animate on load?
- [ ] Do we need alternative visualizations for accessibility (data table)?
- [ ] Should there be a "presentation mode" for AAR slides?
- [ ] What chart resolution for PNG export?

---

## Domain Terms

| Term | Definition |
|------|------------|
| P/S/M/U | Performance rating scale |
| Distribution | Breakdown of observations by rating category |

---

## UI/UX Notes

### Rating Distribution Charts

```
┌─────────────────────────────────────────────────────────────────────────┐
│  PERFORMANCE RATING ANALYSIS                                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  View: [● Overall] [○ By Phase] [○ By Capability]        [📥 Export]   │
│                                                                         │
│  ┌─────────────────────────────┐  ┌──────────────────────────────────┐ │
│  │                             │  │                                  │ │
│  │       ╭───────────╮         │  │  Performed (P)      8   33%     │ │
│  │      ╱   ████      ╲        │  │  ████████████████                │ │
│  │     │  ██    ██     │       │  │                                  │ │
│  │     │ █   24   █    │       │  │  Satisfactory (S)  10   42%     │ │
│  │     │  █ obs  █     │       │  │  ████████████████████            │ │
│  │      ╲  ████  ╱     │       │  │                                  │ │
│  │       ╰───────╯     │       │  │  Marginal (M)       4   17%     │ │
│  │                     │       │  │  ████████                        │ │
│  │  ■ P  ■ S  ■ M  ■ U │       │  │                                  │ │
│  └─────────────────────────────┘  │  Unsatisfactory (U)  2    8%     │ │
│                                   │  ████                            │ │
│                                   └──────────────────────────────────┘ │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### By Phase View

```
┌─────────────────────────────────────────────────────────────────────────┐
│  View: [○ Overall] [● By Phase] [○ By Capability]        [📥 Export]   │
│                                                                         │
│  Phase 1: Initial Response                                              │
│  ████████████████████████████████████░░░░░░░░░░░░░░                    │
│  P: 50%                    S: 38%              M: 12%                   │
│                                                                         │
│  Phase 2: Activation                                                    │
│  ████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒                    │
│  P: 30%          S: 50%              M: 10%    U: 10%                   │
│                                                                         │
│  Phase 3: Operations                                                    │
│  ████████░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓                       │
│  P: 20%    S: 40%              M: 20%       U: 20%                      │
│                                                                         │
│  Phase 4: Demobilization                                                │
│  ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒                    │
│                    S: 50%              M: 50%                           │
│                                                                         │
│  Legend: █ Performed  ░ Satisfactory  ▒ Marginal  ▓ Unsatisfactory     │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Color Scheme

| Rating | Color | Hex |
|--------|-------|-----|
| Performed (P) | Green | #4CAF50 |
| Satisfactory (S) | Blue | #2196F3 |
| Marginal (M) | Amber | #FFC107 |
| Unsatisfactory (U) | Red | #F44336 |

---

## Technical Notes

- Use Recharts library (already in stack for consistency)
- Implement chart container component for reuse
- Export: use html2canvas or similar for PNG generation
- Ensure charts are responsive
- Add ARIA labels for accessibility
- Consider: SVG export for high-quality printing

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 5
