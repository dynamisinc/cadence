/**
 * Date Utilities Tests
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import {
  parseUtcDate,
  formatDateTime,
  formatDate,
  formatTime,
  formatRelativeTime,
  formatSmartDateTime,
} from './dateUtils'

describe('dateUtils', () => {
  describe('parseUtcDate', () => {
    it('parses date string with Z suffix correctly', () => {
      const date = parseUtcDate('2024-01-15T10:30:00Z')
      expect(date.getUTCHours()).toBe(10)
      expect(date.getUTCMinutes()).toBe(30)
    })

    it('treats date string without Z as UTC', () => {
      const date = parseUtcDate('2024-01-15T10:30:00')
      expect(date.getUTCHours()).toBe(10)
      expect(date.getUTCMinutes()).toBe(30)
    })

    it('handles date string with timezone offset', () => {
      const date = parseUtcDate('2024-01-15T10:30:00+05:00')
      // 10:30 + 5 hours = 15:30 UTC, but adjusted to 05:30 UTC
      expect(date.getUTCHours()).toBe(5)
      expect(date.getUTCMinutes()).toBe(30)
    })
  })

  describe('formatDateTime', () => {
    it('formats date with default format', () => {
      const result = formatDateTime('2024-01-15T10:30:00Z')
      // Result depends on locale, but should include date and time
      expect(result).toMatch(/Jan|January/)
      expect(result).toMatch(/15/)
      expect(result).toMatch(/2024/)
    })

    it('accepts custom format', () => {
      const result = formatDateTime('2024-01-15T10:30:00Z', 'yyyy-MM-dd')
      expect(result).toBe('2024-01-15')
    })
  })

  describe('formatDate', () => {
    it('formats date without time', () => {
      const result = formatDate('2024-01-15T10:30:00Z')
      expect(result).toMatch(/Jan|January/)
      expect(result).toMatch(/15/)
      expect(result).toMatch(/2024/)
      // Should not include time
      expect(result).not.toMatch(/:/)
    })
  })

  describe('formatTime', () => {
    it('formats time only', () => {
      const result = formatTime('2024-01-15T10:30:00Z')
      // Should include time indicator
      expect(result).toMatch(/:/)
      // Should not include full date
      expect(result).not.toMatch(/2024/)
    })
  })

  describe('formatRelativeTime', () => {
    beforeEach(() => {
      vi.useFakeTimers()
    })

    afterEach(() => {
      vi.useRealTimers()
    })

    it('shows relative time with suffix', () => {
      // Set current time to 5 minutes after the date
      vi.setSystemTime(new Date('2024-01-15T10:35:00Z'))

      const result = formatRelativeTime('2024-01-15T10:30:00Z')
      expect(result).toMatch(/5 minutes ago/i)
    })

    it('shows relative time without suffix when requested', () => {
      vi.setSystemTime(new Date('2024-01-15T10:35:00Z'))

      const result = formatRelativeTime('2024-01-15T10:30:00Z', false)
      expect(result).toMatch(/5 minutes/i)
      expect(result).not.toMatch(/ago/i)
    })
  })

  describe('formatSmartDateTime', () => {
    beforeEach(() => {
      vi.useFakeTimers()
    })

    afterEach(() => {
      vi.useRealTimers()
    })

    it('shows relative time for recent dates', () => {
      // Set current time to 2 hours after the date
      vi.setSystemTime(new Date('2024-01-15T12:30:00Z'))

      const result = formatSmartDateTime('2024-01-15T10:30:00Z')
      expect(result).toMatch(/hours? ago/i)
    })

    it('shows full date for older dates', () => {
      // Set current time to 2 days after the date
      vi.setSystemTime(new Date('2024-01-17T10:30:00Z'))

      const result = formatSmartDateTime('2024-01-15T10:30:00Z')
      expect(result).toMatch(/Jan|January/)
      expect(result).toMatch(/15/)
    })

    it('respects custom threshold', () => {
      // Set current time to 2 hours after the date
      vi.setSystemTime(new Date('2024-01-15T12:30:00Z'))

      // With 1 hour threshold, 2 hours ago should show full date
      const result = formatSmartDateTime('2024-01-15T10:30:00Z', 1)
      expect(result).toMatch(/Jan|January/)
    })
  })
})
