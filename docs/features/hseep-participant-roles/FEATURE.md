# Feature: Complete HSEEP Participant Role Support

**Phase:** Standard
**Status:** 📋 Not Started

## Overview

Extend Cadence's ExerciseRole enum to include all HSEEP-defined exercise participant types. Currently, the system supports five roles (Administrator, ExerciseDirector, Controller, Evaluator, Observer) focused on exercise management and observation. HSEEP defines additional critical roles for exercise staff who perform specialized functions during conduct. This feature adds Player, Simulator, Facilitator, SafetyOfficer, and TrustedAgent roles to support full HSEEP compliance.

## Problem Statement

Emergency management exercises involve multiple participant types beyond management and observation roles. Full-Scale Exercises (FSE) and Functional Exercises (FE) require specialized exercise staff:

- **Players** must be tracked as participants with limited read/respond access (not external recipients)
- **Simulators** role-play external entities (media, phone calls, coordinating agencies)
- **Facilitators** guide discussion-based exercises (especially Tabletop Exercises)
- **Safety Officers** monitor and control safety during operations-based exercises
- **Trusted Agents** provide subject matter expertise embedded with player teams

Without these roles, exercise directors cannot properly assign responsibilities, control access, or generate accurate After-Action Reports that distinguish between staff types.

## Context: Players in Cadence

**IMPORTANT CLARIFICATION**: The DOMAIN_GLOSSARY currently states "Players are NOT Cadence users—they are the recipients of injects during exercise conduct." This is accurate for many exercise types where players respond through their real-world systems (phones, radios, incident command posts).

However, HSEEP also recognizes scenarios where:
1. **Virtual/Hybrid Exercises**: Players interact through the platform itself
2. **Training Exercises**: Players need access to review what they received
3. **Self-Paced Exercises**: Players advance at their own pace with platform access
4. **AAR Preparation**: Players review inject timeline for lessons learned

This feature adds **Player** as an optional ExerciseRole for these scenarios while maintaining the default pattern of players as external recipients.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-extend-exercise-role-enum.md) | Extend ExerciseRole Enum with HSEEP Roles | P1 | 📋 Not Started |
| [S02](./S02-update-role-assignment-ui.md) | Update Role Assignment UI | P1 | 📋 Not Started |
| [S03](./S03-player-role-permissions.md) | Define Player Role Permissions | P1 | 📋 Not Started |
| [S04](./S04-simulator-role-permissions.md) | Define Simulator Role Permissions | P1 | 📋 Not Started |
| [S05](./S05-facilitator-role-permissions.md) | Define Facilitator Role Permissions | P2 | 📋 Not Started |
| [S06](./S06-safety-officer-role-permissions.md) | Define Safety Officer Role Permissions | P2 | 📋 Not Started |
| [S07](./S07-trusted-agent-role-permissions.md) | Define Trusted Agent Role Permissions | P2 | 📋 Not Started |
| [S08](./S08-role-specific-ui-adaptations.md) | Role-Specific UI Adaptations | P2 | 📋 Not Started |
| [S09](./S09-bulk-import-new-roles.md) | Bulk Import Support for New Roles | P3 | 📋 Not Started |

## User Personas

| Persona | Interaction |
|---------|-------------|
| **Exercise Director** | Assigns staff to specialized roles, ensures role-appropriate access |
| **Player** | (Optional) Views received injects, reviews exercise timeline, participates in platform-mediated exercises |
| **Simulator** | Sends simulated communications, manages SimCell activities |
| **Facilitator** | Guides discussion flow, controls pace of TTX exercises |
| **Safety Officer** | Monitors exercise safety, has pause/stop authority |
| **Trusted Agent** | Observes specific player team, provides embedded expertise |

## Key Concepts

| Term | Definition |
|------|------------|
| **ExerciseRole** | HSEEP-aligned participant type with specific permissions and responsibilities |
| **Player** | Exercise participant being tested/trained (may or may not be Cadence user) |
| **Simulator (SimCell)** | Exercise staff who role-plays external entities during conduct |
| **Facilitator** | Exercise staff who guides discussions, especially in TTX |
| **Safety Officer** | Exercise staff responsible for safety oversight during operations-based exercises |
| **Trusted Agent** | Subject matter expert embedded with player team for observation and guidance |

## HSEEP Role Definitions

Based on HSEEP 2020 documentation:

### Player
**HSEEP Definition**: Personnel who have an active role in responding to the simulated emergency.

**Cadence Scope**: When players are Cadence users (virtual/hybrid exercises), they need limited platform access to receive and acknowledge injects.

**Typical Users**: Responders in training, incident commanders in tabletop exercises, virtual participants

### Simulator (SimCell Staff)
**HSEEP Definition**: Exercise staff who role-play individuals or organizations outside the scope of the exercise, providing realism through simulated interactions.

**Typical Users**: Personnel simulating media, higher headquarters, partner agencies, victims

### Facilitator
**HSEEP Definition**: Exercise staff who guides discussions and manages pace, especially in discussion-based exercises.

**Typical Users**: Exercise planners, senior emergency managers, training professionals

### Safety Officer
**HSEEP Definition**: Exercise staff responsible for safety oversight, with authority to pause or terminate exercise if safety hazards arise.

**Typical Users**: Safety professionals, fire chiefs, emergency managers with safety background

### Trusted Agent
**HSEEP Definition**: Subject matter experts embedded with players to observe performance and provide subtle guidance without disrupting realism.

**Typical Users**: Technical experts, regulatory specialists, experienced practitioners

## Dependencies

- _core/user-roles.md: Core role definitions and permission matrix
- exercise-config/S01: Configure Exercise Roles (enable/disable roles per exercise)
- exercise-config/S02: Assign Participants (assign users to roles)
- bulk-participant-import/S02: Preview and Validate Import (bulk assignment validation)

## Acceptance Criteria (Feature-Level)

- [ ] ExerciseRole enum includes all nine HSEEP-defined roles
- [ ] Role assignment UI displays all roles with accurate HSEEP descriptions
- [ ] Each new role has clearly defined permissions and access levels
- [ ] Permission matrix documentation updated for all roles
- [ ] Bulk participant import supports new role types
- [ ] Frontend components adapt UI based on user's role
- [ ] Migration path defined for existing exercises (default: new roles disabled)

## Out of Scope

- Player-facing mobile app (separate epic)
- SimCell communication templates (future enhancement)
- Facilitator-specific TTX tools (future enhancement)
- Safety incident reporting system (future enhancement)
- Trusted Agent private note-taking (future enhancement)

## Implementation Notes

### Database Migration Strategy

New roles will be added to ExerciseRole enum with values 6-10:
- Player = 6
- Simulator = 7
- Facilitator = 8
- SafetyOfficer = 9
- TrustedAgent = 10

Existing exercises will default to having these roles DISABLED in exercise configuration to avoid breaking changes.

### Permission Philosophy

New roles follow the principle of least privilege:
1. **Player**: Most restricted - view own received injects only
2. **Simulator**: Limited write - send simulated injects, update own simulations
3. **Facilitator**: TTX control - manage exercise pace, guide discussions
4. **SafetyOfficer**: Safety authority - pause/stop exercise, add safety observations
5. **TrustedAgent**: Hybrid observer - observe + record, but cannot modify exercise

## Related Documentation

- HSEEP 2020: Homeland Security Exercise and Evaluation Program doctrine
- _core/user-roles.md: Current five-role implementation
- DOMAIN_GLOSSARY.md: Domain terminology definitions

---

*Last updated: 2026-02-09*
