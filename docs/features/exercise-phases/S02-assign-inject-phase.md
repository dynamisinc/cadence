# Story: S02 - Assign Inject to Phase

## User Story

**As an** Administrator, Exercise Director, or Controller,
**I want** to assign injects to exercise phases,
**So that** injects are organized by the operational stage they support.

## Context

Once phases are defined, injects need to be assigned to them. An inject can belong to only one phase (or no phase). This assignment is typically done during MSEL authoring and can be done individually or in bulk. Phase assignment helps with MSEL organization, filtering, and provides context during exercise conduct.

## Acceptance Criteria

- [ ] **Given** I am creating or editing an inject, **when** I view the form, **then** I see a "Phase" dropdown field
- [ ] **Given** I click the Phase dropdown, **when** it expands, **then** I see all defined phases plus "Unassigned" option
- [ ] **Given** I select a phase, **when** I save the inject, **then** the phase assignment is persisted
- [ ] **Given** I select "Unassigned", **when** I save, **then** the inject has no phase (PhaseId = NULL)
- [ ] **Given** I am viewing the MSEL list, **when** I look at an inject row, **then** I see its phase displayed (or "—" if unassigned)
- [ ] **Given** I select multiple injects in the MSEL, **when** I click "Bulk Actions", **then** I see an option to "Set Phase"
- [ ] **Given** I use bulk "Set Phase", **when** I select a phase and confirm, **then** all selected injects are assigned to that phase
- [ ] **Given** I am filtering the MSEL by phase, **when** I select a phase filter, **then** only injects in that phase are shown
- [ ] **Given** I am filtering, **when** I select "Unassigned", **then** only injects with no phase are shown
- [ ] **Given** an inject's phase is changed, **when** I am viewing MSEL grouped by phase, **then** the inject moves to the new phase group
- [ ] **Given** I am an Evaluator or Observer, **when** I view injects, **then** I can see phases but not change them

## Out of Scope

- Automatic phase assignment based on scheduled time
- Phase assignment validation (e.g., ensuring inject times fall within phase boundaries)
- Drag-and-drop inject between phases in grouped view
- Phase-based inject ordering

## Dependencies

- exercise-phases/S01: Define Phases (phases must exist)
- inject-crud/S01: Create Inject (phase is inject property)
- inject-filtering/S01: Filter Injects (filtering by phase)
- inject-organization/S02: Group Injects (grouping by phase)

## Open Questions

- [ ] Should bulk phase assignment show a preview of affected injects?
- [ ] Should phase changes be tracked in inject history?

## Domain Terms

| Term | Definition |
|------|------------|
| Phase Assignment | The relationship between an inject and its assigned phase |
| Unassigned | An inject that does not belong to any phase |
| Bulk Action | Operation applied to multiple selected items at once |

## UI/UX Notes

### Phase Selection in Inject Form

```
┌─────────────────────────────────────────────────────────────┐
│  Phase                                                      │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 1. Initial Response                              ▼  │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Dropdown options:                                          │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ — Unassigned                                        │   │
│  │ 1. Initial Response                                 │   │
│  │ 2. Sustained Operations                             │   │
│  │ 3. Recovery                                         │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Bulk Phase Assignment

```
┌─────────────────────────────────────────────────────────────┐
│  Set Phase for 5 Injects                                ✕  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Select Phase:                                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ 2. Sustained Operations                          ▼  │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Selected injects:                                          │
│  • INJ-015: Shelter capacity exceeded                      │
│  • INJ-016: Additional resources requested                 │
│  • INJ-017: Media inquiry received                         │
│  • INJ-018: Volunteer coordination needed                  │
│  • INJ-019: Supply chain disruption reported               │
│                                                             │
│                     [Cancel]  [Set Phase]                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### MSEL List with Phase Column

```
┌──────────────────────────────────────────────────────────────────────────┐
│  ☐ │ # │ Scheduled │ Inject Title              │ Phase           │ ...  │
│ ───┼───┼───────────┼───────────────────────────┼─────────────────┼───── │
│  ☐ │ 1 │ 09:00 AM  │ Hurricane warning issued  │ Initial Response│      │
│  ☐ │ 2 │ 09:15 AM  │ EOC activation ordered    │ Initial Response│      │
│  ☐ │ 3 │ 12:00 PM  │ Shelter capacity exceeded │ Sustained Ops   │      │
│  ☐ │ 4 │ 12:30 PM  │ Supply issue noted        │ —               │      │
└──────────────────────────────────────────────────────────────────────────┘

— = Unassigned phase
```

## Technical Notes

- PhaseId is nullable foreign key on Inject table
- Bulk updates should be transactional
- Consider optimistic concurrency for bulk operations
