# Feature: Exercise Lifecycle Management

**Phase:** MVP
**Status:** Not Started

## Overview

Exercise Directors and Administrators can efficiently manage exercise lifecycle beyond the standard status workflow—archiving completed or abandoned exercises to declutter active views, and permanently deleting exercises when appropriate, with proper safeguards to prevent accidental data loss.

## Problem Statement

Emergency management professionals often create test exercises, start exercises that get cancelled, or complete exercises that clutter the active exercise list. Without lifecycle management, users cannot:
- Quickly hide exercises from active views without losing historical data
- Remove draft exercises that were created by mistake
- Permanently delete obsolete data after appropriate review
- Maintain a clean workspace while preserving audit trails for compliance

The standard status workflow (Draft → Published → Active → Completed) doesn't account for exercises that need to be hidden or removed entirely.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-lifecycle-tracking-fields.md) | Lifecycle Tracking Fields | P0 | Ready |
| [S02](./S02-archive-exercise.md) | Archive Exercise | P0 | Ready |
| [S03](./S03-view-archived-exercises.md) | View Archived Exercises | P0 | Ready |
| [S04](./S04-restore-exercise.md) | Restore Exercise | P0 | Ready |
| [S05](./S05-delete-draft-exercise.md) | Delete Draft Exercise | P0 | Ready |
| [S06](./S06-delete-archived-exercise.md) | Permanently Delete Archived Exercise | P1 | Ready |
| [S07](./S07-admin-archive-management.md) | Admin Archive Management Page | P1 | Ready |

## User Personas

| Persona | Interaction |
|---------|-------------|
| **Exercise Director** | Quickly archives exercises from any status to declutter active views without workflow overhead |
| **Administrator** | Views archived exercises, restores them if needed, or permanently deletes obsolete data; manages bulk operations |
| **Exercise Creator** | Deletes own draft exercises that were never published (test/practice exercises) |
| **Controller/Evaluator/Observer** | Cannot archive or delete exercises; focused on exercise conduct only |

## Key Concepts

### Domain Terminology

| Term | Definition |
|------|------------|
| **Archive** | Soft delete that hides exercise from normal views but preserves all data; reversible via restore |
| **Restore** | Return archived exercise to its previous status, making it visible again |
| **Permanent Delete** | Irreversible removal of exercise and all related data from the database |
| **Never Published** | Exercise where `HasBeenPublished` flag is false—eligible for creator delete without archiving first |
| **Creator** | User whose ID matches `CreatedById` on the exercise; can delete own draft exercises |
| **Previous Status** | Status stored when archiving (Draft/Published/Active/Completed) used as restoration target |
| **Two-Step Delete** | Safety pattern requiring archive before permanent delete for published exercises |
| **Conduct Data** | Data created during exercise execution: fired inject timestamps, observations, player responses |

### Exercise Lifecycle States

```
Draft → Published → Active → Completed
  ↓         ↓         ↓         ↓
  └─────────────────────────────┘
              ↓
          Archived
              ↓
    Permanently Deleted
```

### Permission Model

| Action | Who Can Do It |
|--------|---------------|
| Archive any exercise | Exercise Director, Administrator |
| View archived exercises | Administrator only |
| Restore archived exercise | Administrator only |
| Delete draft (never published) | Creator OR Administrator |
| Permanently delete archived | Administrator only |
| Delete published/active/completed | **No one** (must archive first) |

## Dependencies

- **exercise-crud** (Phase B) - Exercise entity and basic CRUD operations must exist
- **authentication** (_cross-cutting/S01) - User authentication and role-based authorization required
- **User entity** - Exercise creator tracking requires User entity with relationships

## Acceptance Criteria (Feature-Level)

- [ ] Exercises can be archived from any status (Draft, Published, Active, Completed) without navigating workflow steps
- [ ] Archived exercises are hidden from default exercise list views
- [ ] Administrators can view a filtered list of archived exercises
- [ ] Administrators can restore archived exercises to their previous status
- [ ] Draft exercises that have never been published can be deleted by their creator or any administrator
- [ ] Archived exercises can be permanently deleted by administrators with strong confirmation
- [ ] All archive, restore, and delete actions create audit records with user, timestamp, and relevant metadata
- [ ] Permanent deletion cascades to all related data (injects, observations, phases, participants)
- [ ] Users without appropriate permissions do not see archive/restore/delete actions

## Implementation Order

| Order | Story | Dependencies | Complexity |
|-------|-------|--------------|------------|
| 1 | S01 - Lifecycle Tracking Fields | None | Low |
| 2 | S02 - Archive Exercise | S01 | Medium |
| 3 | S03 - View Archived Exercises | S02 | Low |
| 4 | S04 - Restore Exercise | S02 | Low |
| 5 | S05 - Delete Draft Exercise | S01 | Medium |
| 6 | S06 - Delete Archived Exercise | S02, S05 | Medium |
| 7 | S07 - Admin Archive Management | S03, S04, S06 | High |

## Notes

### Business Value

- **Declutter workspace** - Remove inactive exercises from views without data loss
- **Undo capability** - Archive/restore pattern allows safe removal with recovery option
- **Compliance** - Maintain audit trail for all lifecycle operations
- **Storage management** - Enable cleanup of obsolete data through permanent deletion
- **User empowerment** - Creators can delete their own test exercises without admin intervention

### Design Decisions

**Why two-step delete for published exercises?**
Published exercises have historical value—they contain conduct data (fired injects, observations) that may be needed for compliance or after-action reports. Requiring archive first provides:
1. A cooling-off period for review
2. Admin oversight for irreversible actions
3. Clear audit trail of who archived and who deleted

**Why allow creators to delete drafts?**
Users often create test exercises to learn the system. Requiring admin intervention for every test cleanup creates unnecessary friction. The `HasBeenPublished` flag ensures only safe-to-delete exercises are eligible.

**Why hide archived from all non-admins?**
Archived exercises are "out of circulation"—showing them to regular users creates confusion about which exercises are active. Administrators need visibility for data management; exercise participants do not.

### Open Questions

- [ ] Should there be a retention policy (auto-delete archived exercises after X days)?
- [ ] Should archived exercises count against any organizational quotas?
- [ ] Should exercise deletion be audited to an external system for compliance?
- [ ] Multi-org: Can org admins see/delete other organizations' archived exercises?
- [ ] Should bulk operations be transactional (all-or-nothing) or allow partial success?
