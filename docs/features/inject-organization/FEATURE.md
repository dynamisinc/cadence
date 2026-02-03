# Feature: Inject Organization

**Phase:** Standard
**Status:** Not Started

## Overview

This feature provides sorting, grouping, and manual reordering capabilities that help users manage complex exercise scenarios efficiently.

## Problem Statement

As MSELs grow to dozens or hundreds of injects, the default chronological view becomes difficult to navigate and manage. Exercise Directors need to view injects organized by phase to coordinate phase transitions. Controllers need to see all pending injects together regardless of time. Evaluators need injects grouped by objective to ensure evaluation coverage. Without flexible organization options, users cannot efficiently manage large MSELs.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-sort-injects.md) | Sort Injects | P1 | 📋 Ready |
| [S02](./S02-group-injects.md) | Group Injects | P1 | 📋 Ready |
| [S03](./S03-reorder-injects.md) | Reorder Injects | P2 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|------------|
| Administrator | Full organization access |
| Exercise Director | Full organization access |
| Controller | Sort and group; limited reorder |
| Evaluator | Sort and group only |
| Observer | Sort and group only |

## Organization Capabilities

| Capability | Description |
|------------|-------------|
| **Sort** | Order injects by column (time, title, status, etc.) |
| **Group** | Collapse injects into categories (phase, status, objective) |
| **Reorder** | Manually drag injects to change sequence |

## Dependencies

- inject-crud/S01: Create Inject (injects must exist)
- exercise-phases/S01: Define Phases (grouping by phase)
- exercise-objectives/S01: Create Objective (grouping by objective)

## Acceptance Criteria (Feature-Level)

- [ ] Users can sort the MSEL by any column
- [ ] Users can group injects by phase, status, or objective
- [ ] Users can manually reorder injects via drag-and-drop
- [ ] Organization preferences persist during session

## Wireframes/Mockups

### MSEL with Grouping

```
┌─────────────────────────────────────────────────────────────────────────┐
│  MSEL - Hurricane Response 2025                                        │
├─────────────────────────────────────────────────────────────────────────┤
│  [Filter ▼]  Group by: [Phase ▼]  Sort: [Scheduled Time ▼]             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ▼ Phase 1: Initial Response (15 injects)                              │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  1 │ 09:00 │ Hurricane warning issued       │ Pending │           │ │
│  │  2 │ 09:15 │ EOC activation ordered         │ Pending │           │ │
│  │  3 │ 09:30 │ Evacuation order issued        │ Pending │           │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ▼ Phase 2: Sustained Operations (22 injects)                          │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ 16 │ 12:00 │ Shelter capacity exceeded      │ Pending │           │ │
│  │ 17 │ 12:15 │ Resource request submitted     │ Pending │           │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  ► Phase 3: Recovery (10 injects)                          [collapsed] │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Notes

- Sort order should reset when grouping changes
- Consider saving preferred organization as user preference
- Drag-and-drop reorder updates inject sequence numbers
