/**
 * Navigation Menu Types
 *
 * Type definitions for the sidebar navigation system with:
 * - Menu items with role-based visibility
 * - Section grouping (CONDUCT, ANALYSIS, SYSTEM)
 * - Disabled state support for context-requiring items
 */

import type { IconDefinition } from '@fortawesome/free-solid-svg-icons'
import type { HseepRole, SystemRole } from '../../../types'
import type { FeatureFlags } from '../../../admin/types/featureFlags'

/**
 * Menu sections matching HSEEP workflow
 */
export type MenuSection = 'conduct' | 'analysis' | 'system'

/**
 * Display labels for menu sections
 */
export const MENU_SECTION_LABELS: Record<MenuSection, string> = {
  conduct: 'CONDUCT',
  analysis: 'ANALYSIS',
  system: 'SYSTEM',
}

/**
 * Configuration for a single menu item
 */
export interface MenuItem {
  /** Unique identifier for the menu item */
  id: string;
  /** Display label */
  label: string;
  /** FontAwesome icon */
  icon: IconDefinition;
  /** Route path - can include :id placeholder for exercise-scoped routes */
  path: string;
  /** Which section this item belongs to */
  section: MenuSection;
  /** HSEEP roles that can see this item (empty = all roles) */
  allowedRoles: HseepRole[];
  /** System roles that can see this item (empty = all roles) */
  allowedSystemRoles?: SystemRole[];
  /** Whether this item requires being in an exercise context */
  requiresExerciseContext?: boolean;
  /** Tooltip to show when item is disabled */
  disabledTooltip?: string;
  /** Feature flag that controls visibility of this menu item */
  featureFlag?: keyof FeatureFlags;
}

/**
 * Menu items grouped by section
 */
export type GroupedMenuItems = Record<MenuSection, MenuItem[]>

/**
 * Result from useFilteredMenu hook
 */
export interface FilteredMenuResult {
  /** All visible menu items (filtered by role) */
  filteredItems: MenuItem[];
  /** Items grouped by section */
  groupedBySection: GroupedMenuItems;
  /** Sections that have visible items */
  visibleSections: MenuSection[];
  /** Check if a specific item is disabled (visible but not clickable) */
  isItemDisabled: (itemId: string) => boolean;
  /** Get tooltip for a disabled item */
  getDisabledTooltip: (itemId: string) => string | undefined;
}

/**
 * Context about the current exercise (if any)
 */
export interface ExerciseContext {
  /** Exercise ID */
  id: string;
  /** User's role within this specific exercise */
  userRole?: HseepRole;
}
