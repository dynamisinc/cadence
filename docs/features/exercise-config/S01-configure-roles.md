# Story: S01 - Configure Exercise Roles

## User Story

**As an** Administrator or Exercise Director,
**I want** to configure which roles are enabled for an exercise,
**So that** I can tailor the participant structure to match my exercise design.

## Context

Different exercises require different organizational structures. A small tabletop exercise might only need Controllers and Observers, while a full-scale exercise requires all five HSEEP roles. This story allows exercise planners to enable or disable roles at the exercise level, simplifying the interface for smaller exercises.

Note: This is about enabling/disabling role *types* for an exercise, not assigning specific users to roles (see S02).

## Acceptance Criteria

- [ ] **Given** I am editing an exercise, **when** I navigate to the Roles configuration section, **then** I see a list of all five HSEEP roles with toggle switches
- [ ] **Given** I am on the Roles configuration, **when** I view the role list, **then** I see: Administrator, Exercise Director, Controller, Evaluator, Observer
- [ ] **Given** I am on the Roles configuration, **when** I toggle a role off, **then** that role is disabled for the exercise and users cannot be assigned to it
- [ ] **Given** a role is disabled, **when** I toggle it back on, **then** the role becomes available for participant assignment
- [ ] **Given** the Administrator role, **when** I view its toggle, **then** it is always enabled and cannot be disabled (required)
- [ ] **Given** the Exercise Director role, **when** I view its toggle, **then** it is always enabled and cannot be disabled (required)
- [ ] **Given** users are assigned to a role, **when** I attempt to disable that role, **then** I see a warning showing the number of affected users
- [ ] **Given** users are assigned to a role I'm disabling, **when** I confirm the disable action, **then** those users are unassigned from the role
- [ ] **Given** I am a Controller, Evaluator, or Observer, **when** I attempt to access role configuration, **then** I am denied access

## Out of Scope

- Creating custom roles beyond the five HSEEP roles (future consideration)
- Role hierarchy or permission customization (fixed in MVP)
- Assigning specific users to roles (see S02)
- Role-based UI customization

## Dependencies

- exercise-crud/S01: Create Exercise (exercise must exist)
- exercise-crud/S02: Edit Exercise (configuration is part of exercise editing)
- User authentication and authorization system

## Open Questions

- [ ] Should there be exercise templates with pre-configured role settings?
- [ ] Should role configuration be locked once exercise conduct begins?

## Domain Terms

| Term | Definition |
|------|------------|
| Role | A HSEEP-defined participant type with specific permissions and responsibilities |
| Administrator | System-level role with full access to all features |
| Exercise Director | Overall exercise leadership role |
| Controller | Staff who deliver injects and guide players |
| Evaluator | Staff who observe and document performance |
| Observer | Read-only participants monitoring the exercise |

## UI/UX Notes

```
┌─────────────────────────────────────────────────┐
│  Exercise Roles                                 │
├─────────────────────────────────────────────────┤
│                                                 │
│  ☑ Administrator        (Required)    🔒       │
│  ☑ Exercise Director    (Required)    🔒       │
│  ☑ Controller           ─────○        Toggle   │
│  ☑ Evaluator            ─────○        Toggle   │
│  ☐ Observer             ○─────        Toggle   │
│                                                 │
│  💡 Disabled roles won't appear in             │
│     participant assignment.                     │
│                                                 │
└─────────────────────────────────────────────────┘

🔒 = Cannot be disabled
```

## Technical Notes

- Role configuration stored at exercise level
- Changes should trigger UI refresh for participant assignment screens
- Consider caching enabled roles for performance
