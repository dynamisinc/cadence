# Feature: Exercise Configuration

**Parent Epic:** Exercise Setup (E3)

## Description

Exercise Configuration encompasses the settings and assignments that prepare an exercise for conduct. This includes role configuration, participant assignment, and operational settings like time zone. These configurations must be completed before an exercise can be activated.

## User Personas

| Persona | Interest in this Feature |
|---------|-------------------------|
| **Administrator** | Full access to all configuration options |
| **Exercise Director** | Primary user for exercise setup and participant management |
| **Controller** | Views their assignment, no configuration access |
| **Evaluator** | Views their assignment, no configuration access |
| **Observer** | Views their assignment, no configuration access |

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-configure-roles.md) | Configure Exercise Roles | P1 | 📋 Ready |
| [S02](./S02-assign-participants.md) | Assign Participants | P1 | 📋 Ready |
| [S03](./S03-timezone-configuration.md) | Configure Time Zone | P1 | 📋 Ready |

## Feature-Level Acceptance Criteria

- [ ] All configuration options accessible from a unified Exercise Setup view
- [ ] Configuration changes saved immediately with auto-save
- [ ] Clear indication of required vs optional configuration
- [ ] Validation prevents exercise activation with incomplete required configuration
- [ ] All configuration changes audited

## Configuration Requirements by Status

| Configuration | Draft | Active | Completed |
|--------------|-------|--------|-----------|
| Roles | ✏️ Editable | 🔒 Locked | 🔒 Locked |
| Participants | ✏️ Editable | ✏️ Editable* | 🔒 Locked |
| Time Zone | ✏️ Editable | 🔒 Locked | 🔒 Locked |

*Participants can be added during active exercise but not removed

## Dependencies

- Exercise CRUD (exercise-crud/) - Exercise must exist
- User management (authentication system) - Users must exist to assign
- Core entities (_core/) - Exercise and role definitions

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

## Related Documentation

- Role definitions: `_core/user-roles.md`
- Session management: `_cross-cutting/S01-session-management.md`
- Auto-save: `_cross-cutting/S03-auto-save.md`

---

## Clock Modes & Timing Configuration

> **Epic:** Exercise Conduct (MVP-J, MVP-M)
> **Phase:** D
> **Status:** Ready for Implementation

### Overview

Support for dual-mode timing system enabling both clock-driven (real-time) and facilitator-paced (TTX) exercise formats.

### Clock Mode Stories

| Story | Title | Priority | Status | Estimate |
|-------|-------|----------|--------|----------|
| [CLK-01](./S01-timing-configuration-fields.md) | Add Timing Configuration Fields to Exercise Entity | P1 | 📋 Ready | Medium |
| [CLK-02](./S02-inject-delivery-time-field.md) | Add DeliveryTime Field to Inject Entity | P1 | 📋 Ready | Small |
| [CLK-03](./S03-timing-configuration-ui.md) | Exercise Timing Configuration in Create/Edit Form | P1 | 📋 Ready | Medium |
| [CLK-04](./S04-inject-ready-status.md) | Add "Ready" Status to Inject Workflow | P1 | 📋 Ready | Small |
| [CLK-05](./S05-auto-ready-injects.md) | Auto-Ready Injects When Clock Reaches DeliveryTime | P1 | 📋 Ready | Large |
| [CLK-06](./S06-clock-driven-conduct-view.md) | Clock-Driven Conduct View Sections | P1 | 📋 Ready | Medium |
| [CLK-07](./S07-facilitator-paced-conduct-view.md) | Facilitator-Paced Conduct View | P1 | 📋 Ready | Large |
| [CLK-08](./S08-story-time-display.md) | Display Story Time in Clock Area | P2 | 📋 Ready | Medium |
| [CLK-09](./S09-fire-confirmation-dialog.md) | Fire Confirmation Dialog for Critical Injects | P2 | 📋 Ready | Small |
| [CLK-10](./S10-sequence-drag-drop-reorder.md) | Sequence Number Reordering via Drag-Drop | P2 | 📋 Ready | Medium |

### Clock Mode Documents

- [Gap Analysis](./gap-analysis-exercise-clock-modes.md) - Current implementation vs requirements
- [Requirements](./exercise-clock-modes-requirements.md) - Full requirements specification

### Implementation Phases

```
Phase 1: Foundation       → CLK-01, CLK-02, CLK-04 (database)
Phase 2: Configuration UI → CLK-03 (form)
Phase 3: Clock-Driven     → CLK-05, CLK-06, CLK-08 (conduct)
Phase 4: Facilitator Mode → CLK-07 (conduct)
Phase 5: Polish           → CLK-09, CLK-10 (UX enhancements)
```

### Estimated Effort

~15-20 developer days total (3 Small, 5 Medium, 2 Large stories)
