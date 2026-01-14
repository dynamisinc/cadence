# E6-S25: Export Review Data

**Feature:** review-mode  
**Priority:** P2  
**Estimate:** 1.5 days

## User Story

**As** James (Exercise Director),  
**I want** to export review data for AAR report preparation,  
**So that** I can create official documentation outside the system.

## Context

HSEEP requires formal After-Action Reports. While automated report generation is future scope, exporting structured data helps report writers.

## Acceptance Criteria

- [ ] **Given** I am in Review Mode, **when** I click "Export", **then** I see export format options
- [ ] **Given** export options, **when** I select "Excel", **then** I download a spreadsheet with inject outcomes and observations
- [ ] **Given** export options, **when** I select "PDF Summary", **then** I download a formatted summary document
- [ ] **Given** the Excel export, **when** I open it, **then** I see sheets for: Inject Summary, Observations, Statistics
- [ ] **Given** the PDF export, **when** I open it, **then** I see: Exercise info, Phase summaries, Key statistics, Observation highlights

## Out of Scope

- Full AAR document generation (future feature)
- Custom export templates
- Integration with Word/Google Docs

## Dependencies

- E6-S20: Access Review Mode
- E6-S22: Inject Outcome Summary
- E6-S23: Observation Review Panel

## UI/UX Notes

### Export Button Location

```
┌─────────────────────────────────────────────────────────────────┐
│ [Conduct]  [Review]                            [📥 Export ▼]   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Export Options:                                                │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 📊 Excel Workbook (.xlsx)                               │   │
│  │    Inject outcomes, observations, and statistics        │   │
│  ├─────────────────────────────────────────────────────────┤   │
│  │ 📄 PDF Summary                                          │   │
│  │    Formatted summary for printing/sharing               │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Excel Export Structure

| Sheet | Contents |
|-------|----------|
| Summary | Exercise info, total counts, timing stats |
| Injects | All injects with status, times, variance, fired by |
| Observations | All observations with inject link, rating, notes |
| By Phase | Phase-grouped inject outcomes |

### PDF Export Structure

```
EXERCISE AFTER-ACTION REVIEW DATA
Hurricane Response TTX 2026
Conducted: January 14, 2026

─────────────────────────────────────────

EXERCISE SUMMARY
• Total Injects: 14
• Fired: 11 (79%)
• Skipped: 2 (14%)
• Not Executed: 1 (7%)

TIMING PERFORMANCE
• On Time: 8 (73%)
• Early: 1 (9%)
• Late: 2 (18%)
• Average Variance: +3 minutes

─────────────────────────────────────────

PHASE SUMMARIES

Phase 1: Warning & Preparation
Status: Complete (3/3)
...

─────────────────────────────────────────

OBSERVATION HIGHLIGHTS
• 12 total observations recorded
• Areas for Improvement: 4
• Coverage Gaps: 1 objective

─────────────────────────────────────────
```

## Technical Notes

- Excel export: Use EPPlus or ClosedXML (backend)
- PDF export: Use QuestPDF or similar (backend)
- Consider async generation for large exercises
- Include exercise metadata (name, date, type, location)
