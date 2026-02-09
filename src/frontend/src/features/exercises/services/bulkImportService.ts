/**
 * Bulk Participant Import Service
 *
 * API client for bulk participant import operations.
 * Handles file upload, preview classification, import execution, and history.
 *
 * @module features/exercises/services
 */

import api from '@/core/services/api'
import type {
  FileParseResult,
  ImportPreviewResult,
  BulkImportResult,
  BulkImportRecordDto,
  BulkImportRowResultDto,
  PendingExerciseAssignmentDto,
} from '../types/bulkImport'

/**
 * Get the base URL for bulk import endpoints
 */
const getBaseUrl = (exerciseId: string): string =>
  `/exercises/${exerciseId}/participants/bulk-import`

/**
 * Bulk participant import service
 */
export const bulkImportService = {
  /**
   * Upload and parse a participant file (CSV or Excel)
   *
   * @param exerciseId - Exercise ID
   * @param file - CSV or Excel file to upload
   * @returns Parsed file result with validation errors
   */
  async uploadFile(exerciseId: string, file: File): Promise<FileParseResult> {
    const formData = new FormData()
    formData.append('file', file)

    const response = await api.post<FileParseResult>(
      `${getBaseUrl(exerciseId)}/upload`,
      formData,
      {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      },
    )
    return response.data
  },

  /**
   * Get preview with classified rows
   *
   * Analyzes parsed rows and classifies each as:
   * - Assign: Existing user, not in exercise yet
   * - Update: Existing participant, role change
   * - Invite: User doesn't exist, send invitation
   * - Error: Invalid data or business rule violation
   *
   * @param exerciseId - Exercise ID
   * @param sessionId - Session ID from upload
   * @returns Import preview with classification
   */
  async getPreview(exerciseId: string, sessionId: string): Promise<ImportPreviewResult> {
    const response = await api.get<ImportPreviewResult>(
      `${getBaseUrl(exerciseId)}/${sessionId}/preview`,
    )
    return response.data
  },

  /**
   * Confirm and execute the import
   *
   * Processes all valid rows from the preview:
   * - Assigns existing users to exercise
   * - Updates existing participant roles
   * - Sends invitations to new users
   * - Skips error rows
   *
   * @param exerciseId - Exercise ID
   * @param sessionId - Session ID from upload
   * @returns Import execution result
   */
  async confirmImport(exerciseId: string, sessionId: string): Promise<BulkImportResult> {
    const response = await api.post<BulkImportResult>(
      `${getBaseUrl(exerciseId)}/${sessionId}/confirm`,
    )
    return response.data
  },

  /**
   * Get import history for an exercise
   *
   * @param exerciseId - Exercise ID
   * @returns List of past import records
   */
  async getHistory(exerciseId: string): Promise<BulkImportRecordDto[]> {
    const response = await api.get<BulkImportRecordDto[]>(`${getBaseUrl(exerciseId)}/history`)
    return response.data
  },

  /**
   * Get detailed row results for a specific import
   *
   * @param exerciseId - Exercise ID
   * @param recordId - Import record ID
   * @returns Detailed results for each row
   */
  async getRowResults(
    exerciseId: string,
    recordId: string,
  ): Promise<BulkImportRowResultDto[]> {
    const response = await api.get<BulkImportRowResultDto[]>(
      `${getBaseUrl(exerciseId)}/${recordId}/rows`,
    )
    return response.data
  },

  /**
   * Get pending exercise assignments (invited users who haven't registered)
   *
   * @param exerciseId - Exercise ID
   * @returns List of pending assignments
   */
  async getPendingAssignments(exerciseId: string): Promise<PendingExerciseAssignmentDto[]> {
    const response = await api.get<PendingExerciseAssignmentDto[]>(
      `${getBaseUrl(exerciseId)}/pending`,
    )
    return response.data
  },

  /**
   * Get URL for downloading the participant template
   *
   * @param exerciseId - Exercise ID
   * @param format - Template format (csv or xlsx)
   * @returns Download URL
   */
  getTemplateUrl(exerciseId: string, format: 'csv' | 'xlsx' = 'csv'): string {
    const baseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5071'
    return `${baseUrl}/api${getBaseUrl(exerciseId)}/template?format=${format}`
  },
}

export default bulkImportService
