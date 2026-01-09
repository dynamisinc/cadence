# exercise-crud/S01: Create Exercise

## Story

**As an** Administrator or Exercise Director,
**I want** to create a new exercise with basic information,
**So that** I can begin configuring and populating the MSEL for an upcoming exercise.

## Context

Creating an exercise is the first step in Cadence. Users need to establish the container that will hold all exercise configuration, objectives, phases, and injects. The creation process should be quick—capturing only essential information—so users can iterate on details later.

New exercises start in **Draft** status, allowing full editing before activation. Only Administrators and Exercise Directors can create exercises, ensuring organizational control over exercise proliferation.

## Acceptance Criteria

### Basic Creation

- [ ] **Given** I am logged in as Administrator or Exercise Director, **when** I navigate to the exercise list, **then** I see a "Create Exercise" button
- [ ] **Given** I am logged in as Controller, Evaluator, or Observer, **when** I navigate to the exercise list, **then** I do NOT see a "Create Exercise" button
- [ ] **Given** I click "Create Exercise", **when** the form loads, **then** I see fields for: Name (required), Exercise Type (required), Description (optional), Start Date (optional), End Date (optional)
- [ ] **Given** I am on the create form, **when** I submit with Name and Exercise Type provided, **then** the exercise is created successfully
- [ ] **Given** I create an exercise, **when** it is saved, **then** it has status "Draft" automatically
- [ ] **Given** I create an exercise, **when** it is saved, **then** it has Practice Mode set to "Off" by default
- [ ] **Given** I create an exercise, **when** it is saved, **then** the CreatedBy field is set to my user ID

### Validation

- [ ] **Given** I am on the create form, **when** I submit without a Name, **then** I see a validation error "Exercise name is required"
- [ ] **Given** I am on the create form, **when** I submit without an Exercise Type, **then** I see a validation error "Exercise type is required"
- [ ] **Given** I am on the create form, **when** I enter a Name longer than 200 characters, **then** I see a validation error "Name must be 200 characters or less"
- [ ] **Given** I provide Start Date and End Date, **when** End Date is before Start Date, **then** I see a validation error "End date must be after start date"

### Post-Creation

- [ ] **Given** I successfully create an exercise, **when** the save completes, **then** I am navigated to the Exercise Setup view
- [ ] **Given** I successfully create an exercise, **when** I view my exercise list, **then** the new exercise appears in the list
- [ ] **Given** I am on the create form, **when** I click "Cancel", **then** I am returned to the exercise list without creating an exercise

## Out of Scope

- Setting time zone during creation (see exercise-config/S03)
- Enabling Practice Mode during creation (see S05)
- Assigning participants (see exercise-config/S02)
- Creating objectives during exercise creation (separate epic)
- Importing from template or duplicating existing exercise

## Dependencies

- User authentication and role assignment
- Exercise entity schema (see `_core/exercise-entity.md`)
- Exercise list view (S03)

## Open Questions

- [ ] Should exercise name be unique within the organization, or can duplicates exist?
- [ ] Should we capture Organization/Agency as a field, or derive from user's organization?
- [ ] Is there a need for exercise templates in MVP?

## Domain Terms

| Term | Definition |
|------|------------|
| Exercise | A planned event involving coordinated activities to test emergency response capabilities |
| Exercise Type | Category of exercise: TTX, Functional, Full-Scale, CAX, or Hybrid |
| Draft Status | Initial exercise state allowing full configuration before activation |
| Practice Mode | Flag indicating exercise is for training, excluded from production reports |

## UI/UX Notes

- Form should be a modal or dedicated page (TBD based on design patterns)
- Exercise Type should use a dropdown with clear descriptions
- Consider placeholder text showing expected formats
- Auto-focus on Name field when form opens
- Show character count for Name field as user types

## Technical Notes

- Exercise ID should be a GUID generated server-side
- CreatedAt timestamp set server-side in UTC
- Ensure audit trail captures creation event
