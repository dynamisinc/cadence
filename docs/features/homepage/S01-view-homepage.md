# homepage/S01: View Homepage with Role-Aware Content

## Story

**As a** user with any HSEEP role,
**I want** to view a personalized homepage that displays my role and recent exercises,
**So that** I understand my capabilities and can quickly access exercises I'm working on.

## Context

The homepage is the first screen users see after authenticating. It serves as an orientation point, helping users understand their role within Cadence and providing immediate access to their exercises.

Different HSEEP roles have different responsibilities during exercise conduct, so the homepage adapts its welcome messaging to help users understand what they can do. For example, an Exercise Director needs to know they can create and manage exercises, while an Observer needs to understand they have view-only access.

The homepage prioritizes recent activity by showing up to 5 non-archived exercises, with a link to view the full exercise list if more exist. This balance keeps the page focused while ensuring users can find their work quickly.

## Acceptance Criteria

### Welcome Section

- [ ] **Given** I am authenticated, **when** I navigate to the homepage, **then** I see a welcome section with "Welcome to Cadence" heading
- [ ] **Given** I am on the homepage, **when** the welcome section renders, **then** I see "HSEEP-Compliant MSEL Management Platform" subtitle
- [ ] **Given** I am on the homepage, **when** the welcome section renders, **then** I see my role displayed with label "Your Role:"
- [ ] **Given** I have MANAGE permission, **when** I view my role, **then** it displays as "Exercise Director"
- [ ] **Given** I have CONTRIBUTOR permission, **when** I view my role, **then** it displays as "Controller"
- [ ] **Given** I have READONLY permission, **when** I view my role, **then** it displays as "Observer"

### Role-Specific Messaging

- [ ] **Given** I have MANAGE permission, **when** I view the welcome section, **then** I see message "You have full access to create and manage exercises."
- [ ] **Given** I have CONTRIBUTOR permission, **when** I view the welcome section, **then** I see message "You can view exercises and fire injects during conduct."
- [ ] **Given** I have READONLY permission, **when** I view the welcome section, **then** I see message "You have read-only access to view exercises."

### Exercise List Display

- [ ] **Given** I have access to exercises, **when** I view the homepage, **then** I see a "Your Exercises" section heading
- [ ] **Given** exercises exist, **when** the list renders, **then** I see up to 5 exercises displayed in a table
- [ ] **Given** exercises exist, **when** the list renders, **then** each row shows: Name, Type, Status, Scheduled Date, and Practice indicator
- [ ] **Given** more than 5 exercises exist, **when** I scroll to the bottom of the list, **then** I see a "View All X Exercises" button with the total count
- [ ] **Given** 5 or fewer exercises exist, **when** I view the list, **then** I do NOT see a "View All" button

### Exercise Metadata Display

- [ ] **Given** an exercise has Practice Mode enabled, **when** it appears in the list, **then** I see a wrench icon (🔧) in the Practice column
- [ ] **Given** I hover over the practice mode icon, **when** the tooltip appears, **then** it shows "Practice Mode - excluded from production reports"
- [ ] **Given** an exercise has Practice Mode disabled, **when** it appears in the list, **then** the Practice column is empty for that row
- [ ] **Given** an exercise has a scheduled date, **when** it displays, **then** the date is formatted as "MMM d, yyyy" (e.g., "Jun 15, 2025")
- [ ] **Given** an exercise has a status, **when** it displays, **then** I see a status chip with appropriate color (Draft: neutral, Active: success/green, Completed: muted)

### Filtering and Ordering

- [ ] **Given** I have access to both archived and non-archived exercises, **when** I view the homepage, **then** only non-archived exercises are shown
- [ ] **Given** archived exercises exist, **when** I view the homepage, **then** they do not appear in the exercise list
- [ ] **Given** I have more than 5 non-archived exercises, **when** the list renders, **then** only the first 5 are displayed

### Navigation

- [ ] **Given** I click on an exercise row, **when** the click registers, **then** I am navigated to that exercise's detail page at `/exercises/{id}`
- [ ] **Given** I click the "View All X Exercises" button, **when** the click registers, **then** I am navigated to the full exercise list at `/exercises`

### Loading State

- [ ] **Given** exercises are loading, **when** I view the homepage, **then** I see skeleton placeholders for the exercise list table
- [ ] **Given** exercises are loading, **when** I view the homepage, **then** I see 3 skeleton rows with appropriate column widths

### Error State

- [ ] **Given** an error occurs while fetching exercises, **when** I view the homepage, **then** I see an error message in place of the exercise list
- [ ] **Given** I see an error message, **when** I click "Retry", **then** the page reloads and attempts to fetch exercises again

### Empty State - All Exercises Archived

- [ ] **Given** I have exercises but all are archived, **when** I view the homepage, **then** I see message "All exercises are archived. Check the full exercise list to view archived exercises."
- [ ] **Given** I see the "all archived" message, **when** I view the page, **then** the message is displayed in a dashed-border box with muted styling

## Out of Scope

- Creating exercises from the homepage (see S02 for quick actions)
- Filtering or searching exercises on the homepage
- Sorting exercises by different criteria
- Displaying exercise metrics or statistics
- Showing inject counts or completion percentages
- Live updates via SignalR (future enhancement)
- Customizing which exercises appear (favorites/pinning)

## Dependencies

- exercise-crud/S03: View Exercise List (uses `useExercises` hook)
- _cross-cutting/S01: Session Management (user authentication and role)
- _core/exercise-entity.md: Exercise schema (status, type, practice mode fields)

## Open Questions

- [ ] Should the 5-exercise limit be configurable per user?
- [ ] Should we show last modified date in addition to scheduled date?
- [ ] Should archived exercises be accessible via a toggle on the homepage, or only via full list?

## Domain Terms

| Term | Definition |
|------|------------|
| Homepage | Landing page displayed after user authentication |
| Role-Aware | Interface adapts based on user's HSEEP role assignment |
| Exercise Director | HSEEP role with full exercise management capabilities (MANAGE permission) |
| Controller | HSEEP role that delivers injects during conduct (CONTRIBUTOR permission) |
| Observer | HSEEP role with view-only access to exercises (READONLY permission) |
| Practice Mode | Flag indicating exercise is for training, excluded from production reports |
| Archived | Status indicating exercise is hidden from normal views but retained for reference |

## UI/UX Notes

### Welcome Section Styling
- Gradient background (light blue/gray gradient)
- Generous padding for visual emphasis
- Role name displayed in primary color with bold weight
- Horizontal divider separates header from role information

### Exercise List Table
- Small/compact table size for efficient space usage
- Hover effect on rows indicates clickability
- Practice column uses icon-only display to save space
- Status chips use color-coded badges for at-a-glance comprehension

### Layout Spacing
- Main window padding uses `CobraStyles.Padding.MainWindow`
- Sections separated by `mb: 3` (24px margin bottom)
- Consistent vertical rhythm throughout

### Touch Targets
- Table rows have minimum 44px height for tablet/mobile accessibility
- All clickable areas meet WCAG touch target size guidelines

## Technical Notes

- Uses `useExercises()` hook from exercise-crud feature
- Uses `usePermissions()` hook to determine role and capabilities
- Date formatting via `date-fns` library (`format(parseISO(dateStr), 'MMM d, yyyy')`)
- Exercise filtering happens client-side (already fetched via hook)
- Navigation uses React Router's `useNavigate` hook
- Status and Type chips reuse components from exercise-crud feature
