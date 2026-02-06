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

### Validation on Edit

- [ ] **Given** I edit an entry, **when** I remove the observation text, **then** I see validation error (required)
- [ ] **Given** I edit an entry, **when** I exceed 2000 characters, **then** I see validation error
- [ ] **Given** I change the Critical Task, **when** saving, **then** the Capability Target updates via the task's parent relationship

### Linked Inject on Edit

- [ ] **Given** I edit an entry with a linked inject, **when** I select a different inject, **then** the new inject reference is saved
- [ ] **Given** I edit an entry with a linked inject, **when** I clear the inject field, **then** the entry no longer has a triggering inject

### Edit Audit Trail

- [ ] **Given** an entry is edited, **when** saved, **then** UpdatedAt and UpdatedBy are recorded
- [ ] **Given** an entry is edited, **when** viewed, **then** I can see it was edited (indicator)
- [ ] **Given** the entry detail, **when** entry was edited, **then** I see "Edited by [Name] at [Time]"

### Concurrent Editing

- [ ] **Given** another user is editing the same entry, **when** I save my changes, **then** I see a conflict notification
- [ ] **Given** a conflict occurs, **when** displayed, **then** I can choose to overwrite, reload, or cancel

### Delete Entry

- [ ] **Given** I am viewing an EEG entry I created, **when** I click Delete, **then** I see confirmation dialog
- [ ] **Given** I am a Director+, **when** I view any entry, **then** I can delete it
- [ ] **Given** I am an Evaluator, **when** I view another's entry, **then** I cannot delete it
- [ ] **Given** the delete confirmation, **when** displayed, **then** I see the entry summary
- [ ] **Given** I confirm delete, **when** processed, **then** entry is removed and list updates
- [ ] **Given** I cancel delete, **when** dialog closes, **then** entry remains

### Exercise Status Restrictions

- [ ] **Given** the exercise is in Draft status, **when** I edit/delete entries, **then** operations are allowed without warnings
- [ ] **Given** the exercise is Active, **when** I edit/delete my entries, **then** operations are allowed
- [ ] **Given** the exercise is Completed, **when** I try to edit my entry, **then** I see warning about completed exercise
- [ ] **Given** the exercise is Completed, **when** I am Director+, **then** I can still edit with acknowledgment
- [ ] **Given** the exercise is Archived, **when** I try to edit, **then** operation is blocked (read-only)

### Offline Support

- [ ] **Given** I am offline, **when** I edit an entry, **then** changes queue for sync
- [ ] **Given** I am offline, **when** I delete an entry, **then** delete queues for sync
- [ ] **Given** offline edits conflict with server, **when** sync runs, **then** conflict resolution applies
- [ ] **Given** I come online, **when** sync completes, **then** changes are reflected

## Permission Matrix

| Action | Admin | Director | Controller | Evaluator | Observer |
|--------|-------|----------|------------|-----------|----------|
| Edit own entry | ✅ | ✅ | ✅* | ✅ | ❌ |
| Edit others' entry | ✅ | ✅ | ❌ | ❌ | ❌ |
| Delete own entry | ✅ | ✅ | ✅* | ✅ | ❌ |
| Delete others' entry | ✅ | ✅ | ❌ | ❌ | ❌ |
| Edit in Completed exercise | ✅ | ✅** | ❌ | ❌ | ❌ |

*Controllers can only edit/delete if they also have Evaluator permissions (can create entries)
**With warning acknowledgment

## API Specification

### PUT /api/eeg-entries/{entryId}

**Request Body:**
```json
{
  "criticalTaskId": "guid",
  "observationText": "string (1-2000 chars)",
  "rating": "P|S|M|U",
  "observedAt": "datetime",
  "triggeringInjectId": "guid|null"
}
```

**Response 200:**
```json
{
  "id": "guid",
  "criticalTaskId": "guid",
  "criticalTaskDescription": "string",
  "capabilityTargetId": "guid",
  "capabilityTargetDescription": "string",
  "observationText": "string",
  "rating": "P",
  "observedAt": "2026-02-03T10:45:00Z",
  "recordedAt": "2026-02-03T10:47:00Z",
  "updatedAt": "2026-02-03T11:30:00Z",
  "wasEdited": true,
  "updatedBy": {
    "id": "guid",
    "name": "string"
  },
  "evaluator": {
    "id": "guid",
    "name": "string"
  },
  "triggeringInject": {
    "id": "guid",
    "injectNumber": "INJ-003",
    "title": "string"
  }
}
```

**Response 400:** Validation error
```json
{
  "errors": {
    "observationText": ["Observation text is required"],
    "observationText": ["Observation text cannot exceed 2000 characters"]
  }
}
```

**Response 401:** Unauthorized
**Response 403:** Not authorized to edit this entry
**Response 404:** Entry not found
**Response 409:** Concurrent edit conflict (optimistic concurrency)
```json
{
  "error": "ConcurrencyConflict",
  "message": "This entry was modified by another user",
  "serverVersion": "datetime"
}
```

### DELETE /api/eeg-entries/{entryId}

**Response 204:** Successfully deleted
**Response 401:** Unauthorized
**Response 403:** Not authorized to delete this entry
**Response 404:** Entry not found

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
│  Critical Task *                                                        │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Activate emergency communication plan                        ▼  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Observation *                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ EOC issued activation notification at 09:15. All stakeholders   │   │
│  │ confirmed receipt within 10 minutes. Communication plan         │   │
│  │ followed correctly per SOP 5.2.                                 │   │
│  │                                                                  │   │
│  │ [ADDED: Note about Field Unit 3 delay]                          │   │
│  │ Minor delay in reaching Field Unit 3 due to radio interference. │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                      1,247 / 2,000     │
│                                                                         │
│  Performance Rating *                                                   │
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

### Conflict Resolution Dialog

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Conflict Detected                                              [X]    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ⚠️ This entry was modified by Sarah Kim while you were editing.       │
│                                                                         │
│  Their changes: Rating changed from P to S                              │
│  Last modified: 11:32 AM                                                │
│                                                                         │
│  What would you like to do?                                             │
│                                                                         │
│  [Overwrite Their Changes]  [Reload Their Version]  [Cancel]           │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Out of Scope

- Edit history / version tracking (future enhancement)
- Bulk edit operations (future enhancement)
- Bulk delete operations (future enhancement)
- Entry locking by Admin (future enhancement)
- Undo delete (future enhancement)

## Dependencies

- S06: EEG Entry creation (reuse form components)
- S07: View EEG Entries (edit/delete accessed from list/detail)
- Exercise status workflow

## Technical Notes

- Reuse EEG Entry form component from S06 for edit
- Track UpdatedAt and UpdatedBy for audit
- Use optimistic concurrency with ETag or version field for conflict detection
- Consider soft delete for recovery capability (future)
- Offline deletes need careful sync handling — mark as "pending delete" locally

## Test Scenarios

### Unit Tests
- Edit form populates with existing values
- Validation on edit (same as create)
- Delete confirmation renders correctly
- Character counter updates on text change
- Conflict dialog shows correctly

### Integration Tests
- Edit entry updates in database
- Delete entry removes from database
- Permission checks for own vs. others' entries
- Completed exercise restrictions work
- Offline edit/delete syncs correctly
- Edited indicator shows on entry
- Concurrent edit conflict detection works
- UpdatedBy tracks the editor, not original author

---

*Story created: 2026-02-03*
*Revised: 2026-02-05 — Added API spec, concurrent editing, validation details*
