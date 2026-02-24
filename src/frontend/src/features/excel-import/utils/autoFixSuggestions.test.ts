import { describe, it, expect } from 'vitest'
import { computeAutoFixSuggestions } from './autoFixSuggestions'
import type { ValidationResult } from '../types'

const makeResult = (rows: ValidationResult['rows']): ValidationResult => ({
  sessionId: 'test-session',
  totalRows: rows.length,
  validRows: rows.filter(r => r.status === 'Valid').length,
  errorRows: rows.filter(r => r.status === 'Error').length,
  warningRows: rows.filter(r => r.status === 'Warning').length,
  rows,
  allRequiredMappingsConfigured: true,
})

describe('computeAutoFixSuggestions', () => {
  it('returns empty array when all rows are valid', () => {
    const result = makeResult([
      { rowNumber: 2, status: 'Valid', values: { Title: 'Test' }, issues: [] },
    ])
    expect(computeAutoFixSuggestions(result)).toEqual([])
  })

  it('suggests "Use Description as Title" when rows have missing Title but have Description', () => {
    const result = makeResult([
      {
        rowNumber: 2,
        status: 'Error',
        values: { Title: '', Description: 'A description' },
        issues: [{ field: 'Title', severity: 'Error', message: 'Title is required' }],
      },
      {
        rowNumber: 3,
        status: 'Error',
        values: { Title: '', Description: 'Another description' },
        issues: [{ field: 'Title', severity: 'Error', message: 'Title is required' }],
      },
    ])
    const suggestions = computeAutoFixSuggestions(result)
    expect(suggestions).toHaveLength(1)
    expect(suggestions[0].id).toBe('fix-missing-title-from-description')
    expect(suggestions[0].affectedRows).toBe(2)
    expect(suggestions[0].action).toBe('Use Description as Title')
    expect(suggestions[0].updates).toHaveLength(2)
    expect(suggestions[0].updates[0].value).toBe('A description')
  })

  it('does NOT suggest Title fix when Description is also empty', () => {
    const result = makeResult([
      {
        rowNumber: 2,
        status: 'Error',
        values: { Title: '', Description: '' },
        issues: [{ field: 'Title', severity: 'Error', message: 'Title is required' }],
      },
    ])
    const suggestions = computeAutoFixSuggestions(result)
    expect(suggestions).toEqual([])
  })

  it('suggests "Set to 00:00" when rows have unparseable time', () => {
    const result = makeResult([
      {
        rowNumber: 2,
        status: 'Error',
        values: { Title: 'Test', ScheduledTime: 'not-a-time' },
        issues: [
          { field: 'ScheduledTime', severity: 'Error', message: 'Cannot parse time value' },
        ],
      },
    ])
    const suggestions = computeAutoFixSuggestions(result)
    expect(suggestions).toHaveLength(1)
    expect(suggestions[0].id).toBe('fix-unparseable-time')
    expect(suggestions[0].action).toBe('Set to 00:00')
    expect(suggestions[0].updates[0].value).toBe('00:00')
  })

  it('truncates long descriptions to 200 chars with ellipsis', () => {
    const longDesc = 'A'.repeat(250)
    const result = makeResult([
      {
        rowNumber: 2,
        status: 'Error',
        values: { Title: '', Description: longDesc },
        issues: [{ field: 'Title', severity: 'Error', message: 'Title is required' }],
      },
    ])
    const suggestions = computeAutoFixSuggestions(result)
    expect(suggestions[0].updates[0].value).toHaveLength(200)
    expect(suggestions[0].updates[0].value).toMatch(/\.\.\.$/u)
  })

  it('handles mixed scenarios (Title errors and time errors)', () => {
    const result = makeResult([
      {
        rowNumber: 2,
        status: 'Error',
        values: { Title: '', Description: 'Desc', ScheduledTime: 'bad' },
        issues: [
          { field: 'Title', severity: 'Error', message: 'Title is required' },
          { field: 'ScheduledTime', severity: 'Error', message: 'Cannot parse time value' },
        ],
      },
    ])
    const suggestions = computeAutoFixSuggestions(result)
    expect(suggestions).toHaveLength(2)
    expect(suggestions.map(s => s.id)).toContain('fix-missing-title-from-description')
    expect(suggestions.map(s => s.id)).toContain('fix-unparseable-time')
  })
})
