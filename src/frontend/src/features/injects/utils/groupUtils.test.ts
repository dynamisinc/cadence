/**
 * Group Utilities Tests
 */

import { describe, it, expect } from 'vitest'
import {
  groupByStatus,
  groupByPhase,
  groupInjects,
  getInjectsForGroup,
  getGroupIdForInject,
  initExpandedGroups,
  toggleGroupExpansion,
  expandAllGroups,
  collapseAllGroups,
  isGroupExpanded,
  getGroupByOptions,
  getGroupsContainingInjects,
} from './groupUtils'
import type { InjectDto } from '../types'
import type { InjectGroup } from '../types/organization'
import { InjectStatus, InjectType } from '../../../types'

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
  objectiveIds: [],
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  ...overrides,
})

describe('groupByStatus', () => {
  it('groups injects by status in order: Pending → Fired → Skipped', () => {
    const injects = [
      createInject({ id: '1', status: InjectStatus.Skipped }),
      createInject({ id: '2', status: InjectStatus.Pending }),
      createInject({ id: '3', status: InjectStatus.Fired }),
      createInject({ id: '4', status: InjectStatus.Pending }),
    ]

    const result = groupByStatus(injects)

    expect(result).toHaveLength(3)
    expect(result[0].name).toBe('Pending')
    expect(result[0].injectIds).toEqual(['2', '4'])
    expect(result[1].name).toBe('Fired')
    expect(result[1].injectIds).toEqual(['3'])
    expect(result[2].name).toBe('Skipped')
    expect(result[2].injectIds).toEqual(['1'])
  })

  it('omits empty status groups', () => {
    const injects = [
      createInject({ id: '1', status: InjectStatus.Pending }),
      createInject({ id: '2', status: InjectStatus.Pending }),
    ]

    const result = groupByStatus(injects)

    expect(result).toHaveLength(1)
    expect(result[0].name).toBe('Pending')
  })

  it('returns empty array for empty input', () => {
    const result = groupByStatus([])

    expect(result).toEqual([])
  })

  it('includes correct count in each group', () => {
    const injects = [
      createInject({ id: '1', status: InjectStatus.Pending }),
      createInject({ id: '2', status: InjectStatus.Pending }),
      createInject({ id: '3', status: InjectStatus.Fired }),
    ]

    const result = groupByStatus(injects)

    expect(result[0].count).toBe(2)
    expect(result[1].count).toBe(1)
  })

  it('assigns unique group IDs', () => {
    const injects = [
      createInject({ id: '1', status: InjectStatus.Pending }),
      createInject({ id: '2', status: InjectStatus.Fired }),
    ]

    const result = groupByStatus(injects)

    expect(result[0].id).toBe('status-Pending')
    expect(result[1].id).toBe('status-Fired')
  })
})

describe('groupByPhase', () => {
  const phases = [
    { id: 'phase-1', name: 'Initial Response', sequence: 1 },
    { id: 'phase-2', name: 'Sustained Operations', sequence: 2 },
    { id: 'phase-3', name: 'Recovery', sequence: 3 },
  ]

  it('groups injects by phase in sequence order', () => {
    const injects = [
      createInject({ id: '1', phaseId: 'phase-2' }),
      createInject({ id: '2', phaseId: 'phase-1' }),
      createInject({ id: '3', phaseId: 'phase-1' }),
    ]

    const result = groupByPhase(injects, phases)

    expect(result).toHaveLength(2)
    expect(result[0].name).toBe('Initial Response')
    expect(result[0].injectIds).toEqual(['2', '3'])
    expect(result[1].name).toBe('Sustained Operations')
    expect(result[1].injectIds).toEqual(['1'])
  })

  it('puts unassigned (null phase) at the end', () => {
    const injects = [
      createInject({ id: '1', phaseId: null }),
      createInject({ id: '2', phaseId: 'phase-1' }),
    ]

    const result = groupByPhase(injects, phases)

    expect(result).toHaveLength(2)
    expect(result[0].name).toBe('Initial Response')
    expect(result[1].name).toBe('Unassigned')
    expect(result[1].id).toBe('phase-unassigned')
  })

  it('omits empty phase groups', () => {
    const injects = [
      createInject({ id: '1', phaseId: 'phase-1' }),
    ]

    const result = groupByPhase(injects, phases)

    expect(result).toHaveLength(1)
    expect(result[0].name).toBe('Initial Response')
  })

  it('returns empty array for empty input', () => {
    const result = groupByPhase([], phases)

    expect(result).toEqual([])
  })
})

describe('groupInjects', () => {
  const phases = [
    { id: 'phase-1', name: 'Phase 1', sequence: 1 },
  ]

  it('returns null for groupBy "none"', () => {
    const injects = [createInject()]

    const result = groupInjects(injects, 'none', phases)

    expect(result).toBeNull()
  })

  it('groups by status when groupBy is "status"', () => {
    const injects = [
      createInject({ id: '1', status: InjectStatus.Pending }),
      createInject({ id: '2', status: InjectStatus.Fired }),
    ]

    const result = groupInjects(injects, 'status', phases)

    expect(result).not.toBeNull()
    expect(result?.length).toBe(2)
    expect(result?.[0].name).toBe('Pending')
  })

  it('groups by phase when groupBy is "phase"', () => {
    const injects = [
      createInject({ id: '1', phaseId: 'phase-1' }),
    ]

    const result = groupInjects(injects, 'phase', phases)

    expect(result).not.toBeNull()
    expect(result?.[0].name).toBe('Phase 1')
  })
})

describe('getInjectsForGroup', () => {
  it('returns injects matching group IDs', () => {
    const injects = [
      createInject({ id: '1' }),
      createInject({ id: '2' }),
      createInject({ id: '3' }),
    ]

    const group: InjectGroup = {
      id: 'test-group',
      name: 'Test',
      count: 2,
      sortOrder: 0,
      injectIds: ['1', '3'],
    }

    const result = getInjectsForGroup(injects, group)

    expect(result).toHaveLength(2)
    expect(result.map(i => i.id)).toEqual(['1', '3'])
  })

  it('maintains order from injectIds', () => {
    const injects = [
      createInject({ id: '1' }),
      createInject({ id: '2' }),
      createInject({ id: '3' }),
    ]

    const group: InjectGroup = {
      id: 'test-group',
      name: 'Test',
      count: 3,
      sortOrder: 0,
      injectIds: ['3', '1', '2'], // Specific order
    }

    const result = getInjectsForGroup(injects, group)

    expect(result.map(i => i.id)).toEqual(['3', '1', '2'])
  })

  it('handles missing injects gracefully', () => {
    const injects = [
      createInject({ id: '1' }),
    ]

    const group: InjectGroup = {
      id: 'test-group',
      name: 'Test',
      count: 2,
      sortOrder: 0,
      injectIds: ['1', 'missing-id'],
    }

    const result = getInjectsForGroup(injects, group)

    expect(result).toHaveLength(1)
    expect(result[0].id).toBe('1')
  })
})

describe('getGroupIdForInject', () => {
  it('returns status-based ID for status grouping', () => {
    const inject = createInject({ status: InjectStatus.Pending })

    expect(getGroupIdForInject(inject, 'status')).toBe('status-Pending')
  })

  it('returns phase-based ID for phase grouping', () => {
    const inject = createInject({ phaseId: 'phase-1' })

    expect(getGroupIdForInject(inject, 'phase')).toBe('phase-phase-1')
  })

  it('returns unassigned ID for null phase', () => {
    const inject = createInject({ phaseId: null })

    expect(getGroupIdForInject(inject, 'phase')).toBe('phase-unassigned')
  })

  it('returns empty string for "none" grouping', () => {
    const inject = createInject()

    expect(getGroupIdForInject(inject, 'none')).toBe('')
  })
})

describe('Group Expansion Functions', () => {
  const groups: InjectGroup[] = [
    { id: 'group-1', name: 'Group 1', count: 5, sortOrder: 0, injectIds: [] },
    { id: 'group-2', name: 'Group 2', count: 3, sortOrder: 1, injectIds: [] },
    { id: 'group-3', name: 'Group 3', count: 2, sortOrder: 2, injectIds: [] },
  ]

  describe('initExpandedGroups', () => {
    it('returns set of all group IDs (all expanded)', () => {
      const result = initExpandedGroups(groups)

      expect(result.size).toBe(3)
      expect(result.has('group-1')).toBe(true)
      expect(result.has('group-2')).toBe(true)
      expect(result.has('group-3')).toBe(true)
    })
  })

  describe('toggleGroupExpansion', () => {
    it('removes group from set if present (collapse)', () => {
      const expanded = new Set(['group-1', 'group-2'])

      const result = toggleGroupExpansion(expanded, 'group-1')

      expect(result.has('group-1')).toBe(false)
      expect(result.has('group-2')).toBe(true)
    })

    it('adds group to set if not present (expand)', () => {
      const expanded = new Set(['group-1'])

      const result = toggleGroupExpansion(expanded, 'group-2')

      expect(result.has('group-1')).toBe(true)
      expect(result.has('group-2')).toBe(true)
    })

    it('returns new set (immutable)', () => {
      const expanded = new Set(['group-1'])

      const result = toggleGroupExpansion(expanded, 'group-1')

      expect(result).not.toBe(expanded)
    })
  })

  describe('expandAllGroups', () => {
    it('returns set with all group IDs', () => {
      const result = expandAllGroups(groups)

      expect(result.size).toBe(3)
      expect(result.has('group-1')).toBe(true)
      expect(result.has('group-2')).toBe(true)
      expect(result.has('group-3')).toBe(true)
    })
  })

  describe('collapseAllGroups', () => {
    it('returns empty set', () => {
      const result = collapseAllGroups()

      expect(result.size).toBe(0)
    })
  })

  describe('isGroupExpanded', () => {
    it('returns true if group is in set', () => {
      const expanded = new Set(['group-1', 'group-2'])

      expect(isGroupExpanded(expanded, 'group-1')).toBe(true)
      expect(isGroupExpanded(expanded, 'group-2')).toBe(true)
    })

    it('returns false if group is not in set', () => {
      const expanded = new Set(['group-1'])

      expect(isGroupExpanded(expanded, 'group-3')).toBe(false)
    })
  })
})

describe('getGroupByOptions', () => {
  it('returns all group by options', () => {
    const options = getGroupByOptions()

    expect(options).toHaveLength(3)
    expect(options).toContainEqual({ value: 'none', label: 'None' })
    expect(options).toContainEqual({ value: 'phase', label: 'Phase' })
    expect(options).toContainEqual({ value: 'status', label: 'Status' })
  })
})

describe('getGroupsContainingInjects', () => {
  const groups: InjectGroup[] = [
    { id: 'group-1', name: 'Group 1', count: 2, sortOrder: 0, injectIds: ['a', 'b'] },
    { id: 'group-2', name: 'Group 2', count: 2, sortOrder: 1, injectIds: ['c', 'd'] },
    { id: 'group-3', name: 'Group 3', count: 1, sortOrder: 2, injectIds: ['e'] },
  ]

  it('returns empty set when no injects match', () => {
    const injectIds = new Set(['x', 'y', 'z'])

    const result = getGroupsContainingInjects(groups, injectIds)

    expect(result.size).toBe(0)
  })

  it('returns group IDs containing matching injects', () => {
    const injectIds = new Set(['a', 'd'])

    const result = getGroupsContainingInjects(groups, injectIds)

    expect(result.size).toBe(2)
    expect(result.has('group-1')).toBe(true)
    expect(result.has('group-2')).toBe(true)
    expect(result.has('group-3')).toBe(false)
  })

  it('returns single group when only one matches', () => {
    const injectIds = new Set(['e'])

    const result = getGroupsContainingInjects(groups, injectIds)

    expect(result.size).toBe(1)
    expect(result.has('group-3')).toBe(true)
  })

  it('returns all groups when all contain matches', () => {
    const injectIds = new Set(['a', 'c', 'e'])

    const result = getGroupsContainingInjects(groups, injectIds)

    expect(result.size).toBe(3)
  })
})
