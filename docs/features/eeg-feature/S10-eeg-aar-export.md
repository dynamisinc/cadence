# S10: EEG-Based AAR Export

**Feature:** Exercise Evaluation Guide (EEG)
**Priority:** P1
**Status:** Not Started
**Points:** 8

## User Story

**As an** Exercise Director,
**I want** to export EEG data organized by capability for After-Action Review,
**So that** I can prepare HSEEP-compliant AAR documentation efficiently.

## Context

Per HSEEP doctrine, the After-Action Report (AAR) must be completed within 60 days of exercise conduct. The AAR organizes findings by:

1. **Capability** - What organizational function was evaluated
2. **Capability Target** - What performance threshold was measured
3. **Critical Tasks** - What specific actions were assessed
4. **Observations** - What evaluators documented
5. **Ratings** - P/S/M/U assessment
6. **Recommendations** - Suggested corrective actions

This export provides the evaluation data in a structure ready for AAR drafting.

## Acceptance Criteria

### Export Access

- [ ] **Given** I am on the EEG Review page, **when** I click "Export", **then** I see export options
- [ ] **Given** I am a Director+, **when** I access export, **then** I can export EEG data
- [ ] **Given** I am an Evaluator, **when** I access export, **then** I cannot export (or limited export)
- [ ] **Given** the exercise has no EEG entries, **when** I try to export, **then** I see message about no data

### Export Formats

- [ ] **Given** export options, **when** displayed, **then** I can choose Excel (.xlsx) format
- [ ] **Given** export options, **when** displayed, **then** I can choose PDF format (future)
- [ ] **Given** export options, **when** displayed, **then** I can choose JSON format (for API integration)

### Excel Export Structure

- [ ] **Given** I export to Excel, **when** generated, **then** I get a workbook with multiple sheets
- [ ] **Given** the workbook, **when** opened, **then** "Summary" sheet shows exercise info and coverage metrics
- [ ] **Given** the workbook, **when** opened, **then** "By Capability" sheet shows entries grouped by Capability Target
- [ ] **Given** the workbook, **when** opened, **then** "All Entries" sheet shows flat list of all entries
- [ ] **Given** the workbook, **when** opened, **then** "Coverage Gaps" sheet lists unevaluated tasks

### Summary Sheet Content

- [ ] **Given** the Summary sheet, **when** displayed, **then** I see exercise name, date, status
- [ ] **Given** the Summary sheet, **when** displayed, **then** I see total entries, task coverage percentage
- [ ] **Given** the Summary sheet, **when** displayed, **then** I see P/S/M/U distribution
- [ ] **Given** the Summary sheet, **when** displayed, **then** I see evaluator list with entry counts
- [ ] **Given** the Summary sheet, **when** displayed, **then** I see generation timestamp

### By Capability Sheet Content

- [ ] **Given** the By Capability sheet, **when** displayed, **then** entries are grouped under Capability Target headers
- [ ] **Given** each Capability Target section, **when** displayed, **then** I see: target description, capability name
- [ ] **Given** each section, **when** displayed, **then** Critical Tasks are listed with their entries
- [ ] **Given** each entry row, **when** displayed, **then** I see: task, rating, observation, evaluator, timestamp
- [ ] **Given** a task with no entries, **when** displayed, **then** row shows "Not Evaluated"

### All Entries Sheet Content

- [ ] **Given** the All Entries sheet, **when** displayed, **then** each row is one EEG entry
- [ ] **Given** each row, **when** displayed, **then** columns include: timestamp, capability, target, task, rating, observation, evaluator, triggering inject
- [ ] **Given** the sheet, **when** displayed, **then** entries are sorted by timestamp (chronological)
- [ ] **Given** the sheet, **when** data exists, **then** columns have filters enabled

### Coverage Gaps Sheet

- [ ] **Given** the Coverage Gaps sheet, **when** unevaluated tasks exist, **then** they are listed
- [ ] **Given** each gap row, **when** displayed, **then** I see: capability, target, task description
- [ ] **Given** all tasks evaluated, **when** displayed, **then** sheet shows "All tasks evaluated" message

### Export Process

- [ ] **Given** I click Export Excel, **when** processing, **then** I see progress indicator
- [ ] **Given** export completes, **when** ready, **then** file downloads automatically
- [ ] **Given** export fails, **when** error occurs, **then** I see error message with retry option
- [ ] **Given** large exercise (100+ entries), **when** exporting, **then** process completes within 30 seconds

## Wireframes

### Export Dialog

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Export EEG Data                                                [X]    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Exercise: Hurricane Response TTX                                       │
│  Status: Completed                                                      │
│  EEG Entries: 24                                                        │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  Export Format                                                          │
│                                                                         │
│  [● Excel Workbook (.xlsx)]                                            │
│      Multi-sheet workbook organized for AAR preparation                │
│                                                                         │
│  [○ JSON Data]                                                          │
│      Raw data format for integration with other tools                  │
│                                                                         │
│  [○ PDF Report] (Coming Soon)                                          │
│      Formatted report ready for distribution                           │
│                                                                         │
│  ─────────────────────────────────────────────────────────────────────  │
│                                                                         │
│  Include Options                                                        │
│  [✓] Summary statistics                                                │
│  [✓] Entries by capability (for AAR)                                   │
│  [✓] All entries (flat list)                                           │
│  [✓] Coverage gaps                                                      │
│  [ ] Evaluator details (names visible)                                 │
│                                                                         │
│                                          [Cancel]  [Export]             │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Excel Workbook Structure

```
┌─────────────────────────────────────────────────────────────────────────┐
│  📊 EEG_Export_Hurricane_Response_TTX_2026-02-03.xlsx                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Sheets:                                                                │
│  ├── 📋 Summary                                                         │
│  ├── 📋 By Capability                                                   │
│  ├── 📋 All Entries                                                     │
│  └── 📋 Coverage Gaps                                                   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Summary Sheet

```
┌─────────────────────────────────────────────────────────────────────────┐
│  A              │  B                    │  C           │  D             │
├─────────────────┼───────────────────────┼──────────────┼────────────────┤
│  EXERCISE SUMMARY                                                       │
├─────────────────┼───────────────────────┼──────────────┼────────────────┤
│  Exercise Name  │  Hurricane Response TTX                               │
│  Exercise Date  │  2026-02-03                                           │
│  Status         │  Completed                                            │
│  Generated      │  2026-02-03 14:30:00 EST                              │
├─────────────────┼───────────────────────┼──────────────┼────────────────┤
│  COVERAGE METRICS                                                       │
├─────────────────┼───────────────────────┼──────────────┼────────────────┤
│  Total Entries  │  24                                                   │
│  Tasks Evaluated│  8 of 12 (67%)                                        │
├─────────────────┼───────────────────────┼──────────────┼────────────────┤
│  RATING DISTRIBUTION                                                    │
├─────────────────┼───────────────────────┼──────────────┼────────────────┤
│  Rating         │  Count                │  Percentage                   │
│  P - Performed  │  5                    │  42%                          │
│  S - Some Chall │  3                    │  25%                          │
│  M - Major Chall│  2                    │  17%                          │
│  U - Unable     │  2                    │  17%                          │
├─────────────────┼───────────────────────┼──────────────┼────────────────┤
│  EVALUATOR ACTIVITY                                                     │
├─────────────────┼───────────────────────┼──────────────┼────────────────┤
│  Evaluator      │  Entries                                              │
│  Robert Chen    │  6                                                    │
│  Sarah Kim      │  4                                                    │
│  Mike Jones     │  2                                                    │
└─────────────────┴───────────────────────┴──────────────┴────────────────┘
```

### By Capability Sheet

```
┌─────────────────────────────────────────────────────────────────────────┐
│  A              │  B           │  C        │  D           │  E          │
├─────────────────┼──────────────┼───────────┼──────────────┼─────────────┤
│  OPERATIONAL COMMUNICATIONS                                             │
│  Target: Establish interoperable communications within 30 minutes       │
├─────────────────┼──────────────┼───────────┼──────────────┼─────────────┤
│  Task           │  Rating      │  Observation            │  Evaluator  │
├─────────────────┼──────────────┼───────────┼──────────────┼─────────────┤
│  Activate emergency communication plan                                  │
│                 │  S           │  EOC issued activation...│ R. Chen    │
│                 │  P           │  Notification sent...    │ S. Kim     │
├─────────────────┼──────────────┼───────────┼──────────────┼─────────────┤
│  Establish radio net with field units                                   │
│                 │  M           │  Radio net established...│ R. Chen    │
├─────────────────┼──────────────┼───────────┼──────────────┼─────────────┤
│  Test backup communication systems                                      │
│                 │  P           │  Backup systems tested...│ M. Jones   │
├─────────────────┼──────────────┼───────────┼──────────────┼─────────────┤
│                                                                         │
│  MASS CARE SERVICES                                                     │
│  Target: Open and staff shelter within 2 hours of activation            │
├─────────────────┼──────────────┼───────────┼──────────────┼─────────────┤
│  ...                                                                    │
└─────────────────┴──────────────┴───────────┴──────────────┴─────────────┘
```

## API Specification

### GET /api/exercises/{exerciseId}/eeg-export

**Query Parameters:**
- `format`: `xlsx` | `json` (default: `xlsx`)
- `includeEvaluatorNames`: `true` | `false` (default: `true`)

**Response (xlsx):** File download with appropriate headers

**Response (json):**
```json
{
  "exercise": {
    "name": "Hurricane Response TTX",
    "date": "2026-02-03",
    "status": "Completed"
  },
  "summary": {
    "totalEntries": 24,
    "tasksCoverage": { "evaluated": 8, "total": 12, "percentage": 67 },
    "ratingDistribution": { "P": 5, "S": 3, "M": 2, "U": 2 }
  },
  "byCapability": [
    {
      "capabilityName": "Operational Communications",
      "targetDescription": "Establish interoperable communications...",
      "tasks": [
        {
          "taskDescription": "Activate emergency communication plan",
          "entries": [
            {
              "rating": "S",
              "observation": "EOC issued activation...",
              "evaluator": "Robert Chen",
              "observedAt": "2026-02-03T10:45:00Z"
            }
          ]
        }
      ]
    }
  ],
  "coverageGaps": [
    {
      "capabilityName": "Mass Care Services",
      "targetDescription": "Open and staff shelter...",
      "taskDescription": "Coordinate with Red Cross"
    }
  ],
  "generatedAt": "2026-02-03T14:30:00Z"
}
```

## Out of Scope

- PDF export with formatting (future enhancement)
- Word document export (future enhancement)
- AAR template auto-population (future enhancement)
- Improvement Plan (IP) generation (future enhancement)
- Scheduled/automated exports (future enhancement)

## Dependencies

- S01-S04: Capability Targets and Critical Tasks
- S06-S07: EEG Entries
- S09: Coverage metrics (reuse calculations)
- Excel library (EPPlus or ClosedXML)

## Technical Notes

- Use existing Excel export patterns from MSEL export
- Generate server-side, return as file download
- Consider background job for large exports
- Include exercise ID in filename for traceability
- Apply consistent styling to Excel (headers, colors for ratings)

## HSEEP AAR Structure Reference

The export should facilitate creating an AAR with this structure:
1. Executive Summary
2. Exercise Overview
3. Analysis of Capabilities
   - For each Capability:
     - Capability Description
     - Capability Targets
     - Observations (Strengths and Areas for Improvement)
     - Recommendations
4. Conclusion
5. Appendices

## Test Scenarios

### Unit Tests
- Export data aggregation by capability
- Rating distribution calculation
- Coverage gaps identification
- Filename generation

### Integration Tests
- Export generates valid Excel file
- Excel opens correctly in Excel/Sheets
- JSON export contains all required fields
- Large exercise export completes
- Empty exercise export handles gracefully
- Permission check for export access

---

*Story created: 2026-02-03*
