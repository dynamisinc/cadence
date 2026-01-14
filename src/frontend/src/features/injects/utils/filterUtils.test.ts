/**
 * Filter Utilities Tests
 */

import { describe, it, expect } from 'vitest'
import {
  filterByStatus,
  filterByPhase,
  filterByMethod,
  applyFilters,
  countActiveFilters,
  hasActiveFilters,
  clearFilter,
  clearAllFilters,
  getActiveFilterLabels,
  buildPhaseNameMap,
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
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
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
    }

    const result = applyFilters(injects, filters)

    expect(result).toHaveLength(2)
  })
})

describe('countActiveFilters', () => {
  it('returns 0 when no filters active', () => {
    const filters: FilterState = {
      statuses: [],
      phaseIds: [],
      deliveryMethods: [],
    }

    expect(countActiveFilters(filters)).toBe(0)
  })

  it('counts each active filter category', () => {
    const filters: FilterState = {
      statuses: [InjectStatus.Pending, InjectStatus.Fired],
      phaseIds: ['phase-1'],
      deliveryMethods: [],
    }

    expect(countActiveFilters(filters)).toBe(2)
  })

  it('returns 3 when all filters active', () => {
    const filters: FilterState = {
      statuses: [InjectStatus.Pending],
      phaseIds: ['phase-1'],
      deliveryMethods: [DeliveryMethod.Email],
    }

    expect(countActiveFilters(filters)).toBe(3)
  })
})

describe('hasActiveFilters', () => {
  it('returns false when no filters active', () => {
    const filters: FilterState = {
      statuses: [],
      phaseIds: [],
      deliveryMethods: [],
    }

    expect(hasActiveFilters(filters)).toBe(false)
  })

  it('returns true when any filter is active', () => {
    expect(hasActiveFilters({
      statuses: [InjectStatus.Pending],
      phaseIds: [],
      deliveryMethods: [],
    })).toBe(true)

    expect(hasActiveFilters({
      statuses: [],
      phaseIds: ['phase-1'],
      deliveryMethods: [],
    })).toBe(true)

    expect(hasActiveFilters({
      statuses: [],
      phaseIds: [],
      deliveryMethods: [DeliveryMethod.Email],
    })).toBe(true)
  })
})

describe('clearFilter', () => {
  it('clears status filter', () => {
    const filters: FilterState = {
      statuses: [InjectStatus.Pending],
      phaseIds: ['phase-1'],
      deliveryMethods: [DeliveryMethod.Email],
    }

    const result = clearFilter(filters, 'status')

    expect(result.statuses).toEqual([])
    expect(result.phaseIds).toEqual(['phase-1'])
    expect(result.deliveryMethods).toEqual([DeliveryMethod.Email])
  })

  it('clears phase filter', () => {
    const filters: FilterState = {
      statuses: [InjectStatus.Pending],
      phaseIds: ['phase-1'],
      deliveryMethods: [],
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
    }

    const result = clearFilter(filters, 'method')

    expect(result.deliveryMethods).toEqual([])
  })
})

describe('clearAllFilters', () => {
  it('returns empty filter state', () => {
    const result = clearAllFilters()

    expect(result.statuses).toEqual([])
    expect(result.phaseIds).toEqual([])
    expect(result.deliveryMethods).toEqual([])
  })
})

describe('getActiveFilterLabels', () => {
  it('returns empty array when no filters active', () => {
    const filters: FilterState = {
      statuses: [],
      phaseIds: [],
      deliveryMethods: [],
    }

    const result = getActiveFilterLabels(filters, new Map())

    expect(result).toEqual([])
  })

  it('returns label for single status filter', () => {
    const filters: FilterState = {
      statuses: [InjectStatus.Pending],
      phaseIds: [],
      deliveryMethods: [],
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
    }

    const result = getActiveFilterLabels(filters, new Map())

    expect(result[0].value).toBe('2 selected')
  })

  it('uses phase map for phase names', () => {
    const filters: FilterState = {
      statuses: [],
      phaseIds: ['phase-1'],
      deliveryMethods: [],
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
    }

    const phaseMap = new Map<string | null, string>([
      [null, 'Unassigned'],
    ])

    const result = getActiveFilterLabels(filters, phaseMap)

    expect(result[0].value).toBe('Unassigned')
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
