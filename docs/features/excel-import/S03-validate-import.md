# Story: S03 - Validate Import Data

## User Story

**As an** Administrator or Exercise Director,
**I want** to review and fix validation errors before importing,
**So that** I can ensure data quality and avoid importing bad data.

## Context

After mapping columns, the system validates all rows against Cadence's data requirements. This story covers displaying validation results, allowing users to fix errors, and providing clear feedback about what will be imported. Users can choose to import valid rows while skipping invalid ones, or fix all issues first.

## Acceptance Criteria

### Validation Execution
- [ ] **Given** I proceed to the validation step, **when** the system validates, **then** I see a progress indicator during validation
- [ ] **Given** validation completes, **when** I view results, **then** I see: total rows, valid rows, rows with errors, rows with warnings

### Error Types
- [ ] **Given** a row has an empty Title, **when** validation runs, **then** it's flagged as Error: "Title is required"
- [ ] **Given** a row has an empty Scheduled Time, **when** validation runs, **then** it's flagged as Error: "Scheduled Time is required"
- [ ] **Given** a row has an invalid time format, **when** validation runs, **then** it's flagged as Error: "Cannot parse time value"
- [ ] **Given** a row has a Scenario Time without Scenario Day, **when** validation runs, **then** it's flagged as Error: "Scenario Day required when Time is set"
- [ ] **Given** a row has a duplicate Inject Number, **when** validation runs, **then** it's flagged as Warning: "Duplicate inject number"
- [ ] **Given** a row references a non-existent Phase name, **when** validation runs, **then** it's flagged as Warning: "Phase not found, will be created"

### Results Display
- [ ] **Given** there are validation errors, **when** I view the results, **then** I see a summary card showing error count
- [ ] **Given** I view the validation table, **when** I look at error rows, **then** they are highlighted in red
- [ ] **Given** I view the validation table, **when** I look at warning rows, **then** they are highlighted in yellow
- [ ] **Given** I click on an error row, **when** the details expand, **then** I see specific error messages

### Filtering Results
- [ ] **Given** I view the validation table, **when** I use the filter, **then** I can show: All rows, Errors only, Warnings only, Valid only
- [ ] **Given** I filter to Errors only, **when** the filter applies, **then** I see only rows with errors

### Import Options
- [ ] **Given** there are errors, **when** I view import options, **then** I can choose: "Import valid rows only" or "Go back and fix"
- [ ] **Given** I choose "Import valid rows only", **when** I confirm, **then** only error-free rows are imported
- [ ] **Given** all rows are valid, **when** I view the interface, **then** I see a green success indicator and can proceed

### Error Correction
- [ ] **Given** I click on an error cell, **when** editing mode activates, **then** I can modify the value directly
- [ ] **Given** I modify a value, **when** I press Enter or Tab, **then** the row is re-validated
- [ ] **Given** I fix an error, **when** re-validation passes, **then** the row changes from red to normal

### Import Execution
- [ ] **Given** I click "Import", **when** import starts, **then** I see a progress bar
- [ ] **Given** import completes, **when** I see the result, **then** I know: injects created, injects skipped, any import errors
- [ ] **Given** import succeeds, **when** I click "View MSEL", **then** I see the imported injects

## Out of Scope

- Automatic error correction suggestions
- Batch find-and-replace for errors
- Export validation report to Excel
- Undo import after completion

## Dependencies

- excel-import/S01: Upload Excel (file must be uploaded)
- excel-import/S02: Map Columns (columns must be mapped)
- inject-crud/S01: Create Inject (import creates injects)
- exercise-phases/S01: Define Phases (phase name matching)
- exercise-objectives/S01: Create Objective (objective matching)

## Open Questions

- [ ] Should warnings block import or just notify?
- [ ] Should we auto-create missing phases/objectives?
- [ ] Maximum number of errors before suggesting file review?

## Domain Terms

| Term | Definition |
|------|------------|
| Validation Error | Data issue that prevents import (must be fixed) |
| Validation Warning | Data issue that allows import with caveats |
| Valid Row | Row that passes all validation rules |
| Inline Edit | Modifying data directly in the validation table |

## UI/UX Notes

### Validation Results

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Import MSEL from Excel                                             ✕   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Step 3 of 4: Validate Data                                            │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━●━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   │
│                                                                         │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐        │
│  │  47             │  │  43             │  │  3              │        │
│  │  Total Rows     │  │  ✓ Valid        │  │  ⚠ Warnings    │        │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘        │
│                                                                         │
│  ┌─────────────────┐                                                   │
│  │  1              │  ⬅ Fix this error before importing               │
│  │  ✗ Errors       │                                                   │
│  └─────────────────┘                                                   │
│                                                                         │
│  Filter: [All ▼]  [Show errors only]  [Show warnings only]             │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Row │ Status │ Title                │ Time     │ Issue          │   │
│  │ ────┼────────┼──────────────────────┼──────────┼──────────────  │   │
│  │  1  │ ✓      │ Hurricane warning    │ 09:00 AM │                │   │
│  │  2  │ ✓      │ EOC activation       │ 09:15 AM │                │   │
│  │  3  │ ⚠      │ Evacuation order     │ 09:30 AM │ Phase not found│   │
│  │  4  │ ✗      │ [empty]              │ 09:45 AM │ Title required │   │
│  │  5  │ ✓      │ Shelter opened       │ 10:00 AM │                │   │
│  │ ...                                                              │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Click on a row to view details or edit values.                        │
│                                                                         │
│                 [← Back]  [Cancel]  [Import Valid Rows Only]  [Fix →]  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Error Row Expanded

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Row 4 - Error Details                                          [✕]    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ✗ ERROR: Title is required                                            │
│                                                                         │
│  Current values:                                                       │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Title:          [                                          ]   │   │
│  │  Scheduled Time: [09:45 AM                                  ]   │   │
│  │  Description:    [Resource status update                    ]   │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Edit the Title field above to fix this error.                         │
│                                                                         │
│                                      [Skip This Row]  [Save & Validate] │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Import Complete

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Import MSEL from Excel                                             ✕   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Step 4 of 4: Import Complete                                          │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━●━━   │
│                                                                         │
│                            ✓                                           │
│                                                                         │
│                    Import Successful!                                  │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  46 injects created                                             │   │
│  │   1 row skipped (errors)                                        │   │
│  │   2 phases created: "Initial Response", "Recovery"              │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│                                        [Import Another]  [View MSEL]   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Validation should run in background/worker to avoid UI blocking
- Cache validation results for quick re-validation after edits
- Consider batch insert for performance on large imports
- Log import details for audit trail
- Transaction should rollback if import fails midway
