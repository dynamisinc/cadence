# Story: S01 - Select MSEL Version

## User Story

**As an** Administrator or Exercise Director,
**I want** to understand which MSEL is active for my exercise,
**So that** I know which injects will be used during conduct.

## Context

In Cadence's MVP, each exercise has exactly one MSEL (created automatically with the exercise). This story documents the simplified model and provides the foundation for future version management.

While full MSEL versioning is deferred to a later phase, this story ensures users understand the current MSEL state and can see basic metadata about it.

## Acceptance Criteria

- [ ] **Given** I create a new exercise, **when** creation completes, **then** a MSEL is automatically created and associated with the exercise
- [ ] **Given** I view an exercise, **when** I look at the MSEL section, **then** I see MSEL metadata (inject count, last modified date)
- [ ] **Given** I view MSEL metadata, **when** I look at the details, **then** I see: Total Injects, Last Modified, Modified By
- [ ] **Given** I click "View MSEL", **when** the view loads, **then** I see the inject list for this exercise's MSEL
- [ ] **Given** the exercise is in Draft status, **when** I view the MSEL, **then** I can add, edit, and delete injects
- [ ] **Given** the exercise is in Active status (during conduct), **when** I view the MSEL, **then** I can fire injects but not add/edit/delete
- [ ] **Given** the exercise is Archived, **when** I view the MSEL, **then** it is read-only
- [ ] **Given** I am a Controller, **when** I view the MSEL, **then** I see the active MSEL for the exercise

## Out of Scope (Deferred to Standard/Advanced Phase)

- Multiple MSEL versions per exercise
- Version selection dropdown
- Version comparison/diff
- Version rollback
- Branching and merging MSELs
- Version naming and descriptions

## Dependencies

- exercise-crud/S01: Create Exercise (triggers MSEL creation)
- inject-crud/S01: Create Inject (adds to MSEL)
- exercise-conduct: Exercise Conduct (uses MSEL)

## Open Questions

- [ ] Should MSEL have a separate name from the exercise?
- [ ] Should there be a "MSEL Overview" dashboard showing summary stats?
- [ ] When should we introduce formal versioning?

## Domain Terms

| Term | Definition |
|------|------------|
| MSEL | Master Scenario Events List - the collection of injects for an exercise |
| Active MSEL | The MSEL used during exercise conduct (in MVP, the only MSEL) |
| MSEL Metadata | Summary information about the MSEL (count, dates, etc.) |

## UI/UX Notes

### Exercise Overview - MSEL Section

```
┌─────────────────────────────────────────────────────────────┐
│  MSEL                                         [View MSEL]   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Total Injects:     43                                      │
│  Last Modified:     Jan 8, 2025 at 2:34 PM                  │
│  Modified By:       James Washington                        │
│                                                             │
│  Status Breakdown:                                          │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ ████████████████████░░░░░ │ 85% Complete            │   │
│  │ 37 Ready │ 4 Draft │ 2 Review                       │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Future: Version Selector (Standard Phase)

```
┌─────────────────────────────────────────────────────────────┐
│  MSEL Version                                               │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ v3 - Final (Active)                              ▼  │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Version History:                                           │
│  • v3 - Final (Active) - Jan 8, 2025                       │
│  • v2 - After SME Review - Jan 5, 2025                     │
│  • v1 - Initial Draft - Jan 2, 2025                        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Technical Notes

- MSEL is a logical concept; it's the collection of Inject records for an Exercise
- Consider adding Version table for future expansion
- MSEL metadata can be computed from Inject aggregate queries
