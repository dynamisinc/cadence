# Story: Core Capability Performance

**Feature**: Metrics  
**Story ID**: S06  
**Priority**: P1 (Standard)  
**Phase**: Standard Implementation

---

## User Story

**As a** Director or Emergency Manager,  
**I want** to see performance metrics broken down by FEMA Core Capability,  
**So that** I can identify which organizational capabilities need improvement and prioritize training.

---

## Context

FEMA's National Preparedness Goal defines 32 Core Capabilities across five mission areas. HSEEP requires exercises to evaluate specific capabilities. This metrics view shows which capabilities were evaluated and their performance ratings, directly supporting improvement planning.

Core Capabilities include: Planning, Public Information and Warning, Operational Coordination, Intelligence and Information Sharing, etc.

---

## Acceptance Criteria

- [ ] **Given** I am viewing exercise metrics, **when** observations have capability tags, **then** I see performance by Core Capability
- [ ] **Given** the capability view, **when** displayed, **then** I see each capability with observation count and average rating
- [ ] **Given** the capability view, **when** displayed, **then** capabilities are sorted by rating (worst first) to highlight improvement areas
- [ ] **Given** the capability view, **when** I click a capability, **then** I see all observations tagged to that capability
- [ ] **Given** the capability view, **when** displayed, **then** I see which target capabilities had NO observations (gaps)
- [ ] **Given** the capability view, **when** I hover on a capability, **then** I see rating distribution (P/S/M/U counts)
- [ ] **Given** exercise objectives mapped to capabilities, **when** viewing, **then** I see objective alignment
- [ ] **Given** no capability tags exist, **when** viewing, **then** I see message encouraging capability tagging

---

## Out of Scope

- Capability trend analysis across exercises (org-level)
- FEMA capability definitions/descriptions
- Capability-specific recommendations
- Automated capability inference from observation text

---

## Dependencies

- S03: Exercise Observation Summary Metrics
- Core Capability list in system
- Observations tagged with capabilities

---

## Open Questions

- [ ] Should we show all 32 FEMA capabilities or just those targeted?
- [ ] How to handle organization-specific capabilities not in FEMA list?
- [ ] Should capability ratings weight by observation importance?
- [ ] Do we need mission area grouping (Prevention, Protection, etc.)?

---

## Domain Terms

| Term | Definition |
|------|------------|
| Core Capability | FEMA-defined capability area from National Preparedness Goal |
| Mission Area | Grouping of capabilities: Prevention, Protection, Mitigation, Response, Recovery |
| Target Capability | Capability specifically designated for exercise evaluation |

---

## UI/UX Notes

### Core Capability Performance View

```
┌─────────────────────────────────────────────────────────────────────────┐
│  CORE CAPABILITY PERFORMANCE                                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  View: [● Rating Order] [○ Alphabetical] [○ Mission Area]              │
│                                                                         │
│  Target Capabilities Evaluated: 6 of 8                                  │
│  ████████████████████████████████████░░░░░░░░░░  75% coverage          │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ⚠ IMPROVEMENT NEEDED                                                   │
│  ─────────────────────                                                  │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Mass Care Services                                               │ │
│  │  ████ Avg: Marginal (2.8)  │  Observations: 4                     │ │
│  │  P: 0  S: 1  M: 2  U: 1                                   [View →]│ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Public Information and Warning                                   │ │
│  │  ████ Avg: Marginal (2.5)  │  Observations: 3                     │ │
│  │  P: 0  S: 2  M: 0  U: 1                                   [View →]│ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ✓ SATISFACTORY PERFORMANCE                                            │
│  ──────────────────────────                                            │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Operational Coordination                                         │ │
│  │  ████ Avg: Satisfactory (1.9)  │  Observations: 8         [View →]│ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  Planning                                                         │ │
│  │  ████ Avg: Performed (1.5)  │  Observations: 6            [View →]│ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  ⊘ NOT EVALUATED (Target Capabilities)                                 │
│  ─────────────────────────────────────                                 │
│                                                                         │
│  • Intelligence and Information Sharing                                │
│  • Logistics and Supply Chain Management                               │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Rating Scale Visualization

```
Capability Rating Scale:
│  1.0 ──── 2.0 ──── 3.0 ──── 4.0  │
│   P        S        M        U   │
│  ────▲────────────────────────── │  Planning (1.5)
│  ─────────────▲───────────────── │  Operational Coord (1.9)
│  ────────────────────▲────────── │  Mass Care (2.8)
```

---

## Technical Notes

- Store Core Capabilities as reference data
- Many-to-many: Observations ↔ Capabilities
- Calculate average: `(P*1 + S*2 + M*3 + U*4) / count`
- Target capabilities should be flagged on exercise setup
- Consider: pre-populate FEMA capability list in seed data
- API: `GET /api/exercises/{id}/metrics/capabilities`

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 5
