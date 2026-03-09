/**
 * Application version information injected at build time.
 *
 * @example
 * import { appVersion } from '@/config/version';
 * import { devLog } from '@/core/utils/logger';
 * devLog(`Running Cadence v${appVersion.version}`);
 */
import { devLog } from '@/core/utils/logger'

export const appVersion = {
  /** Semantic version from package.json */
  version: __APP_VERSION__,
  /** ISO timestamp of build */
  buildDate: __BUILD_DATE__,
  /** Abbreviated git commit SHA (7 chars) */
  commitSha: __COMMIT_SHA__,
} as const

// Log version on app initialization (helps with support/debugging)
devLog(
  `%c Cadence v${appVersion.version} (${appVersion.commitSha})`,
  'color: #1976d2; font-weight: bold;',
)
