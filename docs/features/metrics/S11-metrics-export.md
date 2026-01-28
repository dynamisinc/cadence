# Story: Metrics Export

**Feature**: Metrics  
**Story ID**: S11  
**Priority**: P1 (Standard)  
**Phase**: Standard Implementation

---

## User Story

**As a** Director or Administrator,  
**I want** to export metrics data in various formats,  
**So that** I can include exercise results in AAR reports, presentations, and organizational documentation.

---

## Context

HSEEP requires formal After-Action Reports (AARs) and Improvement Plans. Metrics data needs to flow into:

- AAR documents (typically Word/PDF)
- Executive briefings (PowerPoint)
- Data analysis (Excel)
- Records retention (PDF)

Export capabilities make Cadence metrics actionable beyond the platform.

---

## Acceptance Criteria

- [ ] **Given** I am viewing exercise metrics, **when** I click Export, **then** I see format options: PDF, Excel, PNG (charts)
- [ ] **Given** I select PDF export, **when** I export, **then** I get a formatted report with all visible metrics sections
- [ ] **Given** I select Excel export, **when** I export, **then** I get a workbook with raw data tables for each metric category
- [ ] **Given** I view a chart, **when** I click export on the chart, **then** I can download it as PNG for presentations
- [ ] **Given** I am exporting, **when** the export generates, **then** exercise name and date are included in filename
- [ ] **Given** organization metrics, **when** I export, **then** the date range is included in the export
- [ ] **Given** I select multiple metric sections, **when** I export PDF, **then** all selected sections are included
- [ ] **Given** I have no data, **when** I try to export, **then** I see appropriate message (nothing to export)

---

## Out of Scope

- Scheduled/automated report generation
- Email delivery of exports
- Custom report templates
- Direct integration with AAR document generation
- PowerPoint export format

---

## Dependencies

- All exercise-level metrics stories (S01-S08)
- Organization metrics (S09-S10)
- Backend report generation (PDF library)

---

## Open Questions

- [ ] Should PDF include Cadence branding or be brand-neutral?
- [ ] What chart resolution for PNG export (72dpi, 150dpi, 300dpi)?
- [ ] Should Excel export include formulas or just values?
- [ ] Do we need CSV option for simple data import elsewhere?

---

## Domain Terms

| Term | Definition |
|------|------------|
| AAR | After-Action Report - formal HSEEP document summarizing exercise results |
| Export | Generate downloadable file from metrics data |

---

## UI/UX Notes

### Export Menu

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Exercise Metrics: Hurricane Response TTX           [📥 Export ▼]      │
│                                                                         │
│                                          ┌────────────────────────────┐ │
│                                          │  Export Options            │ │
│                                          ├────────────────────────────┤ │
│                                          │  📄 PDF Report             │ │
│                                          │     Full metrics summary   │ │
│                                          │                            │ │
│                                          │  📊 Excel Workbook         │ │
│                                          │     Raw data tables        │ │
│                                          │                            │ │
│                                          │  🖼 Charts Only (PNG)      │ │
│                                          │     For presentations      │ │
│                                          └────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────┘
```

### PDF Report Preview

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Export PDF Report                                                 [X]  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Include in report:                                                     │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  [✓]  Exercise Summary (name, date, type, duration)             │   │
│  │  [✓]  Inject Performance Metrics                                │   │
│  │  [✓]  Observation Summary & P/S/M/U Distribution                │   │
│  │  [✓]  Timeline Analysis                                         │   │
│  │  [ ]  Controller Activity (optional)                            │   │
│  │  [ ]  Evaluator Coverage (optional)                             │   │
│  │  [✓]  Core Capability Performance                               │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  Page orientation: [● Portrait] [○ Landscape]                          │
│                                                                         │
│                                      [Cancel]   [📥 Generate PDF]      │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Excel Export Structure

```
Workbook: Hurricane_Response_TTX_Metrics_2026-01-15.xlsx

Sheets:
├── Summary
│   └── Exercise info, key metrics
├── Inject_Performance
│   └── All inject data with timing
├── Observations
│   └── All observation records
├── P_S_M_U_Distribution
│   └── Rating counts and percentages
├── By_Phase
│   └── Phase-level breakdown
├── By_Controller
│   └── Controller activity data
└── By_Capability
    └── Core capability metrics
```

### Chart Export

When hovering over any chart:

```
┌─────────────────────────────────────┐
│  P/S/M/U Distribution         [📥] │  ← Export icon appears
│                                     │
│       ╭───────────╮                 │
│      ╱   ████      ╲                │
│     │  ██    ██     │               │
│     ...                             │
└─────────────────────────────────────┘
```

---

## Technical Notes

- PDF generation: Use QuestPDF or similar server-side library
- Excel generation: Use ClosedXML or EPPlus
- PNG export: Use html2canvas on frontend, or server-side rendering
- Generate files asynchronously for large datasets
- Include timestamp in all exports for audit trail
- File naming: `{ExerciseName}_Metrics_{Date}.{ext}`

---

## Estimation

**T-Shirt Size**: M  
**Story Points**: 5
