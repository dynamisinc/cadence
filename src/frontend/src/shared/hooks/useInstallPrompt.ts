/**
 * useInstallPrompt Hook
 *
 * Handles the PWA installation prompt (Add to Home Screen).
 * Captures the beforeinstallprompt event and provides methods to trigger installation.
 */

import { useState, useEffect, useCallback } from 'react'

/**
 * Extended Event interface for beforeinstallprompt event.
 * This event is fired by browsers when the PWA installation criteria are met.
 */
interface BeforeInstallPromptEvent extends Event {
  /** Shows the installation prompt to the user */
  prompt(): Promise<void>
  /** Promise that resolves with the user's choice */
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>
}

export interface UseInstallPromptReturn {
  /** True if the app can be installed (prompt is available and not already installed) */
  canInstall: boolean
  /** True if the app is already installed (running in standalone mode) */
  isInstalled: boolean
  /** Triggers the native installation prompt. Returns true if user accepted. */
  promptInstall: () => Promise<boolean>
}

/**
 * Hook for managing PWA installation prompts.
 *
 * Captures the `beforeinstallprompt` event and provides a method to trigger
 * the native installation dialog.
 *
 * @example
 * ```tsx
 * const { canInstall, isInstalled, promptInstall } = useInstallPrompt();
 *
 * if (canInstall) {
 *   return <button onClick={promptInstall}>Install Cadence</button>;
 * }
 * ```
 */
export function useInstallPrompt(): UseInstallPromptReturn {
  const [installPrompt, setInstallPrompt] = useState<BeforeInstallPromptEvent | null>(null)
  const [isInstalled, setIsInstalled] = useState(false)

  useEffect(() => {
    // Check if already installed (running in standalone mode)
    const isStandalone = window.matchMedia('(display-mode: standalone)').matches
    // Also check iOS standalone mode
    const isIOSStandalone = ('standalone' in window.navigator) &&
      (window.navigator as Navigator & { standalone?: boolean }).standalone === true

    if (isStandalone || isIOSStandalone) {
      setIsInstalled(true)
      return
    }

    // Capture the beforeinstallprompt event
    const handleBeforeInstall = (e: Event) => {
      // Prevent the mini-infobar from appearing on mobile
      e.preventDefault()
      // Store the event for later use
      setInstallPrompt(e as BeforeInstallPromptEvent)
      console.log('[PWA] Install prompt captured')
    }

    // Listen for successful installation
    const handleAppInstalled = () => {
      setIsInstalled(true)
      setInstallPrompt(null)
      console.log('[PWA] App was installed')
    }

    window.addEventListener('beforeinstallprompt', handleBeforeInstall)
    window.addEventListener('appinstalled', handleAppInstalled)

    return () => {
      window.removeEventListener('beforeinstallprompt', handleBeforeInstall)
      window.removeEventListener('appinstalled', handleAppInstalled)
    }
  }, [])

  const promptInstall = useCallback(async (): Promise<boolean> => {
    if (!installPrompt) {
      console.log('[PWA] No install prompt available')
      return false
    }

    try {
      // Show the native installation prompt
      await installPrompt.prompt()

      // Wait for the user's choice
      const { outcome } = await installPrompt.userChoice
      console.log(`[PWA] User ${outcome} the install prompt`)

      // Clear the stored prompt (can only be used once)
      setInstallPrompt(null)

      return outcome === 'accepted'
    } catch (error) {
      console.error('[PWA] Error prompting for install:', error)
      return false
    }
  }, [installPrompt])

  return {
    canInstall: !!installPrompt && !isInstalled,
    isInstalled,
    promptInstall,
  }
}
