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
  notes: FeatureFlagState;
  exampleTool1: FeatureFlagState;
  exampleTool2: FeatureFlagState;
}

/**
 * Metadata for each feature flag
 */
export interface FeatureFlagInfo {
  key: keyof FeatureFlags;
  label: string;
  description: string;
  category: 'tools' | 'features' | 'experimental';
}

/**
 * Feature flag metadata for admin UI
 */
export const featureFlagInfo: FeatureFlagInfo[] = [
  {
    key: 'notes',
    label: 'Notes Tool',
    description: 'Simple notes tool for demonstrating COBRA patterns',
    category: 'tools',
  },
  {
    key: 'exampleTool1',
    label: 'Example Tool 1',
    description: 'Placeholder for additional tool implementation',
    category: 'tools',
  },
  {
    key: 'exampleTool2',
    label: 'Example Tool 2',
    description: 'Another placeholder for tool implementation',
    category: 'experimental',
  },
]

/**
 * Default feature flag values
 */
export const defaultFeatureFlags: FeatureFlags = {
  notes: 'Active',
  exampleTool1: 'ComingSoon',
  exampleTool2: 'Hidden',
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
