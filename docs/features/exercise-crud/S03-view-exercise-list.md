# exercise-crud/S03: View Exercise List

## Story

**As a** user with any role,
**I want** to view a list of exercises I have access to,
**So that** I can find and navigate to exercises I'm working on.

## Context

The exercise list is the primary navigation hub in Cadence. Users need to quickly find their exercises, understand their status, and access them for configuration or conduct. The list should surface the most relevant information at a glance while supporting users managing multiple exercises.

All authenticated users can view exercises—but only exercises they're assigned to. Administrators see all exercises across the organization.

## Acceptance Criteria

### List Display

- [ ] **Given** I am authenticated, **when** I navigate to the exercise list, **then** I see all exercises I have access to
- [ ] **Given** I am an Administrator, **when** I view the exercise list, **then** I see all exercises in the organization
- [ ] **Given** I am not an Administrator, **when** I view the exercise list, **then** I see only exercises where I am assigned a role
- [ ] **Given** exercises exist, **when** the list loads, **then** each exercise displays: Name, Exercise Type, Status, Start Date, Practice Mode indicator

### Practice Mode Indicator

- [ ] **Given** an exercise has Practice Mode enabled, **when** displayed in the list, **then** it shows a visual indicator (🔧 icon and/or "Practice" badge)
- [ ] **Given** an exercise has Practice Mode disabled, **when** displayed in the list, **then** no practice indicator is shown

### Status Display

- [ ] **Given** an exercise is in Draft status, **when** displayed in the list, **then** it shows a "Draft" badge with neutral styling
- [ ] **Given** an exercise is in Active status, **when** displayed in the list, **then** it shows an "Active" badge with success/green styling
- [ ] **Given** an exercise is in Completed status, **when** displayed in the list, **then** it shows a "Completed" badge with muted styling
- [ ] **Given** an exercise is in Archived status, **when** displayed in the list, **then** it is hidden from the default view

### Filtering and Sorting

- [ ] **Given** I view the exercise list, **when** I click on a column header (Name, Type, Status, Start Date), **then** the list sorts by that column
- [ ] **Given** I click a column header twice, **when** the list re-sorts, **then** it toggles between ascending and descending order
- [ ] **Given** the list has a status filter, **when** I select "Show Archived", **then** archived exercises appear in the list
- [ ] **Given** the list has a search box, **when** I type a search term, **then** the list filters to exercises with matching names

### Default Behavior

- [ ] **Given** I have no exercises assigned, **when** I view the exercise list, **then** I see an empty state with message "No exercises found" and guidance based on my role
- [ ] **Given** I am Administrator or Exercise Director with no exercises, **when** I see the empty state, **then** I see a "Create Exercise" call-to-action
- [ ] **Given** I am Controller, Evaluator, or Observer with no exercises, **when** I see the empty state, **then** I see a message "You haven't been assigned to any exercises yet"
- [ ] **Given** exercises exist, **when** the list loads, **then** they are sorted by Start Date descending (most recent first) by default

### Navigation

- [ ] **Given** I click on an exercise row, **when** the click registers, **then** I am navigated to that exercise's detail/setup view
- [ ] **Given** I am on the exercise list, **when** I click "Create Exercise" (if visible), **then** I am navigated to the create exercise form

### Responsive Design

- [ ] **Given** I am on a tablet in portrait mode, **when** I view the exercise list, **then** the layout adapts to show essential columns only (Name, Status, Date)
- [ ] **Given** I am on a tablet in landscape mode, **when** I view the exercise list, **then** all columns are visible

## Out of Scope

- Inline actions (edit, archive, delete) from list view—actions are in detail view
- Advanced filtering (by date range, type, multiple statuses)
- Pagination/infinite scroll—handle in Standard phase if list grows large
- Exercise thumbnails or preview cards
- Favorites or pinned exercises

## Dependencies

- User authentication and role assignment
- Exercise entity schema (see `_core/exercise-entity.md`)
- Responsive design guidelines (see `_cross-cutting/responsive-design.md`)

## Open Questions

- [ ] Should the list show last modified date or only start date?
- [ ] Should we show inject count per exercise as a quick metric?
- [ ] For the "Type" column, should we show full name or abbreviation (e.g., "TTX" vs "Table Top Exercise")?

## Domain Terms

| Term | Definition |
|------|------------|
| Exercise | A planned event involving coordinated activities to test emergency response capabilities |
| Practice Mode | Flag indicating exercise is for training, excluded from production reports |
| Archived | Status indicating exercise is hidden from normal views but retained for reference |

## UI/UX Notes

### List Layout
```
┌─────────────────────────────────────────────────────────────────┐
│  [Search...🔍]                              [+ Create Exercise] │
├─────────────────────────────────────────────────────────────────┤
│  Name ▼        │ Type    │ Status   │ Start Date │ Practice    │
├─────────────────────────────────────────────────────────────────┤
│  Hurricane Ex  │ FSE     │ ● Active │ Jan 15     │             │
│  Cyber TTX 🔧  │ TTX     │ ○ Draft  │ Feb 1      │ 🔧          │
│  Flood Drill   │ FE      │ ◐ Done   │ Dec 10     │             │
└─────────────────────────────────────────────────────────────────┘
```

- Clicking row navigates to exercise
- Status uses color-coded badges
- Practice mode uses subtle but clear indicator
- Touch targets are 44px minimum height for tablet use

## Technical Notes

- Implement server-side filtering for role-based access
- Consider caching exercise list for offline access
- Use virtual scrolling if list exceeds 100 items (Standard phase)
