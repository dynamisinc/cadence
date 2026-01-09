# Story: S01 - Export MSEL to Excel

## User Story

**As an** Administrator, Exercise Director, or Controller,
**I want** to export the MSEL to an Excel file,
**So that** I can share it with stakeholders, create backups, or use Excel for additional formatting.

## Context

While Cadence is the system of record during exercise conduct, organizations often need Excel versions of the MSEL for distribution to participants, printing, or archival. The export should produce a well-formatted file that can also be re-imported if needed.

## Acceptance Criteria

### Export Initiation
- [ ] **Given** I am viewing the MSEL, **when** I click "Export", **then** I see the export options dialog
- [ ] **Given** I am on the exercise overview, **when** I use the actions menu, **then** I see "Export to Excel" option

### Format Options
- [ ] **Given** I view export options, **when** I see format choices, **then** I can select: Excel (.xlsx) or CSV (.csv)
- [ ] **Given** I select Excel format, **when** I view additional options, **then** I can choose formatting options
- [ ] **Given** I select CSV format, **when** I view options, **then** formatting options are disabled (not applicable)

### Export Content
- [ ] **Given** I export without filters, **when** export completes, **then** all injects are included
- [ ] **Given** I have active filters on the MSEL, **when** I export, **then** I can choose: "Export all" or "Export filtered only"
- [ ] **Given** I export to Excel, **when** I open the file, **then** I see columns for all inject fields in logical order
- [ ] **Given** I export to Excel, **when** I check the header row, **then** column names match Cadence field names

### Column Order
The export should include columns in this order:
- [ ] Inject Number, Title, Scheduled Date, Scheduled Time, Scenario Day, Scenario Time
- [ ] Description, From, To, Method, Expected Action, Notes
- [ ] Phase, Objectives, Status

### Formatting (Excel only)
- [ ] **Given** formatting is enabled, **when** I open the export, **then** column widths are auto-fitted
- [ ] **Given** formatting is enabled, **when** I view headers, **then** they have bold text and background color
- [ ] **Given** formatting is enabled, **when** I view the file, **then** rows have alternating colors for readability
- [ ] **Given** formatting is enabled, **when** I check filters, **then** Excel auto-filter is enabled on header row

### Additional Worksheets
- [ ] **Given** "Include objectives" is checked, **when** I open the export, **then** I see an "Objectives" worksheet
- [ ] **Given** "Include phases" is checked, **when** I open the export, **then** I see a "Phases" worksheet
- [ ] **Given** "Include conduct data" is checked, **when** I view injects, **then** I see: Status, Fired At, Fired By columns

### File Download
- [ ] **Given** I click "Export", **when** export completes, **then** the file downloads automatically
- [ ] **Given** export takes more than 2 seconds, **when** processing, **then** I see a progress indicator
- [ ] **Given** export fails, **when** the error occurs, **then** I see an error message with details

### File Naming
- [ ] **Given** I export, **when** I see the filename field, **then** it defaults to: "{ExerciseName}_MSEL_{Date}.xlsx"
- [ ] **Given** I want a different filename, **when** I edit the field, **then** I can customize the name

## Out of Scope

- Direct export to cloud storage (SharePoint, Google Drive)
- Scheduled/automatic exports
- Custom column selection
- Export to PDF format

## Dependencies

- inject-crud/S01: Create Inject (injects must exist)
- exercise-objectives/S01: Create Objective (for objectives worksheet)
- exercise-phases/S01: Define Phases (for phases worksheet)

## Open Questions

- [ ] Should we include a metadata worksheet with exercise info?
- [ ] Should export include audit data (created by, modified date)?
- [ ] Should filtered exports note the filter criteria used?

## Domain Terms

| Term | Definition |
|------|------------|
| Export | Generating a downloadable file from MSEL data |
| Round-trip | Ability to export and re-import without data loss |
| Auto-filter | Excel feature enabling column filtering |

## UI/UX Notes

### Export Dialog

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Export MSEL                                                        ✕   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Hurricane Response 2025                                               │
│  47 injects │ 4 objectives │ 3 phases                                  │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  Format                                                                │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ ● Excel Workbook (.xlsx)                                       │   │
│  │ ○ CSV (.csv)                                                   │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Include                                                               │
│  ☑ Header formatting (bold, colors, auto-width)                       │
│  ☑ Objectives worksheet                                               │
│  ☑ Phases worksheet                                                   │
│  ☐ Conduct data (status, fired times)                                 │
│                                                                         │
│  Scope                                                                 │
│  ● All 47 injects                                                     │
│  ○ Filtered injects only (12 currently shown)                         │
│                                                                         │
│  Filename                                                              │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ Hurricane_Response_MSEL_2025-01-15                          .xlsx│   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│                                         [Cancel]  [Export]             │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Export Progress

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Exporting MSEL...                                                     │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ████████████████████░░░░░░░░░░ 65%                                    │
│                                                                         │
│  Generating worksheets...                                              │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Use EPPlus or ClosedXML for Excel generation
- Stream large exports to avoid memory issues
- Set appropriate MIME type for download (application/vnd.openxmlformats-officedocument.spreadsheetml.sheet)
- Consider caching export for repeated downloads
