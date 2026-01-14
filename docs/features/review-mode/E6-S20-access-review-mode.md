# E6-S20: Access Review Mode

**Feature:** review-mode  
**Priority:** P1  
**Estimate:** 0.5 days

## User Story

**As** James (Exercise Director),  
**I want** to access a Review Mode for completed or paused exercises,  
**So that** I can analyze what happened without the real-time conduct interface.

## Context

During conduct, the interface is optimized for "what's next" (time-sorted). After conduct, users need "what happened" (outcome-sorted). Review Mode provides this alternate view.

## Acceptance Criteria

- [ ] **Given** an exercise with status Active or Completed, **when** I view the exercise, **then** I see a "Review Mode" button/tab
- [ ] **Given** I click "Review Mode", **when** the view loads, **then** I see a phase-organized view of all injects
- [ ] **Given** I am in Review Mode, **when** I click "Conduct Mode", **then** I return to the real-time conduct view
- [ ] **Given** an exercise with status Draft, **when** I view the exercise, **then** Review Mode is not available (nothing to review)
- [ ] **Given** the exercise clock is running, **when** I access Review Mode, **then** I see a warning: "Exercise is in progress. Review data may change."

## Out of Scope

- Restricting Review Mode to certain roles (all roles can access)
- Locking Review Mode after a certain time

## Dependencies

- Phase D complete (Clock, Fire functionality)
- Phase E complete (Observations)

## UI/UX Notes

- Tab or toggle at top of Conduct page: `[Conduct] [Review]`
- Review Mode should feel distinct (different layout, muted colors)
- Read-only — no Fire/Skip buttons in Review Mode
