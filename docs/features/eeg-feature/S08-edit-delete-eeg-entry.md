# S08: Edit and Delete EEG Entry

**Feature:** Exercise Evaluation Guide (EEG)
**Priority:** P1
**Status:** Not Started
**Points:** 3

## User Story

**As an** Evaluator,
**I want** to edit or delete my EEG entries,
**So that** I can correct mistakes or refine my assessments during or after exercise conduct.

## Context

Evaluators may need to modify entries for various reasons:
- Correcting typos or unclear observations
- Updating a rating based on additional information
- Removing duplicate or erroneous entries
- Refining observations during post-exercise review

This story covers the edit and delete workflows for EEG entries.

## Acceptance Criteria

### Edit Entry

- [ ] **Given** I am viewing an EEG entry I created, **when** I click Edit, **then** I see the edit form
- [ ] **Given** I am a Director+, **when** I view any entry, **then** I can edit it
- [ ] **Given** I am an Evaluator, **when** I view another's entry, **then** I cannot edit it
- [ ] **Given** the edit form, **when** displayed, **then** all fields are editable (Task, Observation, Rating, Inject)
- [ ] **Given** I change the Critical Task, **when** saving, **then** the Capability Target updates accordingly
- [ ] **Given** I save edits, **when** successful, **then** I see the updated entry
- [ ] **Given** I cancel edits, **when** there are unsaved changes, **then** I see discard confirmation

### Edit Audit Trail

- [ ] **Given** an entry is edited, **when** saved, **then** UpdatedAt timestamp is recorded
- [ ] **Given** an entry is edited, **when** viewed, **then** I can see it was edited (indicator)
- [ ] **Given** the entry detail, **when** entry was edited, **then** I see "Edited" with timestamp

### Delete Entry

- [ ] **Given** I am viewing an EEG entry I created, **when** I click Delete, **then** I see confirmation dialog
- [ ] **Given** I am a Director+, **when** I view any entry, **then** I can delete it
- [ ] **Given** I am an Evaluator, **when** I view another's entry, **then** I cannot delete it
- [ ] **Given** the delete confirmation, **when** displayed, **then** I see the entry summary
- [ ] **Given** I confirm delete, **when** processed, **then** entry is removed and list updates
- [ ] **Given** I cancel delete, **when** dialog closes, **then** entry remains

### Exercise Status Restrictions

- [ ] **Given** the exercise is Active, **when** I edit/delete my entries, **then** operations are allowed
- [ ] **Given** the exercise is Completed, **when** I try to edit my entry, **then** I see warning about completed exercise
- [ ] **Given** the exercise is Completed, **when** I am Director+, **then** I can still edit with acknowledgment
- [ ] **Given** the exercise is Archived, **when** I try to edit, **then** operation is blocked (read-only)

### Offline Support

- [ ] **Given** I am offline, **when** I edit an entry, **then** changes queue for sync
- [ ] **Given** I am offline, **when** I delete an entry, **then** delete queues for sync
- [ ] **Given** offline edits conflict with server, **when** sync runs, **then** conflict resolution applies
- [ ] **Given** I come online, **when** sync completes, **then** changes are reflected

## Wireframes

### Edit Form

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Edit EEG Entry                                                 [X]    │
│  Originally recorded: 10:45 by Robert Chen                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Capability Target                                                      │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Operational Communications                                   ▼  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Critical Task                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Activate emergency communication plan                        ▼  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Observation                                                            │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ EOC issued activation notification at 09:15. All stakeholders   │   │
│  │ confirmed receipt within 10 minutes. Communication plan         │   │
│  │ followed correctly per SOP 5.2.                                 │   │
│  │                                                                  │   │
│  │ [EDITED: Added note about Field Unit 3 delay]                   │   │
│  │ Minor delay in reaching Field Unit 3 due to radio interference. │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Performance Rating                                                     │
│  [○ P] [● S] [○ M] [○ U]                                               │
│                                                                         │
│  Triggered by Inject                                                    │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ INJ-003: EOC Activation Notice                               ▼  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│                                          [Cancel]  [Save Changes]       │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Delete Confirmation

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Delete EEG Entry?                                              [X]    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Are you sure you want to delete this EEG entry?                       │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │ Task: Activate emergency communication plan                       │ │
│  │ Rating: S - Performed with Some Challenges                        │ │
│  │ Recorded: 10:45 by Robert Chen                                    │ │
│  │                                                                   │ │
│  │ "EOC issued activation notification at 09:15..."                  │ │
│  └───────────────────────────────────────────────────────────────────┘ │
│                                                                         │
│  This action cannot be undone.                                          │
│                                                                         │
│                                          [Cancel]  [Delete Entry]       │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Completed Exercise Warning

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Edit Entry in Completed Exercise                               [X]    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ⚠️ This exercise has been marked as Completed.                        │
│                                                                         │
│  Editing entries after completion may affect:                           │
│  • After-Action Report accuracy                                         │
│  • Historical metrics and analysis                                      │
│                                                                         │
│  Are you sure you want to edit this entry?                             │
│                                                                         │
│                               [Cancel]  [Edit Anyway]                   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Permission Matrix

| Action | Admin | Director | Controller | Evaluator | Observer |
|--------|:-----:|:--------:|:----------:|:---------:|:--------:|
| Edit own entry | ✅ | ✅ | ✅ | ✅ | ❌ |
| Edit others' entry | ✅ | ✅ | ❌ | ❌ | ❌ |
| Delete own entry | ✅ | ✅ | ✅ | ✅ | ❌ |
| Delete others' entry | ✅ | ✅ | ❌ | ❌ | ❌ |
| Edit in Completed exercise | ✅ | ✅* | ❌ | ❌ | ❌ |

*With warning acknowledgment

## Out of Scope

- Edit history / version tracking (future enhancement)
- Bulk edit operations (future enhancement)
- Entry locking by Admin (future enhancement)
- Undo delete (future enhancement)

## Dependencies

- S06: EEG Entry creation (reuse form components)
- S07: View EEG Entries (edit/delete accessed from list/detail)
- Exercise status workflow

## Technical Notes

- Reuse EEG Entry form component from S06 for edit
- Track UpdatedAt and UpdatedBy for audit
- Consider soft delete for recovery capability (future)
- Offline deletes need careful sync handling

## Test Scenarios

### Unit Tests
- Edit form populates with existing values
- Validation on edit (same as create)
- Delete confirmation renders correctly

### Integration Tests
- Edit entry updates in database
- Delete entry removes from database
- Permission checks for own vs. others' entries
- Completed exercise restrictions work
- Offline edit/delete syncs correctly
- Edited indicator shows on entry

---

*Story created: 2026-02-03*
