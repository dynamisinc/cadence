# Story: S06 - Download Participant Template

## User Story

**As an** Exercise Director,
**I want** to download a correctly formatted template file for participant upload,
**So that** I can distribute it to partner agencies and ensure the data comes back in the right format.

## Context

Exercise Directors need to collect participant information from multiple partner agencies before exercise day. Rather than describing the file format verbally or in emails, a downloadable template ensures consistency. The template should be available in both CSV and XLSX formats and include clear instructions on valid values for each column.

This story has no dependencies on other bulk import stories and can be developed independently.

## Acceptance Criteria

### Template Download

- [ ] **Given** I am on the exercise participants screen, **when** I click "Download Template", **then** I can choose between CSV and XLSX formats
- [ ] **Given** I select CSV format, **when** the download completes, **then** I receive a file named `participant-template.csv`
- [ ] **Given** I select XLSX format, **when** the download completes, **then** I receive a file named `participant-template.xlsx`
- [ ] **Given** I am on the bulk import upload dialog, **when** I see the template link, **then** I can download the template from there as well

### Template Content - CSV

- [ ] **Given** I open the CSV template, **when** I view the first row, **then** it contains the column headers: Email, Exercise Role, Display Name, Organization Role
- [ ] **Given** I open the CSV template, **when** I view the second row, **then** it contains an example row: `jane.doe@agency.gov, Controller, Jane Doe, OrgUser`
- [ ] **Given** I open the CSV template, **when** I view the third row, **then** it contains a comment row explaining valid Exercise Role values: `# Valid Exercise Roles: ExerciseDirector, Controller, Evaluator, Observer`
- [ ] **Given** I open the CSV template, **when** I view the fourth row, **then** it contains a comment row explaining valid Organization Roles: `# Valid Organization Roles: OrgAdmin, OrgManager, OrgUser (default: OrgUser)`

### Template Content - XLSX

- [ ] **Given** I open the XLSX template, **when** I view the "Participants" sheet, **then** it contains column headers: Email, Exercise Role, Display Name, Organization Role
- [ ] **Given** I open the XLSX template, **when** I view the "Participants" sheet, **then** it contains two example rows with realistic data
- [ ] **Given** I open the XLSX template, **when** I view the "Participants" sheet, **then** the Exercise Role column has a dropdown validation with valid role values
- [ ] **Given** I open the XLSX template, **when** I view the "Instructions" sheet, **then** it explains each column, valid values, and import rules
- [ ] **Given** I open the XLSX template, **when** I view the formatting, **then** required columns (Email, Exercise Role) have bold headers and optional columns have normal headers

### Template Accessibility

- [ ] **Given** I am any authenticated user viewing an exercise, **when** I can see participants, **then** I can download the template (not restricted to directors)
- [ ] **Given** the template is downloaded, **when** I open it in Excel, Google Sheets, or LibreOffice, **then** it displays correctly

## Out of Scope

- Pre-populating the template with existing participant data
- Organization-specific template customization
- Template versioning (the template format is tied to the current import version)

## Dependencies

- None (can be developed independently)

## Open Questions

- [ ] Should the template include the exercise name and date in a header row for context?
- [ ] Should the XLSX template include conditional formatting (e.g., red highlight for invalid roles)?
- [ ] Should the comment/instruction rows in CSV be prefixed with `#` or placed in a separate section?

## Domain Terms

| Term | Definition |
|------|------------|
| **Participant Template** | A pre-formatted file that partner agencies fill in with their participant information |
| **Column Validation** | Excel data validation that restricts cell values to a predefined list |
| **Exercise Role** | HSEEP-defined role for exercise participation: ExerciseDirector, Controller, Evaluator, Observer |

## UI/UX Notes

### XLSX Template - Participants Sheet

```
┌──────────────────┬────────────────┬──────────────┬──────────────────┐
│ Email (required) │ Exercise Role  │ Display Name │ Organization Role│
│                  │ (required)     │ (optional)   │ (optional)       │
├──────────────────┼────────────────┼──────────────┼──────────────────┤
│ jane@agency.gov  │ Controller     │ Jane Doe     │ OrgUser          │
│ bob@partner.org  │ Evaluator      │ Bob Smith    │ OrgUser          │
│                  │                │              │                  │
│                  │                │              │                  │
└──────────────────┴────────────────┴──────────────┴──────────────────┘
```

### XLSX Template - Instructions Sheet

```
Cadence Participant Import Template
====================================

REQUIRED COLUMNS:
  Email          - Participant's email address
  Exercise Role  - One of: ExerciseDirector, Controller, Evaluator, Observer

OPTIONAL COLUMNS:
  Display Name       - Participant's full name (used in invitation if needed)
  Organization Role  - OrgAdmin, OrgManager, or OrgUser (defaults to OrgUser)

NOTES:
  - One row per participant
  - Remove example rows before uploading
  - Maximum 500 rows per upload
  - Participants who are not organization members will receive an invitation
```

## Technical Notes

- CSV template is a simple static file; can be generated on the frontend without a backend call
- XLSX template with dropdown validation requires a backend endpoint using EPPlus (already a project dependency for inject import/export)
- Consider caching the generated XLSX template since it does not change per-request
- The template download endpoint should not require organization context (generic template)
