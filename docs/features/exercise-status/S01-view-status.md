# exercise-status/S01: View Exercise Status

## Story

**As an** Exercise user (any role),
**I want** to see the current status of an exercise at all times,
**So that** I understand what phase the exercise is in and what actions are available.

## Context

Exercise status is the single most important piece of contextual information for users. It determines:
- What actions are available (edit MSEL, fire injects, record observations)
- What data is editable vs read-only
- Whether the exercise clock can be started or is locked

Status must be visible in every view that displays exercise information: list views, detail views, MSEL views, and dashboards. The status badge provides immediate visual feedback about the exercise lifecycle phase.

This is a foundational story - all status transition stories depend on this visual indicator being implemented first.

## Acceptance Criteria

### Status Badge Display

- [ ] **Given** I view an exercise in any list, **when** the page renders, **then** I see a status badge showing the current status (Draft, Active, Paused, Completed, or Archived)
- [ ] **Given** I view exercise detail page, **when** the page loads, **then** I see the status badge in the page header
- [ ] **Given** I view the MSEL page, **when** the page loads, **then** I see the status badge in the MSEL header
- [ ] **Given** an exercise is in Draft status, **when** displayed, **then** the badge is blue with text "Draft"
- [ ] **Given** an exercise is in Active status, **when** displayed, **then** the badge is green with text "Active" and subtle pulsing animation
- [ ] **Given** an exercise is in Paused status, **when** displayed, **then** the badge is yellow/orange with text "Paused"
- [ ] **Given** an exercise is in Completed status, **when** displayed, **then** the badge is gray with text "Completed"
- [ ] **Given** an exercise is in Archived status, **when** displayed, **then** the badge is light gray with text "Archived"

### Status Badge Behavior

- [ ] **Given** the exercise status changes, **when** a real-time update occurs (SignalR), **then** the status badge updates without page refresh
- [ ] **Given** I hover over the status badge, **when** a tooltip appears, **then** it shows the status transition timestamp and user (e.g., "Activated Jan 15, 2026 at 9:00 AM by John Smith")
- [ ] **Given** the exercise has never been activated, **when** I hover over a Draft status badge, **then** the tooltip shows "Created [date] by [user]"

### Visual Consistency

- [ ] **Given** I view status badges in list view, **when** comparing to detail view, **then** the badge styling is consistent (same colors, same text)
- [ ] **Given** I view the status badge on mobile, **when** the screen is narrow, **then** the badge remains readable and properly sized

### Accessibility

- [ ] **Given** I use a screen reader, **when** the status badge is announced, **then** I hear "Exercise status: [status name]"
- [ ] **Given** the status badge has color coding, **when** viewed in high-contrast mode, **then** the text remains readable

## Out of Scope

- Status change actions (covered in S02-S05)
- Status history timeline (future enhancement)
- Custom status labels per organization
- Status-based filtering (covered in exercise-crud/S03)

## Dependencies

- exercise-crud/S01: Create Exercise (status defaults to Draft)
- exercise-crud/S03: View Exercise List (list view displays badges)

## Open Questions

- [ ] Should the Active status badge pulse/animate to draw attention? (Recommendation: Yes, subtle pulse)
- [ ] Should tooltip show full audit trail or just last transition? (Recommendation: Last transition only for S01)
- [ ] Should we show status icon in addition to text badge? (Recommendation: Text-only for MVP)

## Domain Terms

| Term | Definition |
|------|------------|
| Status Badge | Visual indicator (chip/label) showing exercise lifecycle phase |
| Exercise Status | Current lifecycle state (Draft, Active, Paused, Completed, Archived) |
| Status Transition | Change from one status to another (e.g., Draft → Active) |

## UI/UX Notes

### Status Badge Component

```typescript
// Component usage
<ExerciseStatusBadge status={exercise.status} />

// Props interface
interface ExerciseStatusBadgeProps {
  status: ExerciseStatus;
  showTooltip?: boolean;
  size?: 'small' | 'medium' | 'large';
}
```

### Visual Design

```
┌────────────────────────────────────────────────┐
│  Exercise: Hurricane Response 2025             │
│  [Active]  📍 Houston, TX                      │
│  Jan 15, 2026 | 9:00 AM - 5:00 PM             │
└────────────────────────────────────────────────┘

Status Badge Examples:
┌─────────┐  ┌─────────┐  ┌────────┐  ┌───────────┐  ┌──────────┐
│ Draft   │  │ Active  │  │ Paused │  │ Completed │  │ Archived │
│  Blue   │  │  Green  │  │ Yellow │  │   Gray    │  │Lt Gray   │
└─────────┘  └─────────┘  └────────┘  └───────────┘  └──────────┘
```

### List View Integration

```
┌─────────────────────────────────────────────────────────────┐
│  Exercises                              [+ Create Exercise] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  [Active]   Hurricane Response 2025                         │
│             Jan 15, 2026 | Houston, TX                      │
│                                                             │
│  [Draft]    Flood Preparedness Training                     │
│             Feb 1, 2026 | Austin, TX                        │
│                                                             │
│  [Completed] Earthquake Drill 2025                          │
│             Dec 10, 2025 | San Francisco, CA                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Tooltip Content

```
┌────────────────────────────────────┐
│  Active                            │
│  Activated Jan 15, 2026 at 9:00 AM │
│  by John Smith (Exercise Director) │
└────────────────────────────────────┘
```

## Technical Notes

### Frontend Implementation

**File:** `src/frontend/src/shared/components/ExerciseStatusBadge.tsx`

```typescript
import { Chip } from '@mui/material';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faCircle } from '@fortawesome/free-solid-svg-icons';

const statusConfig = {
  Draft: { color: 'info', icon: faCircle },
  Active: { color: 'success', icon: faCircle, pulse: true },
  Paused: { color: 'warning', icon: faCircle },
  Completed: { color: 'default', icon: faCircle },
  Archived: { color: 'default', icon: faCircle, dimmed: true }
};
```

**Animation for Active status:**
```css
@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.7; }
}

.status-badge-active {
  animation: pulse 2s ease-in-out infinite;
}
```

### Backend Considerations

- Exercise entity already has `Status` field (no backend changes needed)
- SignalR event for status changes: `ExerciseStatusChanged(exerciseId, newStatus, timestamp, userId)`
- Include `ActivatedAt`, `ActivatedBy`, `CompletedAt`, `CompletedBy`, `ArchivedAt` in DTOs for tooltip

### Accessibility

- Use ARIA label: `aria-label="Exercise status: Active"`
- Ensure color contrast ratio ≥ 4.5:1 for text
- Do not rely solely on color to convey status (include text label)

---

**Acceptance Criteria Checklist:** 12 criteria
**Estimated Effort:** 0.5 days (frontend component + integration)
**Testing:** Component unit tests + visual regression tests
