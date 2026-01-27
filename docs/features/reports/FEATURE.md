# Feature: Reports

**Parent Epic:** Application Navigation & User Experience
**Phase:** Post-MVP (Navigation Enhancement)

## Description

The Reports feature enables export of exercise data for after-action review, documentation, and compliance purposes. Initial focus is on Excel export capability for MSEL data and observations, with future expansion to formatted reports and analytics.

## Business Value

- **Documentation**: Export exercise records for archival and compliance
- **After-Action Review**: Share data with stakeholders not in the system
- **Interoperability**: Excel format compatible with existing workflows
- **Flexibility**: Users can further analyze data in familiar tools

## User Personas

| Persona | Report Needs |
|---------|--------------|
| **Exercise Director** | Full exercise package for AAR, stakeholder briefings |
| **Administrator** | Organization-wide metrics, audit exports |
| **Evaluator** | Observation summaries for report writing |

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S01](./S01-reports-landing.md) | Reports Landing Page | P1 | 📋 Future |
| [S02](./S02-exercise-metrics.md) | Exercise Metrics Dashboard | P1 | 📋 Future |
| [S03](./S03-observation-summary.md) | Observation Summary Report | P1 | 📋 Future |
| [S04](./S04-timeline-visualization.md) | Timeline Visualization | P2 | 📋 Future |
| [S05](./S05-excel-export.md) | Excel Export | P0 | 📋 Ready |

## Feature-Level Acceptance Criteria

- [ ] Exercise Director and Admin can access Reports section
- [ ] Export MSEL to Excel with inject details and timestamps
- [ ] Export Observations to Excel with ratings and notes
- [ ] Export full exercise package (ZIP with MSEL + Observations)
- [ ] Export maintains HSEEP-compliant field structure

## Export Types

| Export | Contents | Format | Priority |
|--------|----------|--------|----------|
| MSEL | Injects with delivery times, status, notes | .xlsx | P0 |
| Observations | Observations with ratings, injects, capabilities | .xlsx | P0 |
| Full Package | MSEL + Observations + Summary | .zip | P1 |

## Dependencies

- Exercise CRUD (for exercise data)
- Observations feature (for observation export)
- Excel library (EPPlus, ClosedXML, or similar)
- Authentication (role-based access)

## Technical Notes

- Use existing Excel import patterns/library for export
- Generate files server-side, return as download
- Consider background job for large exports
- Follow HSEEP template structure for compatibility

## Related Documentation

- [Excel Import Feature](../excel-import/FEATURE.md)
- [Observations Feature](../observations/FEATURE.md)
- HSEEP 2020 Documentation (template structure)

---

*Feature created: 2026-01-23*
