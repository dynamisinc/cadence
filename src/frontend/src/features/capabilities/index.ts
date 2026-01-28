/**
 * Capabilities Feature Module
 *
 * Exports for organizational capability management.
 */

// Pages
export { CapabilityLibraryPage } from './pages/CapabilityLibraryPage'

// Components
export { CapabilityDialog } from './components/CapabilityDialog'
export { CapabilityList } from './components/CapabilityList'

// Hooks
export { useCapabilities, useCapability, useCheckCapabilityName, capabilityKeys } from './hooks/useCapabilities'

// Services
export { capabilityService } from './services/capabilityService'

// Types
export type {
  CapabilityDto,
  CapabilitySummaryDto,
  CreateCapabilityRequest,
  UpdateCapabilityRequest,
  CapabilityLibrary,
  CapabilityLibraryInfo,
} from './types'

export {
  CAPABILITY_LIBRARIES,
  groupCapabilitiesByCategory,
  getUniqueCategories,
} from './types'
