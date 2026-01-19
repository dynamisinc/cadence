/**
 * Filter Utilities Tests
 */

import { describe, it, expect } from 'vitest'
import {
  filterByStatus,
  filterByPhase,
  filterByMethod,
  filterByObjective,
  applyFilters,
  countActiveFilters,
  hasActiveFilters,
  clearFilter,
  clearAllFilters,
  getActiveFilterLabels,
  buildPhaseNameMap,
  buildObjectiveNameMap,
} from './filterUtils'
import type { InjectDto } from '../types'
import type { FilterState } from '../types/organization'
import { InjectStatus, InjectType, DeliveryMethod } from '../../../types'

// Helper to create test inject data
const createInject = (overrides: Partial<InjectDto> = {}): InjectDto => ({
  id: 'test-id',
  injectNumber: 1,
  title: 'Test Inject',
  description: 'Test description',
  scheduledTime: '09:00:00',
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
  triggerType: 'Manual',
  responsibleController: null,
  locationName: null,
  locationType: null,
  track: null,
  ...overrides,
})

describe('filterByStatus', () => {
  it('returns all injects when no statuses selected', () => {
    const injects = [
      createInject({ id: '1', status: InjectStatus.Pending }),
      createInject({ id: '2', status: InjectStatus.Fired }),
    ]

    const result = filterByStatus(injects, [])

    expect(result).toHaveLength(2)
  })

  it('filters to single status', () => {
    const injects = [
      createInject({ id: '1', status: InjectStatus.Pending }),
      createInject({ id: '2', status: InjectStatus.Fired }),
      createInject({ id: '3', status: InjectStatus.Skipped }),
    ]

    const result = filterByStatus(injects, [InjectStatus.Pending])

    expect(result).toHaveLength(1)
    expect(result[0].status).toBe(InjectStatus.Pending)
  })

  it('filters to multiple statuses (OR logic)', () => {
    const injects = [
      createInject({ id: '1', status: InjectStatus.Pending }),
      createInject({ id: '2', status: InjectStatus.Fired }),
      createInject({ id: '3', status: InjectStatus.Skipped }),
    ]

    const result = filterByStatus(injects, [InjectStatus.Pending, InjectStatus.Fired])

    expect(result).toHaveLength(2)
    expect(result.map(i => i.status)).toContain(InjectStatus.Pending)
    expect(result.map(i => i.status)).toContain(InjectStatus.Fired)
  })
})

describe('filterByPhase', () => {
  it('returns all injects when no phases selected', () => {
    const injects = [
      createInject({ id: '1', phaseId: 'phase-1' }),
      createInject({ id: '2', phaseId: null }),
    ]

    const result = filterByPhase(injects, [])

    expect(result).toHaveLength(2)
  })

  it('filters to single phase', () => {
    const injects = [
      createInject({ id: '1', phaseId: 'phase-1' }),
      createInject({ id: '2', phaseId: 'phase-2' }),
      createInject({ id: '3', phaseId: null }),
    ]

    const result = filterByPhase(injects, ['phase-1'])

    expect(result).toHaveLength(1)
    expect(result[0].phaseId).toBe('phase-1')
  })

  it('filters to unassigned (null phase)', () => {
    const injects = [
      createInject({ id: '1', phaseId: 'phase-1' }),
      createInject({ id: '2', phaseId: null }),
    ]

    const result = filterByPhase(injects, [null])

    expect(result).toHaveLength(1)
    expect(result[0].phaseId).toBeNull()
  })

  it('filters to multiple phases (OR logic)', () => {
    const injects = [
      createInject({ id: '1', phaseId: 'phase-1' }),
      createInject({ id: '2', phaseId: 'phase-2' }),
      createInject({ id: '3', phaseId: null }),
    ]

    const result = filterByPhase(injects, ['phase-1', null])

    expect(result).toHaveLength(2)
  })
})

describe('filterByMethod', () => {
  it('returns all injects when no methods selected', () => {
    const injects = [
      createInject({ id: '1', deliveryMethod: DeliveryMethod.Email }),
      createInject({ id: '2', deliveryMethod: null }),
    ]

    const result = filterByMethod(injects, [])

    expect(result).toHaveLength(2)
  })

  it('filters to single method', () => {
    const injects = [
      createInject({ id: '1', deliveryMethod: DeliveryMethod.Email }),
      createInject({ id: '2', deliveryMethod: DeliveryMethod.Phone }),
    ]

    const result = filterByMethod(injects, [DeliveryMethod.Email])

    expect(result).toHaveLength(1)
    expect(result[0].deliveryMethod).toBe(DeliveryMethod.Email)
  })

  it('excludes injects with null delivery method', () => {
    const injects = [
      createInject({ id: '1', deliveryMethod: DeliveryMethod.Email }),
      createInject({ id: '2', deliveryMethod: null }),
    ]

    const result = filterByMethod(injects, [DeliveryMethod.Email])

    expect(result).toHaveLength(1)
    expect(result[0].deliveryMethod).toBe(DeliveryMethod.Email)
  })
})

describe('filterByObjective', () => {
  it('returns all injects when no objectives selected', () => {
    const injects = [
      createInject({ id: '1', objectiveIds: ['obj-1'] }),
      createInject({ id: '2', objectiveIds: [] }),
    ]

    const result = filterByObjective(injects, [])

    expect(result).toHaveLength(2)
  })

  it('filters to single objective', () => {
    const injects = [
      createInject({ id: '1', objectiveIds: ['obj-1'] }),
      createInject({ id: '2', objectiveIds: ['obj-2'] }),
      createInject({ id: '3', objectiveIds: [] }),
    ]

    const result = filterByObjective(injects, ['obj-1'])

    expect(result).toHaveLength(1)
    expect(result[0].objectiveIds).toContain('obj-1')
  })

  it('filters to multiple objectives (OR logic)', () => {
    const injects = [
      createInject({ id: '1', objectiveIds: ['obj-1'] }),
      createInject({ id: '2', objectiveIds: ['obj-2'] }),
      createInject({ id: '3', objectiveIds: ['obj-3'] }),
    ]

    const result = filterByObjective(injects, ['obj-1', 'obj-2'])

    expect(result).toHaveLength(2)
    expect(result.map(i => i.id)).toContain('1')
    expect(result.map(i => i.id)).toContain('2')
  })

  it('includes injects with no objectives when null is selected', () => {
    const injects = [
      createInject({ id: '1', objectiveIds: ['obj-1'] }),
      createInject({ id: '2', objectiveIds: [] }),
      createInject({ id: '3', objectiveIds: [] }),
    ]

    const result = filterByObjective(injects, [null])

    expect(result).toHaveLength(2)
    expect(result.every(i => i.objectiveIds.length === 0)).toBe(true)
  })

  it('includes injects with objectives AND no objectives when both selected', () => {
    const injects = [
      createInject({ id: '1', objectiveIds: ['obj-1'] }),
      createInject({ id: '2', objectiveIds: [] }),
      createInject({ id: '3', objectiveIds: ['obj-2'] }),
    ]

    const result = filterByObjective(injects, ['obj-1', null])

    expect(result).toHaveLength(2)
    expect(result.map(i => i.id)).toContain('1')
    expect(result.map(i => i.id)).toContain('2')
  })

  it('matches injects with multiple objectives if any match', () => {
    const injects = [
      createInject({ id: '1', objectiveIds: ['obj-1', 'obj-2', 'obj-3'] }),
      createInject({ id: '2', objectiveIds: ['obj-4'] }),
    ]

    const result = filterByObjective(injects, ['obj-2'])

    expect(result).toHaveLength(1)
    expect(result[0].id).toBe('1')
  })
})

describe('applyFilters', () => {
  it('applies all filters with AND logic', () => {
    const injects = [
      createInject({
        id: '1',
        status: InjectStatus.Pending,
        phaseId: 'phase-1',
        deliveryMethod: DeliveryMethod.Email,
      }),
      createInject({
        id: '2',
        status: InjectStatus.Pending,
        phaseId: 'phase-2',
        deliveryMethod: DeliveryMethod.Email,
      }),
      createInject({
        id: '3',
        status: InjectStatus.Fired,
        phaseId: 'phase-1',
        deliveryMethod: DeliveryMethod.Email,
      }),
    ]

    const filters: FilterState = {
      statuses: [InjectStatus.Pending],
      phaseIds: ['phase-1'],
      deliveryMethods: [DeliveryMethod.Email],
      objectiveIds: [],
    }

    const result = applyFilters(injects, filters)

    expect(result).toHaveLength(1)
    expect(result[0].id).toBe('1')
  })

  it('returns all when no filters active', () => {
    const injects = [
      createInject({ id: '1' }),
      createInject({ id: '2' }),
    ]

    const filters: FilterState = {
      statuses: [],
      phaseIds: [],
      deliveryMethods: [],
      objectiveIds: [],
    }

    const result = applyFilters(injects, filters)

    expect(result).toHaveLength(2)
  })

  it('applies objective filter with other filters', () => {
    const injects = [
      createInject({
        id: '1',
        status: InjectStatus.Pending,
        objectiveIds: ['obj-1'],
      }),
      createInject({
        id: '2',
        status: InjectStatus.Pending,
        objectiveIds: ['obj-2'],
      }),
      createInject({
        id: '3',
        status: InjectStatus.Fired,
        objectiveIds: ['obj-1'],
      }),
    ]

    const filters: FilterState = {
      statuses: [InjectStatus.Pending],
      phaseIds: [],
      deliveryMethods: [],
      objectiveIds: ['obj-1'],
    }

    const result = applyFilters(injects, filters)

    expect(result).toHaveLength(1)
    expect(result[0].id).toBe('1')
  })
})

describe('countActiveFilters', () => {
  it('returns 0 when no filters active', () => {
    const filters: FilterState = {
      statuses: [],
      phaseIds: [],
      deliveryMethods: [],
      objectiveIds: [],
    }

    expect(countActiveFilters(filters)).toBe(0)
  })

  it('counts each active filter category', () => {
    const filters: FilterState = {
      statuses: [InjectStatus.Pending, InjectStatus.Fired],
      phaseIds: ['phase-1'],
      deliveryMethods: [],
      objectiveIds: [],
    }

    expect(countActiveFilters(filters)).toBe(2)
  })

  it('returns 4 when all filters active', () => {
    const filters: FilterState = {
      statuses: [InjectStatus.Pending],
      phaseIds: ['phase-1'],
      deliveryMethods: [DeliveryMethod.Email],
      objectiveIds: ['obj-1'],
    }

    expect(countActiveFilters(filters)).toBe(4)
  })
})

describe('hasActiveFilters', () => {
  it('returns false when no filters active', () => {
    const filters: FilterState = {
      statuses: [],
      phaseIds: [],
      deliveryMethods: [],
      objectiveIds: [],
    }

    expect(hasActiveFilters(filters)).toBe(false)
  })

  it('returns true when any filter is active', () => {
    expect(hasActiveFilters({
      statuses: [InjectStatus.Pending],
      phaseIds: [],
      deliveryMethods: [],
      objectiveIds: [],
    })).toBe(true)

    expect(hasActiveFilters({
      statuses: [],
      phaseIds: ['phase-1'],
      deliveryMethods: [],
      objectiveIds: [],
    })).toBe(true)

    expect(hasActiveFilters({
      statuses: [],
      phaseIds: [],
      deliveryMethods: [DeliveryMethod.Email],
      objectiveIds: [],
    })).toBe(true)

    expect(hasActiveFilters({
      statuses: [],
      phaseIds: [],
      deliveryMethods: [],
      objectiveIds: ['obj-1'],
    })).toBe(true)
  })
})

describe('clearFilter', () => {
  it('clears status filter', () => {
    const filters: FilterState = {
      statuses: [InjectStatus.Pending],
      phaseIds: ['phase-1'],
      deliveryMethods: [DeliveryMethod.Email],
      objectiveIds: ['obj-1'],
    }

    const result = clearFilter(filters, 'status')

    expect(result.statuses).toEqual([])
    expect(result.phaseIds).toEqual(['phase-1'])
    expect(result.deliveryMethods).toEqual([DeliveryMethod.Email])
    expect(result.objectiveIds).toEqual(['obj-1'])
  })

  it('clears phase filter', () => {
    const filters: FilterState = {
      statuses: [InjectStatus.Pending],
      phaseIds: ['phase-1'],
      deliveryMethods: [],
      objectiveIds: [],
    }

    const result = clearFilter(filters, 'phase')

    expect(result.statuses).toEqual([InjectStatus.Pending])
    expect(result.phaseIds).toEqual([])
  })

  it('clears method filter', () => {
    const filters: FilterState = {
      statuses: [],
      phaseIds: [],
      deliveryMethods: [DeliveryMethod.Email],
      objectiveIds: [],
    }

    const result = clearFilter(filters, 'method')

    expect(result.deliveryMethods).toEqual([])
  })

  it('clears objective filter', () => {
    const filters: FilterState = {
      statuses: [],
      phaseIds: [],
      deliveryMethods: [],
      objectiveIds: ['obj-1', 'obj-2'],
    }

    const result = clearFilter(filters, 'objective')

    expect(result.objectiveIds).toEqual([])
  })
})

describe('clearAllFilters', () => {
  it('returns empty filter state', () => {
    const result = clearAllFilters()

    expect(result.statuses).toEqual([])
    expect(result.phaseIds).toEqual([])
    expect(result.deliveryMethods).toEqual([])
    expect(result.objectiveIds).toEqual([])
  })
})

describe('getActiveFilterLabels', () => {
  it('returns empty array when no filters active', () => {
    const filters: FilterState = {
      statuses: [],
      phaseIds: [],
      deliveryMethods: [],
      objectiveIds: [],
    }

    const result = getActiveFilterLabels(filters, new Map())

    expect(result).toEqual([])
  })

  it('returns label for single status filter', () => {
    const filters: FilterState = {
      statuses: [InjectStatus.Pending],
      phaseIds: [],
      deliveryMethods: [],
      objectiveIds: [],
    }

    const result = getActiveFilterLabels(filters, new Map())

    expect(result).toHaveLength(1)
    expect(result[0]).toEqual({
      type: 'status',
      label: 'Status',
      value: 'Pending',
    })
  })

  it('returns count for multiple status selections', () => {
    const filters: FilterState = {
      statuses: [InjectStatus.Pending, InjectStatus.Fired],
      phaseIds: [],
      deliveryMethods: [],
      objectiveIds: [],
    }

    const result = getActiveFilterLabels(filters, new Map())

    expect(result[0].value).toBe('2 selected')
  })

  it('uses phase map for phase names', () => {
    const filters: FilterState = {
      statuses: [],
      phaseIds: ['phase-1'],
      deliveryMethods: [],
      objectiveIds: [],
    }

    const phaseMap = new Map<string | null, string>([
      ['phase-1', 'Initial Response'],
    ])

    const result = getActiveFilterLabels(filters, phaseMap)

    expect(result[0].value).toBe('Initial Response')
  })

  it('shows Unassigned for null phase', () => {
    const filters: FilterState = {
      statuses: [],
      phaseIds: [null],
      deliveryMethods: [],
      objectiveIds: [],
    }

    const phaseMap = new Map<string | null, string>([
      [null, 'Unassigned'],
    ])

    const result = getActiveFilterLabels(filters, phaseMap)

    expect(result[0].value).toBe('Unassigned')
  })

  it('uses objective map for objective names', () => {
    const filters: FilterState = {
      statuses: [],
      phaseIds: [],
      deliveryMethods: [],
      objectiveIds: ['obj-1'],
    }

    const objectiveMap = new Map<string | null, string>([
      ['obj-1', 'OBJ-1: Test Objective'],
    ])

    const result = getActiveFilterLabels(filters, new Map(), objectiveMap)

    expect(result).toHaveLength(1)
    expect(result[0]).toEqual({
      type: 'objective',
      label: 'Objective',
      value: 'OBJ-1: Test Objective',
    })
  })

  it('shows No objectives for null objective', () => {
    const filters: FilterState = {
      statuses: [],
      phaseIds: [],
      deliveryMethods: [],
      objectiveIds: [null],
    }

    const objectiveMap = new Map<string | null, string>([
      [null, 'No objectives'],
    ])

    const result = getActiveFilterLabels(filters, new Map(), objectiveMap)

    expect(result[0].value).toBe('No objectives')
  })

  it('returns count for multiple objective selections', () => {
    const filters: FilterState = {
      statuses: [],
      phaseIds: [],
      deliveryMethods: [],
      objectiveIds: ['obj-1', 'obj-2'],
    }

    const result = getActiveFilterLabels(filters, new Map())

    expect(result).toHaveLength(1)
    expect(result[0].value).toBe('2 selected')
  })
})

describe('buildPhaseNameMap', () => {
  it('creates map with Unassigned for null', () => {
    const phases = [
      { id: 'phase-1', name: 'Phase 1' },
    ]

    const result = buildPhaseNameMap(phases)

    expect(result.get(null)).toBe('Unassigned')
    expect(result.get('phase-1')).toBe('Phase 1')
  })

  it('handles empty phases array', () => {
    const result = buildPhaseNameMap([])

    expect(result.get(null)).toBe('Unassigned')
    expect(result.size).toBe(1)
  })
})

describe('buildObjectiveNameMap', () => {
  it('creates map with No objectives for null', () => {
    const objectives = [
      { id: 'obj-1', name: 'Test Communications', objectiveNumber: 'OBJ-1' },
    ]

    const result = buildObjectiveNameMap(objectives)

    expect(result.get(null)).toBe('No objectives')
    expect(result.get('obj-1')).toBe('OBJ-1: Test Communications')
  })

  it('handles multiple objectives', () => {
    const objectives = [
      { id: 'obj-1', name: 'Test Communications', objectiveNumber: 'OBJ-1' },
      { id: 'obj-2', name: 'Test Evacuation', objectiveNumber: 'OBJ-2' },
    ]

    const result = buildObjectiveNameMap(objectives)

    expect(result.size).toBe(3) // 2 objectives + null
    expect(result.get('obj-1')).toBe('OBJ-1: Test Communications')
    expect(result.get('obj-2')).toBe('OBJ-2: Test Evacuation')
  })

  it('handles empty objectives array', () => {
    const result = buildObjectiveNameMap([])

    expect(result.get(null)).toBe('No objectives')
    expect(result.size).toBe(1)
  })
})
