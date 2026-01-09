# exercise-crud/S05: Practice Mode

## Story

**As an** Administrator or Exercise Director,
**I want** to designate an exercise as a practice/training exercise,
**So that** training activities are clearly distinguished from production exercises and excluded from official reports and metrics.

## Context

Organizations need to train their staff on Cadence and run rehearsal exercises before high-stakes events. Practice Mode provides a safe environment where users can experiment without polluting production metrics or creating confusion about what constitutes official exercise data.

Practice exercises are:
- **Visually distinct** throughout the application
- **Excluded** from official exercise counts and reports
- **Included** in user-specific training metrics (optional future feature)

This concept was identified during EXIS analysis where lack of training mode caused organizations to create workarounds that cluttered their exercise data.

## Acceptance Criteria

### Enabling Practice Mode

- [ ] **Given** I am an Administrator or Exercise Director, **when** I view a Draft exercise, **then** I see a "Practice Mode" toggle
- [ ] **Given** I am logged in as Controller, Evaluator, or Observer, **when** I view an exercise, **then** I do NOT see the Practice Mode toggle
- [ ] **Given** I toggle Practice Mode ON, **when** I save, **then** the exercise is marked as a practice exercise
- [ ] **Given** I toggle Practice Mode OFF, **when** I save, **then** the exercise is marked as a production exercise

### Status Restrictions

- [ ] **Given** an exercise is in Draft status, **when** I view Practice Mode toggle, **then** it is enabled and can be changed
- [ ] **Given** an exercise is in Active status, **when** I view Practice Mode toggle, **then** it is disabled with tooltip "Cannot change during active exercise"
- [ ] **Given** an exercise is in Completed or Archived status, **when** I view Practice Mode toggle, **then** it is disabled

### Visual Indicators

- [ ] **Given** an exercise has Practice Mode ON, **when** displayed in the exercise list, **then** it shows a practice indicator (🔧 icon or "Practice" badge)
- [ ] **Given** an exercise has Practice Mode ON, **when** I am working within that exercise, **then** a persistent banner shows "Practice Exercise - Not included in reports"
- [ ] **Given** an exercise has Practice Mode ON, **when** viewing the exercise header anywhere, **then** the practice status is clearly visible
- [ ] **Given** an exercise has Practice Mode OFF, **when** displayed anywhere, **then** no practice indicators are shown

### Report Exclusion

- [ ] **Given** I generate any exercise report or metrics, **when** the report is created, **then** exercises with Practice Mode ON are excluded by default
- [ ] **Given** I view aggregate organization metrics (future), **when** metrics are calculated, **then** practice exercises are excluded
- [ ] **Given** I export data, **when** the export includes practice exercises, **then** they are clearly marked as practice in the export

### Filtering

- [ ] **Given** I view the exercise list, **when** filtering options are available, **then** I can filter to show "Practice Only" or "Production Only" or "All"
- [ ] **Given** the filter is set to "Production Only" (default), **when** I view the list, **then** practice exercises are hidden
- [ ] **Given** the filter is set to "All" or "Practice Only", **when** I view the list, **then** practice exercises are visible with indicators

## Out of Scope

- Training progress tracking for individual users
- Practice mode for individual injects within production exercises
- Automatic conversion from practice to production exercise
- Time-limited practice exercises
- Practice exercise templates

## Dependencies

- exercise-crud/S01: Create Exercise
- exercise-crud/S02: Edit Exercise
- exercise-crud/S03: View Exercise List
- Exercise entity with IsPracticeMode field (see `_core/exercise-entity.md`)

## Open Questions

- [ ] Should the default filter show or hide practice exercises?
- [ ] Should practice exercises count against any user quotas or limits?
- [ ] Can a practice exercise be "promoted" to production by clearing the flag?
- [ ] Should there be a visual theme change (e.g., different color accent) for practice exercises?

## Domain Terms

| Term | Definition |
|------|------------|
| Practice Mode | Exercise flag indicating it is for training/rehearsal purposes |
| Practice Exercise | An exercise with Practice Mode enabled, excluded from production reports |
| Production Exercise | A standard exercise that counts toward official metrics and reports |

## UI/UX Notes

### Practice Mode Toggle
```
┌─────────────────────────────────────────────┐
│  Exercise Settings                          │
├─────────────────────────────────────────────┤
│                                             │
│  Practice Mode  [OFF ─────● ON]             │
│                                             │
│  When enabled, this exercise is marked      │
│  as training and excluded from official     │
│  reports and metrics.                       │
│                                             │
└─────────────────────────────────────────────┘
```

### Practice Banner (shown when in practice exercise)
```
┌─────────────────────────────────────────────────────────────────┐
│ 🔧 Practice Exercise - Not included in reports                  │
└─────────────────────────────────────────────────────────────────┘
```

- Banner should be dismissible but return on next page load
- Use distinct but not alarming color (amber/yellow suggested)
- Icon should be consistent across all practice indicators

## Technical Notes

- IsPracticeMode is a boolean field on Exercise entity
- Default value: false (production)
- Include in all relevant queries as filter parameter
- Consider analytics events to track practice vs production usage
