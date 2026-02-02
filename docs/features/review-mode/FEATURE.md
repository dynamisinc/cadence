# Feature: Exercise Review & AAR Mode

**Phase:** Standard
**Status:** Not Started

## Overview

A dedicated view for reviewing exercise execution after conduct, supporting After-Action Review (AAR) discussions and report preparation. Unlike the real-time Conduct view (time-sorted), Review Mode organizes information by phase and outcome to facilitate analysis.

## Problem Statement

After exercise conduct, Exercise Directors and Evaluators need to analyze what happened, identify strengths and improvement areas, and prepare AAR reports. The real-time Conduct view is optimized for action, not analysis. Teams need a post-exercise view that groups events by phase, highlights outcomes, and surfaces observation patterns to support structured AAR discussions and HSEEP-compliant reporting.

## User Stories

| Story | Title | Priority | Status |
|-------|-------|----------|--------|
| [S20](./S20-access-review-mode.md) | Access Review Mode | P1 | 📋 Ready |
| [S21](./S21-phase-grouped-timeline.md) | Phase-Grouped Timeline | P1 | 📋 Ready |
| [S22](./S22-inject-outcome-summary.md) | Inject Outcome Summary | P1 | 📋 Ready |
| [S23](./S23-observation-review-panel.md) | Observation Review Panel | P2 | 📋 Ready |
| [S24](./S24-exercise-statistics-dashboard.md) | Exercise Statistics Dashboard | P2 | 📋 Ready |
| [S25](./S25-export-review-data.md) | Export Review Data | P2 | 📋 Ready |

## User Personas

| Persona | Interaction |
|---------|-------------|
| **Exercise Director (James)** | Reviews exercise flow, identifies issues, prepares AAR briefing |
| **Evaluator (Robert)** | Reviews observations in context, verifies coverage of objectives |
| **Observer (Patricia)** | Understands what happened if joined late or reviewing after |

## Key Concepts

| Term | Definition |
|------|------------|
| **Review Mode** | Post-conduct view optimized for analysis rather than real-time action |
| **AAR** | After-Action Review — HSEEP-required review meeting after exercise |
| **Time Variance** | Difference between scheduled inject time and actual fire time |
| **Coverage Gap** | Objective with no linked observations |
| **P/S/M/U** | HSEEP rating scale: Performed, Some Difficulty, Major Difficulty, Unable to Perform |

## Dependencies

- Phase D complete (Clock, Fire functionality)
- Phase E complete (Observations)
- Fired/Skipped inject data with timestamps

## Acceptance Criteria (Feature-Level)

- [ ] Users can access Review Mode after exercise conduct
- [ ] Injects are grouped by phase with outcome summaries
- [ ] Time variance between scheduled and actual delivery is visible
- [ ] Observations are linked to their associated injects and objectives
- [ ] Exercise statistics (fire rate, timing accuracy) are calculated
- [ ] Review data can be exported for AAR report preparation

## Out of Scope

- Automated AAR report generation
- Video/audio playback integration
- Comparison across multiple exercises
- AI-generated insights

## Notes

- Review Mode is read-only — uses same data as Conduct Mode
- Consider caching statistics calculations for large exercises
- Charts should handle exercises with 100+ injects gracefully
- Export functionality may require backend endpoint for PDF generation
