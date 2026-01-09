# Story: S01 - Upload Excel File

## User Story

**As an** Administrator or Exercise Director,
**I want** to upload an Excel file containing MSEL data,
**So that** I can import injects from my existing planning spreadsheet.

## Context

Many organizations develop MSELs in Excel during exercise planning. This story covers the first step of the import workflow: selecting and uploading the Excel file. The system reads the file, identifies worksheets and columns, and prepares for column mapping.

## Acceptance Criteria

### File Selection
- [ ] **Given** I am on the MSEL view, **when** I click "Import", **then** I see the import wizard with file upload step
- [ ] **Given** I am on the upload step, **when** I view the interface, **then** I see a drag-and-drop zone and a "Browse" button
- [ ] **Given** I drag a file into the drop zone, **when** I release, **then** the file is selected for upload
- [ ] **Given** I click "Browse", **when** I select a file, **then** the file is selected for upload

### File Validation
- [ ] **Given** I select an .xlsx file, **when** the file is read, **then** upload proceeds successfully
- [ ] **Given** I select an .xls file, **when** the file is read, **then** upload proceeds successfully
- [ ] **Given** I select a .csv file, **when** the file is read, **then** upload proceeds successfully
- [ ] **Given** I select an unsupported file type, **when** I try to upload, **then** I see an error message listing supported formats
- [ ] **Given** I select a file larger than 10MB, **when** I try to upload, **then** I see a file size error
- [ ] **Given** I select a password-protected Excel file, **when** the system tries to read it, **then** I see an error asking to remove protection

### File Preview
- [ ] **Given** upload succeeds, **when** the file is processed, **then** I see: file name, size, worksheet count
- [ ] **Given** the Excel file has multiple worksheets, **when** I view the preview, **then** I can select which worksheet to import
- [ ] **Given** I select a worksheet, **when** selection is confirmed, **then** I see a preview of the first 5 rows
- [ ] **Given** I see the preview, **when** I review it, **then** I can verify it's the correct data

### Progress Indication
- [ ] **Given** I upload a large file, **when** processing begins, **then** I see a progress indicator
- [ ] **Given** an error occurs during upload, **when** the error is caught, **then** I see a clear error message with suggestion

## Out of Scope

- Multiple file upload at once
- Direct URL import (from SharePoint, Google Sheets)
- Importing from Google Sheets format
- Automatic scheduled imports

## Dependencies

- exercise-crud/S01: Create Exercise (must import into existing exercise)
- excel-import/S02: Map Columns (next step in wizard)

## Open Questions

- [ ] Should we support .xlsm (macro-enabled) files?
- [ ] What's the maximum row count we should support?
- [ ] Should previous upload configurations be remembered?

## Domain Terms

| Term | Definition |
|------|------------|
| Import Wizard | Multi-step process for importing Excel data |
| Worksheet | A single sheet/tab within an Excel workbook |
| Drop Zone | UI area where files can be dragged and dropped |

## UI/UX Notes

### Upload Step

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Import MSEL from Excel                                             ✕   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Step 1 of 4: Upload File                                              │
│  ━━━━━━━━━━━━●━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                                                                 │   │
│  │                        📁                                       │   │
│  │                                                                 │   │
│  │           Drag and drop your Excel file here                   │   │
│  │                                                                 │   │
│  │                    ─── or ───                                  │   │
│  │                                                                 │   │
│  │                  [Browse Files]                                 │   │
│  │                                                                 │   │
│  │           Supported formats: .xlsx, .xls, .csv                 │   │
│  │           Maximum size: 10 MB                                   │   │
│  │                                                                 │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  💡 Don't have a file ready? [Download MSEL Template]                  │
│                                                                         │
│                                            [Cancel]  [Next →]          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### File Selected - Worksheet Selection

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Import MSEL from Excel                                             ✕   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Step 1 of 4: Upload File                                              │
│  ━━━━━━━━━━━━●━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   │
│                                                                         │
│  ✓ Hurricane_Exercise_MSEL.xlsx                                        │
│    Size: 245 KB │ 3 worksheets │ [Remove]                              │
│                                                                         │
│  Select worksheet to import:                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ ○ MSEL (47 rows)                                               │   │
│  │ ○ Objectives (4 rows)                                          │   │
│  │ ○ Contacts (12 rows)                                           │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Preview (first 5 rows):                                               │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ # │ Time  │ Day │ Inject Title           │ From    │ To       │   │
│  │ 1 │ 09:00 │ 1   │ Hurricane warning...   │ NWS     │ EOC      │   │
│  │ 2 │ 09:15 │ 1   │ EOC activation...      │ County  │ Staff    │   │
│  │ 3 │ 09:30 │ 1   │ Evacuation order...    │ County  │ Muni     │   │
│  │ ...                                                             │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│                                       [← Back]  [Cancel]  [Next →]     │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Upload Error

```
┌─────────────────────────────────────────────────────────────────────────┐
│  ⚠️ Upload Error                                                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Could not read the uploaded file.                                     │
│                                                                         │
│  Error: File appears to be password protected                          │
│                                                                         │
│  Please remove password protection from the Excel file and             │
│  try again.                                                            │
│                                                                         │
│                                                    [Try Again]         │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Use EPPlus or ClosedXML for .xlsx parsing
- Use NPOI for .xls (legacy format) support
- Stream large files rather than loading entirely into memory
- Store uploaded file temporarily during import wizard session
- Clean up temporary files after import completes or is cancelled
