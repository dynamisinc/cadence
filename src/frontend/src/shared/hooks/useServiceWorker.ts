/**
 * useServiceWorker Hook
 *
 * Manages PWA service worker registration, updates, and offline readiness.
 * Uses vite-plugin-pwa's virtual module for service worker lifecycle.
 */

import { useRegisterSW } from 'virtual:pwa-register/react'
import { useCallback } from 'react'
import { devLog } from '@/core/utils/logger'

export interface UseServiceWorkerReturn {
  /** True when a new version is available and waiting to activate */
  needRefresh: boolean
  /** True when the app is ready to work offline */
  offlineReady: boolean
  /** Triggers the service worker update (reloads the page) */
  updateServiceWorker: () => Promise<void>
  /** Dismisses the update/offline ready notification */
  dismissNotification: () => void
}

/**
 * Hook for managing PWA service worker lifecycle.
 *
 * Provides state for:
 * - Detecting when an update is available
 * - Knowing when the app is ready for offline use
 * - Triggering the update process
 *
 * @example
 * ```tsx
 * const { needRefresh, offlineReady, updateServiceWorker, dismissNotification } =
 *   useServiceWorker();
 *
 * if (needRefresh) {
 *   return <button onClick={updateServiceWorker}>Update Available</button>;
 * }
 * ```
 */
export function useServiceWorker(): UseServiceWorkerReturn {
  const {
    needRefresh: [needRefresh, setNeedRefresh],
    offlineReady: [offlineReady, setOfflineReady],
    updateServiceWorker,
  } = useRegisterSW({
    onRegistered(registration) {
      if (registration) {
        devLog('[PWA] Service Worker registered successfully')
      }
    },
    onRegisterError(error) {
      console.error('[PWA] Service Worker registration failed:', error)
    },
    onOfflineReady() {
      devLog('[PWA] App is ready to work offline')
    },
    onNeedRefresh() {
      devLog('[PWA] New content available, please refresh')
    },
  })

  const dismissNotification = useCallback(() => {
    setOfflineReady(false)
    setNeedRefresh(false)
  }, [setOfflineReady, setNeedRefresh])

  const triggerUpdate = useCallback(async () => {
    await updateServiceWorker(true)
  }, [updateServiceWorker])

  return {
    needRefresh,
    offlineReady,
    updateServiceWorker: triggerUpdate,
    dismissNotification,
  }
}
