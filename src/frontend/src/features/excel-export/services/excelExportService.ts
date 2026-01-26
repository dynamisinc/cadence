import api from '@/core/services/api'
import type {
  ExportMselRequest,
  ExportObservationsRequest,
  ExportFullPackageRequest,
  ExportResultInfo,
} from '../types'

/**
 * Extract filename from Content-Disposition header.
 * Handles various formats:
 * - attachment; filename="file.xlsx"
 * - attachment; filename=file.xlsx
 * - attachment; filename*=UTF-8''file.xlsx
 * Returns null if filename cannot be extracted.
 */
function extractFilename(contentDisposition: string | undefined): string | null {
  if (!contentDisposition) {
    return null
  }

  // Try quoted filename first: filename="something.xlsx"
  // The regex [^"]+ requires at least one character, so empty quotes won't match
  const quotedMatch = contentDisposition.match(/filename="([^"]+)"/)
  if (quotedMatch?.[1]) {
    return quotedMatch[1]
  }

  // Check for empty quotes case - return null
  if (/filename=""/.test(contentDisposition)) {
    return null
  }

  // Try unquoted filename: filename=something.xlsx
  // Be careful to stop at semicolon or end of string
  const unquotedMatch = contentDisposition.match(/filename=([^;\s]+)/)
  if (unquotedMatch?.[1]) {
    return unquotedMatch[1]
  }

  // Try RFC 5987 encoded filename: filename*=UTF-8''something.xlsx
  const encodedMatch = contentDisposition.match(/filename\*=(?:UTF-8''|utf-8'')([^;\s]+)/i)
  if (encodedMatch?.[1]) {
    try {
      return decodeURIComponent(encodedMatch[1])
    } catch {
      return encodedMatch[1]
    }
  }

  return null
}

/**
 * Service for Excel export operations
 */
export const excelExportService = {
  /**
   * Export MSEL to Excel or CSV format
   * Returns a blob that can be downloaded
   */
  async exportMsel(request: ExportMselRequest): Promise<{ blob: Blob; info: ExportResultInfo }> {
    const response = await api.post('/export/msel', request, {
      responseType: 'blob',
    })

    // Extract metadata from response headers
    const filename =
      extractFilename(response.headers['content-disposition']) ??
      `MSEL_Export.${request.format ?? 'xlsx'}`
    const injectCount = parseInt(response.headers['x-inject-count'] ?? '0', 10)
    const phaseCount = parseInt(response.headers['x-phase-count'] ?? '0', 10)
    const objectiveCount = parseInt(response.headers['x-objective-count'] ?? '0', 10)

    return {
      blob: response.data,
      info: {
        filename,
        injectCount,
        phaseCount,
        objectiveCount,
      },
    }
  },

  /**
   * Download an MSEL template file
   */
  async downloadTemplate(): Promise<{ blob: Blob; filename: string }> {
    const response = await api.get('/export/template', {
      responseType: 'blob',
    })

    const filename =
      extractFilename(response.headers['content-disposition']) ?? 'Cadence_MSEL_Template.xlsx'

    return {
      blob: response.data,
      filename,
    }
  },

  /**
   * Export observations to Excel format
   * Returns a blob that can be downloaded
   */
  async exportObservations(
    request: ExportObservationsRequest
  ): Promise<{ blob: Blob; info: ExportResultInfo }> {
    const params = new URLSearchParams()
    if (request.includeFormatting !== undefined) {
      params.append('includeFormatting', String(request.includeFormatting))
    }
    if (request.filename) {
      params.append('filename', request.filename)
    }

    const url = `/export/exercises/${request.exerciseId}/observations${params.toString() ? `?${params.toString()}` : ''}`
    const response = await api.get(url, {
      responseType: 'blob',
    })

    const filename =
      extractFilename(response.headers['content-disposition']) ?? 'Observations_Export.xlsx'
    const observationCount = parseInt(response.headers['x-observation-count'] ?? '0', 10)

    return {
      blob: response.data,
      info: {
        filename,
        injectCount: 0,
        phaseCount: 0,
        objectiveCount: observationCount,
      },
    }
  },

  /**
   * Export full exercise package as a ZIP file
   * Returns a blob that can be downloaded
   */
  async exportFullPackage(
    request: ExportFullPackageRequest
  ): Promise<{ blob: Blob; info: ExportResultInfo }> {
    const params = new URLSearchParams()
    if (request.includeFormatting !== undefined) {
      params.append('includeFormatting', String(request.includeFormatting))
    }
    if (request.filename) {
      params.append('filename', request.filename)
    }

    const url = `/export/exercises/${request.exerciseId}/full${params.toString() ? `?${params.toString()}` : ''}`
    const response = await api.get(url, {
      responseType: 'blob',
    })

    const filename =
      extractFilename(response.headers['content-disposition']) ?? 'Exercise_Package.zip'
    const injectCount = parseInt(response.headers['x-inject-count'] ?? '0', 10)
    const phaseCount = parseInt(response.headers['x-phase-count'] ?? '0', 10)
    const objectiveCount = parseInt(response.headers['x-objective-count'] ?? '0', 10)

    return {
      blob: response.data,
      info: {
        filename,
        injectCount,
        phaseCount,
        objectiveCount,
      },
    }
  },
}

// Export for testing
export { extractFilename }

/**
 * Utility to trigger a file download from a blob
 */
export function downloadBlob(blob: Blob, filename: string): void {
  const url = window.URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  window.URL.revokeObjectURL(url)
}
