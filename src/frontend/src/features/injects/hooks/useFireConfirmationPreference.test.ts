/**
 * Tests for useFireConfirmationPreference hook
 *
 * Validates sessionStorage-based preference management for fire confirmation dialog.
 */

import { describe, it, expect, beforeEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { useFireConfirmationPreference } from './useFireConfirmationPreference'

describe('useFireConfirmationPreference', () => {
  const STORAGE_KEY = 'cadence_skipFireConfirmation'

  beforeEach(() => {
    // Clear sessionStorage before each test
    sessionStorage.clear()
  })

  it('defaults to false (show confirmation)', () => {
    const { result } = renderHook(() => useFireConfirmationPreference())

    expect(result.current[0]).toBe(false)
  })

  it('returns true when sessionStorage has "true" value', () => {
    sessionStorage.setItem(STORAGE_KEY, 'true')

    const { result } = renderHook(() => useFireConfirmationPreference())

    expect(result.current[0]).toBe(true)
  })

  it('returns false when sessionStorage has "false" value', () => {
    sessionStorage.setItem(STORAGE_KEY, 'false')

    const { result } = renderHook(() => useFireConfirmationPreference())

    expect(result.current[0]).toBe(false)
  })

  it('updates state and sessionStorage when setSkip is called with true', () => {
    const { result } = renderHook(() => useFireConfirmationPreference())

    act(() => {
      result.current[1](true)
    })

    expect(result.current[0]).toBe(true)
    expect(sessionStorage.getItem(STORAGE_KEY)).toBe('true')
  })

  it('updates state and clears sessionStorage when setSkip is called with false', () => {
    // Set initial value
    sessionStorage.setItem(STORAGE_KEY, 'true')

    const { result } = renderHook(() => useFireConfirmationPreference())

    expect(result.current[0]).toBe(true)

    act(() => {
      result.current[1](false)
    })

    expect(result.current[0]).toBe(false)
    expect(sessionStorage.getItem(STORAGE_KEY)).toBeNull()
  })

  it('persists across hook re-renders', () => {
    const { result, rerender } = renderHook(() => useFireConfirmationPreference())

    act(() => {
      result.current[1](true)
    })

    rerender()

    expect(result.current[0]).toBe(true)
  })

  it('resets preference on new session (simulated by clearing storage)', () => {
    const { result } = renderHook(() => useFireConfirmationPreference())

    // Set preference
    act(() => {
      result.current[1](true)
    })

    expect(result.current[0]).toBe(true)

    // Simulate new session by clearing sessionStorage
    sessionStorage.clear()

    // Re-render hook as if new session
    const { result: newResult } = renderHook(() => useFireConfirmationPreference())

    expect(newResult.current[0]).toBe(false)
  })
})
