/**
 * Feature Flags Types and Configuration
 *
 * Defines the feature flag system for controlling feature visibility:
 * - Hidden: Feature is completely hidden from UI
 * - ComingSoon: Feature is visible but disabled with "Coming Soon" badge
 * - Active: Feature is fully enabled and functional
 *
 * Used for gradual feature rollouts and demo configurations.
 */

/**
 * Feature flag state - controls visibility and availability
 */
export type FeatureFlagState = 'Hidden' | 'ComingSoon' | 'Active'

/**
 * Feature flags configuration
 * Add new features here as the app grows
 */
export interface FeatureFlags {
  /** Templates - Exercise and inject templates management */
  templates: FeatureFlagState;
  /** Organization-level Reports - Cross-exercise reporting and analytics */
  reports: FeatureFlagState;
  /** Control Room - Real-time exercise conduct dashboard */
  controlRoom: FeatureFlagState;
  /** Organization Settings - General organization configuration (OrgAdmin) */
  orgSettings: FeatureFlagState;
}

/**
 * Feature flag categories for grouping in admin UI
 */
export type FeatureFlagCategory = 'conduct' | 'analysis' | 'organization' | 'system'

/**
 * Metadata for each feature flag
 */
export interface FeatureFlagInfo {
  key: keyof FeatureFlags;
  label: string;
  description: string;
  category: FeatureFlagCategory;
}

/**
 * Feature flag metadata for admin UI
 */
export const featureFlagInfo: FeatureFlagInfo[] = [
  {
    key: 'controlRoom',
    label: 'Control Room',
    description: 'Real-time exercise conduct dashboard for Controllers and Exercise Directors',
    category: 'conduct',
  },
  {
    key: 'reports',
    label: 'Organization Reports',
    description: 'Cross-exercise reporting and analytics at the organization level',
    category: 'analysis',
  },
  {
    key: 'templates',
    label: 'Templates',
    description: 'Manage inject templates and exercise blueprints for reuse',
    category: 'system',
  },
  {
    key: 'orgSettings',
    label: 'Organization Settings',
    description: 'General organization configuration for OrgAdmins',
    category: 'organization',
  },
]

/**
 * Default feature flag values
 * These features are not yet implemented, so they default to Hidden
 */
export const defaultFeatureFlags: FeatureFlags = {
  templates: 'Hidden',
  reports: 'Hidden',
  controlRoom: 'Hidden',
  orgSettings: 'Hidden',
}

/**
 * Helper to get display color for feature state
 */
export const getFeatureStateColor = (
  state: FeatureFlagState,
): 'success' | 'warning' | 'default' => {
  switch (state) {
    case 'Active':
      return 'success'
    case 'ComingSoon':
      return 'warning'
    case 'Hidden':
      return 'default'
  }
}

/**
 * Helper to get display label for feature state
 */
export const getFeatureStateLabel = (state: FeatureFlagState): string => {
  switch (state) {
    case 'Active':
      return 'Active'
    case 'ComingSoon':
      return 'Coming Soon'
    case 'Hidden':
      return 'Hidden'
  }
}
