# Story: S02 - Export Blank MSEL Template

## User Story

**As an** Administrator or Exercise Director,
**I want** to download a blank MSEL template,
**So that** I can prepare inject data offline in the correct format for import.

## Context

Organizations often develop MSELs collaboratively before entering them into Cadence. Providing a correctly formatted template ensures that when they're ready to import, the data will match Cadence's expected structure. The template includes column headers, data validation, and instructions.

## Acceptance Criteria

### Template Access
- [ ] **Given** I am on the MSEL view, **when** I look for template download, **then** I see a "Download Template" link
- [ ] **Given** I am in the import wizard (upload step), **when** I need a template, **then** I see a "Download Template" link
- [ ] **Given** I click "Download Template", **when** download completes, **then** I receive an Excel file

### Template Structure
- [ ] **Given** I open the template, **when** I view the MSEL worksheet, **then** I see all Cadence inject field columns as headers
- [ ] **Given** I view the template, **when** I check required columns, **then** Title and Scheduled Time are marked with asterisk (*)
- [ ] **Given** I view the template, **when** I see row 2, **then** it contains an example inject for reference
- [ ] **Given** I view the template, **when** I check column order, **then** columns match Cadence's import expectation

### Column Headers
The template should include these columns in order:
- [ ] Inject # | Title* | Scheduled Date* | Scheduled Time* | Scenario Day | Scenario Time
- [ ] Description | From | To | Method | Expected Action | Controller Notes
- [ ] Phase | Objectives

### Data Validation (Excel)
- [ ] **Given** I view the Method column, **when** I click a cell, **then** I see a dropdown with valid method options
- [ ] **Given** I view the Scheduled Time column, **when** I enter invalid time, **then** Excel shows validation warning
- [ ] **Given** I view Scenario Day column, **when** I enter non-numeric value, **then** Excel shows validation error

### Instructions Worksheet
- [ ] **Given** I open the template, **when** I view worksheets, **then** I see an "Instructions" worksheet
- [ ] **Given** I view Instructions, **when** I read it, **then** I see: field descriptions, format requirements, tips
- [ ] **Given** I view Instructions, **when** I look for examples, **then** I see example values for each field

### Sample Data
- [ ] **Given** I view the template MSEL worksheet, **when** I look at rows 2-4, **then** I see sample injects demonstrating proper format
- [ ] **Given** I view sample injects, **when** I'm ready to use, **then** I can easily delete them and add my own data

## Out of Scope

- Exercise-specific templates with pre-filled objectives/phases
- Template customization options
- Templates in formats other than Excel
- Multiple language templates

## Dependencies

- excel-import/S02: Map Columns (template structure should match import expectations)
- Core entity definitions (field specifications)

## Open Questions

- [ ] Should the template include conditional formatting for required fields?
- [ ] Should there be separate templates for different exercise types?
- [ ] Should the template version be tracked for compatibility?

## Domain Terms

| Term | Definition |
|------|------------|
| Template | Pre-formatted Excel file for MSEL data entry |
| Data Validation | Excel rules that restrict input to valid values |
| Instructions Worksheet | Help sheet explaining how to use the template |

## UI/UX Notes

### Template Download Link

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Import MSEL from Excel                                             ✕   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Step 1 of 4: Upload File                                              │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                        📁                                       │   │
│  │           Drag and drop your Excel file here                   │   │
│  │                    or click to browse                          │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  💡 Need a template?                                                   │
│     [📥 Download Cadence MSEL Template]                                │
│                                                                         │
│     Pre-formatted Excel file with all required columns                 │
│     and instructions for preparing your MSEL data.                     │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Template - MSEL Worksheet

```
┌──────────────────────────────────────────────────────────────────────────────────────────────────┐
│ A          │ B           │ C              │ D              │ E           │ F            │ ...   │
├──────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Inject #   │ Title*      │ Scheduled      │ Scheduled      │ Scenario    │ Scenario     │       │
│            │             │ Date*          │ Time*          │ Day         │ Time         │       │
├──────────────────────────────────────────────────────────────────────────────────────────────────┤
│ 1          │ Hurricane   │ 01/15/2025     │ 09:00 AM       │ 1           │ 08:00        │ ...   │
│            │ warning     │                │                │             │              │       │
│            │ issued      │                │                │             │              │       │
├──────────────────────────────────────────────────────────────────────────────────────────────────┤
│ 2          │ EOC         │ 01/15/2025     │ 09:15 AM       │ 1           │ 10:00        │ ...   │
│            │ activation  │                │                │             │              │       │
├──────────────────────────────────────────────────────────────────────────────────────────────────┤
│ 3          │ [Your       │                │                │             │              │       │
│            │ inject      │                │                │             │              │       │
│            │ here]       │                │                │             │              │       │
└──────────────────────────────────────────────────────────────────────────────────────────────────┘

* = Required field
Yellow highlight on required column headers
Row 2-3: Sample data (delete before import)
Row 4+: Enter your injects
```

### Template - Instructions Worksheet

```
┌─────────────────────────────────────────────────────────────────────────┐
│  CADENCE MSEL TEMPLATE - INSTRUCTIONS                                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  GETTING STARTED                                                       │
│  1. Review the MSEL worksheet to see the column structure              │
│  2. Delete the sample rows (2-3) when you're ready                     │
│  3. Enter your inject data starting at row 2                           │
│  4. Save the file and import into Cadence                              │
│                                                                         │
│  REQUIRED FIELDS (marked with *)                                       │
│  • Title: Brief description of the inject (3-200 characters)           │
│  • Scheduled Date: Date to deliver (MM/DD/YYYY format)                 │
│  • Scheduled Time: Time to deliver (HH:MM AM/PM format)                │
│                                                                         │
│  OPTIONAL FIELDS                                                       │
│  • Inject #: Auto-assigned if blank                                    │
│  • Scenario Day: Story day number (1, 2, 3...)                         │
│  • Scenario Time: Story time (24-hour format: 08:00, 14:30)            │
│  • Description: Full inject content                                    │
│  • From: Simulated sender                                              │
│  • To: Target recipient(s)                                             │
│  • Method: Phone Call, Email, Radio, In-Person, Fax, Video, Other     │
│  • Expected Action: What players should do                             │
│  • Controller Notes: Internal notes for Controllers                    │
│  • Phase: Phase name (must match exactly if pre-defined)              │
│  • Objectives: Comma-separated objective numbers (e.g., "1, 2")       │
│                                                                         │
│  TIPS                                                                  │
│  • Don't change column headers - they're used for mapping              │
│  • Use consistent date/time formats throughout                         │
│  • Leave cells blank rather than entering "N/A" or "None"              │
│  • Multi-line text is supported in Description field                   │
│                                                                         │
│  VERSION                                                               │
│  Template version: 1.0                                                 │
│  Compatible with: Cadence MVP                                          │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Technical Notes

- Store template as embedded resource or generate dynamically
- Use Excel data validation for dropdowns (Method field)
- Set column widths appropriately for content
- Include named ranges for easier mapping reference
- Version the template for future compatibility tracking
