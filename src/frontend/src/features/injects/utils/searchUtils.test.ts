/**
 * Search Utilities Tests
 */

import { describe, it, expect } from 'vitest'
import {
  matchesSearch,
  filterBySearch,
  getMatchingFields,
  getSearchMatches,
  createSearchMatchMap,
  findMatchIndices,
  hasNonVisibleMatch,
  getMatchDescription,
  escapeRegex,
} from './searchUtils'
import type { InjectDto } from '../types'
import type { SearchMatch } from '../types/organization'
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
  target: 'EOC',
  source: 'SIMCELL',
  deliveryMethod: null,
  injectType: InjectType.Standard,
  status: InjectStatus.Pending,
  sequence: 1,
  parentInjectId: null,
  triggerCondition: null,
  expectedAction: 'Notify emergency manager',
  controllerNotes: 'Important note for controller',
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

describe('matchesSearch', () => {
  it('returns true for empty search term', () => {
    const inject = createInject()

    expect(matchesSearch(inject, '')).toBe(true)
    expect(matchesSearch(inject, '   ')).toBe(true)
  })

  it('matches in title', () => {
    const inject = createInject({ title: 'Hurricane Warning Issued' })

    expect(matchesSearch(inject, 'hurricane')).toBe(true)
    expect(matchesSearch(inject, 'HURRICANE')).toBe(true)
    expect(matchesSearch(inject, 'Warning')).toBe(true)
  })

  it('matches in description', () => {
    const inject = createInject({ description: 'A severe weather event is expected' })

    expect(matchesSearch(inject, 'severe')).toBe(true)
    expect(matchesSearch(inject, 'weather')).toBe(true)
  })

  it('matches in source (From field)', () => {
    const inject = createInject({ source: 'National Weather Service' })

    expect(matchesSearch(inject, 'national')).toBe(true)
    expect(matchesSearch(inject, 'NWS')).toBe(false) // No partial abbreviation match
  })

  it('matches in target (To field)', () => {
    const inject = createInject({ target: 'Emergency Operations Center' })

    expect(matchesSearch(inject, 'emergency')).toBe(true)
    expect(matchesSearch(inject, 'operations')).toBe(true)
  })

  it('matches in expectedAction', () => {
    const inject = createInject({ expectedAction: 'Activate the EOC' })

    expect(matchesSearch(inject, 'activate')).toBe(true)
    expect(matchesSearch(inject, 'EOC')).toBe(true)
  })

  it('matches in controllerNotes', () => {
    const inject = createInject({ controllerNotes: 'Watch for player reaction' })

    expect(matchesSearch(inject, 'player')).toBe(true)
    expect(matchesSearch(inject, 'reaction')).toBe(true)
  })

  it('matches inject number exactly', () => {
    const inject = createInject({ injectNumber: 42 })

    expect(matchesSearch(inject, '42')).toBe(true)
    expect(matchesSearch(inject, '4')).toBe(true) // Partial match
    expect(matchesSearch(inject, '99')).toBe(false)
  })

  it('returns false when no fields match', () => {
    const inject = createInject({
      title: 'Hurricane',
      description: 'Storm coming',
    })

    expect(matchesSearch(inject, 'earthquake')).toBe(false)
  })

  it('handles null fields gracefully', () => {
    const inject = createInject({
      title: 'Hurricane Alert',
      description: 'Storm warning',
      source: null,
      target: 'EOC',
      expectedAction: null,
      controllerNotes: null,
    })

    // Search term doesn't match any of the non-null fields
    expect(matchesSearch(inject, 'earthquake')).toBe(false)
    // Null fields should not cause errors - search term exists in non-null fields
    expect(matchesSearch(inject, 'hurricane')).toBe(true)
  })
})

describe('filterBySearch', () => {
  it('returns all injects for empty search', () => {
    const injects = [
      createInject({ id: '1' }),
      createInject({ id: '2' }),
    ]

    const result = filterBySearch(injects, '')

    expect(result).toHaveLength(2)
  })

  it('filters to matching injects', () => {
    const injects = [
      createInject({ id: '1', title: 'Hurricane Warning' }),
      createInject({ id: '2', title: 'Earthquake Alert' }),
      createInject({ id: '3', title: 'Hurricane Evacuation' }),
    ]

    const result = filterBySearch(injects, 'hurricane')

    expect(result).toHaveLength(2)
    expect(result.map(i => i.id)).toEqual(['1', '3'])
  })

  it('returns empty array when nothing matches', () => {
    const injects = [
      createInject({ id: '1', title: 'Hurricane' }),
      createInject({ id: '2', title: 'Tornado' }),
    ]

    const result = filterBySearch(injects, 'tsunami')

    expect(result).toHaveLength(0)
  })
})

describe('getMatchingFields', () => {
  it('returns empty array for empty search', () => {
    const inject = createInject()

    expect(getMatchingFields(inject, '')).toEqual([])
  })

  it('returns all matching fields', () => {
    const inject = createInject({
      title: 'Test Event',
      description: 'Test description',
      target: 'Test target',
    })

    const result = getMatchingFields(inject, 'test')

    expect(result).toContain('title')
    expect(result).toContain('description')
    expect(result).toContain('target')
  })

  it('returns only fields that match', () => {
    const inject = createInject({
      title: 'Hurricane Warning',
      description: 'Severe storm approaching',
    })

    const result = getMatchingFields(inject, 'hurricane')

    expect(result).toEqual(['title'])
  })
})

describe('getSearchMatches', () => {
  it('returns empty array for empty search', () => {
    const injects = [createInject()]

    expect(getSearchMatches(injects, '')).toEqual([])
  })

  it('returns matches with their fields', () => {
    const injects = [
      createInject({ id: '1', title: 'Hurricane', description: 'Other' }),
      createInject({ id: '2', title: 'Tornado', description: 'Hurricane effect' }),
    ]

    const result = getSearchMatches(injects, 'hurricane')

    expect(result).toHaveLength(2)
    expect(result[0]).toEqual({ injectId: '1', matchedFields: ['title'] })
    expect(result[1]).toEqual({ injectId: '2', matchedFields: ['description'] })
  })
})

describe('createSearchMatchMap', () => {
  it('creates map from matches', () => {
    const matches: SearchMatch[] = [
      { injectId: '1', matchedFields: ['title'] },
      { injectId: '2', matchedFields: ['description', 'target'] },
    ]

    const result = createSearchMatchMap(matches)

    expect(result.get('1')).toEqual(['title'])
    expect(result.get('2')).toEqual(['description', 'target'])
  })
})

describe('findMatchIndices', () => {
  it('returns empty array for empty search', () => {
    expect(findMatchIndices('Some text', '')).toEqual([])
  })

  it('returns empty array for empty text', () => {
    expect(findMatchIndices('', 'search')).toEqual([])
  })

  it('finds single match', () => {
    const result = findMatchIndices('Hello World', 'world')

    expect(result).toEqual([[6, 11]])
  })

  it('finds multiple matches', () => {
    const result = findMatchIndices('test one test two test', 'test')

    expect(result).toEqual([[0, 4], [9, 13], [18, 22]])
  })

  it('finds case-insensitive matches', () => {
    const result = findMatchIndices('TEST Test test', 'test')

    expect(result).toEqual([[0, 4], [5, 9], [10, 14]])
  })

  it('returns empty array when no match', () => {
    const result = findMatchIndices('Hello World', 'xyz')

    expect(result).toEqual([])
  })
})

describe('hasNonVisibleMatch', () => {
  it('returns false for title-only match', () => {
    expect(hasNonVisibleMatch(['title'])).toBe(false)
  })

  it('returns false for injectNumber-only match', () => {
    expect(hasNonVisibleMatch(['injectNumber'])).toBe(false)
  })

  it('returns true for description match', () => {
    expect(hasNonVisibleMatch(['description'])).toBe(true)
  })

  it('returns true for mixed visible and non-visible matches', () => {
    expect(hasNonVisibleMatch(['title', 'description'])).toBe(true)
  })

  it('returns true for source/target/notes matches', () => {
    expect(hasNonVisibleMatch(['source'])).toBe(true)
    expect(hasNonVisibleMatch(['target'])).toBe(true)
    expect(hasNonVisibleMatch(['expectedAction'])).toBe(true)
    expect(hasNonVisibleMatch(['controllerNotes'])).toBe(true)
  })
})

describe('getMatchDescription', () => {
  it('returns empty string for visible-only matches', () => {
    expect(getMatchDescription(['title'])).toBe('')
    expect(getMatchDescription(['injectNumber'])).toBe('')
    expect(getMatchDescription(['title', 'injectNumber'])).toBe('')
  })

  it('returns single field description', () => {
    expect(getMatchDescription(['description'])).toBe('Match in Description')
    expect(getMatchDescription(['source'])).toBe('Match in From')
    expect(getMatchDescription(['target'])).toBe('Match in To')
    expect(getMatchDescription(['expectedAction'])).toBe('Match in Expected Action')
    expect(getMatchDescription(['controllerNotes'])).toBe('Match in Notes')
  })

  it('returns count for multiple non-visible matches', () => {
    expect(getMatchDescription(['description', 'source'])).toBe('Match in 2 fields')
    expect(getMatchDescription(['description', 'source', 'target'])).toBe('Match in 3 fields')
  })

  it('only counts non-visible matches', () => {
    expect(getMatchDescription(['title', 'description'])).toBe('Match in Description')
    expect(getMatchDescription(['title', 'description', 'source'])).toBe('Match in 2 fields')
  })
})

describe('escapeRegex', () => {
  it('escapes special regex characters', () => {
    expect(escapeRegex('test.*')).toBe('test\\.\\*')
    expect(escapeRegex('(abc)')).toBe('\\(abc\\)')
    expect(escapeRegex('a+b?')).toBe('a\\+b\\?')
    expect(escapeRegex('[0-9]')).toBe('\\[0-9\\]')
  })

  it('leaves normal text unchanged', () => {
    expect(escapeRegex('normal text')).toBe('normal text')
    expect(escapeRegex('hurricane warning')).toBe('hurricane warning')
  })
})
