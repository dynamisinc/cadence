/**
 * useDebounce Hook Tests
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { useDebounce } from './useDebounce'

describe('useDebounce', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  describe('Initial Value', () => {
    it('returns the initial value immediately', () => {
      const { result } = renderHook(() => useDebounce('initial', 300))
      expect(result.current).toBe('initial')
    })

    it('works with different value types', () => {
      const { result: numberResult } = renderHook(() => useDebounce(42, 300))
      expect(numberResult.current).toBe(42)

      const { result: objectResult } = renderHook(() =>
        useDebounce({ key: 'value' }, 300),
      )
      expect(objectResult.current).toEqual({ key: 'value' })

      const { result: arrayResult } = renderHook(() =>
        useDebounce([1, 2, 3], 300),
      )
      expect(arrayResult.current).toEqual([1, 2, 3])
    })
  })

  describe('Debounce Behavior', () => {
    it('does not update value before delay', () => {
      const { result, rerender } = renderHook(
        ({ value }) => useDebounce(value, 300),
        { initialProps: { value: 'initial' } },
      )

      // Change the value
      rerender({ value: 'updated' })

      // Before delay, should still be initial
      expect(result.current).toBe('initial')

      // Advance timer but not past delay
      act(() => {
        vi.advanceTimersByTime(299)
      })

      expect(result.current).toBe('initial')
    })

    it('updates value after delay', () => {
      const { result, rerender } = renderHook(
        ({ value }) => useDebounce(value, 300),
        { initialProps: { value: 'initial' } },
      )

      // Change the value
      rerender({ value: 'updated' })

      // Advance timer past delay
      act(() => {
        vi.advanceTimersByTime(300)
      })

      expect(result.current).toBe('updated')
    })

    it('resets timer on rapid changes', () => {
      const { result, rerender } = renderHook(
        ({ value }) => useDebounce(value, 300),
        { initialProps: { value: 'initial' } },
      )

      // First change
      rerender({ value: 'first' })
      act(() => {
        vi.advanceTimersByTime(200)
      })

      // Second change (resets timer)
      rerender({ value: 'second' })
      act(() => {
        vi.advanceTimersByTime(200)
      })

      // Still showing initial because timer was reset
      expect(result.current).toBe('initial')

      // Complete the delay
      act(() => {
        vi.advanceTimersByTime(100)
      })

      // Now shows the final value
      expect(result.current).toBe('second')
    })
  })

  describe('Custom Delay', () => {
    it('uses default delay of 300ms when not specified', () => {
      const { result, rerender } = renderHook(
        ({ value }) => useDebounce(value),
        { initialProps: { value: 'initial' } },
      )

      rerender({ value: 'updated' })

      act(() => {
        vi.advanceTimersByTime(299)
      })
      expect(result.current).toBe('initial')

      act(() => {
        vi.advanceTimersByTime(1)
      })
      expect(result.current).toBe('updated')
    })

    it('respects custom delay value', () => {
      const { result, rerender } = renderHook(
        ({ value }) => useDebounce(value, 500),
        { initialProps: { value: 'initial' } },
      )

      rerender({ value: 'updated' })

      act(() => {
        vi.advanceTimersByTime(400)
      })
      expect(result.current).toBe('initial')

      act(() => {
        vi.advanceTimersByTime(100)
      })
      expect(result.current).toBe('updated')
    })

    it('handles zero delay (immediate update on next tick)', () => {
      const { result, rerender } = renderHook(
        ({ value }) => useDebounce(value, 0),
        { initialProps: { value: 'initial' } },
      )

      rerender({ value: 'updated' })

      act(() => {
        vi.advanceTimersByTime(0)
      })

      expect(result.current).toBe('updated')
    })
  })

  describe('Cleanup', () => {
    it('cleans up timer on unmount', () => {
      const clearTimeoutSpy = vi.spyOn(global, 'clearTimeout')

      const { unmount, rerender } = renderHook(
        ({ value }) => useDebounce(value, 300),
        { initialProps: { value: 'initial' } },
      )

      rerender({ value: 'updated' })
      unmount()

      expect(clearTimeoutSpy).toHaveBeenCalled()
      clearTimeoutSpy.mockRestore()
    })
  })
})
