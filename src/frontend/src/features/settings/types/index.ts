/**
 * User Settings Types
 *
 * Type definitions for user preferences (display, time format).
 *
 * @module features/settings
 */

/**
 * Theme preference values
 */
export type ThemePreference = 'Light' | 'Dark' | 'System'

/**
 * Display density values
 */
export type DisplayDensity = 'Comfortable' | 'Compact'

/**
 * Time format values
 */
export type TimeFormat = 'TwentyFourHour' | 'TwelveHour'

/**
 * User preferences DTO from API
 */
export interface UserPreferencesDto {
  theme: ThemePreference
  displayDensity: DisplayDensity
  timeFormat: TimeFormat
  updatedAt: string
}

/**
 * Request to update user preferences
 */
export interface UpdateUserPreferencesRequest {
  theme?: ThemePreference
  displayDensity?: DisplayDensity
  timeFormat?: TimeFormat
}

/**
 * Resolved theme mode for MUI (no 'System')
 */
export type ResolvedTheme = 'light' | 'dark'

/**
 * A single email preference category from the API
 */
export interface EmailPreferenceDto {
  category: string
  displayName: string
  description: string
  isEnabled: boolean
  isMandatory: boolean
}

/**
 * Response containing all email preferences
 */
export interface EmailPreferencesResponse {
  preferences: EmailPreferenceDto[]
}

/**
 * Request to update a single email preference
 */
export interface UpdateEmailPreferenceRequest {
  category: string
  isEnabled: boolean
}
