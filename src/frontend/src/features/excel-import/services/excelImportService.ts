/**
 * Excel Import Service
 *
 * API client for Excel import operations.
 */

import api from '@/core/services/api'
import type {
  FileAnalysisResult,
  WorksheetSelectionResult,
  SelectWorksheetRequest,
  ColumnMapping,
  ConfigureMappingsRequest,
  ValidationResult,
  ExecuteImportRequest,
  ImportResult,
  ImportSessionState,
} from '../types'

const BASE_URL = '/import'

/**
 * Upload and analyze an Excel file
 */
export const uploadFile = async (file: File): Promise<FileAnalysisResult> => {
  const formData = new FormData()
  formData.append('file', file)

  const response = await api.post<FileAnalysisResult>(`${BASE_URL}/upload`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  })
  return response.data
}

/**
 * Get the current state of an import session
 */
export const getSessionState = async (sessionId: string): Promise<ImportSessionState> => {
  const response = await api.get<ImportSessionState>(`${BASE_URL}/sessions/${sessionId}`)
  return response.data
}

/**
 * Select a worksheet for import
 */
export const selectWorksheet = async (
  request: SelectWorksheetRequest,
): Promise<WorksheetSelectionResult> => {
  const response = await api.post<WorksheetSelectionResult>(`${BASE_URL}/select-worksheet`, request)
  return response.data
}

/**
 * Get suggested column mappings
 */
export const getSuggestedMappings = async (sessionId: string): Promise<ColumnMapping[]> => {
  const response = await api.get<ColumnMapping[]>(`${BASE_URL}/sessions/${sessionId}/mappings`)
  return response.data
}

/**
 * Validate import data with configured mappings
 */
export const validateImport = async (
  request: ConfigureMappingsRequest,
): Promise<ValidationResult> => {
  const response = await api.post<ValidationResult>(`${BASE_URL}/validate`, request)
  return response.data
}

/**
 * Execute the import
 */
export const executeImport = async (request: ExecuteImportRequest): Promise<ImportResult> => {
  const response = await api.post<ImportResult>(`${BASE_URL}/execute`, request)
  return response.data
}

/**
 * Cancel an import session
 */
export const cancelImport = async (sessionId: string): Promise<void> => {
  await api.delete(`${BASE_URL}/sessions/${sessionId}`)
}

export const excelImportService = {
  uploadFile,
  getSessionState,
  selectWorksheet,
  getSuggestedMappings,
  validateImport,
  executeImport,
  cancelImport,
}

export default excelImportService
