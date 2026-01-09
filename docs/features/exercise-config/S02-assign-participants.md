# Story: S02 - Assign Participants to Exercise

## User Story

**As an** Administrator or Exercise Director,
**I want** to assign users to exercise roles,
**So that** participants have appropriate access and responsibilities during the exercise.

## Context

Once an exercise is created and roles are configured, users must be assigned to participate. This story covers the participant assignment workflow, allowing exercise planners to add users from the system and assign them to enabled roles. Users can have different roles across different exercises but only one role per exercise.

## Acceptance Criteria

- [ ] **Given** I am editing an exercise, **when** I navigate to Participants, **then** I see a list of currently assigned participants grouped by role
- [ ] **Given** I am on the Participants screen, **when** I click "Add Participant", **then** I see a searchable list of system users
- [ ] **Given** I am adding a participant, **when** I select a user, **then** I am prompted to select their role from the enabled roles
- [ ] **Given** I select a role for a user, **when** I confirm, **then** the user is added to the exercise with that role
- [ ] **Given** a user is already assigned to the exercise, **when** I search for users to add, **then** that user appears with their current role indicated
- [ ] **Given** a user is already assigned, **when** I click on them, **then** I can change their role or remove them
- [ ] **Given** I am changing a user's role, **when** I select a new role, **then** the change is saved and they gain the new role's permissions
- [ ] **Given** I am removing a participant, **when** I confirm removal, **then** they no longer have access to the exercise
- [ ] **Given** I am an Exercise Director, **when** I try to remove or demote an Administrator, **then** I am denied (cannot modify higher roles)
- [ ] **Given** the exercise has no Administrator assigned, **when** I try to navigate away, **then** I see a warning (at least one Administrator required)
- [ ] **Given** I am a Controller, Evaluator, or Observer, **when** I access the Participants screen, **then** I can view but not modify assignments

## Out of Scope

- Bulk user import from external systems
- Role-based email notifications on assignment
- Self-registration for exercises
- Team/group assignments

## Dependencies

- exercise-config/S01: Configure Exercise Roles (determines available roles)
- User management system (provides user list)
- Authentication system (enforces role-based access)

## Open Questions

- [ ] Should there be a minimum number of Controllers required?
- [ ] Can users assign themselves to exercises, or only Admins/Directors?
- [ ] Should assignment changes be logged for audit purposes?

## Domain Terms

| Term | Definition |
|------|------------|
| Participant | A user assigned to an exercise with a specific role |
| Assignment | The relationship between a user, exercise, and role |
| Exercise Access | Permission to view and interact with an exercise based on role |

## UI/UX Notes

```
┌─────────────────────────────────────────────────────────────────────┐
│  Participants                                    [+ Add Participant] │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ADMINISTRATORS (1)                                                 │
│  ├─ Maria Chen                          [Change Role] [Remove]      │
│                                                                     │
│  EXERCISE DIRECTORS (2)                                             │
│  ├─ James Washington                    [Change Role] [Remove]      │
│  └─ Sarah Martinez                      [Change Role] [Remove]      │
│                                                                     │
│  CONTROLLERS (4)                                                    │
│  ├─ Michael Brown                       [Change Role] [Remove]      │
│  ├─ Emily Davis                         [Change Role] [Remove]      │
│  ├─ Robert Wilson                       [Change Role] [Remove]      │
│  └─ Jennifer Taylor                     [Change Role] [Remove]      │
│                                                                     │
│  EVALUATORS (3)                                                     │
│  ├─ David Anderson                      [Change Role] [Remove]      │
│  ├─ Lisa Thomas                         [Change Role] [Remove]      │
│  └─ Kevin Jackson                       [Change Role] [Remove]      │
│                                                                     │
│  ⚠️ No Observers assigned                                          │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Add Participant Modal

```
┌─────────────────────────────────────────────┐
│  Add Participant                        ✕   │
├─────────────────────────────────────────────┤
│                                             │
│  Search: [________________] 🔍              │
│                                             │
│  ┌─────────────────────────────────────┐   │
│  │ ○ Alex Johnson                      │   │
│  │ ○ Chris Lee                         │   │
│  │ ○ Pat Williams (Already: Observer)  │   │
│  └─────────────────────────────────────┘   │
│                                             │
│  Assign Role: [Controller        ▼]         │
│                                             │
│           [Cancel]  [Add to Exercise]       │
│                                             │
└─────────────────────────────────────────────┘
```

## Technical Notes

- User search should be debounced (300ms) to reduce API calls
- Consider lazy loading for organizations with many users
- Cache user list during assignment session
