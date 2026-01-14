/**
 * Search Utilities for Inject Organization
 *
 * Pure functions for searching injects by text content.
 * Supports searching across multiple fields with case-insensitive matching.
 */

import type { InjectDto } from '../types'
import type { SearchableField, SearchMatch } from '../types/organization'
import { SEARCHABLE_FIELDS } from '../types/organization'

/**
 * Normalize a string for search comparison
 * @param str String to normalize
 * @returns Lowercase trimmed string, or empty string if null/undefined
 */
function normalizeForSearch(str: string | null | undefined): string {
  if (!str) return ''
  return str.toLowerCase().trim()
}

/**
 * Check if a search term matches a field value
 * @param fieldValue Value to search in
 * @param searchTerm Term to search for
 * @returns True if field contains search term
 */
function matchesField(
  fieldValue: string | number | null | undefined,
  searchTerm: string,
): boolean {
  const normalizedValue = normalizeForSearch(String(fieldValue ?? ''))
  const normalizedTerm = normalizeForSearch(searchTerm)

  if (!normalizedTerm) return true
  if (!normalizedValue) return false

  return normalizedValue.includes(normalizedTerm)
}

/**
 * Get the value of a searchable field from an inject
 * @param inject Inject to get field from
 * @param field Field name
 * @returns Field value as string
 */
function getFieldValue(inject: InjectDto, field: SearchableField): string {
  switch (field) {
    case 'title':
      return inject.title
    case 'description':
      return inject.description
    case 'source':
      return inject.source ?? ''
    case 'target':
      return inject.target
    case 'expectedAction':
      return inject.expectedAction ?? ''
    case 'controllerNotes':
      return inject.controllerNotes ?? ''
    case 'injectNumber':
      return inject.injectNumber.toString()
    default:
      return ''
  }
}

/**
 * Check which fields of an inject match the search term
 * @param inject Inject to search
 * @param searchTerm Search term
 * @returns Array of matching field names
 */
export function getMatchingFields(
  inject: InjectDto,
  searchTerm: string,
): SearchableField[] {
  const normalizedTerm = normalizeForSearch(searchTerm)
  if (!normalizedTerm) return []

  return SEARCHABLE_FIELDS.filter(field =>
    matchesField(getFieldValue(inject, field), normalizedTerm),
  )
}

/**
 * Check if an inject matches the search term in any searchable field
 * @param inject Inject to search
 * @param searchTerm Search term
 * @returns True if any field matches
 */
export function matchesSearch(inject: InjectDto, searchTerm: string): boolean {
  const normalizedTerm = normalizeForSearch(searchTerm)
  if (!normalizedTerm) return true

  return SEARCHABLE_FIELDS.some(field =>
    matchesField(getFieldValue(inject, field), normalizedTerm),
  )
}

/**
 * Filter injects by search term
 * @param injects Array of injects to filter
 * @param searchTerm Search term
 * @returns Filtered injects that match the search
 */
export function filterBySearch(
  injects: InjectDto[],
  searchTerm: string,
): InjectDto[] {
  const normalizedTerm = normalizeForSearch(searchTerm)
  if (!normalizedTerm) return injects

  return injects.filter(inject => matchesSearch(inject, normalizedTerm))
}

/**
 * Get search matches with their matching fields
 * @param injects Array of injects to search
 * @param searchTerm Search term
 * @returns Array of matches with inject IDs and matched fields
 */
export function getSearchMatches(
  injects: InjectDto[],
  searchTerm: string,
): SearchMatch[] {
  const normalizedTerm = normalizeForSearch(searchTerm)
  if (!normalizedTerm) return []

  return injects
    .map(inject => ({
      injectId: inject.id,
      matchedFields: getMatchingFields(inject, normalizedTerm),
    }))
    .filter(match => match.matchedFields.length > 0)
}

/**
 * Create a map of inject IDs to their search match info
 * @param matches Array of search matches
 * @returns Map of inject ID to matched fields
 */
export function createSearchMatchMap(
  matches: SearchMatch[],
): Map<string, SearchableField[]> {
  return new Map(matches.map(m => [m.injectId, m.matchedFields]))
}

/**
 * Escape special regex characters in a string
 * @param str String to escape
 * @returns Escaped string safe for regex
 */
export function escapeRegex(str: string): string {
  return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

/**
 * Find all match indices in a text for highlighting
 * @param text Text to search in
 * @param searchTerm Term to find
 * @returns Array of [start, end] index pairs
 */
export function findMatchIndices(
  text: string,
  searchTerm: string,
): Array<[number, number]> {
  const normalizedTerm = normalizeForSearch(searchTerm)
  if (!normalizedTerm || !text) return []

  const indices: Array<[number, number]> = []
  const lowerText = text.toLowerCase()
  let position = 0

  while (true) {
    const index = lowerText.indexOf(normalizedTerm, position)
    if (index === -1) break
    indices.push([index, index + normalizedTerm.length])
    position = index + 1
  }

  return indices
}

/**
 * Check if a match was found in a non-visible field
 * (i.e., not in title, which is typically shown in the list)
 * @param matchedFields Array of matched field names
 * @returns True if match is only in non-visible fields
 */
export function hasNonVisibleMatch(matchedFields: SearchableField[]): boolean {
  const visibleFields: SearchableField[] = ['title', 'injectNumber']
  return matchedFields.some(field => !visibleFields.includes(field))
}

/**
 * Get a description of where matches were found
 * @param matchedFields Array of matched field names
 * @returns Human-readable description
 */
export function getMatchDescription(matchedFields: SearchableField[]): string {
  const visibleFields: SearchableField[] = ['title', 'injectNumber']
  const nonVisibleMatches = matchedFields.filter(f => !visibleFields.includes(f))

  if (nonVisibleMatches.length === 0) {
    return ''
  }

  const fieldLabels: Record<SearchableField, string> = {
    title: 'Title',
    description: 'Description',
    source: 'From',
    target: 'To',
    expectedAction: 'Expected Action',
    controllerNotes: 'Notes',
    injectNumber: '#',
  }

  if (nonVisibleMatches.length === 1) {
    return `Match in ${fieldLabels[nonVisibleMatches[0]]}`
  }

  return `Match in ${nonVisibleMatches.length} fields`
}
