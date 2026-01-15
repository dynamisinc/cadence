/**
 * useInstallPrompt Hook Tests
 *
 * Tests for PWA installation prompt handling
 */

import { renderHook, act } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { useInstallPrompt } from './useInstallPrompt'

describe('useInstallPrompt', () => {
  let originalMatchMedia: typeof window.matchMedia
  let addEventListenerSpy: ReturnType<typeof vi.spyOn>
  let removeEventListenerSpy: ReturnType<typeof vi.spyOn>

  beforeEach(() => {
    // Store original matchMedia
    originalMatchMedia = window.matchMedia

    // Mock matchMedia to return non-standalone by default
    window.matchMedia = vi.fn().mockImplementation((query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }))

    // Spy on addEventListener/removeEventListener
    addEventListenerSpy = vi.spyOn(window, 'addEventListener')
    removeEventListenerSpy = vi.spyOn(window, 'removeEventListener')
  })

  afterEach(() => {
    window.matchMedia = originalMatchMedia
    vi.restoreAllMocks()
  })

  describe('initial state', () => {
    it('returns canInstall as false when no prompt event captured', () => {
      const { result } = renderHook(() => useInstallPrompt())

      expect(result.current.canInstall).toBe(false)
      expect(result.current.isInstalled).toBe(false)
    })

    it('detects standalone mode (already installed)', () => {
      window.matchMedia = vi.fn().mockImplementation((query: string) => ({
        matches: query === '(display-mode: standalone)',
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      }))

      const { result } = renderHook(() => useInstallPrompt())

      expect(result.current.isInstalled).toBe(true)
      expect(result.current.canInstall).toBe(false)
    })

    it('registers event listeners on mount', () => {
      renderHook(() => useInstallPrompt())

      expect(addEventListenerSpy).toHaveBeenCalledWith(
        'beforeinstallprompt',
        expect.any(Function),
      )
      expect(addEventListenerSpy).toHaveBeenCalledWith(
        'appinstalled',
        expect.any(Function),
      )
    })

    it('removes event listeners on unmount', () => {
      const { unmount } = renderHook(() => useInstallPrompt())

      unmount()

      expect(removeEventListenerSpy).toHaveBeenCalledWith(
        'beforeinstallprompt',
        expect.any(Function),
      )
      expect(removeEventListenerSpy).toHaveBeenCalledWith(
        'appinstalled',
        expect.any(Function),
      )
    })
  })

  describe('beforeinstallprompt event', () => {
    it('captures the install prompt event', () => {
      const { result } = renderHook(() => useInstallPrompt())

      // Get the beforeinstallprompt handler
      const beforeInstallHandler = addEventListenerSpy.mock.calls.find(
        (call: [string, EventListener]) => call[0] === 'beforeinstallprompt',
      )?.[1] as EventListener

      // Create mock event
      const mockEvent = {
        preventDefault: vi.fn(),
        prompt: vi.fn().mockResolvedValue(undefined),
        userChoice: Promise.resolve({ outcome: 'accepted' as const }),
      }

      // Trigger the event
      act(() => {
        beforeInstallHandler(mockEvent as unknown as Event)
      })

      expect(mockEvent.preventDefault).toHaveBeenCalled()
      expect(result.current.canInstall).toBe(true)
    })
  })

  describe('promptInstall', () => {
    it('returns false when no install prompt available', async () => {
      const { result } = renderHook(() => useInstallPrompt())

      let installResult: boolean = true
      await act(async () => {
        installResult = await result.current.promptInstall()
      })

      expect(installResult).toBe(false)
    })

    it('triggers native prompt and returns true when accepted', async () => {
      const { result } = renderHook(() => useInstallPrompt())

      const mockEvent = {
        preventDefault: vi.fn(),
        prompt: vi.fn().mockResolvedValue(undefined),
        userChoice: Promise.resolve({ outcome: 'accepted' as const }),
      }

      // Capture the prompt
      const beforeInstallHandler = addEventListenerSpy.mock.calls.find(
        (call: [string, EventListener]) => call[0] === 'beforeinstallprompt',
      )?.[1] as EventListener

      act(() => {
        beforeInstallHandler(mockEvent as unknown as Event)
      })

      expect(result.current.canInstall).toBe(true)

      // Trigger install
      let installResult: boolean = false
      await act(async () => {
        installResult = await result.current.promptInstall()
      })

      expect(mockEvent.prompt).toHaveBeenCalled()
      expect(installResult).toBe(true)
      expect(result.current.canInstall).toBe(false) // Prompt cleared after use
    })

    it('returns false when user dismisses the prompt', async () => {
      const { result } = renderHook(() => useInstallPrompt())

      const mockEvent = {
        preventDefault: vi.fn(),
        prompt: vi.fn().mockResolvedValue(undefined),
        userChoice: Promise.resolve({ outcome: 'dismissed' as const }),
      }

      const beforeInstallHandler = addEventListenerSpy.mock.calls.find(
        (call: [string, EventListener]) => call[0] === 'beforeinstallprompt',
      )?.[1] as EventListener

      act(() => {
        beforeInstallHandler(mockEvent as unknown as Event)
      })

      let installResult: boolean = true
      await act(async () => {
        installResult = await result.current.promptInstall()
      })

      expect(installResult).toBe(false)
    })
  })

  describe('appinstalled event', () => {
    it('sets isInstalled to true when app is installed', () => {
      const { result } = renderHook(() => useInstallPrompt())

      const appInstalledHandler = addEventListenerSpy.mock.calls.find(
        (call: [string, EventListener]) => call[0] === 'appinstalled',
      )?.[1] as EventListener

      act(() => {
        appInstalledHandler(new Event('appinstalled'))
      })

      expect(result.current.isInstalled).toBe(true)
      expect(result.current.canInstall).toBe(false)
    })
  })
})
