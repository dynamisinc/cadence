/**
 * Admin Module Exports
 *
 * Central export point for all admin-related functionality:
 * - Feature flags context and hooks
 * - Admin components
 * - Admin pages
 * - Types
 */

// Contexts
export { FeatureFlagsProvider, useFeatureFlags, useFeatureFlagState } from './contexts/FeatureFlagsContext'

// Components
export { FeatureFlagsAdmin } from './components/FeatureFlagsAdmin'

// Pages
export { AdminPage } from './pages/AdminPage'
export { ArchivedExercisesPage } from './pages/ArchivedExercisesPage'

// Types
export type { FeatureFlags, FeatureFlagState, FeatureFlagInfo } from './types/featureFlags'
export {
  defaultFeatureFlags,
  featureFlagInfo,
  getFeatureStateColor,
  getFeatureStateLabel,
} from './types/featureFlags'
