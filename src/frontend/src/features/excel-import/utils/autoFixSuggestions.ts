/**
 * Auto-Fix Suggestions
 *
 * Computes bulk-fix suggestions from validation results.
 * Pure function - no side effects, highly testable.
 */

import type { ValidationResult, AutoFixSuggestion } from '../types'

/**
 * Computes auto-fix suggestions from validation results.
 * Groups issues by pattern and generates bulk-fix proposals.
 */
export function computeAutoFixSuggestions(result: ValidationResult): AutoFixSuggestion[] {
  const suggestions: AutoFixSuggestion[] = []

  // Pattern 1: Missing Title, has Description -> "Use Description as Title"
  const missingTitleRows = result.rows.filter(
    row =>
      row.issues?.some(
        i => i.field === 'Title' && i.severity === 'Error' && i.message.includes('required'),
      ) &&
      row.values['Description'] &&
      String(row.values['Description']).trim() !== '',
  )
  if (missingTitleRows.length > 0) {
    suggestions.push({
      id: 'fix-missing-title-from-description',
      description: `${missingTitleRows.length} row${missingTitleRows.length !== 1 ? 's' : ''} missing Title`,
      action: 'Use Description as Title',
      affectedRows: missingTitleRows.length,
      severity: 'Error',
      updates: missingTitleRows.map(row => ({
        rowNumber: row.rowNumber,
        field: 'Title',
        value: truncateForTitle(String(row.values['Description'])),
      })),
    })
  }

  // Pattern 2: Unparseable ScheduledTime -> "Set to 00:00"
  const badTimeRows = result.rows.filter(row =>
    row.issues?.some(
      i =>
        i.field === 'ScheduledTime' &&
        i.severity === 'Error' &&
        i.message.includes('Cannot parse'),
    ),
  )
  if (badTimeRows.length > 0) {
    suggestions.push({
      id: 'fix-unparseable-time',
      description: `${badTimeRows.length} row${badTimeRows.length !== 1 ? 's' : ''} with unparseable time`,
      action: 'Set to 00:00',
      affectedRows: badTimeRows.length,
      severity: 'Error',
      updates: badTimeRows.map(row => ({
        rowNumber: row.rowNumber,
        field: 'ScheduledTime',
        value: '00:00',
      })),
    })
  }

  return suggestions
}

function truncateForTitle(description: string): string {
  if (description.length <= 200) return description
  return description.substring(0, 197) + '...'
}
