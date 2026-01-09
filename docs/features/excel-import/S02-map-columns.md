# Story: S02 - Map Excel Columns

## User Story

**As an** Administrator or Exercise Director,
**I want** to map Excel columns to Cadence inject fields,
**So that** my spreadsheet data is correctly imported into the system.

## Context

Excel files from different organizations use different column names and structures. This story covers the column mapping step where users specify which Excel column corresponds to each Cadence inject field. The system attempts auto-mapping based on common column names, but users can override these mappings.

## Acceptance Criteria

### Auto-Mapping
- [ ] **Given** I proceed to the mapping step, **when** the system analyzes column headers, **then** it attempts to auto-map columns to fields
- [ ] **Given** a column is named "Title" or "Inject Title", **when** auto-mapping runs, **then** it maps to the Title field
- [ ] **Given** a column is named "Time" or "Scheduled Time", **when** auto-mapping runs, **then** it maps to Scheduled Time field
- [ ] **Given** a column header doesn't match any known pattern, **when** auto-mapping runs, **then** it remains unmapped

### Manual Mapping
- [ ] **Given** I view the mapping interface, **when** I look at a Cadence field, **then** I see a dropdown to select the source Excel column
- [ ] **Given** I click on a field's dropdown, **when** it expands, **then** I see all available Excel columns plus "Don't import"
- [ ] **Given** I select a different column, **when** I confirm, **then** the mapping is updated
- [ ] **Given** I select "Don't import", **when** I confirm, **then** the field will not receive data from any column

### Required Fields
- [ ] **Given** I am viewing required fields, **when** I see them, **then** they are marked with asterisk (*)
- [ ] **Given** a required field (Title, Scheduled Time) is not mapped, **when** I try to proceed, **then** I see a validation error
- [ ] **Given** all required fields are mapped, **when** I click Next, **then** I proceed to validation step

### Preview
- [ ] **Given** I map a column, **when** I look at the preview, **then** I see sample data from that column
- [ ] **Given** I view the preview table, **when** I look at it, **then** I see how data will appear after mapping (first 5 rows)

### Time Format Detection
- [ ] **Given** the Scheduled Time column contains various formats, **when** I map it, **then** I can specify the expected date/time format
- [ ] **Given** I don't specify a format, **when** import runs, **then** the system attempts to auto-detect common formats

### Saving Mapping Configuration
- [ ] **Given** I complete mapping, **when** I see the save option, **then** I can save this mapping as a template
- [ ] **Given** I saved a mapping template, **when** I import again, **then** I can select the saved template to auto-apply

## Out of Scope

- Complex data transformations during mapping
- Conditional mapping based on cell values
- Mapping multiple columns to a single field
- Regular expression-based mapping

## Dependencies

- excel-import/S01: Upload Excel (file must be uploaded)
- excel-import/S03: Validate Import (next step)
- Core entity definitions (inject field specifications)

## Open Questions

- [ ] Should we support mapping to custom fields?
- [ ] How should multi-value fields (like objectives) be mapped?
- [ ] Should there be a "preview all rows" option?

## Domain Terms

| Term | Definition |
|------|------------|
| Column Mapping | Association between Excel column and Cadence field |
| Auto-Mapping | System's automatic attempt to match columns to fields |
| Mapping Template | Saved column mapping configuration for reuse |

## UI/UX Notes

### Column Mapping Interface

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Import MSEL from Excel                                             ✕   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Step 2 of 4: Map Columns                                              │
│  ━━━━━━━━━━━━━━━━━━●━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   │
│                                                                         │
│  Match your Excel columns to Cadence fields:                           │
│                                                                         │
│  REQUIRED FIELDS                                                        │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Cadence Field        Excel Column                  Sample      │   │
│  │  ─────────────────────────────────────────────────────────────  │   │
│  │  Title *              [Column B: "Inject Title" ▼]  Hurricane w.│   │
│  │  Scheduled Time *     [Column C: "Time"         ▼]  09:00 AM    │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  OPTIONAL FIELDS                                                        │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  Cadence Field        Excel Column                  Sample      │   │
│  │  ─────────────────────────────────────────────────────────────  │   │
│  │  Inject Number        [Column A: "#"            ▼]  1           │   │
│  │  Scenario Day         [Column D: "Day"          ▼]  1           │   │
│  │  Scenario Time        [Column E: "Story Time"   ▼]  08:00       │   │
│  │  Description          [Column F: "Description"  ▼]  The County..│   │
│  │  From                 [Column G: "From"         ▼]  NWS         │   │
│  │  To                   [Column H: "To"           ▼]  EOC         │   │
│  │  Method               [— Don't import —         ▼]  —           │   │
│  │  Expected Action      [Column I: "Response"     ▼]  Acknowledge.│   │
│  │  Notes                [— Don't import —         ▼]  —           │   │
│  │  Phase                [Column J: "Phase"        ▼]  Initial Res.│   │
│  │  Objectives           [Column K: "Objectives"   ▼]  1, 2        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ☐ Save this mapping as template: [_________________________]          │
│                                                                         │
│  Load saved template: [Select template...                     ▼]       │
│                                                                         │
│                                       [← Back]  [Cancel]  [Next →]     │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Date/Time Format Selection

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Scheduled Time Format                                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Sample values from your Excel file:                                   │
│  • "09:00 AM"                                                          │
│  • "9:15:00"                                                           │
│  • "09:30"                                                             │
│                                                                         │
│  Date format:                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ ○ No date in column (use exercise date)                        │   │
│  │ ● MM/DD/YYYY                                                    │   │
│  │ ○ DD/MM/YYYY                                                    │   │
│  │ ○ YYYY-MM-DD                                                    │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Time format:                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ ● 12-hour (9:00 AM)                                            │   │
│  │ ○ 24-hour (09:00)                                              │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│                                         [Cancel]  [Apply Format]       │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Store mapping templates in user preferences or exercise settings
- Auto-mapping keywords should be configurable
- Consider fuzzy matching for column name similarity
- Handle Excel date serial numbers (they're stored as numbers, not dates)
