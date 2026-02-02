# Feature: Home Page / Dashboard

**Phase:** MVP
**Status:** Complete

## Overview

The Home Page serves as the primary landing page and dashboard for Cadence users. It provides a role-aware welcome experience, quick actions based on user permissions, and immediate access to recent exercises.

## Problem Statement

When users first log in to Cadence, they need immediate orientation to their role capabilities and quick access to their exercises. Without a role-aware landing experience, users waste time searching for appropriate actions and don't understand what they can do in the system. Exercise Directors need quick access to create new exercises, while Controllers and Evaluators need to find their assigned exercises efficiently.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-view-homepage.md) | View Homepage with Role-Aware Content | P0 | ✅ Complete |
| [S02](./S02-quick-actions.md) | Quick Actions Based on Permissions | P0 | ✅ Complete |

## User Personas

| Persona | Interaction |
|---------|------------|
| Administrator | Sees full capabilities message, Create Exercise action, all exercises |
| Exercise Director | Sees management capabilities message, Create Exercise action, assigned exercises |
| Controller | Sees contributor capabilities message, View All action, assigned exercises |
| Evaluator | Sees observer capabilities message, View All action, assigned exercises |
| Observer | Sees read-only capabilities message, View All action, assigned exercises |

## Key Concepts

### Role-Aware Welcome

The homepage adapts its messaging and available actions based on the user's HSEEP role:

| Role | Display Name | Capabilities Message |
|------|--------------|---------------------|
| MANAGE | Exercise Director | "You have full access to create and manage exercises." |
| CONTRIBUTOR | Controller | "You can view exercises and fire injects during conduct." |
| READONLY | Observer | "You have read-only access to view exercises." |

### Quick Actions

Users see context-appropriate actions in a prominent action bar:

- **Create Exercise** - Available to MANAGE role only
- **View All Exercises** - Available to all authenticated users

### Exercise List Widget

Displays up to 5 recent non-archived exercises with:
- Exercise name (clickable to navigate to detail)
- Exercise type (TTX, FSE, FE, etc.)
- Status (Draft, Active, Completed)
- Scheduled date
- Practice mode indicator (🔧 icon with tooltip)

When more than 5 exercises exist, a "View All X Exercises" button appears below the list.

### Empty States

The homepage provides role-specific messaging when no exercises exist:

**For Managers (MANAGE role):**
- Visual emphasis with gradient background and icon
- "Create Your First Exercise" call-to-action
- Prominent Create Exercise button

**For Contributors/Viewers (CONTRIBUTOR, READONLY roles):**
- "No Exercises Assigned" message
- Guidance to contact Exercise Director
- No action button (cannot create)

## Dependencies

- exercise-crud/S03: View Exercise List (exercise retrieval and filtering)
- _cross-cutting/S01: Session Management (authentication and role assignment)
- _cross-cutting/S04: Responsive Design (tablet/mobile layouts)

## Acceptance Criteria (Feature-Level)

- [ ] ✅ All authenticated users see a role-aware welcome message
- [ ] ✅ Users see quick actions appropriate to their permission level
- [ ] ✅ Up to 5 recent exercises are displayed in a summary table
- [ ] ✅ Empty states provide role-specific messaging and guidance
- [ ] ✅ Clicking an exercise navigates to its detail page
- [ ] ✅ "View All" link appears when more than 5 exercises exist

## Wireframes/Mockups

### Manager View (with exercises)

```
┌─────────────────────────────────────────────────────────────────────┐
│  Welcome to Cadence                                                 │
│  HSEEP-Compliant MSEL Management Platform                          │
│  ─────────────────────────────────────────────────────────────────  │
│  Your Role: Exercise Director                                       │
│  You have full access to create and manage exercises.               │
└─────────────────────────────────────────────────────────────────────┘

[+ Create Exercise]  [View All Exercises]

Your Exercises
┌─────────────────────────────────────────────────────────────────────┐
│  Name                    │ Type │ Status  │ Date       │ Practice   │
│  ─────────────────────────────────────────────────────────────────  │
│  Hurricane Response 2025 │ TTX  │ Active  │ Jun 15, 2025│           │
│  Cyber Incident FSE      │ FSE  │ Draft   │ Jul 22, 2025│ 🔧        │
│  Mass Casualty FE        │ FE   │ Active  │ Aug 10, 2025│           │
│  Flood Response TTX      │ TTX  │ Draft   │ Sep 5, 2025 │           │
│  Wildfire Drill          │ FE   │ Draft   │ Oct 1, 2025 │ 🔧        │
└─────────────────────────────────────────────────────────────────────┘

                    [View All 12 Exercises]
```

### Observer View (no exercises)

```
┌─────────────────────────────────────────────────────────────────────┐
│  Welcome to Cadence                                                 │
│  HSEEP-Compliant MSEL Management Platform                          │
│  ─────────────────────────────────────────────────────────────────  │
│  Your Role: Observer                                                │
│  You have read-only access to view exercises.                       │
└─────────────────────────────────────────────────────────────────────┘

[View All Exercises]

Your Exercises
┌─────────────────────────────────────────────────────────────────────┐
│                                                                     │
│                      📋 No Exercises Assigned                       │
│                                                                     │
│  You haven't been assigned to any exercises yet. Contact your      │
│  Exercise Director to get added to an upcoming exercise.            │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Future Enhancements

The homepage is intentionally designed to accommodate future widgets and dashboard elements:

### Standard Phase
- Exercise metrics summary (total exercises, active exercises, completion rate)
- Recent activity feed (inject firing, observations added)
- Upcoming exercises timeline

### Advanced Phase
- Live exercise clock widget for active exercises
- Observation queue for Evaluators
- Progress dashboard with objectives completion
- Notifications center
- Team activity stream

## Notes

- The homepage uses the existing `useExercises` hook from exercise-crud feature
- Exercise list is filtered to exclude archived exercises
- Date formatting uses `date-fns` for consistent display
- Practice mode indicator uses Material-UI `BuildIcon` with tooltip
- Empty states use different visual treatments (gradients, icons) to convey role context
- All actions use COBRA styled components for consistency
