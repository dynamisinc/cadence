# homepage/S02: Quick Actions Based on Permissions

## Story

**As a** user with appropriate permissions,
**I want** to see quick action buttons on the homepage,
**So that** I can quickly create exercises or navigate to the full exercise list without navigating through multiple pages.

## Context

The homepage provides immediate access to common actions through a prominent quick actions section. These actions are permission-aware, ensuring users only see buttons for actions they're authorized to perform.

Exercise Directors (MANAGE role) need quick access to create new exercises, as this is a frequent workflow. All authenticated users need quick access to the full exercise list to search, filter, and browse beyond the 5 exercises shown on the homepage.

The quick actions appear above the exercise list, making them immediately visible without scrolling. This placement follows the F-shaped reading pattern and ensures key actions are accessible on tablet and mobile devices.

## Acceptance Criteria

### Create Exercise Button (Managers Only)

- [ ] **Given** I have MANAGE permission, **when** I view the homepage, **then** I see a "Create Exercise" button in the quick actions section
- [ ] **Given** I have MANAGE permission, **when** I view the Create Exercise button, **then** it is styled as a primary button (blue, bold)
- [ ] **Given** I have MANAGE permission, **when** I view the Create Exercise button, **then** it includes a plus icon (AddIcon) as a start icon
- [ ] **Given** I have MANAGE permission, **when** I click the Create Exercise button, **then** I am navigated to `/exercises/new`
- [ ] **Given** I have CONTRIBUTOR or READONLY permission, **when** I view the homepage, **then** I do NOT see the Create Exercise button

### View All Exercises Button (All Users)

- [ ] **Given** I am authenticated with any role, **when** I view the homepage, **then** I see a "View All Exercises" button in the quick actions section
- [ ] **Given** I view the View All Exercises button, **when** it renders, **then** it is styled as a secondary button (outlined)
- [ ] **Given** I view the View All Exercises button, **when** it renders, **then** it includes a list icon (ListAltIcon) as a start icon
- [ ] **Given** I click the View All Exercises button, **when** the click registers, **then** I am navigated to `/exercises`

### Layout and Positioning

- [ ] **Given** I have MANAGE permission, **when** I view the quick actions, **then** the Create Exercise button appears first (left), followed by View All Exercises
- [ ] **Given** I have CONTRIBUTOR or READONLY permission, **when** I view the quick actions, **then** only the View All Exercises button is shown
- [ ] **Given** I view the quick actions section, **when** it renders, **then** it appears below the welcome section and above the exercise list
- [ ] **Given** I view the quick actions section, **when** it renders, **then** buttons are arranged horizontally with consistent spacing

### Empty State Integration

- [ ] **Given** I have MANAGE permission and no exercises exist, **when** I view the empty state, **then** I see an additional "Create Exercise" button within the empty state card
- [ ] **Given** I have MANAGE permission and no exercises exist, **when** I click the empty state Create Exercise button, **then** I am navigated to `/exercises/new` (same as quick action button)
- [ ] **Given** I have CONTRIBUTOR or READONLY permission and no exercises exist, **when** I view the empty state, **then** I do NOT see a Create Exercise button
- [ ] **Given** I have CONTRIBUTOR or READONLY permission and no exercises exist, **when** I view the empty state, **then** I see message "You haven't been assigned to any exercises yet. Contact your Exercise Director to get added to an upcoming exercise."

### Empty State Styling - Manager

- [ ] **Given** I have MANAGE permission and no exercises exist, **when** I view the empty state, **then** I see a gradient blue background with primary color borders
- [ ] **Given** I have MANAGE permission and no exercises exist, **when** I view the empty state, **then** I see a circular icon container with PlaylistAddIcon
- [ ] **Given** I have MANAGE permission and no exercises exist, **when** I view the empty state, **then** the heading reads "Create Your First Exercise"
- [ ] **Given** I have MANAGE permission and no exercises exist, **when** I view the empty state, **then** I see descriptive text about getting started

### Empty State Styling - Viewer

- [ ] **Given** I have CONTRIBUTOR or READONLY permission and no exercises exist, **when** I view the empty state, **then** I see a muted gray background with dashed borders
- [ ] **Given** I have CONTRIBUTOR or READONLY permission and no exercises exist, **when** I view the empty state, **then** I see a circular icon container with AssignmentIcon in gray
- [ ] **Given** I have CONTRIBUTOR or READONLY permission and no exercises exist, **when** I view the empty state, **then** the heading reads "No Exercises Assigned"

## Out of Scope

- Customizing which quick actions appear (user preferences)
- Additional quick actions beyond Create Exercise and View All
- Recent actions or most-used actions
- Keyboard shortcuts for quick actions (see _cross-cutting/S02 for keyboard navigation)
- Role-specific quick actions beyond exercise creation (e.g., "View My Observations")
- Quick action tooltips or help text

## Dependencies

- exercise-crud/S01: Create Exercise (navigation target for Create button)
- exercise-crud/S03: View Exercise List (navigation target for View All button)
- _cross-cutting/S01: Session Management (permission checking)
- homepage/S01: View Homepage (context for where quick actions appear)

## Open Questions

- [ ] Should we add a tooltip to the Create Exercise button explaining what it does for first-time users?
- [ ] Should quick actions persist as a sticky header when scrolling on long exercise lists (future enhancement)?
- [ ] Should we track quick action usage for analytics?

## Domain Terms

| Term | Definition |
|------|------------|
| Quick Actions | Prominent action buttons providing immediate access to common tasks |
| MANAGE Permission | Role permission level for Exercise Directors and Administrators |
| CONTRIBUTOR Permission | Role permission level for Controllers |
| READONLY Permission | Role permission level for Evaluators and Observers |
| Empty State | UI shown when no data exists, providing contextual guidance |

## UI/UX Notes

### Button Hierarchy

Primary button (Create Exercise):
- Blue background (#1976d2)
- White text
- Contained style (filled)
- Draws attention as the primary action

Secondary button (View All Exercises):
- Outlined style
- Blue border and text
- Lighter visual weight
- Available to all users

### Icon Usage

- **AddIcon**: Universal symbol for creation/addition
- **ListAltIcon**: Represents a list or directory view
- **PlaylistAddIcon** (empty state): Larger icon suggesting starting a new collection
- **AssignmentIcon** (empty state): Represents work/tasks, neutral connotation

### Spacing and Alignment

- Quick actions section has 16px horizontal spacing between buttons
- Quick actions section has 24px bottom margin to separate from exercise list
- Empty state card has generous vertical padding (40px-48px) for visual breathing room
- Empty state icon circle is 64-80px diameter for visual prominence

### Responsive Behavior

- On tablet landscape: Buttons remain side-by-side
- On tablet portrait: Buttons may stack vertically (future enhancement)
- On mobile: Buttons stack vertically with full width (future enhancement)
- Touch targets meet 44px minimum height for accessibility

### Empty State Visual Hierarchy

Manager empty state:
1. Large icon in gradient circle (draws attention)
2. Bold heading
3. Descriptive text
4. Prominent primary button

Viewer empty state:
1. Smaller muted icon
2. Heading
3. Informational text
4. No action button (cannot create)

## Technical Notes

- Permission checking uses `usePermissions()` hook with `canManage` boolean
- Button components use COBRA styled components (`CobraPrimaryButton`, `CobraSecondaryButton`)
- Navigation uses React Router's `useNavigate()` hook
- Empty state is rendered by `ExerciseList` component, receives `canManage` and `onCreateClick` props
- Empty state for managers has `backgroundColor: 'primary.50'` and `borderColor: 'primary.200'`
- Empty state for viewers has `backgroundColor: 'grey.50'` and `borderColor: 'grey.300'`
- Icon containers use circular shape with `borderRadius: '50%'`
