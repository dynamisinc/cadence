# Feature: My Assignments

**Phase:** Post-MVP
**Status:** Ready

## Overview

My Assignments provides a personalized dashboard showing exercises where the current user has been assigned a role. This becomes the natural landing page for users, showing them what they need to focus on across active, upcoming, and completed exercises.

## Problem Statement

Users participating in multiple exercises need a centralized view of their assignments across different exercises and roles. Without a personalized dashboard, users must navigate through all exercises to find the ones relevant to them, wasting time and creating confusion about priorities.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-my-assignments-view.md) | My Assignments View | P0 | 📋 Ready |
| [S03](./S03-role-based-landing.md) | Role-Based Landing | P0 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|-------------|
| Controller | See exercises where I need to fire injects, sorted by urgency |
| Evaluator | See exercises where I need to capture observations |
| Exercise Director | Overview of all exercises I'm responsible for |
| Observer | See exercises I can monitor |

## Key Concepts

| Term | Definition |
|------|------------|
| Assignment | A user's role assignment in a specific exercise |
| Active Exercise | Exercise currently being conducted (clock may be running) |
| Upcoming Exercise | Exercise scheduled for future conduct |
| Completed Exercise | Exercise that has finished conduct |
| Smart Landing | Routing users to role-appropriate pages automatically |
| Hub | Central exercise dashboard showing overview information |
| Inject Queue | Controller's primary view for firing injects |

## Dependencies

- Navigation Shell (P0-01, P0-02) for sidebar integration
- Authentication context (provides current user)
- Exercise entity with status workflow
- ExerciseParticipant entity (may need creation)

## Acceptance Criteria (Feature-Level)

- [ ] Users see exercises grouped by status: Active, Upcoming, Completed
- [ ] Each assignment shows user's role in that exercise
- [ ] Active exercises show current clock status (running, paused, elapsed time)
- [ ] Clicking an assignment navigates to role-appropriate page
- [ ] Empty states handled gracefully for each section
- [ ] Page is accessible from sidebar (My Assignments menu item)

## Notes

### Business Value

- **Efficiency**: Users immediately see their work queue without searching
- **Role Clarity**: Clear indication of user's role in each exercise
- **Time Savings**: Quick access to active exercises reduces navigation
- **Prioritization**: Organized by status helps users focus on active work

### Data Requirements

#### ExerciseParticipant Entity
```
ExerciseParticipant:
  - Id (GUID)
  - UserId (FK → User)
  - ExerciseId (FK → Exercise)
  - Role (HseepRole enum)
  - AssignedAt (DateTime)
  - AssignedBy (FK → User)
```

#### API Endpoints
```
GET /api/assignments/my
Response: {
  active: Assignment[],
  upcoming: Assignment[],
  completed: Assignment[]
}

Assignment: {
  exerciseId,
  exerciseName,
  role,
  status,
  scheduledStart,
  clockState,
  elapsedTime?
}
```

### Technical Notes

- Use React Query for data fetching with appropriate stale time
- Assignment data can leverage existing exercise queries with filtering
- Role-based landing routes should be centralized for reuse
- Consider WebSocket subscription for active exercise updates

### Related Documentation

- [Navigation Shell Feature](../navigation-shell/FEATURE.md)
- [Exercise CRUD](../exercise-crud/FEATURE.md)

---

*Feature created: 2026-01-23*
