/**
 * Story Time Utility Tests
 *
 * Tests for story time calculation and formatting functions.
 */

import { describe, it, expect } from 'vitest'
import {
  calculateStoryTime,
  formatStoryTime,
  parseInjectScenarioTime,
  type StoryTime,
  type StoryTimeConfig,
} from './storyTime'
import { TimelineMode } from '../../../types'

describe('calculateStoryTime', () => {
  const defaultConfig: StoryTimeConfig = {
    startDay: 1,
    startHours: 0,
    startMinutes: 0,
  }

  describe('RealTime mode', () => {
    it('returns same time as elapsed for RealTime mode', () => {
      const elapsedMs = 2 * 60 * 60 * 1000 + 30 * 60 * 1000 // 2:30:00
      const result = calculateStoryTime(elapsedMs, TimelineMode.RealTime, null, defaultConfig)

      expect(result).toEqual({
        day: 1,
        hours: 2,
        minutes: 30,
      })
    })

    it('handles zero elapsed time', () => {
      const result = calculateStoryTime(0, TimelineMode.RealTime, null, defaultConfig)

      expect(result).toEqual({
        day: 1,
        hours: 0,
        minutes: 0,
      })
    })
  })

  describe('Compressed mode', () => {
    it('multiplies elapsed by timeScale for Compressed mode', () => {
      const elapsedMs = 15 * 60 * 1000 // 15 real minutes
      const timeScale = 4
      const result = calculateStoryTime(
        elapsedMs, TimelineMode.Compressed, timeScale, defaultConfig,
      )

      // 15 real minutes × 4 = 60 story minutes = 1 hour
      expect(result).toEqual({
        day: 1,
        hours: 1,
        minutes: 0,
      })
    })

    it('handles day rollover at midnight', () => {
      const elapsedMs = 6 * 60 * 60 * 1000 // 6 real hours
      const timeScale = 4
      const result = calculateStoryTime(
        elapsedMs, TimelineMode.Compressed, timeScale, defaultConfig,
      )

      // 6 real hours × 4 = 24 story hours = 1 day
      expect(result).toEqual({
        day: 2,
        hours: 0,
        minutes: 0,
      })
    })

    it('handles multiple day rollovers', () => {
      const elapsedMs = 12 * 60 * 60 * 1000 // 12 real hours
      const timeScale = 4
      const result = calculateStoryTime(
        elapsedMs, TimelineMode.Compressed, timeScale, defaultConfig,
      )

      // 12 real hours × 4 = 48 story hours = 2 days
      expect(result).toEqual({
        day: 3,
        hours: 0,
        minutes: 0,
      })
    })

    it('handles timeScale < 1 (slow motion)', () => {
      const elapsedMs = 2 * 60 * 60 * 1000 // 2 real hours
      const timeScale = 0.5
      const result = calculateStoryTime(
        elapsedMs, TimelineMode.Compressed, timeScale, defaultConfig,
      )

      // 2 real hours × 0.5 = 1 story hour
      expect(result).toEqual({
        day: 1,
        hours: 1,
        minutes: 0,
      })
    })
  })

  describe('Start time configuration', () => {
    it('uses start config for non-zero starting time', () => {
      const config: StoryTimeConfig = {
        startDay: 1,
        startHours: 8,
        startMinutes: 30,
      }
      const elapsedMs = 90 * 60 * 1000 // 1:30:00
      const result = calculateStoryTime(elapsedMs, TimelineMode.RealTime, null, config)

      // Start at 08:30, add 1:30 = 10:00
      expect(result).toEqual({
        day: 1,
        hours: 10,
        minutes: 0,
      })
    })

    it('handles day rollover from non-zero start time', () => {
      const config: StoryTimeConfig = {
        startDay: 1,
        startHours: 22,
        startMinutes: 0,
      }
      const elapsedMs = 3 * 60 * 60 * 1000 // 3:00:00
      const result = calculateStoryTime(elapsedMs, TimelineMode.RealTime, null, config)

      // Start at 22:00 (Day 1), add 3 hours = 01:00 (Day 2)
      expect(result).toEqual({
        day: 2,
        hours: 1,
        minutes: 0,
      })
    })

    it('starts from later day', () => {
      const config: StoryTimeConfig = {
        startDay: 3,
        startHours: 14,
        startMinutes: 0,
      }
      const elapsedMs = 2 * 60 * 60 * 1000 // 2:00:00
      const result = calculateStoryTime(elapsedMs, TimelineMode.RealTime, null, config)

      // Start at Day 3 14:00, add 2 hours = Day 3 16:00
      expect(result).toEqual({
        day: 3,
        hours: 16,
        minutes: 0,
      })
    })
  })

  describe('StoryOnly mode', () => {
    it('returns placeholder for StoryOnly mode', () => {
      const result = calculateStoryTime(0, TimelineMode.StoryOnly, null, defaultConfig)

      expect(result).toEqual({
        day: 0,
        hours: 0,
        minutes: 0,
      })
    })
  })

  describe('Edge cases', () => {
    it('handles fractional minutes correctly', () => {
      const elapsedMs = 2.5 * 60 * 1000 // 2 minutes 30 seconds
      const result = calculateStoryTime(elapsedMs, TimelineMode.RealTime, null, defaultConfig)

      // Should truncate to 2 minutes
      expect(result).toEqual({
        day: 1,
        hours: 0,
        minutes: 2,
      })
    })

    it('handles null timeScale in Compressed mode', () => {
      const elapsedMs = 60 * 60 * 1000 // 1 hour
      const result = calculateStoryTime(elapsedMs, TimelineMode.Compressed, null, defaultConfig)

      // Defaults to scale of 1
      expect(result).toEqual({
        day: 1,
        hours: 1,
        minutes: 0,
      })
    })
  })
})

describe('formatStoryTime', () => {
  it('formats as "Day N • HH:MM"', () => {
    const storyTime: StoryTime = {
      day: 1,
      hours: 8,
      minutes: 32,
    }

    expect(formatStoryTime(storyTime)).toBe('Day 1 • 08:32')
  })

  it('pads single-digit hours and minutes', () => {
    const storyTime: StoryTime = {
      day: 2,
      hours: 5,
      minutes: 7,
    }

    expect(formatStoryTime(storyTime)).toBe('Day 2 • 05:07')
  })

  it('handles midnight', () => {
    const storyTime: StoryTime = {
      day: 3,
      hours: 0,
      minutes: 0,
    }

    expect(formatStoryTime(storyTime)).toBe('Day 3 • 00:00')
  })

  it('handles end of day', () => {
    const storyTime: StoryTime = {
      day: 1,
      hours: 23,
      minutes: 59,
    }

    expect(formatStoryTime(storyTime)).toBe('Day 1 • 23:59')
  })

  it('handles double-digit day numbers', () => {
    const storyTime: StoryTime = {
      day: 15,
      hours: 14,
      minutes: 30,
    }

    expect(formatStoryTime(storyTime)).toBe('Day 15 • 14:30')
  })
})

describe('parseInjectScenarioTime', () => {
  describe('Valid formats', () => {
    it('parses HH:MM format correctly', () => {
      const result = parseInjectScenarioTime(1, '08:30')

      expect(result).toEqual({
        day: 1,
        hours: 8,
        minutes: 30,
      })
    })

    it('parses HH:MM:SS format correctly (ignores seconds)', () => {
      const result = parseInjectScenarioTime(2, '14:45:30')

      expect(result).toEqual({
        day: 2,
        hours: 14,
        minutes: 45,
      })
    })

    it('handles midnight (00:00)', () => {
      const result = parseInjectScenarioTime(1, '00:00')

      expect(result).toEqual({
        day: 1,
        hours: 0,
        minutes: 0,
      })
    })

    it('handles end of day (23:59)', () => {
      const result = parseInjectScenarioTime(1, '23:59')

      expect(result).toEqual({
        day: 1,
        hours: 23,
        minutes: 59,
      })
    })

    it('handles single-digit hours and minutes with leading zeros', () => {
      const result = parseInjectScenarioTime(3, '05:07')

      expect(result).toEqual({
        day: 3,
        hours: 5,
        minutes: 7,
      })
    })
  })

  describe('Invalid input handling', () => {
    it('returns null when scenarioDay is null', () => {
      const result = parseInjectScenarioTime(null, '08:30')

      expect(result).toBeNull()
    })

    it('returns null when scenarioTime is null', () => {
      const result = parseInjectScenarioTime(1, null)

      expect(result).toBeNull()
    })

    it('returns null when scenarioTime has no colon (e.g., "12")', () => {
      const result = parseInjectScenarioTime(1, '12')

      expect(result).toBeNull()
    })

    it('returns null when scenarioTime has only one part before colon (e.g., "12:")', () => {
      const result = parseInjectScenarioTime(1, '12:')

      expect(result).toBeNull()
    })

    it('returns null when scenarioTime has empty hours part (e.g., ":30")', () => {
      const result = parseInjectScenarioTime(1, ':30')

      expect(result).toBeNull()
    })

    it('returns null when hours is not a valid number', () => {
      const result = parseInjectScenarioTime(1, 'abc:30')

      expect(result).toBeNull()
    })

    it('returns null when minutes is not a valid number', () => {
      const result = parseInjectScenarioTime(1, '08:xyz')

      expect(result).toBeNull()
    })

    it('returns null when hours is out of range (> 23)', () => {
      const result = parseInjectScenarioTime(1, '25:30')

      expect(result).toBeNull()
    })

    it('returns null when hours is negative', () => {
      const result = parseInjectScenarioTime(1, '-5:30')

      expect(result).toBeNull()
    })

    it('returns null when minutes is out of range (> 59)', () => {
      const result = parseInjectScenarioTime(1, '08:75')

      expect(result).toBeNull()
    })

    it('returns null when minutes is negative', () => {
      const result = parseInjectScenarioTime(1, '08:-15')

      expect(result).toBeNull()
    })
  })

  describe('Edge cases', () => {
    it('handles multiple colons (HH:MM:SS:MS format) by using first two parts', () => {
      const result = parseInjectScenarioTime(1, '12:30:45:123')

      expect(result).toEqual({
        day: 1,
        hours: 12,
        minutes: 30,
      })
    })

    it('handles whitespace-free input', () => {
      const result = parseInjectScenarioTime(5, '09:15')

      expect(result).toEqual({
        day: 5,
        hours: 9,
        minutes: 15,
      })
    })
  })
})
