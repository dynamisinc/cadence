import { renderHook } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { useReleaseNotes, getReleaseNotesForVersion } from './useReleaseNotes'

describe('useReleaseNotes', () => {
  it('should return bundled release notes', () => {
    const { result } = renderHook(() => useReleaseNotes())

    expect(result.current.releaseNotes).toBeDefined()
    expect(result.current.releaseNotes.length).toBeGreaterThan(0)
    expect(result.current.isLoading).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it('should include version 1.0.0 in release notes', () => {
    const { result } = renderHook(() => useReleaseNotes())

    const v1Release = result.current.releaseNotes.find(r => r.version === '1.0.0')
    expect(v1Release).toBeDefined()
    expect(v1Release?.date).toBeDefined()
  })
})

describe('getReleaseNotesForVersion', () => {
  const mockReleaseNotes = [
    { version: '1.0.0', date: '2026-01-30', features: ['Feature 1'], fixes: [] },
    { version: '0.9.0', date: '2026-01-15', features: ['Feature 0'], fixes: ['Fix 1'] },
  ]

  it('should find release notes for existing version', () => {
    const result = getReleaseNotesForVersion(mockReleaseNotes, '1.0.0')

    expect(result).toBeDefined()
    expect(result?.version).toBe('1.0.0')
    expect(result?.features).toContain('Feature 1')
  })

  it('should return undefined for non-existent version', () => {
    const result = getReleaseNotesForVersion(mockReleaseNotes, '2.0.0')

    expect(result).toBeUndefined()
  })
})
