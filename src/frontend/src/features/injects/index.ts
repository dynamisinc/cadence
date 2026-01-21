/**
 * Injects Feature Module
 *
 * Exports all inject-related components, hooks, services, and types.
 */

// Components
export * from './components'

// Hooks
export * from './hooks'

// Pages
export * from './pages'

// Services
export { injectService } from './services/injectService'

// Utils
export * from './utils'

// Types
export type {
  InjectDto,
  CreateInjectRequest,
  UpdateInjectRequest,
  FireInjectRequest,
  SkipInjectRequest,
  InjectFormValues,
  PhaseGroup,
} from './types'
export {
  INJECT_FIELD_LIMITS,
  formatScenarioTime,
  formatScheduledTime,
  calculateVariance,
  parseTimeToMs,
  calculateScheduledOffset,
  formatOffset,
  formatTimeRemaining,
  UPCOMING_WINDOW_MS,
  DUE_SOON_THRESHOLD_MS,
} from './types'
