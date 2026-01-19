# Feature: Exercise Lifecycle Management

**Parent Epic:** Exercise Management

## Description

Exercise Directors and Administrators can efficiently manage exercise lifecycle beyond the standard status workflow—archiving completed or abandoned exercises to declutter active views, and permanently deleting exercises when appropriate, with proper safeguards to prevent accidental data loss.

## Business Value

- Declutter exercise lists without losing historical data
- Provide "undo" capability through archive/restore pattern
- Enable cleanup of test/draft exercises that were never used
- Maintain audit trail for compliance
- Reduce storage costs by allowing permanent deletion of obsolete data

## User Personas

| Persona | Description | Key Needs |
|---------|-------------|-----------|
| Exercise Director | Manages exercise lifecycle | Quick archive without workflow overhead |
| Administrator | System-wide management | Permanent delete capability, bulk operations |
| Exercise Creator | Person who created the exercise | Delete own draft exercises |

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| S01 | Lifecycle Tracking Fields | P0 | 🔲 Ready |
| S02 | Archive Exercise | P0 | 🔲 Ready |
| S03 | View Archived Exercises | P0 | 🔲 Ready |
| S04 | Restore Exercise | P0 | 🔲 Ready |
| S05 | Delete Draft Exercise | P0 | 🔲 Ready |
| S06 | Delete Archived Exercise | P1 | 🔲 Ready |
| S07 | Admin Archive Management Page | P1 | 🔲 Ready |

## Permission Summary

| Action | Who Can Do It |
|--------|---------------|
| Archive any exercise | Exercise Director, Administrator |
| View archived exercises | Administrator |
| Restore archived exercise | Administrator |
| Delete draft (never published) | Creator OR Administrator |
| Permanently delete archived | Administrator |
| Delete published/active/completed | **No one** (must archive first) |

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

## Domain Terms

| Term | Definition |
|------|------------|
| Archive | Soft delete that hides exercise from normal views but preserves all data |
| Restore | Return archived exercise to its previous status |
| Permanent Delete | Irreversible removal of exercise and all related data |
| Never Published | Exercise where `HasBeenPublished` flag is false (eligible for creator delete) |
| Creator | User whose ID matches `CreatedById` on the exercise |

## Acceptance Criteria (Feature-Level)

- [ ] Exercises can be archived from any status without workflow steps
- [ ] Archived exercises are hidden from default views
- [ ] Administrators can view, restore, or permanently delete archived exercises
- [ ] Draft exercises (never published) can be deleted by creator or admin
- [ ] All archive/restore/delete actions create audit records
- [ ] Permanent deletion cascades to all related data

## Dependencies

- Exercise CRUD (Phase B) ✅
- User authentication and roles (Phase J)

## Open Questions

- [ ] Should there be a retention policy (auto-delete archived after X days)?
- [ ] Should archived exercises count against any quotas?
- [ ] Should exercise deletion be audited to an external system?
- [ ] Multi-org: Can org admins see/delete other org's archived exercises?
