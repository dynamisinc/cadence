# Story: S02 - Update Role Assignment UI

**Feature:** hseep-participant-roles
**Status:** 📋 Not Started

## User Story

**As an** Exercise Director,
**I want** to see and assign all HSEEP-defined roles when adding participants,
**So that** I can properly organize my exercise staff according to their actual responsibilities.

## Context

The existing participant assignment UI (from exercise-config/S02) displays five roles. After extending the ExerciseRole enum (S01), the UI must be updated to:
1. Display all ten roles with accurate HSEEP descriptions
2. Visually distinguish role categories (Management, Exercise Staff, Participants)
3. Show role-appropriate icons and colors
4. Provide tooltips explaining each role's purpose
5. Handle role selection for both single and bulk participant assignment

This story focuses on UI/UX updates only - permissions are handled in S03-S07.

## Acceptance Criteria

### Role Selection Dropdown

- [ ] **AC-01**: Given I am assigning a participant, when I open the role dropdown, then I see all ten roles grouped by category
  - Test: `AssignParticipantDialog.test.tsx::displays_all_roles_grouped_by_category`
  - Categories:
    - **Management**: Administrator, Exercise Director
    - **Exercise Control Staff**: Controller, Evaluator, Observer
    - **Specialized Staff**: Facilitator, Simulator, Safety Officer, Trusted Agent
    - **Participants**: Player

- [ ] **AC-02**: Given I am viewing the role dropdown, when I hover over a role, then I see a tooltip with the HSEEP description
  - Test: `AssignParticipantDialog.test.tsx::shows_role_description_on_hover`
  - Descriptions from S01 ExerciseRoleDescriptions

- [ ] **AC-03**: Given I am viewing the role dropdown, when I see each role, then it has an appropriate FontAwesome icon
  - Test: `AssignParticipantDialog.test.tsx::displays_role_icons`
  - Suggested icons:
    - Administrator: `faUserShield`
    - ExerciseDirector: `faUserTie`
    - Controller: `faGamepad`
    - Evaluator: `faClipboardCheck`
    - Observer: `faEye`
    - Player: `faUsers`
    - Simulator: `faTheaterMasks`
    - Facilitator: `faChalkboardTeacher`
    - SafetyOfficer: `faShieldHalved`
    - TrustedAgent: `faUserGraduate`

- [ ] **AC-04**: Given I am viewing the role dropdown, when I see each role, then it uses the color defined in S01 AC-08
  - Test: `AssignParticipantDialog.test.tsx::displays_role_colors`

### Participants List View

- [ ] **AC-05**: Given I am viewing the participants list, when I see participants grouped by role, then all ten roles appear as potential group headers
  - Test: `ParticipantsPage.test.tsx::displays_all_role_groups`
  - Groups only shown if participants are assigned to that role

- [ ] **AC-06**: Given I am viewing a participant's role badge, when I see it, then it uses the role's color and icon
  - Test: `ParticipantRow.test.tsx::displays_role_badge_with_color_and_icon`

- [ ] **AC-07**: Given I am viewing the participants list, when no participants are assigned to a role, then that role group is not displayed
  - Test: `ParticipantsPage.test.tsx::hides_empty_role_groups`
  - Exception: Show warning if SafetyOfficer is not assigned to an operations-based exercise (FSE/FE)

### Bulk Import UI

- [ ] **AC-08**: Given I am uploading a participant file, when I see the role column preview, then all ten roles are recognized
  - Test: `BulkImportDialog.test.tsx::recognizes_all_role_names_in_upload`
  - Must handle role name variations (e.g., "Safety Officer", "SafetyOfficer", "Safety")

- [ ] **AC-09**: Given I am reviewing import preview, when I see participants by role, then the new roles are displayed with correct styling
  - Test: `ImportPreviewStep.test.tsx::displays_new_roles_in_preview`

### Role Configuration UI

- [ ] **AC-10**: Given I am configuring enabled roles for an exercise, when I see the role toggles, then all ten roles are listed
  - Test: `ConfigureRolesDialog.test.tsx::displays_all_role_toggles`
  - From exercise-config/S01 - no changes to logic, just display all roles

- [ ] **AC-11**: Given I am viewing role configuration, when I see the new roles, then they default to DISABLED for existing exercises
  - Test: `ConfigureRolesDialog.test.tsx::new_roles_default_disabled_for_existing_exercises`
  - New exercises: All roles enabled by default

## Out of Scope

- Role-specific permissions and access control (S03-S07)
- Role-specific UI adaptations (S08)
- Exercise type-specific role recommendations (future enhancement)

## Dependencies

- hseep-participant-roles/S01: Extend ExerciseRole Enum
- exercise-config/S01: Configure Exercise Roles (existing)
- exercise-config/S02: Assign Participants (existing)

## UI/UX Design

### Role Selection Dialog

```
┌────────────────────────────────────────────────────────────┐
│  Assign Role to Jane Smith                            ✕   │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  MANAGEMENT                                                │
│  ○ 🛡️  Administrator                                      │
│     System-wide configuration and user management          │
│                                                            │
│  ○ 👔 Exercise Director                                   │
│     Full exercise management authority                     │
│                                                            │
│  EXERCISE CONTROL STAFF                                    │
│  ● 🎮 Controller                                          │
│     Inject delivery and conduct management                 │
│                                                            │
│  ○ ✅ Evaluator                                           │
│     Observation recording for AAR                          │
│                                                            │
│  ○ 👁️  Observer                                           │
│     Read-only exercise monitoring                          │
│                                                            │
│  SPECIALIZED STAFF                                         │
│  ○ 🎭 Simulator                                           │
│     SimCell staff role-playing external entities           │
│                                                            │
│  ○ 👨‍🏫 Facilitator                                        │
│     Exercise staff guiding discussions and managing pace   │
│                                                            │
│  ○ 🛡️  Safety Officer                                     │
│     Safety oversight with pause/stop authority             │
│                                                            │
│  ○ 🎓 Trusted Agent                                       │
│     Subject matter experts embedded with players           │
│                                                            │
│  PARTICIPANTS                                              │
│  ○ 👥 Player                                              │
│     Exercise participants being tested or trained          │
│                                                            │
│                 [Cancel]  [Assign Role]                    │
└────────────────────────────────────────────────────────────┘
```

### Participants List with New Roles

```
┌─────────────────────────────────────────────────────────────────────┐
│  Participants                                    [+ Add Participant] │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  EXERCISE DIRECTOR (2)                                              │
│  ├─ James Washington                    [Change Role] [Remove]      │
│  └─ Sarah Martinez                      [Change Role] [Remove]      │
│                                                                     │
│  CONTROLLER (3)                                                     │
│  ├─ Michael Brown                       [Change Role] [Remove]      │
│  ├─ Emily Davis                         [Change Role] [Remove]      │
│  └─ Robert Wilson                       [Change Role] [Remove]      │
│                                                                     │
│  SIMULATOR (2) 🎭                                                   │
│  ├─ Alex Thompson                       [Change Role] [Remove]      │
│  └─ Chris Lee                           [Change Role] [Remove]      │
│                                                                     │
│  SAFETY OFFICER (1) 🛡️                                              │
│  └─ Jordan Martinez                     [Change Role] [Remove]      │
│                                                                     │
│  PLAYER (25) 👥                                                     │
│  ├─ [Show All Players...]                                          │
│                                                                     │
│  ⚠️ No Evaluator assigned                                          │
│  ⚠️ No Facilitator assigned (Recommended for TTX exercises)        │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## Technical Implementation

### Updated Components

**File**: `src/frontend/src/features/exercises/components/AssignParticipantDialog.tsx`

```typescript
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import {
  faUserShield,
  faUserTie,
  faGamepad,
  faClipboardCheck,
  faEye,
  faUsers,
  faTheaterMasks,
  faChalkboardTeacher,
  faShieldHalved,
  faUserGraduate,
} from '@fortawesome/free-solid-svg-icons';

const roleIcons: Record<ExerciseRole, IconDefinition> = {
  [ExerciseRole.Administrator]: faUserShield,
  [ExerciseRole.ExerciseDirector]: faUserTie,
  [ExerciseRole.Controller]: faGamepad,
  [ExerciseRole.Evaluator]: faClipboardCheck,
  [ExerciseRole.Observer]: faEye,
  [ExerciseRole.Player]: faUsers,
  [ExerciseRole.Simulator]: faTheaterMasks,
  [ExerciseRole.Facilitator]: faChalkboardTeacher,
  [ExerciseRole.SafetyOfficer]: faShieldHalved,
  [ExerciseRole.TrustedAgent]: faUserGraduate,
};

const roleCategories = [
  {
    name: 'Management',
    roles: [ExerciseRole.Administrator, ExerciseRole.ExerciseDirector],
  },
  {
    name: 'Exercise Control Staff',
    roles: [ExerciseRole.Controller, ExerciseRole.Evaluator, ExerciseRole.Observer],
  },
  {
    name: 'Specialized Staff',
    roles: [ExerciseRole.Facilitator, ExerciseRole.Simulator, ExerciseRole.SafetyOfficer, ExerciseRole.TrustedAgent],
  },
  {
    name: 'Participants',
    roles: [ExerciseRole.Player],
  },
];
```

**File**: `src/frontend/src/features/exercises/components/RoleBadge.tsx`

```typescript
interface RoleBadgeProps {
  role: ExerciseRole;
  showIcon?: boolean;
  size?: 'small' | 'medium';
}

export const RoleBadge: React.FC<RoleBadgeProps> = ({ role, showIcon = true, size = 'medium' }) => {
  const displayName = ExerciseRoleDisplayNames[role];
  const color = getRoleColor(role);
  const icon = roleIcons[role];

  return (
    <Chip
      label={
        <>
          {showIcon && <FontAwesomeIcon icon={icon} style={{ marginRight: 4 }} />}
          {displayName}
        </>
      }
      size={size}
      sx={{
        backgroundColor: color,
        color: '#fff',
        fontWeight: 500,
      }}
    />
  );
};
```

### Utility Functions

**File**: `src/frontend/src/features/exercises/utils/roleUtils.ts`

```typescript
import { useTheme } from '@mui/material';

export const getRoleColor = (role: ExerciseRole): string => {
  const theme = useTheme();

  switch (role) {
    case ExerciseRole.Administrator:
      return theme.palette.error.dark; // Red
    case ExerciseRole.ExerciseDirector:
      return theme.palette.primary.main; // Blue
    case ExerciseRole.Controller:
      return theme.palette.success.main; // Green
    case ExerciseRole.Evaluator:
      return theme.palette.secondary.main; // Purple
    case ExerciseRole.Observer:
      return theme.palette.grey[600]; // Gray
    case ExerciseRole.Player:
      return theme.palette.info.main; // Light blue
    case ExerciseRole.Simulator:
      return theme.palette.warning.main; // Orange
    case ExerciseRole.Facilitator:
      return theme.palette.success.light; // Light green
    case ExerciseRole.SafetyOfficer:
      return theme.palette.error.main; // Bright red
    case ExerciseRole.TrustedAgent:
      return theme.palette.secondary.light; // Light purple
    default:
      return theme.palette.grey[500];
  }
};

export const getRoleIcon = (role: ExerciseRole): IconDefinition => {
  return roleIcons[role];
};

export const getRoleCategory = (role: ExerciseRole): string => {
  for (const category of roleCategories) {
    if (category.roles.includes(role)) {
      return category.name;
    }
  }
  return 'Other';
};
```

## Test Coverage

### Frontend Tests
- `src/frontend/src/features/exercises/components/AssignParticipantDialog.test.tsx`
- `src/frontend/src/features/exercises/components/ParticipantsPage.test.tsx`
- `src/frontend/src/features/exercises/components/ParticipantRow.test.tsx`
- `src/frontend/src/features/exercises/components/RoleBadge.test.tsx`
- `src/frontend/src/features/exercises/utils/roleUtils.test.ts`
- `src/frontend/src/features/bulk-participant-import/components/BulkImportDialog.test.tsx`

## Accessibility Considerations

- [ ] Role icons must have `aria-label` attributes
- [ ] Color is not the only visual indicator (icons also used)
- [ ] Dropdown items are keyboard navigable
- [ ] Role descriptions available to screen readers

## Related Stories

- hseep-participant-roles/S01: Extend ExerciseRole Enum (prerequisite)
- hseep-participant-roles/S08: Role-Specific UI Adaptations
- hseep-participant-roles/S09: Bulk Import Support for New Roles

---

*Last updated: 2026-02-09*
