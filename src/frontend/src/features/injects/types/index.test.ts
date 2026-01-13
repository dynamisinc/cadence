import { describe, it, expect } from 'vitest'
import { formatScenarioTime, formatScheduledTime, calculateVariance } from './index'

describe('formatScenarioTime', () => {
  it('returns null when day is null', () => {
    expect(formatScenarioTime(null, '08:00:00')).toBeNull()
  })

  it('returns day only when time is null', () => {
    expect(formatScenarioTime(1, null)).toBe('Day 1')
  })

  it('formats day and time together', () => {
    expect(formatScenarioTime(1, '08:00:00')).toBe('D1 08:00')
  })

  it('formats multi-digit days', () => {
    expect(formatScenarioTime(10, '14:30:00')).toBe('D10 14:30')
  })
})

describe('formatScheduledTime', () => {
  it('formats morning time in 12-hour format', () => {
    expect(formatScheduledTime('09:30:00')).toBe('9:30 AM')
  })

  it('formats afternoon time in 12-hour format', () => {
    expect(formatScheduledTime('14:45:00')).toBe('2:45 PM')
  })

  it('formats noon correctly', () => {
    expect(formatScheduledTime('12:00:00')).toBe('12:00 PM')
  })

  it('formats midnight correctly', () => {
    expect(formatScheduledTime('00:15:00')).toBe('12:15 AM')
  })
})

describe('calculateVariance', () => {
  it('returns "On time" when fired at scheduled time', () => {
    // Create a date at 09:00 local time
    const firedDate = new Date()
    firedDate.setHours(9, 0, 0, 0)
    const result = calculateVariance('09:00:00', firedDate.toISOString())
    expect(result).toBe('On time')
  })

  it('returns positive variance when fired late', () => {
    // Create a date at 09:05 local time
    const firedDate = new Date()
    firedDate.setHours(9, 5, 0, 0)
    const result = calculateVariance('09:00:00', firedDate.toISOString())
    expect(result).toBe('+5 min')
  })

  it('returns negative variance when fired early', () => {
    // Create a date at 08:55 local time
    const firedDate = new Date()
    firedDate.setHours(8, 55, 0, 0)
    const result = calculateVariance('09:00:00', firedDate.toISOString())
    expect(result).toBe('-5 min')
  })

  it('handles larger variances', () => {
    // Create a date at 09:30 local time
    const firedDate = new Date()
    firedDate.setHours(9, 30, 0, 0)
    const result = calculateVariance('09:00:00', firedDate.toISOString())
    expect(result).toBe('+30 min')
  })
})
