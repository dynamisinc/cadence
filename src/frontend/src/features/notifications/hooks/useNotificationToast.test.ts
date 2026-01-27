/**
 * useNotificationToast Hook Tests
 *
 * Tests for toast notification management based on priority.
 */
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { getToastConfig, useNotificationToast } from './useNotificationToast'
import type { NotificationDto } from '../types'

describe('getToastConfig', () => {
  it('returns correct config for High priority', () => {
    const config = getToastConfig('High')

    expect(config).toEqual({
      showToast: true,
      autoDismissMs: null, // Never auto-dismiss
      backgroundColor: '#fff3e0',
      borderColor: '#ff9800',
    })
  })

  it('returns correct config for Medium priority', () => {
    const config = getToastConfig('Medium')

    expect(config).toEqual({
      showToast: true,
      autoDismissMs: 10000, // 10 seconds
      backgroundColor: '#e3f2fd',
      borderColor: '#2196f3',
    })
  })

  it('returns correct config for Low priority', () => {
    const config = getToastConfig('Low')

    expect(config).toEqual({
      showToast: false, // Bell only, no toast
      autoDismissMs: 5000, // 5 seconds
      backgroundColor: '#f5f5f5',
      borderColor: '#9e9e9e',
    })
  })
})

describe('useNotificationToast', () => {
  // Mock notification factory
  const createMockNotification = (
    priority: 'Low' | 'Medium' | 'High',
    overrides?: Partial<NotificationDto>,
  ): NotificationDto => ({
    id: `notification-${Date.now()}-${Math.random()}`,
    type: 'InjectReady',
    priority,
    title: `${priority} Priority Notification`,
    message: 'Test message',
    actionUrl: null,
    relatedEntityType: null,
    relatedEntityId: null,
    isRead: false,
    createdAt: new Date().toISOString(),
    readAt: null,
    ...overrides,
  })

  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.restoreAllMocks()
    vi.useRealTimers()
  })

  it('initializes with empty toasts array', () => {
    const { result } = renderHook(() => useNotificationToast())

    expect(result.current.toasts).toEqual([])
  })

  it('addToast adds a High priority toast to the toasts array', () => {
    const { result } = renderHook(() => useNotificationToast())
    const notification = createMockNotification('High')

    act(() => {
      result.current.addToast(notification)
    })

    expect(result.current.toasts).toHaveLength(1)
    expect(result.current.toasts[0].notification).toEqual(notification)
    expect(result.current.toasts[0].id).toContain(notification.id)
    expect(result.current.toasts[0].createdAt).toBeInstanceOf(Date)
  })

  it('addToast adds a Medium priority toast to the toasts array', () => {
    const { result } = renderHook(() => useNotificationToast())
    const notification = createMockNotification('Medium')

    act(() => {
      result.current.addToast(notification)
    })

    expect(result.current.toasts).toHaveLength(1)
    expect(result.current.toasts[0].notification).toEqual(notification)
  })

  it('addToast skips Low priority notifications (showToast: false)', () => {
    const { result } = renderHook(() => useNotificationToast())
    const notification = createMockNotification('Low')

    act(() => {
      result.current.addToast(notification)
    })

    // Low priority notifications should not create a toast
    expect(result.current.toasts).toHaveLength(0)
  })

  it('addToast limits to MAX_VISIBLE_TOASTS (3)', () => {
    const { result } = renderHook(() => useNotificationToast())

    // Add 4 toasts
    act(() => {
      result.current.addToast(createMockNotification('High', { id: 'notif-1' }))
      result.current.addToast(createMockNotification('High', { id: 'notif-2' }))
      result.current.addToast(createMockNotification('High', { id: 'notif-3' }))
      result.current.addToast(createMockNotification('High', { id: 'notif-4' }))
    })

    // Should only keep 3 toasts (newest first)
    expect(result.current.toasts).toHaveLength(3)
    expect(result.current.toasts[0].notification.id).toBe('notif-4')
    expect(result.current.toasts[1].notification.id).toBe('notif-3')
    expect(result.current.toasts[2].notification.id).toBe('notif-2')
    // notif-1 should be removed (oldest)
  })

  it('addToast sets auto-dismiss timer for Medium priority', () => {
    const { result } = renderHook(() => useNotificationToast())
    const notification = createMockNotification('Medium')

    act(() => {
      result.current.addToast(notification)
    })

    expect(result.current.toasts).toHaveLength(1)

    // Fast-forward 10 seconds (Medium priority auto-dismiss time)
    act(() => {
      vi.advanceTimersByTime(10000)
    })

    // Toast should be auto-dismissed
    expect(result.current.toasts).toHaveLength(0)
  })

  it('addToast does not set auto-dismiss timer for High priority', () => {
    const { result } = renderHook(() => useNotificationToast())
    const notification = createMockNotification('High')

    act(() => {
      result.current.addToast(notification)
    })

    expect(result.current.toasts).toHaveLength(1)

    // Fast-forward way beyond any reasonable timeout
    act(() => {
      vi.advanceTimersByTime(60000) // 60 seconds
    })

    // Toast should still be present (High priority never auto-dismisses)
    expect(result.current.toasts).toHaveLength(1)
  })

  it('removeToast removes a toast by ID', () => {
    const { result } = renderHook(() => useNotificationToast())
    const notification = createMockNotification('High')

    act(() => {
      result.current.addToast(notification)
    })

    expect(result.current.toasts).toHaveLength(1)
    const toastId = result.current.toasts[0].id

    act(() => {
      result.current.removeToast(toastId)
    })

    expect(result.current.toasts).toHaveLength(0)
  })

  it('removeToast clears the timer if exists', () => {
    const { result } = renderHook(() => useNotificationToast())
    const notification = createMockNotification('Medium')

    act(() => {
      result.current.addToast(notification)
    })

    const toastId = result.current.toasts[0].id

    // Remove the toast before auto-dismiss
    act(() => {
      result.current.removeToast(toastId)
    })

    expect(result.current.toasts).toHaveLength(0)

    // Advance time to verify timer was cleared (no side effects)
    act(() => {
      vi.advanceTimersByTime(10000)
    })

    // Should still be 0 toasts
    expect(result.current.toasts).toHaveLength(0)
  })

  it('pauseAutoDismiss clears the timer for a toast', () => {
    const { result } = renderHook(() => useNotificationToast())
    const notification = createMockNotification('Medium')

    act(() => {
      result.current.addToast(notification)
    })

    const toastId = result.current.toasts[0].id

    // Pause auto-dismiss
    act(() => {
      result.current.pauseAutoDismiss(toastId)
    })

    // Fast-forward past the auto-dismiss time
    act(() => {
      vi.advanceTimersByTime(10000)
    })

    // Toast should still be present (timer was paused)
    expect(result.current.toasts).toHaveLength(1)
  })

  it('resumeAutoDismiss restarts the timer', () => {
    const { result } = renderHook(() => useNotificationToast())
    const notification = createMockNotification('Medium')

    act(() => {
      result.current.addToast(notification)
    })

    const toastId = result.current.toasts[0].id

    // Pause auto-dismiss
    act(() => {
      result.current.pauseAutoDismiss(toastId)
    })

    // Fast-forward (timer is paused, so toast should remain)
    act(() => {
      vi.advanceTimersByTime(5000)
    })

    expect(result.current.toasts).toHaveLength(1)

    // Resume auto-dismiss
    act(() => {
      result.current.resumeAutoDismiss(toastId, 'Medium')
    })

    // Fast-forward another 10 seconds (full auto-dismiss time)
    act(() => {
      vi.advanceTimersByTime(10000)
    })

    // Toast should now be dismissed
    expect(result.current.toasts).toHaveLength(0)
  })

  it('resumeAutoDismiss does not set timer for High priority', () => {
    const { result } = renderHook(() => useNotificationToast())
    const notification = createMockNotification('High')

    act(() => {
      result.current.addToast(notification)
    })

    const toastId = result.current.toasts[0].id

    // Pause (no-op for High priority, but ensures clean state)
    act(() => {
      result.current.pauseAutoDismiss(toastId)
    })

    // Resume auto-dismiss
    act(() => {
      result.current.resumeAutoDismiss(toastId, 'High')
    })

    // Fast-forward
    act(() => {
      vi.advanceTimersByTime(60000)
    })

    // Toast should still be present (High priority never auto-dismisses)
    expect(result.current.toasts).toHaveLength(1)
  })

  it('clearAll removes all toasts', () => {
    const { result } = renderHook(() => useNotificationToast())

    act(() => {
      result.current.addToast(createMockNotification('High', { id: 'notif-1' }))
      result.current.addToast(createMockNotification('Medium', { id: 'notif-2' }))
      result.current.addToast(createMockNotification('High', { id: 'notif-3' }))
    })

    expect(result.current.toasts).toHaveLength(3)

    act(() => {
      result.current.clearAll()
    })

    expect(result.current.toasts).toHaveLength(0)
  })

  it('clearAll clears all timers', () => {
    const { result } = renderHook(() => useNotificationToast())

    act(() => {
      result.current.addToast(createMockNotification('Medium', { id: 'notif-1' }))
      result.current.addToast(createMockNotification('Medium', { id: 'notif-2' }))
    })

    expect(result.current.toasts).toHaveLength(2)

    // Clear all toasts and timers
    act(() => {
      result.current.clearAll()
    })

    expect(result.current.toasts).toHaveLength(0)

    // Advance time to verify timers were cleared (no side effects)
    act(() => {
      vi.advanceTimersByTime(10000)
    })

    // Should still be 0 toasts
    expect(result.current.toasts).toHaveLength(0)
  })

  it('cleans up timers on unmount', () => {
    const clearTimeoutSpy = vi.spyOn(global, 'clearTimeout')
    const { result, unmount } = renderHook(() => useNotificationToast())

    act(() => {
      result.current.addToast(createMockNotification('Medium', { id: 'notif-1' }))
      result.current.addToast(createMockNotification('Medium', { id: 'notif-2' }))
    })

    // Unmount the hook
    unmount()

    // Verify clearTimeout was called for each timer
    expect(clearTimeoutSpy).toHaveBeenCalledTimes(2)
  })

  it('handles multiple toasts with different priorities', () => {
    const { result } = renderHook(() => useNotificationToast())

    act(() => {
      result.current.addToast(createMockNotification('High', { id: 'high-1' }))
      result.current.addToast(createMockNotification('Medium', { id: 'medium-1' }))
      result.current.addToast(createMockNotification('Low', { id: 'low-1' })) // Should be skipped
    })

    // Only High and Medium should be added
    expect(result.current.toasts).toHaveLength(2)
    expect(result.current.toasts[0].notification.id).toBe('medium-1')
    expect(result.current.toasts[1].notification.id).toBe('high-1')

    // Fast-forward 10 seconds
    act(() => {
      vi.advanceTimersByTime(10000)
    })

    // Medium should be auto-dismissed, High should remain
    expect(result.current.toasts).toHaveLength(1)
    expect(result.current.toasts[0].notification.id).toBe('high-1')
  })

  it('generates unique toast IDs for the same notification', () => {
    const { result } = renderHook(() => useNotificationToast())
    const notification = createMockNotification('High', { id: 'notif-1' })

    act(() => {
      result.current.addToast(notification)
    })

    const firstToastId = result.current.toasts[0].id

    // Advance time to ensure Date.now() returns a different value
    act(() => {
      vi.advanceTimersByTime(1)
    })

    act(() => {
      result.current.addToast(notification)
    })

    const secondToastId = result.current.toasts[0].id

    // Toast IDs should be unique even for the same notification
    expect(firstToastId).not.toBe(secondToastId)
    expect(result.current.toasts).toHaveLength(2)
  })
})
