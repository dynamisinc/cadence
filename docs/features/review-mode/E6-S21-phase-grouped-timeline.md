# E6-S21: Phase-Grouped Timeline

**Feature:** review-mode  
**Priority:** P1  
**Estimate:** 1.5 days

## User Story

**As** James (Exercise Director),  
**I want** to see injects grouped by phase with completion status,  
**So that** I can review how each phase of the exercise unfolded.

## Context

Phases represent major segments of the exercise scenario. Seeing injects grouped by phase helps identify which phases went smoothly and which had issues.

## Acceptance Criteria

- [ ] **Given** I am in Review Mode, **when** the view loads, **then** injects are grouped under phase headers
- [ ] **Given** a phase header, **when** I view it, **then** I see: Phase name, description, and completion stats (e.g., "5 of 6 fired, 1 skipped")
- [ ] **Given** a phase section, **when** I view the injects, **then** they are sorted by scheduled time within the phase
- [ ] **Given** a fired inject, **when** I view it, **then** I see: Title, fired time, fired by, time variance (early/late/on-time)
- [ ] **Given** a skipped inject, **when** I view it, **then** I see: Title, skipped time, skipped by, skip reason
- [ ] **Given** a pending inject (not fired), **when** I view it, **then** I see: Title, scheduled time, "Not Executed" label
- [ ] **Given** a phase with all injects fired, **when** I view the header, **then** it shows a completion indicator (✓)
- [ ] **Given** I click a phase header, **when** the section toggles, **then** it expands or collapses the inject list

## Time Variance Calculation

| Variance | Definition | Display |
|----------|------------|---------|
| On Time | Fired within ±2 min of scheduled | ✓ On time |
| Early | Fired >2 min before scheduled | ⏪ 5 min early |
| Late | Fired >2 min after scheduled | ⏩ 12 min late |

## Dependencies

- E6-S20: Access Review Mode

## UI/UX Notes

```
▼ Phase 1: Warning & Preparation                    3/3 complete ✓
  ┌────────────────────────────────────────────────────────────────┐
  │ #1  NWS Hurricane Watch                                        │
  │     Scheduled: +00:00  •  Fired: 9:02 AM  •  ✓ On time        │
  │     Fired by: Maria Chen                                       │
  ├────────────────────────────────────────────────────────────────┤
  │ #2  Media Inquiry                                              │
  │     Scheduled: +00:15  •  Fired: 9:18 AM  •  ✓ On time        │
  │     Fired by: Maria Chen                                       │
  ├────────────────────────────────────────────────────────────────┤
  │ #3  School District Inquiry                                    │
  │     Scheduled: +00:30  •  Fired: 9:33 AM  •  ✓ On time        │
  │     Fired by: Sarah Martinez                                   │
  └────────────────────────────────────────────────────────────────┘

▼ Phase 2: Evacuation & Shelter                     4/5 complete
  ┌────────────────────────────────────────────────────────────────┐
  │ #4  Hurricane Warning Upgraded                                 │
  │     Scheduled: +00:45  •  Fired: 9:52 AM  •  ⏩ 7 min late    │
  │     Fired by: Maria Chen                                       │
  ├────────────────────────────────────────────────────────────────┤
  │ #6  Evacuation Route Flooding                        SKIPPED   │
  │     Scheduled: +01:00  •  Skipped: 10:05 AM                   │
  │     Reason: "Players already addressed route concerns"         │
  └────────────────────────────────────────────────────────────────┘

▶ Phase 3: Response & Life Safety                   0/6 complete
  [collapsed]
```
