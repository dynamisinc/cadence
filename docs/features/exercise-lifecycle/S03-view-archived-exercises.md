# Story: View Archived Exercises

## S03-view-archived-exercises.md

**As an** Administrator,
**I want** to view a list of archived exercises,
**So that** I can manage, restore, or permanently delete them.

### Context

Archived exercises need to remain accessible to administrators for restoration or permanent deletion. Since archived exercises are hidden from default views, administrators need a way to find and manage them. This is accomplished through a filter on the exercise list.

### Acceptance Criteria

- [ ] **Given** I am an Administrator, **when** I am on the Exercises page, **then** I see a "Show Archived" toggle/filter option
- [ ] **Given** I am an Exercise Director (non-admin), **when** I view the Exercises page, **then** I do NOT see the "Show Archived" option
- [ ] **Given** I am a Controller, Evaluator, or Observer, **when** I view the Exercises page, **then** I do NOT see the "Show Archived" option
- [ ] **Given** I enable "Show Archived", **when** the list refreshes, **then** I see ONLY archived exercises (not mixed with active)
- [ ] **Given** I view archived exercises, **when** I see each exercise card, **then** it displays an "Archived" status chip
- [ ] **Given** I view archived exercises, **when** I see each exercise card, **then** it displays the archive date
- [ ] **Given** I view archived exercises, **when** I see each exercise card, **then** it displays who archived it
- [ ] **Given** I click on an archived exercise, **when** the detail page loads, **then** I can view its details in read-only mode
- [ ] **Given** I am viewing archived exercises, **when** I see each exercise card, **then** I see "Restore" and "Delete" actions (not "Archive")

### Out of Scope

- Dedicated admin archive management page (S07)
- Bulk operations (S07)
- Restore functionality (S04)
- Delete functionality (S06)

### UI/UX Notes

**Filter Toggle (Admin Only):**

```
Exercises                                         [+ New Exercise]
─────────────────────────────────────────────────────────────────
[Status: All ▼]  [Show Archived ☐]  🔍 Search...

┌─────────────────────────────────────────────────────────────────┐
│ 2026 Full Scale Exercise          Draft        Dec 15, 2025    │
│ Q1 Tabletop                       Completed    Mar 10, 2026    │
└─────────────────────────────────────────────────────────────────┘
```

**When "Show Archived" is checked:**

```
Exercises                                         [+ New Exercise]
─────────────────────────────────────────────────────────────────
[Status: All ▼]  [Show Archived ☑]  🔍 Search...

┌─────────────────────────────────────────────────────────────────┐
│ Old Test Exercise     Archived (was Draft)    Jan 5, 2026      │
│                       Archived by: J. Smith                     │
├─────────────────────────────────────────────────────────────────┤
│ Cancelled FE 2025     Archived (was Published) Dec 28, 2025    │
│                       Archived by: A. Jones                     │
└─────────────────────────────────────────────────────────────────┘
```

**Archived Exercise Card Styling:**
- Muted/grayed background or reduced opacity
- "Archived" chip in amber/orange color
- Show previous status in parentheses: "Archived (was Completed)"
- Show archive date and archived by user
- Actions: Restore, Permanently Delete (not Archive, Edit)

### API Specification

**Updated Endpoint:**
```
GET /api/exercises?includeArchived=false    (default - excludes archived)
GET /api/exercises?archivedOnly=true        (admin only - shows only archived)
```

**Query Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| includeArchived | boolean | false | Include archived in results |
| archivedOnly | boolean | false | Show ONLY archived (admin only) |

**Authorization:**
- `archivedOnly=true` requires Administrator role
- Non-admins always receive `includeArchived=false` regardless of parameter

### Domain Terms

| Term | Definition |
|------|------------|
| Archived | Exercise with status = Archived, hidden from default views |
| Previous Status | The status before archiving, shown for context |

### Dependencies

- S02 - Archive Exercise (creates archived exercises)

### Deliverables

1. Update `GetExercisesAsync` to support `archivedOnly` filter
2. Add authorization check for archived filter (admin only)
3. Add "Show Archived" toggle to exercise list page (admin only)
4. Update `ExerciseCard` to display archived state with styling
5. Update `ExerciseCard` actions for archived exercises (Restore, Delete)
6. Store filter preference (URL param or local storage)
7. Unit tests for filter logic
8. Test authorization for archived filter
