# Story: S04 - Delete Inject

## User Story

**As an** Administrator or Exercise Director,
**I want** to delete injects from the MSEL,
**So that** I can remove injects that are no longer needed.

## Context

During MSEL development, injects may need to be removed - perhaps they're duplicates, no longer relevant to the scenario, or were created in error. Deletion should be straightforward but protected by confirmation to prevent accidental loss.

## Acceptance Criteria

### Basic Deletion
- [ ] **Given** I am viewing an inject detail, **when** I click "Delete", **then** I see a confirmation dialog
- [ ] **Given** I see the confirmation dialog, **when** I view it, **then** I see the inject number and title for verification
- [ ] **Given** I confirm deletion, **when** the operation completes, **then** the inject is removed and I'm returned to the MSEL list
- [ ] **Given** I cancel deletion, **when** the dialog closes, **then** the inject remains unchanged

### Bulk Deletion
- [ ] **Given** I select multiple injects in the MSEL list, **when** I click "Delete Selected" from bulk actions, **then** I see a confirmation dialog
- [ ] **Given** I am confirming bulk delete, **when** I view the dialog, **then** I see the count and list of inject titles
- [ ] **Given** I confirm bulk deletion, **when** the operation completes, **then** all selected injects are removed

### Exercise Status Restrictions
- [ ] **Given** the exercise is in Draft status, **when** I try to delete an inject, **then** deletion is allowed
- [ ] **Given** the exercise is in Active status, **when** I try to delete a Pending inject, **then** I see a warning but deletion is allowed
- [ ] **Given** the exercise is Active and the inject has been Fired, **when** I try to delete, **then** deletion is blocked with explanation
- [ ] **Given** the exercise is Archived, **when** I try to delete, **then** deletion is blocked

### Role Restrictions
- [ ] **Given** I am an Administrator or Exercise Director, **when** I view delete options, **then** I can delete any inject (subject to status restrictions)
- [ ] **Given** I am a Controller, **when** I view delete options, **then** I can only delete injects I created (in Draft exercises)
- [ ] **Given** I am an Evaluator or Observer, **when** I view inject detail, **then** I do not see a Delete option

### Side Effects
- [ ] **Given** an inject has linked objectives, **when** I delete it, **then** the objective links are removed (objectives remain)
- [ ] **Given** an inject is assigned to a phase, **when** I delete it, **then** the phase is updated (inject count decreases)
- [ ] **Given** an inject has observations recorded, **when** I try to delete, **then** deletion is blocked (preserve evaluation data)

## Out of Scope

- Soft delete / recycle bin functionality
- Undo deletion
- Cascading delete of related records
- Archive inject (instead of delete)

## Dependencies

- inject-crud/S01: Create Inject (inject must exist)
- exercise-objectives/S03: Link Objective (links must be handled)
- exercise-conduct: Observations (prevent deletion if observations exist)

## Open Questions

- [ ] Should we implement soft delete for recovery?
- [ ] How long should deleted inject audit records be retained?
- [ ] Should bulk delete have a maximum count?

## Domain Terms

| Term | Definition |
|------|------------|
| Hard Delete | Permanent removal of inject from database |
| Soft Delete | Marking inject as deleted but retaining data |
| Bulk Delete | Deleting multiple injects in a single operation |

## UI/UX Notes

### Single Delete Confirmation

```
┌─────────────────────────────────────────────────────────────┐
│  🗑️ Delete Inject?                                      ✕   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Are you sure you want to delete this inject?               │
│                                                             │
│  INJ-003: County issues mandatory evacuation order          │
│                                                             │
│  This action cannot be undone.                              │
│                                                             │
│                          [Cancel]  [Delete Inject]          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Bulk Delete Confirmation

```
┌─────────────────────────────────────────────────────────────┐
│  🗑️ Delete 5 Injects?                                   ✕   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Are you sure you want to delete these injects?             │
│                                                             │
│  • INJ-012: Weather update briefing                         │
│  • INJ-013: Resource status check                           │
│  • INJ-014: Shift change notification                       │
│  • INJ-015: Duplicate shelter report                        │
│  • INJ-016: Test inject - please delete                     │
│                                                             │
│  This action cannot be undone.                              │
│                                                             │
│                          [Cancel]  [Delete 5 Injects]       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Delete Blocked (Fired Inject)

```
┌─────────────────────────────────────────────────────────────┐
│  ⚠️ Cannot Delete Inject                                ✕   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  This inject cannot be deleted because it has already       │
│  been fired during exercise conduct.                        │
│                                                             │
│  INJ-003: County issues mandatory evacuation order          │
│  Fired at: 09:32 AM on January 15, 2025                    │
│                                                             │
│  Fired injects are preserved for after-action review.       │
│                                                             │
│                                            [OK]             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Delete Blocked (Has Observations)

```
┌─────────────────────────────────────────────────────────────┐
│  ⚠️ Cannot Delete Inject                                ✕   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  This inject cannot be deleted because it has               │
│  2 observations recorded against it.                        │
│                                                             │
│  Injects with evaluation data must be preserved.            │
│                                                             │
│                                            [OK]             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Consider soft delete pattern for future undo capability
- Log deletions to audit trail before removing
- Bulk delete should use transaction for atomicity
- Re-sequence inject numbers after deletion (or leave gaps)
