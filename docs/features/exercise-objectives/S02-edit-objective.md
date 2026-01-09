# Story: S02 - Edit Exercise Objective

## User Story

**As an** Administrator or Exercise Director,
**I want** to edit existing objectives,
**So that** I can refine objective language and correct errors before exercise conduct.

## Context

Objectives may need refinement as exercise planning progresses. This story covers editing objective properties including number, name, and description. Editing maintains existing inject linkages unless the objective is deleted.

## Acceptance Criteria

- [ ] **Given** I am viewing the Objectives list, **when** I click on an objective row, **then** I see the objective details in an edit form
- [ ] **Given** I am viewing an objective, **when** I click an "Edit" button or icon, **then** the fields become editable
- [ ] **Given** I am editing an objective, **when** I modify the Name, **then** I can save if it meets validation (3+ characters)
- [ ] **Given** I am editing an objective, **when** I modify the Objective Number to a value already in use, **then** I see a validation error on save
- [ ] **Given** I am editing an objective, **when** I clear the Name field, **then** I cannot save (Name is required)
- [ ] **Given** I have unsaved changes, **when** I click Cancel, **then** I see a confirmation dialog asking if I want to discard changes
- [ ] **Given** I confirm discarding changes, **when** the dialog closes, **then** the objective reverts to its previous state
- [ ] **Given** I save changes, **when** the save completes, **then** I see a success message and the updated values are displayed
- [ ] **Given** an objective has linked injects, **when** I edit the objective, **then** a note shows "Linked to X injects"
- [ ] **Given** an objective has linked injects, **when** I edit and save, **then** the linkages are preserved
- [ ] **Given** the exercise is in Active status (conduct started), **when** I try to edit an objective, **then** I can still edit (but see a warning)
- [ ] **Given** I am a Controller, Evaluator, or Observer, **when** I view an objective, **then** I see view-only mode with no edit option

## Out of Scope

- Bulk editing multiple objectives
- Objective versioning/history
- Merging duplicate objectives
- Reordering objectives (covered by drag-drop in list view)

## Dependencies

- exercise-objectives/S01: Create Objective (objective must exist)
- exercise-objectives/S03: Link Objective to Inject (editing preserves links)

## Open Questions

- [ ] Should editing be locked once evaluation begins on linked injects?
- [ ] Should there be an "archive" option instead of delete?

## Domain Terms

| Term | Definition |
|------|------------|
| Objective | A specific capability or outcome the exercise aims to test |
| Linked Inject | An inject that references this objective |

## UI/UX Notes

### Edit Objective View

```
┌─────────────────────────────────────────────────────────────┐
│  Objective 1                               [Edit] [Delete]  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Name                                                       │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Demonstrate EOC activation procedures               │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Description                                                │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ Test the ability of county emergency management    │   │
│  │ to activate the Emergency Operations Center...      │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ℹ️ Linked to 5 injects                                    │
│                                                             │
│                              [Cancel]  [Save Changes]       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Delete Confirmation

```
┌─────────────────────────────────────────────────────────────┐
│  ⚠️ Delete Objective?                                  ✕   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Are you sure you want to delete this objective?            │
│                                                             │
│  "Demonstrate EOC activation procedures"                    │
│                                                             │
│  ⚠️ This objective is linked to 5 injects.                 │
│     Links will be removed but injects will remain.          │
│                                                             │
│  This action cannot be undone.                              │
│                                                             │
│                        [Cancel]  [Delete Objective]         │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Implement optimistic locking to prevent concurrent edit conflicts
- Audit trail should capture objective changes
- Consider soft delete to allow recovery
