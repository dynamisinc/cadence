# Feature: Excel Export

**Parent Epic:** MSEL Authoring (E4)

## Description

Organizations need to share MSELs with stakeholders who may not have Cadence access, create backups, or use Excel for final formatting. This feature allows users to export the MSEL to Excel format, preserving data structure and optionally applying formatting for readability.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-export-msel.md) | Export MSEL to Excel | P0 | 📋 Ready |
| [S02](./S02-export-template.md) | Export Blank Template | P1 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|------------|
| Administrator | Full export access |
| Exercise Director | Export exercises they manage |
| Controller | Export MSELs they have access to |
| Evaluator | Export MSELs (read-only data) |
| Observer | No export access (or limited) |

## Export Formats

| Format | Extension | Use Case |
|--------|-----------|----------|
| Excel Workbook | .xlsx | Full-featured export with formatting |
| CSV | .csv | Simple data export for other tools |

## Dependencies

- inject-crud/S01: Create Inject (injects to export)
- excel-import/S02: Map Columns (export should match import structure)

## Acceptance Criteria (Feature-Level)

- [ ] Users can export MSEL data to Excel format
- [ ] Export includes all inject fields
- [ ] Export preserves column order matching import template
- [ ] Users can download a blank template for data entry
- [ ] Exported files can be re-imported (round-trip)

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
