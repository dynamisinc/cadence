/**
 * Menu Configuration
 *
 * Defines all navigation menu items with:
 * - HSEEP role-based visibility
 * - Section grouping (CONDUCT, ANALYSIS, SYSTEM)
 * - Exercise context requirements
 *
 * Menu structure per HSEEP workflow:
 * - CONDUCT: Day-to-day exercise operations
 * - ANALYSIS: Post-exercise review and reporting
 * - SYSTEM: Administration and configuration
 */

import {
  faClipboardList,
  faFolderOpen,
  faGamepad,
  faListCheck,
  faBinoculars,
  faChartBar,
  faFileAlt,
  faUsers,
  faCog,
} from '@fortawesome/free-solid-svg-icons'
import { HseepRole, SystemRole } from '../../../types'
import type { MenuItem } from './types'

/**
 * All roles - used for items visible to everyone
 */
const ALL_HSEEP_ROLES: HseepRole[] = [
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
]

/**
 * Roles that can access reporting features
 */
const REPORTING_ROLES: HseepRole[] = [
  HseepRole.Administrator,
  HseepRole.ExerciseDirector,
]

/**
 * Admin-only features
 */
const ADMIN_ROLES: HseepRole[] = [
  HseepRole.Administrator,
]

/**
 * Complete menu configuration
 *
 * 9 menu items across 3 sections:
 * - CONDUCT (4): My Assignments, Exercises, Control Room, Inject Queue
 * - ANALYSIS (2): Observations, Reports
 * - SYSTEM (3): Templates, Users, Settings
 */
export const MENU_ITEMS: MenuItem[] = [
  // ============================================================================
  // CONDUCT Section - Day-to-day exercise operations
  // ============================================================================
  {
    id: 'my-assignments',
    label: 'My Assignments',
    icon: faClipboardList,
    path: '/assignments',
    section: 'conduct',
    allowedRoles: ALL_HSEEP_ROLES,
  },
  {
    id: 'exercises',
    label: 'Exercises',
    icon: faFolderOpen,
    path: '/exercises',
    section: 'conduct',
    allowedRoles: ALL_HSEEP_ROLES,
  },
  {
    id: 'control-room',
    label: 'Control Room',
    icon: faGamepad,
    path: '/exercises/:id/control',
    section: 'conduct',
    allowedRoles: CONTROL_ROLES,
    requiresExerciseContext: true,
    disabledTooltip: 'Enter an exercise first',
  },
  {
    id: 'inject-queue',
    label: 'Inject Queue',
    icon: faListCheck,
    path: '/exercises/:id/queue',
    section: 'conduct',
    allowedRoles: CONTROL_ROLES,
    requiresExerciseContext: true,
    disabledTooltip: 'Enter an exercise first',
  },

  // ============================================================================
  // ANALYSIS Section - Post-exercise review and reporting
  // ============================================================================
  {
    id: 'observations',
    label: 'Observations',
    icon: faBinoculars,
    path: '/exercises/:id/observations',
    section: 'analysis',
    allowedRoles: OBSERVATION_ROLES,
    requiresExerciseContext: true,
    disabledTooltip: 'Enter an exercise first',
  },
  {
    id: 'reports',
    label: 'Reports',
    icon: faChartBar,
    path: '/reports',
    section: 'analysis',
    allowedRoles: REPORTING_ROLES,
  },

  // ============================================================================
  // SYSTEM Section - Administration and configuration
  // ============================================================================
  {
    id: 'templates',
    label: 'Templates',
    icon: faFileAlt,
    path: '/templates',
    section: 'system',
    allowedRoles: ADMIN_ROLES,
    allowedSystemRoles: [SystemRole.Admin],
  },
  {
    id: 'users',
    label: 'Users',
    icon: faUsers,
    path: '/users',
    section: 'system',
    allowedRoles: ADMIN_ROLES,
    allowedSystemRoles: [SystemRole.Admin],
  },
  {
    id: 'settings',
    label: 'Settings',
    icon: faCog,
    path: '/settings',
    section: 'system',
    allowedRoles: ALL_HSEEP_ROLES,
  },
]

/**
 * Get a menu item by ID
 */
export const getMenuItemById = (id: string): MenuItem | undefined => {
  return MENU_ITEMS.find(item => item.id === id)
}

/**
 * Get all menu items for a specific section
 */
export const getMenuItemsBySection = (section: MenuItem['section']): MenuItem[] => {
  return MENU_ITEMS.filter(item => item.section === section)
}
