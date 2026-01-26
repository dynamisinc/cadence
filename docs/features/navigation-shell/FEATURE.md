# Feature: Navigation Shell

**Parent Epic:** Application Navigation & User Experience
**Phase:** Post-MVP (Navigation Enhancement)

## Description

The navigation shell provides the primary UI structure for Cadence, including the sidebar menu, role-based visibility, and contextual transformation when entering/exiting exercises. This feature establishes consistent navigation patterns across all application states.

## Business Value

- **Role Clarity**: Users see only navigation options relevant to their permissions
- **Context Awareness**: UI adapts when working within a specific exercise
- **Efficiency**: Quick access to role-appropriate features reduces clicks
- **Discoverability**: Organized menu structure helps users find capabilities

## User Personas

| Persona | Navigation Needs |
|---------|------------------|
| **Administrator** | Full access to all menu items including system settings |
| **Exercise Director** | Exercise management, reports, participant management |
| **Controller** | Inject queue, control room, MSEL access |
| **Evaluator** | Observations, exercise hub |
| **Observer** | Read-only exercise access |

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-updated-sidebar-menu.md) | Updated Sidebar Menu Structure | P0 | 📋 Ready |
| [S02](./S02-role-based-menu-visibility.md) | Role-Based Menu Visibility | P0 | 📋 Ready |
| [S03](./S03-in-exercise-context-navigation.md) | In-Exercise Context Navigation | P0 | 📋 Ready |
| [S04](./S04-exercise-header-with-clock.md) | Exercise Header with Clock | P0 | 📋 Ready |

## Feature-Level Acceptance Criteria

- [ ] Sidebar displays organized menu sections (CONDUCT, ANALYSIS, SYSTEM)
- [ ] Menu items filtered based on user's system role
- [ ] Sidebar transforms when entering an exercise context
- [ ] Exercise clock visible in sidebar header during conduct
- [ ] Navigation state persists across page refreshes
- [ ] Mobile-responsive drawer behavior maintained

## Menu Structure

### Global Navigation (Outside Exercise)
```
CONDUCT
├── My Assignments        [All roles]
├── Exercises            [All roles]
├── Control Room         [Controller, Director, Admin] (disabled)
└── Inject Queue         [Controller, Director, Admin] (disabled)

ANALYSIS
├── Observations         [Evaluator, Director, Admin] (disabled)
└── Reports              [Director, Admin]

SYSTEM
├── Templates            [Admin]
├── Users                [Admin]
└── Settings             [All roles]
```

### In-Exercise Navigation (Inside Exercise)
```
[← Back to Exercises]
[Exercise Name]
[Clock: 00:00:00 ● Active]
─────────────────────────
├── Hub                  [All roles]
├── MSEL                 [Controller, Director, Admin]
├── Inject Queue         [Controller, Director, Admin]
├── Observations         [Evaluator, Director, Admin]
├── Participants         [Director, Admin]
├── Metrics              [Director, Admin]
└── Settings             [Director, Admin]
```

## Dependencies

- Authentication system (provides user role)
- Exercise context (provides exercise-scoped role)
- Exercise clock feature (CLK stories - ✅ Complete)
- React Router (route-based active states)

## Technical Notes

- Use React Context for exercise navigation state
- Persist context in sessionStorage for refresh survival
- Reuse existing ClockDisplay component from CLK-06
- Follow COBRA styling patterns

## Related Documentation

- [Exercise Clock Modes](../exercise-config/exercise-clock-modes-requirements.md)
- [CLK-06 Implementation](../exercise-config/CLK-06-IMPLEMENTATION.md)
- [COBRA Styling Guide](../../COBRA_STYLING.md)

---

*Feature created: 2026-01-23*
