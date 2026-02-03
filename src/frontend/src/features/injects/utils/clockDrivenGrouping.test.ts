/**
 * Tests for clock-driven inject grouping utilities
 *
 * Tests the grouping logic for CLK-06: Clock-Driven Conduct View Sections
 */

import { describe, it, expect } from 'vitest'
import {
  groupInjectsForClockDriven,
  formatCountdown,
} from './clockDrivenGrouping'
import { InjectStatus } from '../../../types'
import type { InjectDto } from '../types'

// Test helper to create a mock inject
const createMockInject = (
  id: string,
  status: InjectStatus,
  deliveryTime: string | null = null,
  injectNumber: number = 1,
): InjectDto => ({
  id,
  injectNumber,
  title: `Inject ${id}`,
  description: 'Test inject description',
  scheduledTime: '09:00:00',
  deliveryTime,
  scenarioDay: 1,
  scenarioTime: '09:00:00',
  target: 'Test Target',
  source: null,
  deliveryMethod: null,
  deliveryMethodId: null,
  deliveryMethodName: null,
  deliveryMethodOther: null,
  injectType: 'Standard',
  status,
  sequence: injectNumber,
  parentInjectId: null,
  triggerCondition: null,
  expectedAction: null,
  controllerNotes: null,
  readyAt: null,
  firedAt: null,
  firedBy: null,
  firedByName: null,
  skippedAt: null,
  skippedBy: null,
  skippedByName: null,
  skipReason: null,
  mselId: 'msel-1',
  phaseId: null,
  phaseName: null,
  objectiveIds: [],
  createdAt: '2025-01-20T00:00:00Z',
  updatedAt: '2025-01-20T00:00:00Z',
  sourceReference: null,
  priority: null,
  triggerType: 'Manual',
  responsibleController: null,
  locationName: null,
  locationType: null,
  track: null,
})

describe('groupInjectsForClockDriven', () => {
  it('puts Synchronized status injects in ready array', () => {
    const injects: InjectDto[] = [
      createMockInject('1', InjectStatus.Synchronized, '00:30:00'),
      createMockInject('2', InjectStatus.Synchronized, '00:45:00'),
    ]

    const result = groupInjectsForClockDriven(injects, 40 * 60 * 1000) // 40 minutes elapsed

    expect(result.ready).toHaveLength(2)
    expect(result.ready[0].id).toBe('1')
    expect(result.ready[1].id).toBe('2')
    expect(result.upcoming).toHaveLength(0)
    expect(result.completed).toHaveLength(0)
  })

  it('puts Draft injects within 30 min in upcoming array', () => {
    const injects: InjectDto[] = [
      createMockInject('1', InjectStatus.Draft, '00:50:00'), // 10 min away
      createMockInject('2', InjectStatus.Draft, '01:00:00'), // 20 min away
      createMockInject('3', InjectStatus.Draft, '01:10:00'), // 30 min away
    ]

    const result = groupInjectsForClockDriven(injects, 40 * 60 * 1000) // 40 minutes elapsed

    expect(result.upcoming).toHaveLength(3)
    expect(result.upcoming[0].id).toBe('1')
    expect(result.upcoming[1].id).toBe('2')
    expect(result.upcoming[2].id).toBe('3')
    expect(result.ready).toHaveLength(0)
    expect(result.completed).toHaveLength(0)
  })

  it('excludes Draft injects beyond 30 min from upcoming', () => {
    const injects: InjectDto[] = [
      createMockInject('1', InjectStatus.Draft, '00:50:00'), // 10 min away - included
      createMockInject('2', InjectStatus.Draft, '01:15:00'), // 35 min away - excluded
      createMockInject('3', InjectStatus.Draft, '02:00:00'), // 80 min away - excluded
    ]

    const result = groupInjectsForClockDriven(injects, 40 * 60 * 1000) // 40 minutes elapsed

    expect(result.upcoming).toHaveLength(1)
    expect(result.upcoming[0].id).toBe('1')
  })

  it('puts Released and Deferred injects in completed array', () => {
    const injects: InjectDto[] = [
      createMockInject('1', InjectStatus.Released, '00:30:00'),
      createMockInject('2', InjectStatus.Deferred, '00:45:00'),
      createMockInject('3', InjectStatus.Released, '01:00:00'),
    ]

    const result = groupInjectsForClockDriven(injects, 40 * 60 * 1000)

    expect(result.completed).toHaveLength(3)
    expect(result.completed.find(i => i.id === '1')).toBeDefined()
    expect(result.completed.find(i => i.id === '2')).toBeDefined()
    expect(result.completed.find(i => i.id === '3')).toBeDefined()
    expect(result.ready).toHaveLength(0)
    expect(result.upcoming).toHaveLength(0)
  })

  it('sorts upcoming by DeliveryTime ascending', () => {
    const injects: InjectDto[] = [
      createMockInject('1', InjectStatus.Draft, '01:00:00', 1),
      createMockInject('2', InjectStatus.Draft, '00:50:00', 2),
      createMockInject('3', InjectStatus.Draft, '00:45:00', 3),
    ]

    const result = groupInjectsForClockDriven(injects, 40 * 60 * 1000) // 40 minutes elapsed

    expect(result.upcoming).toHaveLength(3)
    expect(result.upcoming[0].id).toBe('3') // 00:45:00
    expect(result.upcoming[1].id).toBe('2') // 00:50:00
    expect(result.upcoming[2].id).toBe('1') // 01:00:00
  })

  it('handles injects with null deliveryTime', () => {
    const injects: InjectDto[] = [
      createMockInject('1', InjectStatus.Draft, null),
      createMockInject('2', InjectStatus.Synchronized, null),
    ]

    const result = groupInjectsForClockDriven(injects, 40 * 60 * 1000)

    // Injects without deliveryTime should not be in upcoming
    expect(result.upcoming).toHaveLength(0)
    // Synchronized status still goes to ready regardless of deliveryTime
    expect(result.ready).toHaveLength(1)
    expect(result.ready[0].id).toBe('2')
  })

  it('handles empty inject array', () => {
    const result = groupInjectsForClockDriven([], 0)

    expect(result.ready).toHaveLength(0)
    expect(result.upcoming).toHaveLength(0)
    expect(result.completed).toHaveLength(0)
  })

  it('handles Draft injects that have passed their delivery time', () => {
    const injects: InjectDto[] = [
      // This inject's delivery time has passed but it's still Draft (not auto-synchronized yet)
      createMockInject('1', InjectStatus.Draft, '00:30:00'),
    ]

    const result = groupInjectsForClockDriven(injects, 40 * 60 * 1000) // 40 minutes elapsed

    // Draft injects that have passed their time should not appear in any section
    // (They should have been auto-synchronized by CLK-05, but if not, we don't show them)
    expect(result.upcoming).toHaveLength(0)
    expect(result.ready).toHaveLength(0)
  })
})

describe('formatCountdown', () => {
  it('formats minutes and seconds correctly', () => {
    const targetMs = 30 * 60 * 1000 // 30 minutes
    const currentMs = 17 * 60 * 1000 + 15 * 1000 // 17:15

    const result = formatCountdown(targetMs, currentMs)

    expect(result).toBe('in 12:45')
  })

  it('shows hours for times over 60 minutes', () => {
    const targetMs = 120 * 60 * 1000 // 120 minutes
    const currentMs = 30 * 60 * 1000 // 30 minutes

    const result = formatCountdown(targetMs, currentMs)

    expect(result).toBe('in 1h 30m')
  })

  it('returns "now" for zero difference', () => {
    const targetMs = 30 * 60 * 1000
    const currentMs = 30 * 60 * 1000

    const result = formatCountdown(targetMs, currentMs)

    expect(result).toBe('now')
  })

  it('returns "now" for negative difference', () => {
    const targetMs = 30 * 60 * 1000
    const currentMs = 35 * 60 * 1000

    const result = formatCountdown(targetMs, currentMs)

    expect(result).toBe('now')
  })

  it('handles very large time differences', () => {
    const targetMs = 10 * 60 * 60 * 1000 // 10 hours
    const currentMs = 0

    const result = formatCountdown(targetMs, currentMs)

    expect(result).toBe('in 10h 0m')
  })

  it('formats single-digit minutes with leading zero', () => {
    const targetMs = 5 * 60 * 1000 // 5 minutes
    const currentMs = 0

    const result = formatCountdown(targetMs, currentMs)

    expect(result).toBe('in 5:00')
  })
})
