import { useMutation } from '@tanstack/react-query'
import { excelExportService, downloadBlob } from '../services/excelExportService'
import type {
  ExportMselRequest,
  ExportObservationsRequest,
  ExportFullPackageRequest,
} from '../types'

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

/**
 * Hook for exporting observations to Excel
 * Automatically downloads the file on success
 */
export function useExportObservations() {
  return useMutation({
    mutationFn: async (request: ExportObservationsRequest) => {
      const { blob, info } = await excelExportService.exportObservations(request)
      downloadBlob(blob, info.filename)
      return info
    },
  })
}

/**
 * Hook for exporting full exercise package (ZIP)
 * Automatically downloads the file on success
 */
export function useExportFullPackage() {
  return useMutation({
    mutationFn: async (request: ExportFullPackageRequest) => {
      const { blob, info } = await excelExportService.exportFullPackage(request)
      downloadBlob(blob, info.filename)
      return info
    },
  })
}
