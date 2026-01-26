/**
 * Navigation Module Exports
 */

// Types
export type {
  MenuSection,
  MenuItem,
  GroupedMenuItems,
  FilteredMenuResult,
  ExerciseContext,
} from './types'

export { MENU_SECTION_LABELS } from './types'

// Configuration
export {
  MENU_ITEMS,
  getMenuItemById,
  getMenuItemsBySection,
} from './menuConfig'

// Exercise-specific menu configuration
export {
  EXERCISE_MENU_ITEMS,
  getExerciseMenuItems,
  buildExerciseMenuPath,
  type ExerciseMenuItem,
} from './exerciseMenuConfig'
