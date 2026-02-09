# Story: S02 - Preview and Validate Import

## User Story

**As an** Exercise Director or OrgManager,
**I want** to preview the upload results before committing,
**So that** I can review who will be assigned, invited, or flagged as errors before any changes are made.

## Context

After uploading and parsing a participant file (S01), the system must show the user exactly what will happen for each row before making any changes. Each row is classified into one of four scenarios based on the participant's current relationship with the platform and organization. This preview step prevents costly mistakes and follows the same pattern as the MSEL Excel import (validate before commit).

The four participant classifications are:
1. **Assign** - Existing org member, not yet in this exercise (immediate assignment)
2. **Update** - Already assigned to this exercise (role update or no change)
3. **Invite** - Existing Cadence user or new user, not in this organization (requires org invitation)
4. **Error** - Invalid data that cannot be processed

## Acceptance Criteria

### Classification Display

- [ ] **Given** the file has been parsed, **when** the preview screen loads, **then** each row shows its classification: Assign, Update, Invite, or Error
- [ ] **Given** a row's email matches an existing org member not in this exercise, **when** previewing, **then** it is classified as "Assign" with a green indicator
- [ ] **Given** a row's email matches a user already assigned to this exercise with the same role, **when** previewing, **then** it is classified as "Update - No Change" with a gray indicator
- [ ] **Given** a row's email matches a user already assigned to this exercise with a different role, **when** previewing, **then** it is classified as "Update - Role Change" with a yellow indicator showing old and new roles
- [ ] **Given** a row's email matches an existing Cadence user not in this org, **when** previewing, **then** it is classified as "Invite" with a blue indicator
- [ ] **Given** a row's email matches no existing Cadence user, **when** previewing, **then** it is classified as "Invite (New Account)" with a blue indicator
- [ ] **Given** a row has validation errors, **when** previewing, **then** it is classified as "Error" with a red indicator and the specific error reason

### Summary Counts

- [ ] **Given** the preview is displayed, **when** I view the summary header, **then** I see counts for each classification: "N to assign, N to update, N to invite, N errors"
- [ ] **Given** there are zero errors, **when** viewing the summary, **then** the error count is hidden
- [ ] **Given** all rows are errors, **when** viewing the summary, **then** the "Confirm Import" button is disabled

### Filtering and Review

- [ ] **Given** the preview is displayed, **when** I click a classification filter (Assign, Update, Invite, Error), **then** only rows of that classification are shown
- [ ] **Given** the preview is displayed, **when** I view a row, **then** I see: Email, Display Name, Exercise Role, Classification, and any notes/warnings
- [ ] **Given** a row is classified as "Invite", **when** I view its details, **then** I see the Organization Role that will be assigned (OrgUser by default)

### Warnings

- [ ] **Given** a row assigns the Exercise Director role, **when** the user's System Role cannot be verified (new user), **then** a warning displays: "Exercise Director role will be validated when the user accepts their invitation"
- [ ] **Given** a row assigns Exercise Director to an existing user whose System Role is User (not Admin/Manager), **when** previewing, **then** the row is flagged as an error: "Exercise Director requires Admin or Manager system role"
- [ ] **Given** the exercise has no Exercise Director assigned and none is included in the import, **when** previewing, **then** a warning banner displays: "This exercise has no Exercise Director"
- [ ] **Given** a row's email has a pending org invitation already, **when** previewing, **then** it shows: "Existing pending invitation will be updated with exercise assignment"

### Commit or Cancel

- [ ] **Given** the preview is displayed, **when** I click "Cancel", **then** no changes are made and I return to the participants screen
- [ ] **Given** the preview has at least one non-error row, **when** I click "Confirm Import", **then** processing begins (S03, S04)
- [ ] **Given** I click "Confirm Import", **when** processing starts, **then** I see a progress indicator and cannot navigate away until processing completes

## Out of Scope

- Editing individual rows in the preview (must re-upload corrected file)
- Removing individual error rows from the preview
- Saving draft imports for later review

## Dependencies

- bulk-participant-import/S01: Upload Participant File (provides parsed data)
- Organization membership data (for classification logic)
- ExerciseParticipant data (for duplicate detection)

## Open Questions

- [ ] Should we allow partial commits (only non-error rows) or require a clean file?
- [ ] Should the preview support pagination for large uploads, or show all rows?
- [ ] Should there be a "Download Errors" button to export just the error rows for correction?

## Domain Terms

| Term | Definition |
|------|------------|
| **Classification** | The system's determination of what action is needed for each row |
| **Pending Invitation** | An organization invitation that has been sent but not yet accepted |
| **Partial Commit** | Processing only the valid rows while skipping errors |

## UI/UX Notes

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Import Preview                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │ 12 to assign  │  3 to update  │  8 to invite  │  2 errors      │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  Filter: [All] [Assign] [Update] [Invite] [Errors]                     │
│                                                                         │
│  ┌─────┬───────────────────┬──────────────┬─────────────┬───────────┐ │
│  │  #  │ Email             │ Exercise Role│ Status      │ Notes     │ │
│  ├─────┼───────────────────┼──────────────┼─────────────┼───────────┤ │
│  │  1  │ jane@fema.gov     │ Controller   │ ● Assign    │           │ │
│  │  2  │ bob@state.gov     │ Evaluator    │ ● Assign    │           │ │
│  │  3  │ alice@county.gov  │ Controller   │ ● Update    │ Role:     │ │
│  │     │                   │              │             │ Observer→ │ │
│  │     │                   │              │             │ Controller│ │
│  │  4  │ new@agency.org    │ Observer     │ ● Invite    │ New acct  │ │
│  │  5  │ bad-email         │ Controller   │ ● Error     │ Invalid   │ │
│  │     │                   │              │             │ email     │ │
│  └─────┴───────────────────┴──────────────┴─────────────┴───────────┘ │
│                                                                         │
│  ⚠ Warning: 2 rows require new org invitations.                        │
│                                                                         │
│               [Cancel]    [Confirm Import (23 participants)]            │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Classification requires lookups against `ApplicationUser`, `OrganizationMembership`, and `ExerciseParticipant` tables
- Use batch queries (WHERE email IN (...)) rather than per-row lookups for performance
- Session-based state: parsed data should persist between upload (S01) and preview (S02) without re-parsing
- Consider caching classification results to avoid re-querying on filter changes
