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
  faChartBar,
  faFileAlt,
  faUsers,
  faCog,
  faShieldHalved,
  faBuilding,
  faUserShield,
  faPuzzlePiece,
  faBoxArchive,
  faLightbulb,
  faPaperPlane,
  faComments,
} from '@fortawesome/free-solid-svg-icons'
import { HseepRole, SystemRole } from '../../../types'
import type { MenuItem } from './types'
import type { OrgRole } from '../../../features/organizations/types'

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
 * Organization admin roles - for org management features
 */
const ORG_ADMIN_ROLES: OrgRole[] = ['OrgAdmin']

/**
 * Complete menu configuration
 *
 * Menu items across 3 sections:
 * - CONDUCT (2): My Assignments, Exercises
 * - ANALYSIS (1): Reports
 * - SYSTEM (6): System Settings, Templates, Users, Organizations, Delivery Methods, My Preferences
 *
 * Note: Exercise-scoped items (Control Room, Observations) are only shown
 * in the exercise-specific sidebar (exerciseMenuConfig.ts), not here.
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

  // ============================================================================
  // ANALYSIS Section - Post-exercise review and reporting
  // ============================================================================
  {
    id: 'reports',
    label: 'Reports',
    icon: faChartBar,
    path: '/reports',
    section: 'analysis',
    allowedRoles: REPORTING_ROLES,
    featureFlag: 'reports',
  },

  // ============================================================================
  // ORGANIZATION Section - Current organization management (OrgAdmin only)
  // ============================================================================
  {
    id: 'org-details',
    label: 'Details',
    icon: faBuilding,
    path: '/organization/details',
    section: 'organization',
    allowedRoles: ALL_HSEEP_ROLES,
    allowedOrgRoles: ORG_ADMIN_ROLES,
  },
  {
    id: 'org-members',
    label: 'Members',
    icon: faUsers,
    path: '/organization/members',
    section: 'organization',
    allowedRoles: ALL_HSEEP_ROLES,
    allowedOrgRoles: ORG_ADMIN_ROLES,
  },
  {
    id: 'org-approval',
    label: 'Inject Approval',
    icon: faUserShield,
    path: '/organization/approval',
    section: 'organization',
    allowedRoles: ALL_HSEEP_ROLES,
    allowedOrgRoles: ORG_ADMIN_ROLES,
  },
  {
    id: 'org-capabilities',
    label: 'Capability Library',
    icon: faPuzzlePiece,
    path: '/organization/capabilities',
    section: 'organization',
    allowedRoles: ALL_HSEEP_ROLES,
    allowedOrgRoles: ORG_ADMIN_ROLES,
  },
  {
    id: 'org-suggestions',
    label: 'Autocomplete',
    icon: faLightbulb,
    path: '/organization/suggestions',
    section: 'organization',
    allowedRoles: ALL_HSEEP_ROLES,
    allowedOrgRoles: ORG_ADMIN_ROLES,
  },
  {
    id: 'org-archived',
    label: 'Archived Exercises',
    icon: faBoxArchive,
    path: '/organization/archived',
    section: 'organization',
    allowedRoles: ALL_HSEEP_ROLES,
    allowedOrgRoles: ORG_ADMIN_ROLES,
  },
  {
    id: 'org-settings',
    label: 'Settings',
    icon: faCog,
    path: '/organization/settings',
    section: 'organization',
    allowedRoles: ALL_HSEEP_ROLES,
    allowedOrgRoles: ORG_ADMIN_ROLES,
    featureFlag: 'orgSettings',
  },

  // ============================================================================
  // SYSTEM Section - Administration and configuration
  // ============================================================================
  {
    id: 'admin',
    label: 'System Settings',
    icon: faShieldHalved,
    path: '/admin',
    section: 'system',
    allowedRoles: ADMIN_ROLES,
    allowedSystemRoles: [SystemRole.Admin],
  },
  {
    id: 'templates',
    label: 'Templates',
    icon: faFileAlt,
    path: '/templates',
    section: 'system',
    allowedRoles: ADMIN_ROLES,
    allowedSystemRoles: [SystemRole.Admin],
    featureFlag: 'templates',
  },
  {
    id: 'users',
    label: 'Users',
    icon: faUsers,
    path: '/admin/users',
    section: 'system',
    allowedRoles: ADMIN_ROLES,
    allowedSystemRoles: [SystemRole.Admin],
  },
  {
    id: 'organizations',
    label: 'Organizations',
    icon: faBuilding,
    path: '/admin/organizations',
    section: 'system',
    allowedRoles: ADMIN_ROLES,
    allowedSystemRoles: [SystemRole.Admin],
  },
  {
    id: 'delivery-methods',
    label: 'Delivery Methods',
    icon: faPaperPlane,
    path: '/admin/delivery-methods',
    section: 'system',
    allowedRoles: ADMIN_ROLES,
    allowedSystemRoles: [SystemRole.Admin],
  },
  {
    id: 'feedback',
    label: 'Feedback Reports',
    icon: faComments,
    path: '/admin/feedback',
    section: 'system',
    allowedRoles: ADMIN_ROLES,
    allowedSystemRoles: [SystemRole.Admin],
  },
  {
    id: 'settings',
    label: 'My Preferences',
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
