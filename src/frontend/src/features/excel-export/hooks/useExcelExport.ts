import { useMutation } from '@tanstack/react-query'
import { excelExportService, downloadBlob } from '../services/excelExportService'
import type { ExportMselRequest } from '../types'

/**
 * Hook for exporting MSEL to Excel/CSV
 * Automatically downloads the file on success
 */
export function useExportMsel() {
  return useMutation({
    mutationFn: async (request: ExportMselRequest) => {
      const { blob, info } = await excelExportService.exportMsel(request)
      downloadBlob(blob, info.filename)
      return info
    },
  })
}

/**
 * Hook for downloading the MSEL template
 */
export function useDownloadTemplate() {
  return useMutation({
    mutationFn: async () => {
      const { blob, filename } = await excelExportService.downloadTemplate()
      downloadBlob(blob, filename)
      return { filename }
    },
  })
}
