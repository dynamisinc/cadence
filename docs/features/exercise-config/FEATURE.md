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
