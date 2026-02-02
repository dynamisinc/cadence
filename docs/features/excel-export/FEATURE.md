# Feature: Excel Export

**Phase:** MVP
**Status:** Not Started

## Overview

Users can export MSEL data to Excel format for sharing with stakeholders, creating backups, or final formatting outside Cadence.

## Problem Statement

Organizations need to share MSELs with stakeholders who may not have Cadence access, create backups for records retention, or use Excel for final formatting and printing. Without export capability, users would need to manually recreate MSEL data in Excel, leading to errors and wasted time.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-export-msel.md) | Export MSEL to Excel | P0 | 📋 Ready |
| [S02](./S02-export-template.md) | Export Blank Template | P1 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|-------------|
| Administrator | Full export access |
| Exercise Director | Export exercises they manage |
| Controller | Export MSELs they have access to |
| Evaluator | Export MSELs (read-only data) |
| Observer | No export access (or limited) |

## Key Concepts

| Term | Definition |
|------|------------|
| Export Format | File format for exported data (.xlsx, .csv) |
| Round-Trip | Ability to export and re-import without data loss |
| Metadata Sheet | Additional worksheet with exercise information |

## Dependencies

- inject-crud/S01 - Create Inject (injects to export)
- excel-import/S02 - Map Columns (export should match import structure)

## Acceptance Criteria (Feature-Level)

- [ ] Users can export MSEL data to Excel format (.xlsx)
- [ ] Export includes all inject fields in consistent column order
- [ ] Export preserves column order matching import template for round-trip compatibility
- [ ] Users can download a blank template for data entry
- [ ] Exported files can be re-imported without data loss

## Notes

- Export should match import column structure for round-trip compatibility
- Consider including metadata sheet with exercise info
- Large exports (1000+ injects) may need background processing

## Wireframes/Mockups

### Export Options

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Export MSEL                                                        ✕   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Export Format:                                                        │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ ● Excel Workbook (.xlsx) - Recommended                         │   │
│  │ ○ CSV (.csv) - Simple format                                   │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Options:                                                              │
│  ☑ Include formatting (headers, column widths)                        │
│  ☑ Include objectives worksheet                                       │
│  ☑ Include phases worksheet                                           │
│  ☐ Include conduct data (fired times, status)                         │
│                                                                         │
│  Filename: [Hurricane_Response_MSEL_2025-01-15.xlsx    ]              │
│                                                                         │
│                                         [Cancel]  [Export]             │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Notes

- Export should match import column structure for round-trip compatibility
- Consider including metadata sheet with exercise info
- Large exports (1000+ injects) may need background processing
