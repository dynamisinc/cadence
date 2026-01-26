# S01: My Assignments View

## Story

**As a** Cadence user with exercise assignments,
**I want** to see all my assigned exercises grouped by status,
**So that** I can quickly find and access the exercises I'm working on.

## Context

Users participate in multiple exercises with different roles. This view consolidates all assignments in one place, organized by urgency/status to help users prioritize. This becomes the primary dashboard after login.

## Acceptance Criteria

### Assignment Display
- [ ] **Given** I have exercise assignments, **when** I navigate to My Assignments, **then** I see exercises grouped into Active, Upcoming, and Completed sections
- [ ] **Given** an assignment, **when** displayed, **then** it shows: Exercise name, My role, Status indicator
- [ ] **Given** an active exercise, **when** displayed, **then** it shows elapsed time or "Not Started"
- [ ] **Given** an upcoming exercise, **when** displayed, **then** it shows scheduled start date/time

### Grouping Logic
- [ ] **Given** "Active" section, **when** rendered, **then** it contains exercises with status = Active/InProgress
- [ ] **Given** "Upcoming" section, **when** rendered, **then** it contains exercises with status = Published/Scheduled and start date in future
- [ ] **Given** "Completed" section, **when** rendered, **then** it contains exercises with status = Completed
- [ ] **Given** multiple exercises in a section, **when** displayed, **then** they are sorted by start date (soonest first for upcoming, most recent first for completed)

### Empty States
- [ ] **Given** no assignments in a section, **when** rendered, **then** show appropriate empty state message
- [ ] **Given** no assignments at all, **when** page loads, **then** show "No assignments yet" with guidance

### Navigation
- [ ] **Given** an assignment card, **when** I click it, **then** I navigate to that exercise
- [ ] **Given** I click an active exercise, **when** navigating, **then** I go to my role-appropriate page (see S03)

### Data Loading
- [ ] **Given** I navigate to My Assignments, **when** data is loading, **then** I see loading skeletons
- [ ] **Given** the API request fails, **when** displaying, **then** I see an error message with retry option

## Out of Scope

- Filtering assignments by role
- Sorting options
- Search within assignments
- Bulk actions

## Dependencies

- ExerciseParticipant entity/table
- GET /api/assignments/my endpoint
- Exercise status workflow

## Domain Terms

| Term | Definition |
|------|------------|
| Assignment | A user's role assignment in a specific exercise |
| Active | Exercise currently being conducted (clock may be running) |
| Upcoming | Exercise scheduled for future conduct |
| Completed | Exercise that has finished conduct |

## UI/UX Notes

### Page Layout
```
┌─────────────────────────────────────────────────────┐
│  My Assignments                                     │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ACTIVE (2)                                         │
│  ┌─────────────────────────────────────────────┐    │
│  │  🔴 Hurricane Response 2025                 │    │
│  │     Controller  •  00:45:23 elapsed         │    │
│  └─────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────┐    │
│  │  🟡 Cyber Incident TTX                      │    │
│  │     Evaluator  •  Paused at 00:15:00        │    │
│  └─────────────────────────────────────────────┘    │
│                                                     │
│  UPCOMING (1)                                       │
│  ┌─────────────────────────────────────────────┐    │
│  │  ⚪ Spring Flood Exercise                   │    │
│  │     Controller  •  Starts Feb 15, 2026      │    │
│  └─────────────────────────────────────────────┘    │
│                                                     │
│  COMPLETED (3)                                      │
│  ┌─────────────────────────────────────────────┐    │
│  │  ✓ Earthquake Drill 2024                    │    │
│  │     Evaluator  •  Completed Jan 10, 2026    │    │
│  └─────────────────────────────────────────────┘    │
│  ... more cards                                     │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### Assignment Card Design
- Clear visual hierarchy: Exercise name prominent
- Role displayed as badge/chip
- Status indicator (colored dot or icon)
- Time information contextual to status
- Hover state indicates clickability

## Technical Notes

### API Response Shape
```typescript
interface MyAssignmentsResponse {
  active: Assignment[];
  upcoming: Assignment[];
  completed: Assignment[];
}

interface Assignment {
  exerciseId: string;
  exerciseName: string;
  role: HseepRole;
  exerciseStatus: ExerciseStatus;
  scheduledStart: string | null;
  clockState: ClockState | null;
  elapsedSeconds: number | null;
  completedAt: string | null;
}
```

### Component Structure
```
MyAssignmentsPage/
├── MyAssignmentsPage.tsx       # Main page component
├── AssignmentSection.tsx       # Section with header and cards
├── AssignmentCard.tsx          # Individual assignment display
├── useMyAssignments.ts         # React Query hook
└── myAssignmentsApi.ts         # API functions
```

---

*Story created: 2026-01-23*
