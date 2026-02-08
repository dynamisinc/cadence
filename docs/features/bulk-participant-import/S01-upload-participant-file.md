# Story: S01 - Upload Participant File

## User Story

**As an** Exercise Director or OrgManager,
**I want** to upload a CSV or Excel file containing participant information,
**So that** I can add multiple participants to my exercise at once instead of one at a time.

## Context

Full-Scale Exercises and multi-agency Functional Exercises often involve dozens to hundreds of participants from multiple agencies. Exercise Directors typically receive participant lists as spreadsheets from partner agencies. This story covers the file upload, parsing, and initial validation step of the bulk import workflow. After upload and validation, the user proceeds to the preview screen (S02) before any changes are committed.

The system must accept both CSV and XLSX formats since agencies use different tools. The existing Excel import infrastructure (used for inject import) provides reusable patterns for file parsing and column detection.

## Acceptance Criteria

### File Upload

- [ ] **Given** I am on the exercise participants screen, **when** I have the role of Exercise Director or OrgManager (or higher), **then** I see a "Bulk Import" button
- [ ] **Given** I click "Bulk Import", **when** the upload dialog opens, **then** I can select a .csv or .xlsx file from my device
- [ ] **Given** I select a file, **when** the file is larger than 10 MB, **then** the upload is rejected with a clear error message
- [ ] **Given** I select a file, **when** the file contains more than 500 rows, **then** the upload is rejected with a row count limit message
- [ ] **Given** I select a file, **when** the file is not .csv or .xlsx format, **then** the upload is rejected with a format error

### Column Detection and Mapping

- [ ] **Given** I upload a valid file, **when** the system parses the headers, **then** it auto-detects columns for Email, Exercise Role, Display Name, and Organization Role
- [ ] **Given** the file uses common synonyms (e.g., "E-mail", "Email Address", "Role", "HSEEP Role"), **when** parsing headers, **then** the system maps them to the correct fields
- [ ] **Given** the required "Email" column is missing, **when** parsing completes, **then** the upload fails with a message identifying the missing column
- [ ] **Given** the required "Exercise Role" column is missing, **when** parsing completes, **then** the upload fails with a message identifying the missing column
- [ ] **Given** optional columns (Display Name, Organization Role) are missing, **when** parsing completes, **then** the upload succeeds with defaults applied

### Row-Level Validation

- [ ] **Given** the file is parsed, **when** a row has an invalid email format, **then** that row is flagged as an error with the reason "Invalid email format"
- [ ] **Given** the file is parsed, **when** a row has an unrecognized Exercise Role, **then** that row is flagged as an error listing valid roles
- [ ] **Given** the file is parsed, **when** a row has a duplicate email within the same file, **then** the duplicate row is flagged as an error
- [ ] **Given** the file is parsed, **when** a row has an empty email, **then** that row is skipped (blank rows are ignored)
- [ ] **Given** the file is parsed, **when** all rows pass validation, **then** the user is directed to the preview screen (S02)
- [ ] **Given** the file is parsed, **when** some rows have errors, **then** the user is directed to the preview screen with errors highlighted

### Permission Boundaries

- [ ] **Given** I am an Exercise Director with OrgUser role, **when** I upload a file, **then** I see a warning that rows requiring org invitations will need OrgManager approval
- [ ] **Given** I am an Exercise Director with OrgManager or OrgAdmin role, **when** I upload a file, **then** I can process all rows including those requiring org invitations
- [ ] **Given** I am a Controller, Evaluator, or Observer, **when** I view the participants screen, **then** I do not see the "Bulk Import" button

## Out of Scope

- Drag-and-drop file upload (future enhancement)
- Import from external directories (Active Directory, LDAP)
- Multi-exercise bulk import (one exercise per upload)
- Custom column mapping UI (auto-detection only for MVP)

## Dependencies

- exercise-config/S02: Assign Participants (existing participant management UI)
- excel-import/S01: Upload Excel (existing file upload patterns)

## Open Questions

- [ ] Should the row limit be configurable per organization, or is 500 a universal cap?
- [ ] Should we support additional file formats (Google Sheets export, ODS)?
- [ ] What column synonyms are most common across emergency management agencies?

## Domain Terms

| Term | Definition |
|------|------------|
| **Participant** | A user assigned to an exercise with a specific HSEEP role |
| **Exercise Role** | One of the five HSEEP-defined roles: Exercise Director, Controller, Evaluator, Observer, Administrator |
| **Organization Role** | The user's role within the organization: OrgAdmin, OrgManager, OrgUser |
| **Column Synonym** | Alternative column header names that map to the same field (e.g., "E-mail" maps to "Email") |

## UI/UX Notes

```
┌─────────────────────────────────────────────────────────────────────┐
│  Participants                      [Bulk Import]  [+ Add Participant] │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ... existing participant list ...                                    │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  Bulk Import Participants                                      ✕    │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  Upload a CSV or Excel file with participant information.             │
│  Required columns: Email, Exercise Role                               │
│  Optional columns: Display Name, Organization Role                    │
│                                                                       │
│  ┌─────────────────────────────────────────────────────────────┐     │
│  │                                                             │     │
│  │         Drag file here or click to browse                   │     │
│  │                                                             │     │
│  │         Supported: .csv, .xlsx (max 10 MB, 500 rows)       │     │
│  │                                                             │     │
│  └─────────────────────────────────────────────────────────────┘     │
│                                                                       │
│  [Download Template]                                                  │
│                                                                       │
│           [Cancel]                                                    │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Reuse `ExcelImportService` patterns for file parsing and column auto-detection
- Use the existing synonym mapping approach from inject import for column header matching
- File parsing should happen server-side to validate data against existing users/memberships
- Consider a session-based import flow (similar to inject import) to preserve parsed data between upload and preview steps
- The exercise must be in Draft or Active status to accept bulk imports
