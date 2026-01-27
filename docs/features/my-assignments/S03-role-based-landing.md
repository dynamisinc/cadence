# S03: Role-Based Landing

## Story

**As a** user clicking into an exercise from My Assignments,
**I want** to land on the most relevant page for my role,
**So that** I can immediately start my work without extra navigation.

## Context

Different roles have different primary tasks during exercise conduct. Controllers need the inject queue, Evaluators need observations, Directors need the hub. This story implements "smart landing" that routes users to their most useful starting point.

## Acceptance Criteria

### Role-Based Routing
- [ ] **Given** I am a Controller, **when** I click an active exercise, **then** I navigate to the Inject Queue
- [ ] **Given** I am an Evaluator, **when** I click an active exercise, **then** I navigate to Observations
- [ ] **Given** I am an Exercise Director, **when** I click an active exercise, **then** I navigate to the Hub
- [ ] **Given** I am an Observer, **when** I click an active exercise, **then** I navigate to the Hub
- [ ] **Given** I am an Administrator, **when** I click an active exercise, **then** I navigate to the Hub

### Non-Active Exercise Routing
- [ ] **Given** an exercise is not active (upcoming/completed), **when** I click it, **then** I navigate to the Hub regardless of role
- [ ] **Given** an exercise is in Draft status, **when** I click it, **then** I navigate to the Exercise Detail page (not conduct)

### Route Mapping
- [ ] **Given** the routing utility, **when** called with role and exercise status, **then** it returns the appropriate route

## Out of Scope

- User preference override for landing page
- Remembering last visited page per exercise
- Role-specific redirects from other entry points (e.g., exercise list)

## Dependencies

- S01 (My Assignments View)
- Navigation Shell (P0-02) for in-exercise routes
- Exercise status workflow

## Domain Terms

| Term | Definition |
|------|------------|
| Smart Landing | Routing users to role-appropriate pages automatically |
| Hub | Central exercise dashboard showing overview information |
| Inject Queue | Controller's primary view for firing injects |
| Observations | Evaluator's primary view for capturing observations |

## Role → Landing Page Matrix

| Role | Active Exercise | Non-Active Exercise |
|------|-----------------|---------------------|
| Administrator | Hub | Hub |
| Exercise Director | Hub | Hub |
| Controller | Inject Queue | Hub |
| Evaluator | Observations | Hub |
| Observer | Hub | Hub |

## UI/UX Notes

No specific UI for this story - it's routing logic. The user experience is:

1. User clicks exercise card in My Assignments
2. Brief loading/transition
3. User lands directly on their primary workspace

### Route Patterns
```
Controller → /exercises/:id/queue
Evaluator → /exercises/:id/observations
Others → /exercises/:id/hub
```

## Technical Notes

### Routing Utility
```typescript
// src/frontend/src/utils/roleRouting.ts

export function getDefaultRouteForRole(
  exerciseId: string,
  role: HseepRole,
  exerciseStatus: ExerciseStatus
): string {
  // Draft exercises go to detail page
  if (exerciseStatus === 'Draft') {
    return `/exercises/${exerciseId}`;
  }
  
  // Non-active exercises go to hub
  if (!isActiveStatus(exerciseStatus)) {
    return `/exercises/${exerciseId}/hub`;
  }
  
  // Active exercises route by role
  switch (role) {
    case 'Controller':
      return `/exercises/${exerciseId}/queue`;
    case 'Evaluator':
      return `/exercises/${exerciseId}/observations`;
    default:
      return `/exercises/${exerciseId}/hub`;
  }
}

function isActiveStatus(status: ExerciseStatus): boolean {
  return ['Active', 'InProgress', 'Running'].includes(status);
}
```

### Usage in AssignmentCard
```typescript
const handleClick = () => {
  const route = getDefaultRouteForRole(
    assignment.exerciseId,
    assignment.role,
    assignment.exerciseStatus
  );
  navigate(route);
};
```

### Testing Scenarios
1. Controller clicking active exercise → lands on queue
2. Controller clicking upcoming exercise → lands on hub
3. Evaluator clicking active exercise → lands on observations
4. Director clicking any exercise → lands on hub
5. Any role clicking draft exercise → lands on detail page

---

*Story created: 2026-01-23*
