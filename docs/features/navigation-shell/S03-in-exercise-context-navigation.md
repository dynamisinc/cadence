# S03: In-Exercise Context Navigation

## Story

**As a** user working within an exercise,
**I want** the sidebar to transform to show exercise-specific navigation,
**So that** I can quickly access exercise features without navigating back to the exercise list.

## Context

When a user enters an exercise (e.g., clicks into "Hurricane Response 2025"), the navigation context changes. The sidebar should reflect this by showing exercise-specific options, the exercise name, and a way to exit back to the global view. This is the "Option B" pattern - sidebar transformation.

## Acceptance Criteria

### Context Entry
- [ ] **Given** I am on the exercises list, **when** I click into an exercise, **then** the sidebar transforms to exercise-specific navigation
- [ ] **Given** I navigate to /exercises/:id/*, **when** the page loads, **then** the sidebar shows exercise context
- [ ] **Given** I refresh the page while in an exercise, **when** the page reloads, **then** the exercise context is restored

### Sidebar Header
- [ ] **Given** I am in exercise context, **when** I view the sidebar header, **then** I see "← Back" link
- [ ] **Given** I am in exercise context, **when** I view the sidebar header, **then** I see the exercise name
- [ ] **Given** a long exercise name, **when** displayed, **then** it truncates with ellipsis

### Exercise Menu Items
- [ ] **Given** I am in exercise context, **when** I view the menu, **then** I see: Hub, MSEL, Inject Queue, Observations, Participants, Metrics, Settings
- [ ] **Given** I am a Controller in this exercise, **when** I view the menu, **then** I see: Hub, MSEL, Inject Queue
- [ ] **Given** I am an Evaluator in this exercise, **when** I view the menu, **then** I see: Hub, Observations

### Back Navigation
- [ ] **Given** I click "← Back", **when** navigating, **then** I return to the previous page (or /exercises if no history)
- [ ] **Given** I click "← Back", **when** the sidebar updates, **then** it transforms back to global navigation
- [ ] **Given** I navigate to a non-exercise route manually, **when** the route changes, **then** exercise context is cleared

### Context Persistence
- [ ] **Given** I am in exercise context, **when** I navigate between exercise pages (/exercises/:id/*), **then** context is maintained
- [ ] **Given** I am in exercise context, **when** I refresh the browser, **then** context is restored from sessionStorage

## Out of Scope

- Multiple exercises open simultaneously
- Exercise breadcrumbs
- Quick-switch between exercises

## Dependencies

- S01 (Updated Sidebar Menu Structure)
- S02 (Role-Based Menu Visibility)
- React Router
- Exercise data fetching

## Domain Terms

| Term | Definition |
|------|------------|
| Exercise Context | State indicating user is working within a specific exercise |
| Context Entry | Moment when user transitions from global to exercise-specific navigation |
| Context Exit | Moment when user leaves exercise-specific navigation |

## UI/UX Notes

### Sidebar in Exercise Context
```
┌─────────────────────────────────────┐
│  ← Back                             │
│                                     │
│  Hurricane Response 2025            │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  00:32:15  ● Active                 │
│                                     │
├─────────────────────────────────────┤
│  🏠 Hub                             │
│  📋 MSEL                            │
│  📥 Inject Queue            ←active │
│  👁️ Observations                    │
│  👥 Participants                    │
│  📊 Metrics                         │
│  ⚙️ Settings                        │
└─────────────────────────────────────┘
```

### Exercise-Specific Menu (Role-Filtered)

| Item | Admin | Director | Controller | Evaluator | Observer |
|------|:-----:|:--------:|:----------:|:---------:|:--------:|
| Hub | ✓ | ✓ | ✓ | ✓ | ✓ |
| MSEL | ✓ | ✓ | ✓ | - | - |
| Inject Queue | ✓ | ✓ | ✓ | - | - |
| Observations | ✓ | ✓ | - | ✓ | ✓* |
| Participants | ✓ | ✓ | - | - | - |
| Metrics | ✓ | ✓ | - | - | - |
| Settings | ✓ | ✓ | - | - | - |

*Observer can view observations but not create

## Technical Notes

- Create ExerciseNavigationContext provider
- Store context in sessionStorage for refresh survival
- Use React Router's useParams to detect exercise routes
- Context should include: exerciseId, exerciseName, exerciseStatus, userRole

### Context Shape
```typescript
interface ExerciseNavigationContext {
  exerciseId: string | null;
  exerciseName: string | null;
  exerciseStatus: ExerciseStatus | null;
  userRole: HseepRole | null;
  enterExercise: (exercise: Exercise, role: HseepRole) => void;
  exitExercise: () => void;
  isInExerciseContext: boolean;
}
```

---

*Story created: 2026-01-23*
