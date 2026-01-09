# Story: S03 - Reorder Injects

## User Story

**As an** Administrator or Exercise Director,
**I want** to manually reorder injects by dragging them,
**So that** I can adjust the MSEL sequence to match my desired inject order.

## Context

While injects have scheduled times that determine when they're delivered, there are scenarios where manual ordering is useful: reorganizing for logical flow, adjusting sequence after bulk import, or preparing for time-compressed exercises. Drag-and-drop reordering provides an intuitive way to rearrange injects.

Note: Reordering affects the display sequence (inject numbers) but does not change scheduled times. Users must manually adjust times if the new order requires different delivery times.

## Acceptance Criteria

### Drag-and-Drop Basics
- [ ] **Given** I am viewing the MSEL in flat list view (no grouping), **when** I hover over an inject row, **then** I see a drag handle (≡)
- [ ] **Given** I grab the drag handle, **when** I drag the row, **then** a visual indicator shows the inject moving
- [ ] **Given** I am dragging an inject, **when** I hover between other rows, **then** I see a drop indicator line
- [ ] **Given** I release the inject, **when** it drops, **then** it moves to the new position

### Inject Number Update
- [ ] **Given** I move inject #5 to position 2, **when** the move completes, **then** inject numbers update: old #2 becomes #3, old #3 becomes #4, etc.
- [ ] **Given** inject numbers update, **when** I view the list, **then** numbers are sequential with no gaps

### Multi-Select Reorder
- [ ] **Given** I select multiple injects (checkboxes), **when** I drag one of them, **then** all selected injects move together
- [ ] **Given** I move 3 selected injects, **when** they drop, **then** they maintain their relative order

### Restrictions
- [ ] **Given** grouping is active, **when** I view the list, **then** drag handles are hidden (reorder disabled)
- [ ] **Given** a sort other than inject # is active, **when** I try to drag, **then** I see a tooltip explaining sort must be by inject #
- [ ] **Given** the exercise is Active (during conduct), **when** I try to drag fired injects, **then** they cannot be moved
- [ ] **Given** I am a Controller, **when** I view the MSEL, **then** I cannot reorder (view-only)
- [ ] **Given** I am an Evaluator or Observer, **when** I view the MSEL, **then** drag handles are not shown

### Undo
- [ ] **Given** I complete a reorder, **when** the operation finishes, **then** I see an "Undo" option (toast notification)
- [ ] **Given** I click Undo within 10 seconds, **when** undo executes, **then** injects return to previous positions

### Persistence
- [ ] **Given** I reorder injects, **when** I refresh the page, **then** the new order persists
- [ ] **Given** I reorder injects, **when** another user views the MSEL, **then** they see the new order

## Out of Scope

- Drag between groups (move inject to different phase)
- Keyboard-based reordering
- Reorder with automatic time adjustment
- Undo beyond the most recent action

## Dependencies

- inject-crud/S01: Create Inject (injects must exist)
- inject-organization/S01: Sort Injects (reorder disabled when custom sort active)
- inject-organization/S02: Group Injects (reorder disabled when grouped)

## Open Questions

- [ ] Should reorder be available during active exercise conduct?
- [ ] Should we offer "auto-number" to reset sequence after manual changes?
- [ ] Should reordering trigger a warning about scheduled time misalignment?

## Domain Terms

| Term | Definition |
|------|------------|
| Reorder | Changing the sequence/position of injects in the MSEL |
| Drag Handle | UI element (≡) indicating a row can be dragged |
| Drop Indicator | Visual line showing where a dragged item will land |
| Inject Number | Sequential identifier that updates when injects are reordered |

## UI/UX Notes

### Drag Handle and Hover State

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  #  │ Scheduled  │ Title                           │ Status │             │
│ ────┼────────────┼─────────────────────────────────┼────────┼──────────── │
│≡  1 │ 09:00 AM   │ Hurricane warning issued        │ Pending│ •••        │
│≡  2 │ 09:15 AM   │ EOC activation ordered          │ Pending│ •••        │  
│≡  3 │ 09:30 AM   │ Evacuation order issued         │ Pending│ •••  ← hover│
│≡  4 │ 09:45 AM   │ Shelter activation              │ Pending│ •••        │
└─────────────────────────────────────────────────────────────────────────────┘

≡ = Drag handle (visible on hover or always, based on preference)
```

### During Drag

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  #  │ Scheduled  │ Title                           │ Status │             │
│ ────┼────────────┼─────────────────────────────────┼────────┼──────────── │
│≡  1 │ 09:00 AM   │ Hurricane warning issued        │ Pending│             │
│ ───────────────────────────────────────────────────────────── drop here ── │
│≡  2 │ 09:15 AM   │ EOC activation ordered          │ Pending│             │
│≡  3 │ 09:30 AM   │ Evacuation order issued         │ Pending│             │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │≡  4 │ 09:45 AM   │ Shelter activation          │ Pending│           │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│  ↑ Dragging inject #4                                                      │
└─────────────────────────────────────────────────────────────────────────────┘

Blue line indicates drop position
Dragged row appears "lifted" with shadow
```

### After Drop - Numbers Updated

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  #  │ Scheduled  │ Title                           │ Status │             │
│ ────┼────────────┼─────────────────────────────────┼────────┼──────────── │
│≡  1 │ 09:00 AM   │ Hurricane warning issued        │ Pending│             │
│≡  2 │ 09:45 AM   │ Shelter activation ← moved here │ Pending│ ✓ highlight │
│≡  3 │ 09:15 AM   │ EOC activation ordered          │ Pending│             │
│≡  4 │ 09:30 AM   │ Evacuation order issued         │ Pending│             │
└─────────────────────────────────────────────────────────────────────────────┘

⚠️ Note: Scheduled times now out of sequence. 
   Consider adjusting times to match new order.

Toast: "Inject reordered" [Undo]
```

### Reorder Disabled State

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Reorder is disabled when:                                                 │
│  • Grouping is active (Group by: None to enable)                          │
│  • Sorted by column other than Inject # (Sort by: # to enable)            │
│  • You don't have edit permissions                                         │
└─────────────────────────────────────────────────────────────────────────────┘

Tooltip on disabled drag handle:
"Reorder is disabled. Remove grouping and sort by Inject # to enable."
```

## Technical Notes

- Use react-beautiful-dnd or similar library for drag-and-drop
- Update SortOrder column (not inject number) for efficiency
- Batch update all affected rows in single transaction
- Implement optimistic UI with rollback on error
- Consider debouncing for rapid reorder operations
