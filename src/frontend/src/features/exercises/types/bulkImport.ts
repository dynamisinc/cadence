/**
 * Bulk Participant Import Types
 *
 * TypeScript types for bulk participant import operations.
 * Matches backend DTOs in Cadence.Core.Features.Exercises.BulkImport
 *
 * @module features/exercises/types
 */

// =========================================================================
// Classification and Status Enums
// =========================================================================

/**
 * Participant classification during import preview
 */
export type ParticipantClassification = 'Assign' | 'Update' | 'Invite' | 'Error'

/**
 * Status of a pending exercise assignment (for invited users)
 */
export type PendingAssignmentStatus = 'Pending' | 'Activated' | 'Expired' | 'Cancelled'

/**
 * Status of an import row after execution
 */
export type BulkImportRowStatus = 'Success' | 'Skipped' | 'Failed'

// =========================================================================
// Upload and Parse Types
// =========================================================================

/**
 * Column mapping from file parsing
 */
export interface ColumnMapping {
  /** Original header text from the file */
  originalHeader: string
  /** Mapped field name (Email, ExerciseRole, DisplayName, OrganizationRole) */
  mappedField: string
  /** Zero-based column index */
  columnIndex: number
}

/**
 * Parsed participant row from uploaded file
 */
export interface ParsedParticipantRow {
  /** Row number in the source file (1-based) */
  rowNumber: number
  /** Email address */
  email: string
  /** Exercise role as entered in file */
  exerciseRole: string
  /** Normalized HSEEP role name */
  normalizedExerciseRole?: string
  /** Display name (optional) */
  displayName?: string
  /** Organization role as entered in file (optional) */
  organizationRole?: string
  /** Normalized organization role */
  normalizedOrgRole?: string
  /** Validation errors for this row */
  validationErrors: string[]
  /** Whether the row is valid */
  isValid: boolean
}

/**
 * Result of file upload and parsing
 */
export interface FileParseResult {
  /** Session ID for subsequent API calls */
  sessionId: string
  /** Original filename */
  fileName: string
  /** Total rows parsed */
  totalRows: number
  /** Column mappings detected */
  columnMappings: ColumnMapping[]
  /** Parsed rows */
  rows: ParsedParticipantRow[]
  /** Warning messages */
  warnings: string[]
  /** Error messages (file-level) */
  errors: string[]
  /** Whether the file is valid for import */
  isValid: boolean
}

// =========================================================================
// Preview and Classification Types
// =========================================================================

/**
 * Classified participant row after backend analysis
 */
export interface ClassifiedParticipantRow {
  /** Original parsed row */
  parsedRow: ParsedParticipantRow
  /** Classification result */
  classification: ParticipantClassification
  /** Human-readable classification label */
  classificationLabel: string
  /** Existing user ID (if found) */
  existingUserId?: string
  /** Existing user's display name */
  existingDisplayName?: string
  /** Current exercise role (if already a participant) */
  currentExerciseRole?: string
  /** Whether this is a role change */
  isRoleChange: boolean
  /** Whether user has a pending invitation */
  hasPendingInvitation: boolean
  /** Whether this would create a new account */
  isNewAccount: boolean
  /** Additional notes about classification */
  notes: string[]
  /** Error message (if classification is Error) */
  errorMessage?: string
}

/**
 * Import preview result with classified rows
 */
export interface ImportPreviewResult {
  /** Session ID */
  sessionId: string
  /** Total rows in preview */
  totalRows: number
  /** Count of rows to assign existing users */
  assignCount: number
  /** Count of rows to update existing participants */
  updateCount: number
  /** Count of rows to invite new users */
  inviteCount: number
  /** Count of error rows */
  errorCount: number
  /** Classified rows */
  rows: ClassifiedParticipantRow[]
  /** Whether there are any processable rows */
  hasProcessableRows: boolean
}

// =========================================================================
// Execution and Result Types
// =========================================================================

/**
 * Outcome for a single row after import execution
 */
export interface ImportRowOutcome {
  /** Row number from source file */
  rowNumber: number
  /** Email address */
  email: string
  /** Exercise role */
  exerciseRole: string
  /** Original classification */
  classification: ParticipantClassification
  /** Execution status */
  status: BulkImportRowStatus
  /** Result message (error or success details) */
  message?: string
}

/**
 * Result of bulk import execution
 */
export interface BulkImportResult {
  /** Import record ID for history tracking */
  importRecordId: string
  /** Count of users assigned to exercise */
  assignedCount: number
  /** Count of participant roles updated */
  updatedCount: number
  /** Count of invitations sent */
  invitedCount: number
  /** Count of rows that failed */
  errorCount: number
  /** Count of rows skipped */
  skippedCount: number
  /** Detailed outcomes per row */
  rowOutcomes: ImportRowOutcome[]
}

// =========================================================================
// History Types
// =========================================================================

/**
 * Bulk import history record
 */
export interface BulkImportRecordDto {
  /** Import record ID */
  id: string
  /** Exercise ID */
  exerciseId: string
  /** User who performed the import */
  importedById: string
  /** Display name of importer */
  importedByName: string
  /** Import timestamp */
  importedAt: string
  /** Original filename */
  fileName: string
  /** Total rows processed */
  totalRows: number
  /** Users assigned */
  assignedCount: number
  /** Roles updated */
  updatedCount: number
  /** Invitations sent */
  invitedCount: number
  /** Errors */
  errorCount: number
  /** Skipped rows */
  skippedCount: number
}

/**
 * Individual row result from import history
 */
export interface BulkImportRowResultDto {
  /** Row result ID */
  id: string
  /** Row number from source file */
  rowNumber: number
  /** Email address */
  email: string
  /** Exercise role (if successful) */
  exerciseRole?: string
  /** Display name (if provided) */
  displayName?: string
  /** Classification type */
  classification: ParticipantClassification
  /** Execution status */
  status: BulkImportRowStatus
  /** Error message (if failed) */
  errorMessage?: string
  /** Previous role (if updated) */
  previousExerciseRole?: string
}

// =========================================================================
// Pending Assignment Types
// =========================================================================

/**
 * Pending exercise assignment (for invited users who haven't registered yet)
 */
export interface PendingExerciseAssignmentDto {
  /** Pending assignment ID */
  id: string
  /** Related organization invite ID */
  organizationInviteId: string
  /** Email address */
  email: string
  /** Assigned exercise role */
  exerciseRole: string
  /** Display name (if provided) */
  displayName?: string
  /** Status of pending assignment */
  status: PendingAssignmentStatus
  /** Status of related invitation */
  invitationStatus: string
  /** When invitation expires */
  invitationExpiresAt?: string
  /** When assignment was created */
  createdAt: string
}

// =========================================================================
// Flow State Type (for useParticipantImport hook)
// =========================================================================

/**
 * Current step in the import flow
 */
export type ImportFlowStep = 'idle' | 'uploading' | 'preview' | 'confirming' | 'results'
