/**
 * Install Banner Utilities
 *
 * Handles persistent dismiss state for the PWA install banner.
 * Dismiss is stored in localStorage with a 90-day cooldown and
 * resets on major version changes.
 */

import { appVersion } from '../../config/version'

const DISMISS_KEY = 'cadence-install-banner-dismissed'
const COOLDOWN_DAYS = 90

interface DismissState {
  /** Timestamp when the banner was dismissed */
  dismissedAt: number
  /** Major version at the time of dismissal (e.g., "2") */
  majorVersion: string
}

/** Get the major version from the semantic version string (e.g., "2.1.0" -> "2") */
function getMajorVersion(): string {
  return appVersion.version.split('.')[0] ?? '0'
}

/** Check if the install banner should be shown based on localStorage state */
export function shouldShowBanner(): boolean {
  try {
    const stored = localStorage.getItem(DISMISS_KEY)
    if (!stored) return true

    const state: DismissState = JSON.parse(stored)

    // Re-show if major version has changed
    if (state.majorVersion !== getMajorVersion()) return true

    // Re-show after 90-day cooldown
    const daysSinceDismiss =
      (Date.now() - state.dismissedAt) / (1000 * 60 * 60 * 24)
    if (daysSinceDismiss >= COOLDOWN_DAYS) return true

    return false
  } catch {
    // If localStorage is unavailable or corrupted, show the banner
    return true
  }
}

/** Persist the dismiss state to localStorage */
export function persistDismiss(): void {
  try {
    const state: DismissState = {
      dismissedAt: Date.now(),
      majorVersion: getMajorVersion(),
    }
    localStorage.setItem(DISMISS_KEY, JSON.stringify(state))
  } catch {
    // Silently fail if localStorage is unavailable
  }
}
