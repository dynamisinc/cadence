# Feature: Metrics

**Parent Epic**: E8 - Reporting & Analytics

## Description

Cadence provides metrics at two levels: exercise-level metrics that show performance during and after a specific exercise, and organization-level metrics that track trends across multiple exercises. These metrics support HSEEP after-action review (AAR) requirements and help organizations demonstrate improvement over time.

## Business Value

- **Real-Time Awareness**: Directors and Controllers see exercise progress at a glance
- **AAR Support**: Comprehensive data for post-exercise analysis and improvement planning
- **Trend Analysis**: Organizations can track capability development across multiple exercises
- **Accountability**: Quantifiable evidence of exercise program effectiveness
- **Resource Planning**: Data-driven decisions about training and exercise focus areas

## User Personas

| Persona               | Metrics Access               | Key Needs                                      |
| --------------------- | ---------------------------- | ---------------------------------------------- |
| **Administrator**     | All metrics                  | Organization trends, program effectiveness     |
| **Exercise Director** | Exercise + Org view          | Exercise summary, AAR data, comparison to past |
| **Controller**        | Exercise metrics             | Real-time progress, inject status overview     |
| **Evaluator**         | Exercise metrics             | Observation coverage, rating distribution      |
| **Observer**          | Exercise metrics (read-only) | Follow along with exercise progress            |

## Features by Phase

### MVP (P0) — 4 Stories

Essential metrics for exercise situational awareness and basic AAR support.

| Story | File                                  | Description                                        | Est. Points |
| ----- | ------------------------------------- | -------------------------------------------------- | ----------- |
| S01   | `S01-exercise-progress-dashboard.md`  | Real-time progress indicator during conduct        | 5           |
| S02   | `S02-exercise-inject-summary.md`      | Inject delivery statistics (fired/skipped/on-time) | 5           |
| S03   | `S03-exercise-observation-summary.md` | Observation counts and P/S/M/U distribution        | 5           |
| S04   | `S04-exercise-timeline-summary.md`    | Duration, pauses, phase timing analysis            | 5           |

**MVP Total: ~20 story points**

### Standard Implementation (P1) — 7 Stories

Enhanced analytics for comprehensive AAR and organizational tracking.

| Story | File                                 | Description                              | Est. Points |
| ----- | ------------------------------------ | ---------------------------------------- | ----------- |
| S05   | `S05-psmu-distribution-chart.md`     | Interactive visual charts for ratings    | 5           |
| S06   | `S06-core-capability-performance.md` | Performance by FEMA Core Capability      | 5           |
| S07   | `S07-controller-activity-metrics.md` | Controller workload and timing analysis  | 3           |
| S08   | `S08-evaluator-coverage-metrics.md`  | Evaluator observation coverage matrix    | 5           |
| S09   | `S09-org-exercise-history.md`        | Organization exercise activity dashboard | 5           |
| S10   | `S10-org-performance-trends.md`      | Performance trends across exercises      | 8           |
| S11   | `S11-metrics-export.md`              | Export to PDF, Excel, PNG                | 8           |

**P1 Total: ~39 story points**

### Future Enhancement (P2) — 3 Stories

Advanced analytics for mature exercise programs.

| Story | File                              | Description                           | Est. Points |
| ----- | --------------------------------- | ------------------------------------- | ----------- |
| S12   | `S12-comparative-analysis.md`     | Compare metrics between exercises     | 8           |
| S13   | `S13-benchmark-comparison.md`     | Compare to industry/sector benchmarks | 13          |
| S14   | `S14-custom-metrics-dashboard.md` | User-configurable metric views        | 13          |

**P2 Total: ~34 story points**

## Metrics Categories

### Exercise-Level Metrics

**Inject Performance**

- Total inject count
- Injects by status (Fired, Skipped, Pending)
- On-time delivery rate
- Average timing variance
- Breakdown by phase
- Breakdown by controller

**Observation Quality**

- Total observation count
- P/S/M/U rating distribution
- Observations by core capability
- Observations by evaluator
- Coverage rate (% of objectives observed)
- Unlinked observations

**Timeline**

- Planned vs actual duration
- Time per phase
- Pause count and duration
- Inject pacing analysis

**Participation**

- Connected users (peak and current)
- Users by role
- Offline activity events

### Organization-Level Metrics

**Exercise Activity**

- Total exercises conducted
- Exercises by type
- Exercises by status
- Average exercise duration

**Performance Trends**

- P/S/M/U distribution over time
- Performance by core capability
- Top improvement areas
- Evaluator rating consistency

**Operational Health**

- On-time inject rate trend
- Observation density trend
- User adoption metrics

## UI/UX Notes

- **During Conduct**: Minimal metrics overlay (progress bar, key counts) - don't distract from operations
- **Post-Exercise**: Comprehensive metrics dashboard for AAR
- **Organization Level**: Dedicated analytics page with filters and date ranges
- All metrics should support export (PDF, Excel)

## Dependencies

- Exercise conduct (Phase D) - inject firing, clock data
- Observations (Phase E) - observation capture, ratings
- Authentication - role-based access
- Exercise history (completed exercises)

## Out of Scope (MVP)

- Custom metric definitions
- Real-time metric streaming (poll-based refresh acceptable)
- Mobile-optimized metrics views
- Scheduled metric reports (email)

## Data Retention

- Exercise metrics: Retained indefinitely with exercise
- Organization metrics: Calculated on-demand from exercise history
- Consider: Pre-calculated aggregates for performance

## Open Questions

- [ ] Should metrics be visible during exercise, or only after completion? - During an exercise
- [ ] What date range should organization metrics default to (YTD, last 12 months)? - YTD, but can change
- [ ] Do we need role-based metric access (e.g., Evaluators can't see Controller metrics)? - No
- [ ] Should there be metric-specific permissions? - Not initially
