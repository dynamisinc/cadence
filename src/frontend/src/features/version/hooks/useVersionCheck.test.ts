import { renderHook, act } from '@testing-library/react'
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { useVersionCheck } from './useVersionCheck'

const STORAGE_KEY = 'cadence_last_seen_version'

// Mock the version module
vi.mock('@/config/version', () => ({
  appVersion: {
    version: '1.0.0',
    buildDate: '2026-01-30T00:00:00.000Z',
    commitSha: 'abc1234',
  },
}))

describe('useVersionCheck', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('should not show modal on first visit', () => {
    const { result } = renderHook(() => useVersionCheck())

    expect(result.current.showWhatsNew).toBe(false)
    expect(result.current.previousVersion).toBeNull()
    expect(result.current.currentVersion).toBe('1.0.0')
  })

  it('should store version on first visit', () => {
    renderHook(() => useVersionCheck())

    expect(localStorage.getItem(STORAGE_KEY)).toBe('1.0.0')
  })

  it('should show modal when version changes', () => {
    localStorage.setItem(STORAGE_KEY, '0.9.0')

    const { result } = renderHook(() => useVersionCheck())

    expect(result.current.showWhatsNew).toBe(true)
    expect(result.current.previousVersion).toBe('0.9.0')
  })

  it('should not show modal when version matches', () => {
    localStorage.setItem(STORAGE_KEY, '1.0.0')

    const { result } = renderHook(() => useVersionCheck())

    expect(result.current.showWhatsNew).toBe(false)
  })

  it('should update storage when dismissed', () => {
    localStorage.setItem(STORAGE_KEY, '0.9.0')

    const { result } = renderHook(() => useVersionCheck())

    expect(result.current.showWhatsNew).toBe(true)

    act(() => {
      result.current.dismissWhatsNew()
    })

    expect(result.current.showWhatsNew).toBe(false)
    expect(localStorage.getItem(STORAGE_KEY)).toBe('1.0.0')
  })
})
