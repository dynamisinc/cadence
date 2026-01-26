/**
 * Exercise Menu Configuration
 *
 * Defines navigation menu items shown when user is inside an exercise context.
 * Items are role-filtered based on the user's role within the specific exercise.
 *
 * Menu structure for exercise context:
 * - Hub: Overview/landing page for the exercise
 * - MSEL: Master Scenario Events List
 * - Inject Queue: Real-time inject management
 * - Observations: Evaluator notes and observations
 * - Participants: Exercise participant management
 * - Metrics: Exercise performance metrics
 * - Settings: Exercise configuration
 *
 * @module shared/components/navigation
 * @see docs/features/navigation-shell/S03-in-exercise-context-navigation.md
 */

import {
  faHome,
  faClipboardList,
  faListCheck,
  faBinoculars,
  faUsers,
  faChartBar,
  faCog,
} from '@fortawesome/free-solid-svg-icons'
import type { IconDefinition } from '@fortawesome/free-solid-svg-icons'
import { HseepRole } from '@/types'

/**
 * Exercise menu item configuration
 */
export interface ExerciseMenuItem {
  /** Unique identifier */
  id: string
  /** Display label */
  label: string
  /** FontAwesome icon */
  icon: IconDefinition
  /** Route path relative to /exercises/:id/ */
  path: string
  /** HSEEP roles that can see this item */
  allowedRoles: HseepRole[]
}

/**
 * All HSEEP roles - for items visible to everyone
 */
const ALL_ROLES: HseepRole[] = [
  HseepRole.Administrator,
  HseepRole.ExerciseDirector,
  HseepRole.Controller,
  HseepRole.Evaluator,
  HseepRole.Observer,
]

/**
 * Roles that can access control/inject features
 */
const CONTROL_ROLES: HseepRole[] = [
  HseepRole.Administrator,
  HseepRole.ExerciseDirector,
  HseepRole.Controller,
]

/**
 * Roles that can access observation features
 */
const OBSERVATION_ROLES: HseepRole[] = [
  HseepRole.Administrator,
  HseepRole.ExerciseDirector,
  HseepRole.Evaluator,
  HseepRole.Observer, // Can view but not create
]

/**
 * Roles that can access management features
 */
const MANAGEMENT_ROLES: HseepRole[] = [
  HseepRole.Administrator,
  HseepRole.ExerciseDirector,
]

/**
 * Exercise-specific menu items
 *
 * These appear in the sidebar when user is in exercise context.
 * Paths are relative and will be prefixed with /exercises/:id/
 */
export const EXERCISE_MENU_ITEMS: ExerciseMenuItem[] = [
  {
    id: 'hub',
    label: 'Hub',
    icon: faHome,
    path: '', // /exercises/:id
    allowedRoles: ALL_ROLES,
  },
  {
    id: 'msel',
    label: 'MSEL',
    icon: faClipboardList,
    path: 'msel',
    allowedRoles: CONTROL_ROLES,
  },
  {
    id: 'inject-queue',
    label: 'Inject Queue',
    icon: faListCheck,
    path: 'conduct',
    allowedRoles: CONTROL_ROLES,
  },
  {
    id: 'observations',
    label: 'Observations',
    icon: faBinoculars,
    path: 'observations',
    allowedRoles: OBSERVATION_ROLES,
  },
  {
    id: 'participants',
    label: 'Participants',
    icon: faUsers,
    path: 'participants',
    allowedRoles: MANAGEMENT_ROLES,
  },
  {
    id: 'metrics',
    label: 'Metrics',
    icon: faChartBar,
    path: 'metrics',
    allowedRoles: MANAGEMENT_ROLES,
  },
  {
    id: 'settings',
    label: 'Settings',
    icon: faCog,
    path: 'settings',
    allowedRoles: MANAGEMENT_ROLES,
  },
]

/**
 * Get menu items filtered by user's role
 */
export function getExerciseMenuItems(userRole: HseepRole): ExerciseMenuItem[] {
  return EXERCISE_MENU_ITEMS.filter(item => item.allowedRoles.includes(userRole))
}

/**
 * Build full path for an exercise menu item
 */
export function buildExerciseMenuPath(exerciseId: string, itemPath: string): string {
  if (!itemPath) {
    return `/exercises/${exerciseId}`
  }
  return `/exercises/${exerciseId}/${itemPath}`
}
