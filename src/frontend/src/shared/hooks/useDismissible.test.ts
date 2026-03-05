/**
 * useDismissible Hook Tests
 */

import { describe, it, expect, beforeEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { useDismissible } from './useDismissible'

describe('useDismissible', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('returns isDismissed as false by default', () => {
    const { result } = renderHook(() => useDismissible('test-key'))
    expect(result.current.isDismissed).toBe(false)
  })

  it('reads existing dismissed state from localStorage', () => {
    localStorage.setItem('cadence:dismissed:test-key', 'true')
    const { result } = renderHook(() => useDismissible('test-key'))
    expect(result.current.isDismissed).toBe(true)
  })

  it('dismiss() sets isDismissed to true and writes to localStorage', () => {
    const { result } = renderHook(() => useDismissible('test-key'))

    act(() => {
      result.current.dismiss()
    })

    expect(result.current.isDismissed).toBe(true)
    expect(localStorage.getItem('cadence:dismissed:test-key')).toBe('true')
  })

  it('reset() sets isDismissed to false and removes from localStorage', () => {
    localStorage.setItem('cadence:dismissed:test-key', 'true')
    const { result } = renderHook(() => useDismissible('test-key'))

    expect(result.current.isDismissed).toBe(true)

    act(() => {
      result.current.reset()
    })

    expect(result.current.isDismissed).toBe(false)
    expect(localStorage.getItem('cadence:dismissed:test-key')).toBeNull()
  })

  it('uses separate storage keys for different instances', () => {
    const { result: result1 } = renderHook(() => useDismissible('key-a'))
    const { result: result2 } = renderHook(() => useDismissible('key-b'))

    act(() => {
      result1.current.dismiss()
    })

    expect(result1.current.isDismissed).toBe(true)
    expect(result2.current.isDismissed).toBe(false)
  })

  it('handles non-"true" localStorage values as not dismissed', () => {
    localStorage.setItem('cadence:dismissed:test-key', 'false')
    const { result } = renderHook(() => useDismissible('test-key'))
    expect(result.current.isDismissed).toBe(false)
  })
})
