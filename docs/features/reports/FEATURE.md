# Feature: Reports

**Phase:** Post-MVP
**Status:** Planned

## Overview

The Reports feature enables export of exercise data for after-action review, documentation, and compliance purposes. Initial focus is on Excel export capability for MSEL data and observations, with future expansion to formatted reports and analytics.

## Problem Statement

Exercise stakeholders need to extract exercise data for after-action reviews, compliance documentation, and analysis in external tools. Without export capabilities, users must manually transcribe data, cannot share information with non-system users, and lack the ability to perform custom analysis in tools like Excel or PowerBI.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S05](./S05-excel-export.md) | Excel Export | P0 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|-------------|
| Exercise Director | Full exercise package for AAR, stakeholder briefings |
| Administrator | Organization-wide metrics, audit exports |
| Evaluator | Observation summaries for report writing |

## Key Concepts

| Term | Definition |
|------|------------|
| After-Action Review (AAR) | Post-exercise analysis and documentation process |
| Export Package | Complete set of exercise data in exportable format |
| MSEL Export | Excel workbook containing inject details and delivery times |
| Observation Export | Excel workbook containing evaluator observations |
| HSEEP-Compliant Export | Export following HSEEP template structure |

## Dependencies

- Exercise CRUD (for exercise data)
- Observations feature (for observation export)
- Excel library (EPPlus, ClosedXML, or similar)
- Authentication (role-based access)

## Acceptance Criteria (Feature-Level)

- [ ] Exercise Director and Admin can access Reports section
- [ ] Export MSEL to Excel with inject details and timestamps
- [ ] Export Observations to Excel with ratings and notes
- [ ] Export full exercise package (ZIP with MSEL + Observations)
- [ ] Export maintains HSEEP-compliant field structure

## Notes

### Business Value

- **Documentation**: Export exercise records for archival and compliance
- **After-Action Review**: Share data with stakeholders not in the system
- **Interoperability**: Excel format compatible with existing workflows
- **Flexibility**: Users can further analyze data in familiar tools

### Export Types

| Export | Contents | Format | Priority |
|--------|----------|--------|----------|
| MSEL | Injects with delivery times, status, notes | .xlsx | P0 |
| Observations | Observations with ratings, injects, capabilities | .xlsx | P0 |
| Full Package | MSEL + Observations + Summary | .zip | P1 |

### Technical Notes

- Use existing Excel import patterns/library for export
- Generate files server-side, return as download
- Consider background job for large exports
- Follow HSEEP template structure for compatibility

### Future Stories (Planned)

The following stories are planned for future implementation:
- S01: Reports Landing Page (P1)
- S02: Exercise Metrics Dashboard (P1)
- S03: Observation Summary Report (P1)
- S04: Timeline Visualization (P2)

### Related Documentation

- [Excel Import Feature](../excel-import/FEATURE.md)
- [Observations Feature](../observations/FEATURE.md)
- HSEEP 2020 Documentation (template structure)

---

*Feature created: 2026-01-23*
