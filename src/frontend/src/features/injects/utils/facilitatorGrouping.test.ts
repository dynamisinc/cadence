/**
 * Facilitator Grouping Utilities Tests
 *
 * Tests for utility functions that group injects for facilitator-paced conduct view.
 *
 * @module features/injects
 * @see exercise-config/S07-facilitator-paced-conduct-view
 */

import { describe, it, expect } from 'vitest'
import { InjectStatus, InjectType, TriggerType } from '../../../types'
import type { InjectDto } from '../types'
import {
  getCurrentInject,
  getUpNextInjects,
  getInjectsToSkip,
} from './facilitatorGrouping'

// Test helper to create inject with minimal required fields
const createInject = (overrides: Partial<InjectDto>): InjectDto => ({
  id: overrides.id || 'test-id',
  injectNumber: overrides.injectNumber || 1,
  title: overrides.title || 'Test Inject',
  description: overrides.description || 'Test description',
  scheduledTime: '08:00:00',
  deliveryTime: null,
  scenarioDay: null,
  scenarioTime: null,
  target: 'Test Target',
  source: null,
  deliveryMethod: null,
  deliveryMethodId: null,
  deliveryMethodName: null,
  deliveryMethodOther: null,
  injectType: InjectType.Standard,
  status: InjectStatus.Pending,
  sequence: overrides.sequence || 1,
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
  mselId: 'test-msel-id',
  phaseId: null,
  phaseName: null,
  objectiveIds: [],
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  sourceReference: null,
  priority: null,
  triggerType: TriggerType.Manual,
  responsibleController: null,
  locationName: null,
  locationType: null,
  track: null,
  ...overrides,
})

describe('getCurrentInject', () => {
  it('returns first pending inject by sequence', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending }),
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Pending }),
      createInject({ id: 'inject-3', sequence: 3, status: InjectStatus.Pending }),
    ]

    const result = getCurrentInject(injects)

    expect(result).not.toBeNull()
    expect(result?.id).toBe('inject-1')
    expect(result?.sequence).toBe(1)
  })

  it('returns null when no pending injects', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Fired }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Skipped }),
    ]

    const result = getCurrentInject(injects)

    expect(result).toBeNull()
  })

  it('ignores fired injects', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Fired }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending }),
    ]

    const result = getCurrentInject(injects)

    expect(result).not.toBeNull()
    expect(result?.id).toBe('inject-2')
  })

  it('ignores skipped injects', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Skipped }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending }),
    ]

    const result = getCurrentInject(injects)

    expect(result).not.toBeNull()
    expect(result?.id).toBe('inject-2')
  })

  it('returns null for empty array', () => {
    const result = getCurrentInject([])

    expect(result).toBeNull()
  })
})

describe('getUpNextInjects', () => {
  it('returns next N pending injects after current sequence', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Fired }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending }),
      createInject({ id: 'inject-3', sequence: 3, status: InjectStatus.Pending }),
      createInject({ id: 'inject-4', sequence: 4, status: InjectStatus.Pending }),
    ]

    const result = getUpNextInjects(injects, 1, 2)

    expect(result).toHaveLength(2)
    expect(result[0].id).toBe('inject-2')
    expect(result[1].id).toBe('inject-3')
  })

  it('defaults to 3 injects when count not specified', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Fired }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending }),
      createInject({ id: 'inject-3', sequence: 3, status: InjectStatus.Pending }),
      createInject({ id: 'inject-4', sequence: 4, status: InjectStatus.Pending }),
      createInject({ id: 'inject-5', sequence: 5, status: InjectStatus.Pending }),
    ]

    const result = getUpNextInjects(injects, 1)

    expect(result).toHaveLength(3)
    expect(result[0].id).toBe('inject-2')
    expect(result[1].id).toBe('inject-3')
    expect(result[2].id).toBe('inject-4')
  })

  it('ignores fired and skipped injects', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Fired }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Fired }),
      createInject({ id: 'inject-3', sequence: 3, status: InjectStatus.Pending }),
      createInject({ id: 'inject-4', sequence: 4, status: InjectStatus.Skipped }),
      createInject({ id: 'inject-5', sequence: 5, status: InjectStatus.Pending }),
    ]

    const result = getUpNextInjects(injects, 1, 3)

    expect(result).toHaveLength(2)
    expect(result[0].id).toBe('inject-3')
    expect(result[1].id).toBe('inject-5')
  })

  it('returns empty array when no injects after current', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Pending }),
    ]

    const result = getUpNextInjects(injects, 5)

    expect(result).toHaveLength(0)
  })

  it('returns fewer injects if not enough available', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Fired }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending }),
    ]

    const result = getUpNextInjects(injects, 1, 5)

    expect(result).toHaveLength(1)
    expect(result[0].id).toBe('inject-2')
  })

  it('sorts by sequence ascending', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-5', sequence: 5, status: InjectStatus.Pending }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending }),
      createInject({ id: 'inject-4', sequence: 4, status: InjectStatus.Pending }),
      createInject({ id: 'inject-3', sequence: 3, status: InjectStatus.Pending }),
    ]

    const result = getUpNextInjects(injects, 1, 3)

    expect(result).toHaveLength(3)
    expect(result[0].sequence).toBe(2)
    expect(result[1].sequence).toBe(3)
    expect(result[2].sequence).toBe(4)
  })
})

describe('getInjectsToSkip', () => {
  it('returns pending injects between current and target', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Pending }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending }),
      createInject({ id: 'inject-3', sequence: 3, status: InjectStatus.Pending }),
      createInject({ id: 'inject-4', sequence: 4, status: InjectStatus.Pending }),
    ]

    const result = getInjectsToSkip(injects, 1, 4)

    // Should include current (1) and injects 2 and 3, but not target (4)
    expect(result).toHaveLength(3)
    expect(result[0].id).toBe('inject-1')
    expect(result[1].id).toBe('inject-2')
    expect(result[2].id).toBe('inject-3')
  })

  it('excludes target inject', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Pending }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending }),
    ]

    const result = getInjectsToSkip(injects, 1, 2)

    expect(result).toHaveLength(1)
    expect(result[0].id).toBe('inject-1')
  })

  it('only includes pending injects', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Pending }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Fired }),
      createInject({ id: 'inject-3', sequence: 3, status: InjectStatus.Pending }),
      createInject({ id: 'inject-4', sequence: 4, status: InjectStatus.Pending }),
    ]

    const result = getInjectsToSkip(injects, 1, 4)

    // Should only include pending injects 1 and 3, not fired inject 2
    expect(result).toHaveLength(2)
    expect(result[0].id).toBe('inject-1')
    expect(result[1].id).toBe('inject-3')
  })

  it('returns empty array when current equals target', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Pending }),
    ]

    const result = getInjectsToSkip(injects, 1, 1)

    expect(result).toHaveLength(0)
  })

  it('returns empty array when no injects between current and target', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Pending }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending }),
    ]

    const result = getInjectsToSkip(injects, 1, 2)

    // Only current inject, no injects in between
    expect(result).toHaveLength(1)
    expect(result[0].id).toBe('inject-1')
  })

  it('sorts by sequence ascending', () => {
    const injects: InjectDto[] = [
      createInject({ id: 'inject-3', sequence: 3, status: InjectStatus.Pending }),
      createInject({ id: 'inject-1', sequence: 1, status: InjectStatus.Pending }),
      createInject({ id: 'inject-2', sequence: 2, status: InjectStatus.Pending }),
      createInject({ id: 'inject-5', sequence: 5, status: InjectStatus.Pending }),
    ]

    const result = getInjectsToSkip(injects, 1, 5)

    expect(result).toHaveLength(3)
    expect(result[0].sequence).toBe(1)
    expect(result[1].sequence).toBe(2)
    expect(result[2].sequence).toBe(3)
  })
})
