/**
 * Excel Import Types
 *
 * TypeScript interfaces for the Excel import wizard.
 */

/**
 * Information about a worksheet in an Excel file.
 */
export interface WorksheetInfo {
  index: number
  name: string
  rowCount: number
  columnCount: number
  looksLikeMsel: boolean
  mselConfidence: number
}

/**
 * Result of analyzing an uploaded Excel file.
 */
export interface FileAnalysisResult {
  sessionId: string
  fileName: string
  fileSize: number
  fileFormat: string
  worksheets: WorksheetInfo[]
  isPasswordProtected: boolean
  warnings?: string[]
}

/**
 * Information about a column in the worksheet.
 */
export interface ColumnInfo {
  index: number
  letter: string
  header: string
  dataType: string
  sampleValues: (string | null)[]
  fillRate: number
}

/**
 * Result of selecting a worksheet for import.
 */
export interface WorksheetSelectionResult {
  sessionId: string
  worksheet: WorksheetInfo
  columns: ColumnInfo[]
  previewRows: Record<string, unknown>[]
  previewRowCount: number
}

/**
 * Request to select a worksheet for import.
 */
export interface SelectWorksheetRequest {
  sessionId: string
  worksheetIndex: number
  previewRowCount?: number
  dataStartRow?: number
  headerRow?: number
}

/**
 * Column mapping configuration for import.
 */
export interface ColumnMapping {
  cadenceField: string
  sourceColumnIndex: number | null
  isRequired: boolean
  displayName: string
  description?: string
  suggestedColumnIndex?: number | null
  suggestedMappingConfidence: number
}

/**
 * Request to configure column mappings.
 */
export interface ConfigureMappingsRequest {
  sessionId: string
  mappings: ColumnMapping[]
  timeFormat?: string
  dateFormat?: string
}

/**
 * A validation issue for a specific field.
 */
export interface ValidationIssue {
  field: string
  severity: 'Error' | 'Warning'
  message: string
  originalValue?: string
}

/**
 * Validation result for a single row.
 */
export interface RowValidationResult {
  rowNumber: number
  status: 'Valid' | 'Warning' | 'Error'
  values: Record<string, unknown>
  issues?: ValidationIssue[]
}

/**
 * Result of validating import data.
 */
export interface ValidationResult {
  sessionId: string
  totalRows: number
  validRows: number
  errorRows: number
  warningRows: number
  rows: RowValidationResult[]
  allRequiredMappingsConfigured: boolean
  missingRequiredMappings?: string[]
}

/**
 * Import strategy options.
 */
export const ImportStrategy = {
  Append: 'Append',
  Replace: 'Replace',
  Merge: 'Merge',
} as const

export type ImportStrategyType = (typeof ImportStrategy)[keyof typeof ImportStrategy]

/**
 * Request to execute the import.
 */
export interface ExecuteImportRequest {
  sessionId: string
  exerciseId: string
  strategy: ImportStrategyType
  skipErrorRows?: boolean
  createMissingPhases?: boolean
  createMissingObjectives?: boolean
}

/**
 * Result of executing an import.
 */
export interface ImportResult {
  success: boolean
  injectsCreated: number
  injectsUpdated: number
  rowsSkipped: number
  phasesCreated: number
  objectivesCreated: number
  errors?: string[]
  warnings?: string[]
  mselId?: string
}

/**
 * State of an import session.
 */
export interface ImportSessionState {
  sessionId: string
  fileName: string
  currentStep: string
  selectedWorksheetIndex?: number
  mappings?: ColumnMapping[]
  createdAt: string
  expiresAt: string
}

/**
 * Import wizard step names.
 */
export const ImportWizardStep = {
  Upload: 'Upload',
  SheetSelection: 'SheetSelection',
  Mapping: 'Mapping',
  Validation: 'Validation',
  Import: 'Import',
  Complete: 'Complete',
} as const

export type ImportWizardStepType = (typeof ImportWizardStep)[keyof typeof ImportWizardStep]

/**
 * A single row value update for auto-fix or inline editing.
 */
export interface RowUpdate {
  rowNumber: number
  field: string
  value: string | null
}

/**
 * Request to update row values in a validation session.
 */
export interface UpdateRowsRequest {
  sessionId: string
  updates: RowUpdate[]
}

/**
 * Response from updating rows - returns only changed rows + updated counts.
 */
export interface UpdateRowsResult {
  sessionId: string
  totalRows: number
  validRows: number
  errorRows: number
  warningRows: number
  updatedRows: RowValidationResult[]
}

/**
 * An auto-fix suggestion computed from validation results.
 */
export interface AutoFixSuggestion {
  /** Unique key for this suggestion type */
  id: string
  /** Human-readable description, e.g. "12 rows missing Title" */
  description: string
  /** What the fix does, e.g. "Use Description as Title" */
  action: string
  /** Number of affected rows */
  affectedRows: number
  /** The row updates to apply if the user accepts */
  updates: RowUpdate[]
  /** Severity of the issues this fixes */
  severity: 'Error' | 'Warning'
}
