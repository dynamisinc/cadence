# Feature: Exercise Configuration

**Phase:** MVP
**Status:** In Progress

## Overview

Exercise Configuration provides the settings and assignments needed to prepare an exercise for conduct, including role configuration, participant assignment, and operational settings.

## Problem Statement

Before an exercise can begin, Exercise Directors need to configure critical settings like participant roles, time zones, and timing modes. Without a structured configuration workflow, exercises may launch with incomplete setup, leading to confusion during conduct. This feature ensures all required settings are in place before activation.

## User Personas

| Persona | Interaction |
|---------|-------------|
| Administrator | Full access to all configuration options |
| Exercise Director | Primary user for exercise setup and participant management |
| Controller | Views their assignment, no configuration access |
| Evaluator | Views their assignment, no configuration access |
| Observer | Views their assignment, no configuration access |

## Key Concepts

| Term | Definition |
|------|------------|
| Exercise Role | HSEEP-defined role assignment per user (Director, Controller, Evaluator, Observer) |
| Participant | User assigned to an exercise with a specific role |
| Time Zone | Geographic time zone for exercise conduct (affects inject scheduling) |
| Clock Mode | Timing strategy (Clock-Driven for real-time, Facilitator-Paced for TTX) |
| Required Configuration | Settings that must be completed before exercise can be activated |

## User Stories

### Core Configuration

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-configure-roles.md) | Configure Exercise Roles | P1 | 📋 Ready |
| [S02](./S02-assign-participants.md) | Assign Participants | P1 | 📋 Ready |
| [S03](./S03-timezone-configuration.md) | Configure Time Zone | P1 | 📋 Ready |

### Clock Modes & Timing Configuration

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01-timing](./S01-timing-configuration-fields.md) | Add Timing Configuration Fields to Exercise Entity | P1 | 📋 Ready |
| [S02-delivery](./S02-inject-delivery-time-field.md) | Add DeliveryTime Field to Inject Entity | P1 | 📋 Ready |
| [S03-ui](./S03-timing-configuration-ui.md) | Exercise Timing Configuration in Create/Edit Form | P1 | 📋 Ready |
| [S04-ready](./S04-inject-ready-status.md) | Add "Ready" Status to Inject Workflow | P1 | 📋 Ready |
| [S05-auto](./S05-auto-ready-injects.md) | Auto-Ready Injects When Clock Reaches DeliveryTime | P1 | 📋 Ready |
| [S06-clock](./S06-clock-driven-conduct-view.md) | Clock-Driven Conduct View Sections | P1 | 📋 Ready |
| [S07-facilitator](./S07-facilitator-paced-conduct-view.md) | Facilitator-Paced Conduct View | P1 | 📋 Ready |
| [S08-story](./S08-story-time-display.md) | Display Story Time in Clock Area | P2 | 📋 Ready |
| [S09-confirm](./S09-fire-confirmation-dialog.md) | Fire Confirmation Dialog for Critical Injects | P2 | 📋 Ready |
| [S10-sequence](./S10-sequence-drag-drop-reorder.md) | Sequence Number Reordering via Drag-Drop | P2 | 📋 Ready |

## Dependencies

- exercise-crud/ - Exercise must exist before configuration
- User management - Users must exist to assign as participants
- Core entities - Exercise and role definitions

## Acceptance Criteria (Feature-Level)

- [ ] All configuration options accessible from a unified Exercise Setup view
- [ ] Configuration changes saved immediately with auto-save
- [ ] Clear indication of required vs optional configuration
- [ ] Validation prevents exercise activation with incomplete required configuration
- [ ] All configuration changes audited

## Notes

See also:
- [Gap Analysis](./gap-analysis-exercise-clock-modes.md) - Current implementation vs requirements
- [Requirements](./exercise-clock-modes-requirements.md) - Full requirements specification for clock modes

## Configuration Requirements by Status

| Configuration | Draft | Active | Completed |
|--------------|-------|--------|-----------|
| Roles | ✏️ Editable | 🔒 Locked | 🔒 Locked |
| Participants | ✏️ Editable | ✏️ Editable* | 🔒 Locked |
| Time Zone | ✏️ Editable | 🔒 Locked | 🔒 Locked |

*Participants can be added during active exercise but not removed

## Wireframes/Mockups

### Exercise Setup Navigation
```
┌─────────────────────────────────────────────────────────────────┐
│  Hurricane Response 2025 - Setup                    [● Draft]   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  [✓] Basic Info        Exercise name, type, dates               │
│  [✓] Time Zone         America/Chicago (UTC-6)                  │
│  [ ] Participants      0 assigned                               │
│  [✓] Objectives        3 defined                                │
│  [✓] Phases            4 phases                                 │
│  [ ] MSEL              0 injects                                │
│                                                                 │
│  ──────────────────────────────────────────────────────────     │
│  Progress: 4 of 6 complete                                      │
│  [Start Exercise] (disabled until required items complete)      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

