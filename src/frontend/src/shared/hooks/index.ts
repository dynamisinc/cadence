/**
 * Shared Hooks Index
 *
 * Central export for all shared custom hooks
 */

export { useConfirmDialog, type ConfirmOptions } from './useConfirmDialog'
export { useDebounce } from './useDebounce'
export { useExerciseSignalR } from './useExerciseSignalR'
export { useInstallPrompt, type UseInstallPromptReturn } from './useInstallPrompt'
export {
  useSystemPermissions,
  getSystemRoleDisplayName,
  getSystemRoleDescription,
  type UseSystemPermissionsReturn,
} from './useSystemPermissions'
export { useServiceWorker, type UseServiceWorkerReturn } from './useServiceWorker'
export { useSignalR } from './useSignalR'
export type { ConnectionState } from './useSignalR'
export { useDismissible, type UseDismissibleReturn } from './useDismissible'
export { useUnsavedChangesWarning, type UnsavedChangesOptions } from './useUnsavedChangesWarning'
export {
  useFilteredMenu,
  MENU_SECTION_LABELS,
  type MenuItem,
  type MenuSection,
  type GroupedMenuItems,
  type FilteredMenuResult,
  type UseFilteredMenuOptions,
} from './useFilteredMenu'
