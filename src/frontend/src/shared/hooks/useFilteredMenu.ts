/**
 * useFilteredMenu - Role-based menu filtering hook
 *
 * Filters navigation menu items based on:
 * - User's effective HSEEP role (from useExerciseRole)
 * - System role (Admin access to Templates/Users)
 * - Exercise context (disabled state for context-requiring items)
 *
 * @module shared/hooks
 * @see docs/features/navigation-shell/S02-role-based-menu-visibility.md
 */

import { useMemo } from 'react'
import { useExerciseRole } from '@/features/auth/hooks/useExerciseRole'
import { useAuth } from '@/contexts/AuthContext'
import type { HseepRole } from '@/types'
import {
  MENU_ITEMS,
  type MenuItem,
  type MenuSection,
  type GroupedMenuItems,
  type FilteredMenuResult,
  MENU_SECTION_LABELS,
} from '../components/navigation'

/**
 * Map ExerciseRole string to HseepRole type
 * The useExerciseRole hook returns string literal types that match HseepRole values
 */
function mapToHseepRole(role: string): HseepRole {
  // The ExerciseRole type in auth/constants matches HseepRole values exactly
  return role as HseepRole
}

/**
 * Check if user's role is allowed for a menu item
 */
function isRoleAllowed(item: MenuItem, effectiveRole: HseepRole): boolean {
  // If no roles specified, item is visible to all
  if (item.allowedRoles.length === 0) {
    return true
  }
  return item.allowedRoles.includes(effectiveRole)
}

/**
 * Check if user's system role allows access (for Admin-only items)
 */
function isSystemRoleAllowed(item: MenuItem, systemRole: string | null): boolean {
  // If no system roles specified, don't restrict by system role
  if (!item.allowedSystemRoles || item.allowedSystemRoles.length === 0) {
    return true
  }
  // Check if user's system role is in the allowed list
  return systemRole !== null && item.allowedSystemRoles.includes(systemRole as never)
}

/**
 * Hook parameters
 */
export interface UseFilteredMenuOptions {
  /** Current exercise ID (null if not in exercise context) */
  exerciseId?: string | null;
}

/**
 * Hook to filter menu items based on user role and exercise context
 *
 * @param options - Configuration options
 * @returns Filtered menu items, grouped by section, with disabled state handlers
 *
 * @example
 * ```tsx
 * const {
 *   groupedBySection, visibleSections, isItemDisabled, getDisabledTooltip
 * } = useFilteredMenu({
 *   exerciseId: currentExercise?.id
 * });
 *
 * // Render sections
 * visibleSections.map(section => (
 *   <MenuSection title={MENU_SECTION_LABELS[section]}>
 *     {groupedBySection[section].map(item => (
 *       <MenuItem
 *         key={item.id}
 *         disabled={isItemDisabled(item.id)}
 *         tooltip={getDisabledTooltip(item.id)}
 *       />
 *     ))}
 *   </MenuSection>
 * ));
 * ```
 */
export function useFilteredMenu(options: UseFilteredMenuOptions = {}): FilteredMenuResult {
  const { exerciseId = null } = options

  // Get user's effective role in the current exercise context
  const { effectiveRole: effectiveRoleString, systemRole } = useExerciseRole(exerciseId)
  const { user } = useAuth()

  // Convert to HseepRole type
  const effectiveRole = mapToHseepRole(effectiveRoleString)

  // Memoize filtered items
  const filteredItems = useMemo(() => {
    // If user is not authenticated, return empty
    if (!user) {
      return []
    }

    return MENU_ITEMS.filter(item => {
      // Check HSEEP role permission
      if (!isRoleAllowed(item, effectiveRole)) {
        return false
      }

      // Check system role permission (for Admin-only items like Templates, Users)
      if (!isSystemRoleAllowed(item, systemRole)) {
        return false
      }

      return true
    })
  }, [user, effectiveRole, systemRole])

  // Memoize grouped items
  const groupedBySection = useMemo((): GroupedMenuItems => {
    const groups: GroupedMenuItems = {
      conduct: [],
      analysis: [],
      system: [],
    }

    filteredItems.forEach(item => {
      groups[item.section].push(item)
    })

    return groups
  }, [filteredItems])

  // Memoize visible sections (sections that have at least one item)
  const visibleSections = useMemo((): MenuSection[] => {
    const sections: MenuSection[] = ['conduct', 'analysis', 'system']
    return sections.filter(section => groupedBySection[section].length > 0)
  }, [groupedBySection])

  // Check if an item is disabled (visible but not clickable due to missing context)
  const isItemDisabled = useMemo(() => {
    return (itemId: string): boolean => {
      const item = filteredItems.find(i => i.id === itemId)
      if (!item) {
        return false
      }

      // Item is disabled if it requires exercise context and we don't have one
      return item.requiresExerciseContext === true && !exerciseId
    }
  }, [filteredItems, exerciseId])

  // Get tooltip for a disabled item
  const getDisabledTooltip = useMemo(() => {
    return (itemId: string): string | undefined => {
      const item = filteredItems.find(i => i.id === itemId)
      if (!item || !isItemDisabled(itemId)) {
        return undefined
      }

      return item.disabledTooltip
    }
  }, [filteredItems, isItemDisabled])

  return {
    filteredItems,
    groupedBySection,
    visibleSections,
    isItemDisabled,
    getDisabledTooltip,
  }
}

// Re-export types and constants for convenience
export { MENU_SECTION_LABELS }
export type { MenuItem, MenuSection, GroupedMenuItems, FilteredMenuResult }
