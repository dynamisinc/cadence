/**
 * Excel Export Types
 * Matches backend DTOs from Cadence.Core.Features.ExcelExport.Models.DTOs
 */

/**
 * Request to export MSEL data to Excel
 */
export interface ExportMselRequest {
  exerciseId: string
  format?: 'xlsx' | 'csv'
  includeFormatting?: boolean
  includeObjectives?: boolean
  includePhases?: boolean
  includeConductData?: boolean
  filename?: string
}

/**
 * Export result metadata (for display purposes)
 */
export interface ExportResultInfo {
  filename: string
  injectCount: number
  phaseCount: number
  objectiveCount: number
}

/**
 * Request to export observations to Excel
 */
export interface ExportObservationsRequest {
  exerciseId: string
  includeFormatting?: boolean
  filename?: string
}

/**
 * Request to export full exercise package (ZIP)
 */
export interface ExportFullPackageRequest {
  exerciseId: string
  includeFormatting?: boolean
  filename?: string
}

/**
 * Export format options
 */
export const ExportFormat = {
  XLSX: 'xlsx',
  CSV: 'csv',
  ZIP: 'zip',
} as const

export type ExportFormatType = (typeof ExportFormat)[keyof typeof ExportFormat]

/**
 * Export type options for the dropdown
 */
export type ExportType = 'msel' | 'observations' | 'full'
