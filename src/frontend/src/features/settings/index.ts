/**
 * Settings Feature Module
 *
 * User preferences for display and behavior settings.
 *
 * @module features/settings
 */

// Context and hooks
export { UserPreferencesProvider, useUserPreferences } from './contexts/UserPreferencesContext'
export { useFormattedTime } from './hooks/useFormattedTime'

// Components
export { UserSettingsDialog } from './components/UserSettingsDialog'
export { EmailNotificationsSection } from './components/EmailNotificationsSection'

// Pages
export { UserSettingsPage } from './pages'

// Services
export { preferencesService } from './services/preferencesService'
export { emailPreferencesService } from './services/emailPreferencesService'

// Utils
export {
  formatTime,
  formatTimeWithSeconds,
  formatDateTime,
  getCurrentTimeFormatted,
  getTimeFormatExamples,
} from './utils/timeFormat'

// Types
export type {
  UserPreferencesDto,
  UpdateUserPreferencesRequest,
  ThemePreference,
  DisplayDensity,
  TimeFormat,
  ResolvedTheme,
  EmailPreferenceDto,
  EmailPreferencesResponse,
  UpdateEmailPreferenceRequest,
} from './types'
