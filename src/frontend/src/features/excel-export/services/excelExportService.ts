import api from '@/core/services/api'
import type { ExportMselRequest, ExportResultInfo } from '../types'

/**
 * Service for Excel export operations
 */
export const excelExportService = {
  /**
   * Export MSEL to Excel or CSV format
   * Returns a blob that can be downloaded
   */
  async exportMsel(request: ExportMselRequest): Promise<{ blob: Blob; info: ExportResultInfo }> {
    const response = await api.post('/api/export/msel', request, {
      responseType: 'blob',
    })

    // Extract metadata from response headers
    const filename =
      response.headers['content-disposition']?.match(/filename="?(.+?)"?$/)?.[1] ??
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
    const response = await api.get('/api/export/template', {
      responseType: 'blob',
    })

    const filename =
      response.headers['content-disposition']?.match(/filename="?(.+?)"?$/)?.[1] ??
      'Cadence_MSEL_Template.xlsx'

    return {
      blob: response.data,
      filename,
    }
  },
}

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
