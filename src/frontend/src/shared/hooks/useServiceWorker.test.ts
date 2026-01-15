/**
 * useServiceWorker Hook Tests
 *
 * Tests for PWA service worker registration and update handling.
 * Note: The actual useRegisterSW is provided by vite-plugin-pwa virtual module,
 * so we mock it for testing.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'

// Mock function needs to be defined at module level for hoisting
const mockUseRegisterSW = vi.fn()

vi.mock('virtual:pwa-register/react', () => {
  return {
    useRegisterSW: (...args: unknown[]) => mockUseRegisterSW(...args),
  }
})

// Import after mocking
import { renderHook, act } from '@testing-library/react'
import { useServiceWorker } from './useServiceWorker'

describe('useServiceWorker', () => {
  const mockSetNeedRefresh = vi.fn()
  const mockSetOfflineReady = vi.fn()
  const mockUpdateServiceWorker = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()

    // Default mock implementation
    mockUseRegisterSW.mockReturnValue({
      needRefresh: [false, mockSetNeedRefresh],
      offlineReady: [false, mockSetOfflineReady],
      updateServiceWorker: mockUpdateServiceWorker,
    })
  })

  describe('initial state', () => {
    it('returns initial state from useRegisterSW', () => {
      const { result } = renderHook(() => useServiceWorker())

      expect(result.current.needRefresh).toBe(false)
      expect(result.current.offlineReady).toBe(false)
    })

    it('passes callbacks to useRegisterSW', () => {
      renderHook(() => useServiceWorker())

      expect(mockUseRegisterSW).toHaveBeenCalledWith({
        onRegistered: expect.any(Function),
        onRegisterError: expect.any(Function),
        onOfflineReady: expect.any(Function),
        onNeedRefresh: expect.any(Function),
      })
    })
  })

  describe('needRefresh state', () => {
    it('reflects needRefresh from useRegisterSW', () => {
      mockUseRegisterSW.mockReturnValue({
        needRefresh: [true, mockSetNeedRefresh],
        offlineReady: [false, mockSetOfflineReady],
        updateServiceWorker: mockUpdateServiceWorker,
      })

      const { result } = renderHook(() => useServiceWorker())

      expect(result.current.needRefresh).toBe(true)
    })
  })

  describe('offlineReady state', () => {
    it('reflects offlineReady from useRegisterSW', () => {
      mockUseRegisterSW.mockReturnValue({
        needRefresh: [false, mockSetNeedRefresh],
        offlineReady: [true, mockSetOfflineReady],
        updateServiceWorker: mockUpdateServiceWorker,
      })

      const { result } = renderHook(() => useServiceWorker())

      expect(result.current.offlineReady).toBe(true)
    })
  })

  describe('updateServiceWorker', () => {
    it('calls updateServiceWorker with true to reload', async () => {
      mockUpdateServiceWorker.mockResolvedValue(undefined)

      const { result } = renderHook(() => useServiceWorker())

      await act(async () => {
        await result.current.updateServiceWorker()
      })

      expect(mockUpdateServiceWorker).toHaveBeenCalledWith(true)
    })
  })

  describe('dismissNotification', () => {
    it('sets both offlineReady and needRefresh to false', () => {
      mockUseRegisterSW.mockReturnValue({
        needRefresh: [true, mockSetNeedRefresh],
        offlineReady: [true, mockSetOfflineReady],
        updateServiceWorker: mockUpdateServiceWorker,
      })

      const { result } = renderHook(() => useServiceWorker())

      act(() => {
        result.current.dismissNotification()
      })

      expect(mockSetOfflineReady).toHaveBeenCalledWith(false)
      expect(mockSetNeedRefresh).toHaveBeenCalledWith(false)
    })
  })

  describe('callbacks', () => {
    it('logs on successful registration', () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {})

      renderHook(() => useServiceWorker())

      // Get the onRegistered callback
      const options = mockUseRegisterSW.mock.calls[0][0]
      const mockRegistration = { scope: '/' } as ServiceWorkerRegistration

      options.onRegistered(mockRegistration)

      expect(consoleSpy).toHaveBeenCalledWith(
        '[PWA] Service Worker registered successfully',
      )

      consoleSpy.mockRestore()
    })

    it('logs error on registration failure', () => {
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

      renderHook(() => useServiceWorker())

      const options = mockUseRegisterSW.mock.calls[0][0]
      const mockError = new Error('Registration failed')

      options.onRegisterError(mockError)

      expect(consoleSpy).toHaveBeenCalledWith(
        '[PWA] Service Worker registration failed:',
        mockError,
      )

      consoleSpy.mockRestore()
    })

    it('logs when offline ready', () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {})

      renderHook(() => useServiceWorker())

      const options = mockUseRegisterSW.mock.calls[0][0]
      options.onOfflineReady()

      expect(consoleSpy).toHaveBeenCalledWith(
        '[PWA] App is ready to work offline',
      )

      consoleSpy.mockRestore()
    })

    it('logs when refresh needed', () => {
      const consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => {})

      renderHook(() => useServiceWorker())

      const options = mockUseRegisterSW.mock.calls[0][0]
      options.onNeedRefresh()

      expect(consoleSpy).toHaveBeenCalledWith(
        '[PWA] New content available, please refresh',
      )

      consoleSpy.mockRestore()
    })
  })
})
