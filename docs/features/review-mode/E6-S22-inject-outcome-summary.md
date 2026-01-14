# E6-S22: Inject Outcome Summary

**Feature:** review-mode  
**Priority:** P1  
**Estimate:** 1 day

## User Story

**As** Robert (Evaluator),  
**I want** to see a summary of inject outcomes,  
**So that** I can quickly identify patterns and issues across the exercise.

## Context

Evaluators need to see the big picture: How many injects were late? Which Controllers were busiest? Were contingency injects used? This summary supports AAR discussion.

## Acceptance Criteria

- [ ] **Given** I am in Review Mode, **when** I view the summary panel, **then** I see total inject counts by status (Fired, Skipped, Pending)
- [ ] **Given** the summary panel, **when** I view timing stats, **then** I see: On-time count, Early count, Late count, Average variance
- [ ] **Given** the summary panel, **when** I view inject type breakdown, **then** I see counts for: Standard, Contingency, Adaptive, Complexity
- [ ] **Given** contingency injects were used, **when** I view the summary, **then** I see which contingency injects were fired and why
- [ ] **Given** the summary panel, **when** I view Controller activity, **then** I see injects fired per Controller

## Dependencies

- E6-S20: Access Review Mode

## UI/UX Notes

```
┌─────────────────────────────────────────────────────────────────┐
│ EXERCISE SUMMARY                                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Inject Status                    Timing Performance            │
│  ─────────────                    ──────────────────            │
│  ✅ Fired:     11                 ✓ On Time:    8 (73%)        │
│  ⏭ Skipped:    2                 ⏪ Early:      1 ( 9%)        │
│  ⏳ Pending:    1                 ⏩ Late:       2 (18%)        │
│  ───────────────                  Avg Variance: +3 min         │
│  Total:       14                                                │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Inject Types Used                Controller Activity           │
│  ─────────────────                ───────────────────           │
│  Standard:      10                Maria Chen:      7            │
│  Contingency:    1 (fired)        Sarah Martinez:  4            │
│  Adaptive:       1 (skipped)      Michael Brown:   0            │
│  Complexity:     0 (skipped)                                    │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ⚠️ Contingency Inject Used                                     │
│  #6 "Evacuation Route Flooding" was fired because players       │
│  did not address route planning in time.                        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```
