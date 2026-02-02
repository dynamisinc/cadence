# Feature: Excel Import

**Phase:** MVP
**Status:** Not Started

## Overview

Users can import inject data from Excel files into Cadence, mapping spreadsheet columns to inject fields to preserve existing MSEL authoring workflows.

## Problem Statement

Many organizations develop their MSELs in Excel before exercise conduct. Without import capability, users would need to manually re-enter hundreds of injects, leading to errors and wasted time. This feature allows users to import existing Excel MSELs directly, preserving their established workflows while gaining the benefits of Cadence for exercise conduct.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-upload-excel.md) | Upload Excel File | P0 | 📋 Ready |
| [S02](./S02-map-columns.md) | Map Excel Columns | P0 | 📋 Ready |
| [S03](./S03-validate-import.md) | Validate Import Data | P0 | 📋 Ready |
| [S04](./S04-delivery-method-synonyms.md) | Delivery Method Synonym Matching | P2 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|-------------|
| Administrator | Full import access |
| Exercise Director | Import for their exercises |
| Controller | May import if granted permission |
| Evaluator | No import access |
| Observer | No import access |

## Key Concepts

| Term | Definition |
|------|------------|
| Column Mapping | Associating Excel columns with Cadence inject fields |
| Validation | Pre-import data quality checks (required fields, data types) |
| Import Wizard | Step-by-step process: Upload → Map → Validate → Import |
| Synonym Matching | Automatic recognition of common delivery method variations |
| Update vs Create | Import can create new injects or update existing by inject number |

## Dependencies

- exercise-crud/S01 - Create Exercise (import into existing exercise)
- inject-crud/S01 - Create Inject (import creates injects)
- Core entity definitions (inject field mapping)

## Acceptance Criteria (Feature-Level)

- [ ] Users can upload Excel files (.xlsx, .xls) or CSV files
- [ ] Users can map Excel columns to Cadence inject fields
- [ ] System validates data before import (required fields, data types)
- [ ] Users can review and fix validation errors before finalizing import
- [ ] Import creates injects in the MSEL
- [ ] Import can update existing injects (matched by inject number)

## Notes

- Import should preserve Excel formatting notes in a log
- Consider providing a Cadence Excel template for download
- Large files (1000+ rows) may need background processing
- Import history should be logged for audit purposes

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
