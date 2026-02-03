# Feature: Setup Progress Dashboard

**Phase:** MVP
**Status:** Ready

## Overview

Setting up an exercise involves multiple configuration steps: creating the exercise, configuring roles, adding participants, defining objectives, setting up phases, and populating the MSEL. This feature provides a visual progress dashboard that guides users through the setup process and shows completion status for each area.

## Problem Statement

Exercise planners need guidance through the multi-step exercise setup process. Without visibility into what's complete and what's missing, users waste time searching for uncompleted tasks, risk launching exercises with incomplete configuration, and lack confidence that their exercise is ready for conduct.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-setup-progress.md) | View Setup Progress | P2 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|-------------|
| Administrator | Views progress for all exercises |
| Exercise Director | Views progress for their exercises |
| Controller | Views progress (read-only) |
| Evaluator | Views progress (read-only) |
| Observer | Limited visibility |

## Key Concepts

| Term | Definition |
|------|------------|
| Setup Progress | Completion status of exercise configuration areas |
| Progress Area | A specific configuration domain (Basic Info, Roles, MSEL, etc.) |
| Completion Criteria | Requirements for marking a progress area as complete |
| Minimum Viable Setup | Minimum configuration needed to conduct an exercise |
| Recommended Setup | Full configuration for optimal exercise conduct |

## Dependencies

- exercise-crud/S01: Create Exercise (exercise must exist)
- exercise-config/S01: Configure Roles
- exercise-config/S02: Assign Participants
- exercise-objectives/S01: Create Objective
- exercise-phases/S01: Define Phases
- inject-crud/S01: Create Inject

## Acceptance Criteria (Feature-Level)

- [ ] Users see a progress overview when viewing an exercise
- [ ] Each setup area shows completion status
- [ ] Clicking an incomplete area navigates to that configuration
- [ ] Progress updates in real-time as setup is completed

## Notes

### Progress Areas

| Area | Completion Criteria |
|------|---------------------|
| Basic Info | Exercise has name, type, date |
| Roles | At least Administrator and Exercise Director configured |
| Participants | At least one participant per enabled role |
| Objectives | At least one objective defined |
| Phases | Optional - at least one phase if used |
| MSEL | At least one inject created |
| Time Zone | Exercise time zone set |

### Wireframes/Mockups

#### Exercise Overview with Progress

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Hurricane Response 2025                                    Status: Draft│
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Setup Progress                                           75% Complete  │
│  ████████████████████████████░░░░░░░░░░                                │
│                                                                         │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐  │
│  │ ✓ Basic Info │ │ ✓ Roles      │ │ ✓ Participants│ │ ✓ Objectives │  │
│  │   Complete   │ │   5 enabled  │ │   10 assigned │ │   4 defined  │  │
│  └──────────────┘ └──────────────┘ └──────────────┘ └──────────────┘  │
│                                                                         │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                    │
│  │ ✓ Phases     │ │ ⚠ MSEL       │ │ ✓ Time Zone  │                    │
│  │   3 defined  │ │   0 injects  │ │   EST        │                    │
│  └──────────────┘ └──────────────┘ └──────────────┘                    │
│                                                                         │
│  ⚠️ Add injects to your MSEL to complete setup.  [Go to MSEL →]       │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Additional Context

- Progress dashboard is guidance, not enforcement - exercises can be run without 100% completion
- Consider adding "minimum viable" vs "recommended" completion levels
- Progress calculation weights areas differently (MSEL is most important)
