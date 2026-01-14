# Feature: Exercise Review & AAR Mode

**Epic:** E6 - Exercise Evaluation  
**Feature ID:** E6-F5

## Description

A dedicated view for reviewing exercise execution after conduct, supporting After-Action Review (AAR) discussions and report preparation. Unlike the real-time Conduct view (time-sorted), Review Mode organizes information by phase and outcome to facilitate analysis.

## Business Value

- Supports HSEEP-required After-Action Review process
- Provides structured data for AAR report writing
- Enables Exercise Directors to assess exercise effectiveness
- Allows stakeholders to review exercise without attending live

## User Personas

| Persona | Need |
|---------|------|
| **James (Exercise Director)** | Review exercise flow, identify issues, prepare AAR briefing |
| **Robert (Evaluator)** | Review observations in context, verify coverage of objectives |
| **Patricia (Observer)** | Understand what happened if joined late or reviewing after |

## Stories

| ID | Name | Priority | Status |
|----|------|----------|--------|
| E6-S20 | Access Review Mode | P1 | 📋 |
| E6-S21 | Phase-Grouped Timeline | P1 | 📋 |
| E6-S22 | Inject Outcome Summary | P1 | 📋 |
| E6-S23 | Observation Review Panel | P2 | 📋 |
| E6-S24 | Exercise Statistics Dashboard | P2 | 📋 |
| E6-S25 | Export Review Data | P2 | 📋 |

## Dependencies

**Requires:**
- Phase D complete (Clock, Fire functionality)
- Phase E complete (Observations)
- Fired/Skipped inject data with timestamps

**Required By:**
- AAR Report Generation (Future)

## Out of Scope (This Feature)

- Automated AAR report generation
- Video/audio playback integration
- Comparison across multiple exercises
- AI-generated insights

## Domain Terms

| Term | Definition |
|------|------------|
| **Review Mode** | Post-conduct view optimized for analysis rather than real-time action |
| **AAR** | After-Action Review — HSEEP-required review meeting after exercise |
| **Time Variance** | Difference between scheduled inject time and actual fire time |
| **Coverage Gap** | Objective with no linked observations |
| **P/S/M/U** | HSEEP rating scale: Performed, Some Difficulty, Major Difficulty, Unable to Perform |

## Technical Notes

- Review Mode is read-only — uses same data as Conduct Mode
- Consider caching statistics calculations for large exercises
- Charts should handle exercises with 100+ injects gracefully
- Export functionality may require backend endpoint for PDF generation
