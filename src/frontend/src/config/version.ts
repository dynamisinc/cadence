/**
 * Application version information injected at build time.
 *
 * @example
 * import { appVersion } from '@/config/version';
 * console.log(`Running Cadence v${appVersion.version}`);
 */
export const appVersion = {
  /** Semantic version from package.json */
  version: __APP_VERSION__,
  /** ISO timestamp of build */
  buildDate: __BUILD_DATE__,
  /** Abbreviated git commit SHA (7 chars) */
  commitSha: __COMMIT_SHA__,
} as const

// Log version on app initialization (helps with support/debugging)
if (import.meta.env.DEV) {
  console.log(
    `%c🎯 Cadence v${appVersion.version} (${appVersion.commitSha})`,
    'color: #1976d2; font-weight: bold;',
  )
}
