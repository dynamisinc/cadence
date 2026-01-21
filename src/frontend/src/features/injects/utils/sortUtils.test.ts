/**
 * Sort Utilities Tests
 */

import { describe, it, expect } from 'vitest'
import {
  sortInjects,
  getNextSortDirection,
  toggleSortConfig,
  buildPhaseSequenceMap,
} from './sortUtils'
import type { InjectDto } from '../types'
import type { SortConfig } from '../types/organization'
import { InjectStatus, InjectType, TriggerType } from '../../../types'

// Helper to create test inject data
const createInject = (overrides: Partial<InjectDto> = {}): InjectDto => ({
  id: 'test-id',
  injectNumber: 1,
  title: 'Test Inject',
  description: 'Test description',
  scheduledTime: '09:00:00',
  deliveryTime: null,
  scenarioDay: 1,
  scenarioTime: '08:00:00',
  target: 'Target',
  source: null,
  deliveryMethod: null,
  deliveryMethodId: null,
  deliveryMethodName: null,
  deliveryMethodOther: null,
  injectType: InjectType.Standard,
  status: InjectStatus.Pending,
  sequence: 1,
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
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  sourceReference: null,
  priority: null,
  triggerType: TriggerType.Manual,
  responsibleController: null,
  locationName: null,
  locationType: null,
  track: null,
  ...overrides,
})

describe('sortInjects', () => {
  describe('No Sort Configuration', () => {
    it('returns original order when column is null', () => {
      const injects = [
        createInject({ id: '1', injectNumber: 3 }),
        createInject({ id: '2', injectNumber: 1 }),
        createInject({ id: '3', injectNumber: 2 }),
      ]
      const config: SortConfig = { column: null, direction: null }

      const result = sortInjects(injects, config)

      expect(result.map(i => i.id)).toEqual(['1', '2', '3'])
    })

    it('returns original order when direction is null', () => {
      const injects = [
        createInject({ id: '1', injectNumber: 3 }),
        createInject({ id: '2', injectNumber: 1 }),
      ]
      const config: SortConfig = { column: 'injectNumber', direction: null }

      const result = sortInjects(injects, config)

      expect(result.map(i => i.id)).toEqual(['1', '2'])
    })
  })

  describe('Sort by Inject Number', () => {
    it('sorts ascending by inject number', () => {
      const injects = [
        createInject({ id: '1', injectNumber: 3 }),
        createInject({ id: '2', injectNumber: 1 }),
        createInject({ id: '3', injectNumber: 2 }),
      ]
      const config: SortConfig = { column: 'injectNumber', direction: 'asc' }

      const result = sortInjects(injects, config)

      expect(result.map(i => i.injectNumber)).toEqual([1, 2, 3])
    })

    it('sorts descending by inject number', () => {
      const injects = [
        createInject({ id: '1', injectNumber: 1 }),
        createInject({ id: '2', injectNumber: 3 }),
        createInject({ id: '3', injectNumber: 2 }),
      ]
      const config: SortConfig = { column: 'injectNumber', direction: 'desc' }

      const result = sortInjects(injects, config)

      expect(result.map(i => i.injectNumber)).toEqual([3, 2, 1])
    })
  })

  describe('Sort by Title', () => {
    it('sorts ascending alphabetically (case-insensitive)', () => {
      const injects = [
        createInject({ id: '1', title: 'Zebra Event' }),
        createInject({ id: '2', title: 'alpha Event' }),
        createInject({ id: '3', title: 'Beta Event' }),
      ]
      const config: SortConfig = { column: 'title', direction: 'asc' }

      const result = sortInjects(injects, config)

      expect(result.map(i => i.title)).toEqual(['alpha Event', 'Beta Event', 'Zebra Event'])
    })

    it('sorts descending alphabetically', () => {
      const injects = [
        createInject({ id: '1', title: 'Alpha' }),
        createInject({ id: '2', title: 'Zebra' }),
        createInject({ id: '3', title: 'Beta' }),
      ]
      const config: SortConfig = { column: 'title', direction: 'desc' }

      const result = sortInjects(injects, config)

      expect(result.map(i => i.title)).toEqual(['Zebra', 'Beta', 'Alpha'])
    })
  })

  describe('Sort by Scheduled Time', () => {
    it('sorts ascending by scheduled time', () => {
      const injects = [
        createInject({ id: '1', scheduledTime: '14:00:00' }),
        createInject({ id: '2', scheduledTime: '09:00:00' }),
        createInject({ id: '3', scheduledTime: '11:30:00' }),
      ]
      const config: SortConfig = { column: 'scheduledTime', direction: 'asc' }

      const result = sortInjects(injects, config)

      expect(result.map(i => i.scheduledTime)).toEqual(['09:00:00', '11:30:00', '14:00:00'])
    })

    it('sorts descending by scheduled time', () => {
      const injects = [
        createInject({ id: '1', scheduledTime: '09:00:00' }),
        createInject({ id: '2', scheduledTime: '14:00:00' }),
        createInject({ id: '3', scheduledTime: '11:30:00' }),
      ]
      const config: SortConfig = { column: 'scheduledTime', direction: 'desc' }

      const result = sortInjects(injects, config)

      expect(result.map(i => i.scheduledTime)).toEqual(['14:00:00', '11:30:00', '09:00:00'])
    })
  })

  describe('Sort by Scenario Time', () => {
    it('sorts by day first, then by time within day', () => {
      const injects = [
        createInject({ id: '1', scenarioDay: 2, scenarioTime: '08:00:00' }),
        createInject({ id: '2', scenarioDay: 1, scenarioTime: '14:00:00' }),
        createInject({ id: '3', scenarioDay: 1, scenarioTime: '08:00:00' }),
      ]
      const config: SortConfig = { column: 'scenarioTime', direction: 'asc' }

      const result = sortInjects(injects, config)

      expect(result.map(i => `D${i.scenarioDay} ${i.scenarioTime}`)).toEqual([
        'D1 08:00:00',
        'D1 14:00:00',
        'D2 08:00:00',
      ])
    })

    it('sorts null scenario days to the end', () => {
      const injects = [
        createInject({ id: '1', scenarioDay: null, scenarioTime: null }),
        createInject({ id: '2', scenarioDay: 1, scenarioTime: '08:00:00' }),
        createInject({ id: '3', scenarioDay: 2, scenarioTime: '08:00:00' }),
      ]
      const config: SortConfig = { column: 'scenarioTime', direction: 'asc' }

      const result = sortInjects(injects, config)

      expect(result.map(i => i.scenarioDay)).toEqual([1, 2, null])
    })
  })

  describe('Sort by Status', () => {
    it('sorts in order: Pending → Fired → Skipped', () => {
      const injects = [
        createInject({ id: '1', status: InjectStatus.Skipped }),
        createInject({ id: '2', status: InjectStatus.Pending }),
        createInject({ id: '3', status: InjectStatus.Fired }),
      ]
      const config: SortConfig = { column: 'status', direction: 'asc' }

      const result = sortInjects(injects, config)

      expect(result.map(i => i.status)).toEqual([
        InjectStatus.Pending,
        InjectStatus.Fired,
        InjectStatus.Skipped,
      ])
    })

    it('sorts descending: Skipped → Fired → Pending', () => {
      const injects = [
        createInject({ id: '1', status: InjectStatus.Pending }),
        createInject({ id: '2', status: InjectStatus.Skipped }),
        createInject({ id: '3', status: InjectStatus.Fired }),
      ]
      const config: SortConfig = { column: 'status', direction: 'desc' }

      const result = sortInjects(injects, config)

      expect(result.map(i => i.status)).toEqual([
        InjectStatus.Skipped,
        InjectStatus.Fired,
        InjectStatus.Pending,
      ])
    })
  })

  describe('Sort by Phase', () => {
    it('sorts by phase sequence, unassigned last', () => {
      const phases = new Map([
        ['phase-1', 1],
        ['phase-2', 2],
        ['phase-3', 3],
      ])
      const injects = [
        createInject({ id: '1', phaseId: null }),
        createInject({ id: '2', phaseId: 'phase-2' }),
        createInject({ id: '3', phaseId: 'phase-1' }),
      ]
      const config: SortConfig = { column: 'phase', direction: 'asc' }

      const result = sortInjects(injects, config, phases)

      expect(result.map(i => i.phaseId)).toEqual(['phase-1', 'phase-2', null])
    })

    it('sorts descending with unassigned first', () => {
      const phases = new Map([
        ['phase-1', 1],
        ['phase-2', 2],
      ])
      const injects = [
        createInject({ id: '1', phaseId: 'phase-1' }),
        createInject({ id: '2', phaseId: null }),
        createInject({ id: '3', phaseId: 'phase-2' }),
      ]
      const config: SortConfig = { column: 'phase', direction: 'desc' }

      const result = sortInjects(injects, config, phases)

      expect(result.map(i => i.phaseId)).toEqual([null, 'phase-2', 'phase-1'])
    })
  })

  describe('Stable Sort', () => {
    it('maintains original order for equal values', () => {
      const injects = [
        createInject({ id: '1', status: InjectStatus.Pending, injectNumber: 1 }),
        createInject({ id: '2', status: InjectStatus.Pending, injectNumber: 2 }),
        createInject({ id: '3', status: InjectStatus.Pending, injectNumber: 3 }),
      ]
      const config: SortConfig = { column: 'status', direction: 'asc' }

      const result = sortInjects(injects, config)

      // All have same status, should maintain inject number order
      expect(result.map(i => i.injectNumber)).toEqual([1, 2, 3])
    })
  })
})

describe('getNextSortDirection', () => {
  it('returns asc from null', () => {
    expect(getNextSortDirection(null)).toBe('asc')
  })

  it('returns desc from asc', () => {
    expect(getNextSortDirection('asc')).toBe('desc')
  })

  it('returns null from desc', () => {
    expect(getNextSortDirection('desc')).toBe(null)
  })
})

describe('toggleSortConfig', () => {
  it('starts with ascending when clicking new column', () => {
    const current: SortConfig = { column: 'title', direction: 'asc' }

    const result = toggleSortConfig(current, 'injectNumber')

    expect(result).toEqual({ column: 'injectNumber', direction: 'asc' })
  })

  it('toggles to descending when clicking same column', () => {
    const current: SortConfig = { column: 'title', direction: 'asc' }

    const result = toggleSortConfig(current, 'title')

    expect(result).toEqual({ column: 'title', direction: 'desc' })
  })

  it('clears sort when cycling past descending', () => {
    const current: SortConfig = { column: 'title', direction: 'desc' }

    const result = toggleSortConfig(current, 'title')

    expect(result).toEqual({ column: null, direction: null })
  })

  it('starts fresh when clicking column with null config', () => {
    const current: SortConfig = { column: null, direction: null }

    const result = toggleSortConfig(current, 'title')

    expect(result).toEqual({ column: 'title', direction: 'asc' })
  })
})

describe('buildPhaseSequenceMap', () => {
  it('creates map from phase array', () => {
    const phases = [
      { id: 'phase-1', sequence: 1 },
      { id: 'phase-2', sequence: 2 },
      { id: 'phase-3', sequence: 3 },
    ]

    const result = buildPhaseSequenceMap(phases)

    expect(result.get('phase-1')).toBe(1)
    expect(result.get('phase-2')).toBe(2)
    expect(result.get('phase-3')).toBe(3)
  })

  it('returns empty map for empty input', () => {
    const result = buildPhaseSequenceMap([])

    expect(result.size).toBe(0)
  })
})
