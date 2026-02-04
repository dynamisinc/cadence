/**
 * FeatureFlagGuard Component
 *
 * Protects routes based on feature flag state:
 * - Hidden: Redirects to home page
 * - ComingSoon: Shows a "Coming Soon" placeholder page
 * - Active: Renders children normally
 *
 * Usage:
 * <Route
 *   path="templates"
 *   element={
 *     <FeatureFlagGuard feature="templates" featureName="Templates">
 *       <TemplatesPage />
 *     </FeatureFlagGuard>
 *   }
 * />
 */

import React from 'react'
import { Navigate } from 'react-router-dom'
import { useFeatureFlags } from '@/admin/contexts/FeatureFlagsContext'
import type { FeatureFlags } from '@/admin/types/featureFlags'
import { GlobalPlaceholderPage } from './GlobalPlaceholderPage'
import type { IconDefinition } from '@fortawesome/free-solid-svg-icons'

interface FeatureFlagGuardProps {
  /** The feature flag key to check */
  feature: keyof FeatureFlags;
  /** Display name for the feature (used in Coming Soon page) */
  featureName: string;
  /** Optional description for Coming Soon page */
  description?: string;
  /** Optional icon for Coming Soon page */
  icon?: IconDefinition;
  /** Path to redirect to when feature is Hidden (defaults to '/') */
  redirectTo?: string;
  /** Children to render when feature is Active */
  children: React.ReactNode;
}

export const FeatureFlagGuard: React.FC<FeatureFlagGuardProps> = ({
  feature,
  featureName,
  description,
  icon,
  redirectTo = '/',
  children,
}) => {
  const { getState } = useFeatureFlags()
  const state = getState(feature)

  // Hidden: Redirect away
  if (state === 'Hidden') {
    return <Navigate to={redirectTo} replace />
  }

  // ComingSoon: Show placeholder
  if (state === 'ComingSoon') {
    return (
      <GlobalPlaceholderPage
        featureName={featureName}
        description={description}
        icon={icon}
      />
    )
  }

  // Active: Render children
  return <>{children}</>
}

export default FeatureFlagGuard
