# Feature: Excel Import

**Parent Epic:** MSEL Authoring (E4)

## Description

Many organizations develop their MSELs in Excel before exercise conduct. This feature allows users to import inject data from Excel files, mapping spreadsheet columns to Cadence inject fields. The goal is to preserve existing workflows while bringing MSEL management into a system designed for exercise conduct.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-upload-excel.md) | Upload Excel File | P0 | 📋 Ready |
| [S02](./S02-map-columns.md) | Map Excel Columns | P0 | 📋 Ready |
| [S03](./S03-validate-import.md) | Validate Import Data | P0 | 📋 Ready |
| [S04](./S04-delivery-method-synonyms.md) | Delivery Method Synonym Matching | P2 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|------------|
| Administrator | Full import access |
| Exercise Director | Import for their exercises |
| Controller | May import if granted permission |
| Evaluator | No import access |
| Observer | No import access |

## Supported Formats

| Format | Extension | Support |
|--------|-----------|---------|
| Excel Workbook | .xlsx | ✅ Full support |
| Excel 97-2003 | .xls | ✅ Full support |
| CSV | .csv | ✅ Basic support |

## Import Workflow

```
┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐
│ Upload  │ -> │  Map    │ -> │Validate │ -> │ Import  │
│  File   │    │ Columns │    │  Data   │    │  Data   │
└─────────┘    └─────────┘    └─────────┘    └─────────┘
     │              │              │              │
     │              │              │              │
  Select        Configure      Review         Create
  .xlsx         mapping        errors         injects
```

## Dependencies

- exercise-crud/S01: Create Exercise (import into existing exercise)
- inject-crud/S01: Create Inject (import creates injects)
- Core entity definitions (inject field mapping)

## Acceptance Criteria (Feature-Level)

- [ ] Users can upload Excel files (.xlsx, .xls) or CSV files
- [ ] Users can map Excel columns to Cadence inject fields
- [ ] System validates data before import
- [ ] Users can review and fix validation errors
- [ ] Import creates injects in the MSEL
- [ ] Import can update existing injects (by inject number)

## Wireframes/Mockups

### Import Wizard Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Import MSEL from Excel                                             ✕   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐             │
│  │    1    │ -> │    2    │ -> │    3    │ -> │    4    │             │
│  │ Upload  │    │   Map   │    │Validate │    │ Import  │             │
│  └─────────┘    └─────────┘    └─────────┘    └─────────┘             │
│     ●              ○              ○              ○                      │
│                                                                         │
│  ═══════════════════════════════════════════════════════════════════   │
│                                                                         │
│  Step 1: Upload File                                                   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                                                                 │   │
│  │           📁 Drag and drop your Excel file here                │   │
│  │                                                                 │   │
│  │                  or click to browse                             │   │
│  │                                                                 │   │
│  │           Supported: .xlsx, .xls, .csv                         │   │
│  │                                                                 │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  💡 Need a template? [Download Cadence MSEL Template]                  │
│                                                                         │
│                                            [Cancel]  [Next →]          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Notes

- Import should preserve Excel formatting notes in a log
- Consider providing a Cadence Excel template
- Large files (1000+ rows) may need background processing
- Import history should be logged for audit purposes
