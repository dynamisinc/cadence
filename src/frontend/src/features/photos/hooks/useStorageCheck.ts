/**
 * useStorageCheck Hook
 *
 * Hook for checking device storage capacity before photo capture.
 * Integrates with IndexedDB photo queue to calculate total storage usage.
 *
 * Returns warning levels based on storage usage:
 * - none: < 80% used
 * - warning: 80-95% used
 * - critical: > 95% used
 *
 * @module features/photos/hooks
 *
 * @example
 * ```tsx
 * const { checkStorage } = useStorageCheck();
 *
 * const handleCapture = async () => {
 *   const result = await checkStorage();
 *   if (result.warningLevel === 'critical') {
 *     // Show critical warning, block capture
 *   } else if (result.warningLevel === 'warning') {
 *     // Show warning, allow continue
 *   }
 * };
 * ```
 */

import { getPhotoStorageUsage } from '../../../core/offline/photoCacheService'

export interface StorageCheckResult {
  /** Whether storage check passed without warnings */
  ok: boolean
  /** Warning level based on usage percentage */
  warningLevel: 'none' | 'warning' | 'critical'
  /** Storage usage percentage (0-100) */
  usagePercent: number
  /** Number of photos queued for sync */
  queuedCount: number
  /** Total size of queued photos in bytes */
  queuedSizeBytes: number
}

/**
 * Hook for checking device storage before photo capture
 */
export function useStorageCheck() {
  /**
   * Check current storage usage and queued photo stats
   */
  const checkStorage = async (): Promise<StorageCheckResult> => {
    try {
      // Get device storage estimate
      let usagePercent = 0
      if (navigator.storage && navigator.storage.estimate) {
        const estimate = await navigator.storage.estimate()
        const usage = estimate.usage ?? 0
        const quota = estimate.quota ?? 1

        usagePercent = quota > 0 ? (usage / quota) * 100 : 0
      }

      // Get queued photo storage usage
      const photoStorage = await getPhotoStorageUsage()
      const queuedCount = photoStorage.pendingCount
      const queuedSizeBytes = photoStorage.totalBytes

      // Determine warning level
      let warningLevel: 'none' | 'warning' | 'critical' = 'none'
      if (usagePercent > 95) {
        warningLevel = 'critical'
      } else if (usagePercent > 80) {
        warningLevel = 'warning'
      }

      return {
        ok: warningLevel === 'none',
        warningLevel,
        usagePercent,
        queuedCount,
        queuedSizeBytes,
      }
    } catch (error) {
      console.error('Failed to check storage:', error)
      // On error, assume storage is OK to not block user
      return {
        ok: true,
        warningLevel: 'none',
        usagePercent: 0,
        queuedCount: 0,
        queuedSizeBytes: 0,
      }
    }
  }

  return { checkStorage }
}

export default useStorageCheck
