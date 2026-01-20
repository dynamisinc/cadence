import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { ReactNode } from 'react'
import { useExportMsel, useDownloadTemplate } from './useExcelExport'
import { excelExportService, downloadBlob } from '../services/excelExportService'

// Mock the service
vi.mock('../services/excelExportService', () => ({
  excelExportService: {
    exportMsel: vi.fn(),
    downloadTemplate: vi.fn(),
  },
  downloadBlob: vi.fn(),
}))

// Helper to create a wrapper with React Query provider
const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
      mutations: {
        retry: false,
      },
    },
  })

  const Wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )

  return { Wrapper, queryClient }
}

describe('useExportMsel', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('calls exportMsel service on mutate', async () => {
    const mockBlob = new Blob(['test'])
    const mockInfo = {
      filename: 'Test_MSEL.xlsx',
      injectCount: 10,
      phaseCount: 3,
      objectiveCount: 5,
    }
    vi.mocked(excelExportService.exportMsel).mockResolvedValue({
      blob: mockBlob,
      info: mockInfo,
    })

    const { Wrapper } = createWrapper()
    const { result } = renderHook(() => useExportMsel(), { wrapper: Wrapper })

    await act(async () => {
      result.current.mutate({
        exerciseId: 'exercise-1',
        format: 'xlsx',
      })
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(excelExportService.exportMsel).toHaveBeenCalledWith({
      exerciseId: 'exercise-1',
      format: 'xlsx',
    })
  })

  it('triggers file download on success', async () => {
    const mockBlob = new Blob(['test'])
    const mockInfo = {
      filename: 'Test_MSEL.xlsx',
      injectCount: 10,
      phaseCount: 3,
      objectiveCount: 5,
    }
    vi.mocked(excelExportService.exportMsel).mockResolvedValue({
      blob: mockBlob,
      info: mockInfo,
    })

    const { Wrapper } = createWrapper()
    const { result } = renderHook(() => useExportMsel(), { wrapper: Wrapper })

    await act(async () => {
      result.current.mutate({
        exerciseId: 'exercise-1',
      })
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(downloadBlob).toHaveBeenCalledWith(mockBlob, 'Test_MSEL.xlsx')
  })

  it('returns export info on success', async () => {
    const mockBlob = new Blob(['test'])
    const mockInfo = {
      filename: 'Test_MSEL.xlsx',
      injectCount: 10,
      phaseCount: 3,
      objectiveCount: 5,
    }
    vi.mocked(excelExportService.exportMsel).mockResolvedValue({
      blob: mockBlob,
      info: mockInfo,
    })

    const { Wrapper } = createWrapper()
    const { result } = renderHook(() => useExportMsel(), { wrapper: Wrapper })

    await act(async () => {
      result.current.mutate({
        exerciseId: 'exercise-1',
      })
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(result.current.data).toEqual(mockInfo)
  })

  it('handles export errors', async () => {
    const error = new Error('Export failed')
    vi.mocked(excelExportService.exportMsel).mockRejectedValue(error)

    const { Wrapper } = createWrapper()
    const { result } = renderHook(() => useExportMsel(), { wrapper: Wrapper })

    await act(async () => {
      result.current.mutate({
        exerciseId: 'exercise-1',
      })
    })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })

    expect(result.current.error).toBe(error)
  })
})

describe('useDownloadTemplate', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('calls downloadTemplate service on mutate', async () => {
    const mockBlob = new Blob(['test'])
    vi.mocked(excelExportService.downloadTemplate).mockResolvedValue({
      blob: mockBlob,
      filename: 'Cadence_MSEL_Template.xlsx',
    })

    const { Wrapper } = createWrapper()
    const { result } = renderHook(() => useDownloadTemplate(), { wrapper: Wrapper })

    await act(async () => {
      result.current.mutate()
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(excelExportService.downloadTemplate).toHaveBeenCalled()
  })

  it('triggers file download on success', async () => {
    const mockBlob = new Blob(['test'])
    vi.mocked(excelExportService.downloadTemplate).mockResolvedValue({
      blob: mockBlob,
      filename: 'Cadence_MSEL_Template.xlsx',
    })

    const { Wrapper } = createWrapper()
    const { result } = renderHook(() => useDownloadTemplate(), { wrapper: Wrapper })

    await act(async () => {
      result.current.mutate()
    })

    await waitFor(() => {
      expect(result.current.isSuccess).toBe(true)
    })

    expect(downloadBlob).toHaveBeenCalledWith(mockBlob, 'Cadence_MSEL_Template.xlsx')
  })

  it('handles download errors', async () => {
    const error = new Error('Download failed')
    vi.mocked(excelExportService.downloadTemplate).mockRejectedValue(error)

    const { Wrapper } = createWrapper()
    const { result } = renderHook(() => useDownloadTemplate(), { wrapper: Wrapper })

    await act(async () => {
      result.current.mutate()
    })

    await waitFor(() => {
      expect(result.current.isError).toBe(true)
    })

    expect(result.current.error).toBe(error)
  })
})
