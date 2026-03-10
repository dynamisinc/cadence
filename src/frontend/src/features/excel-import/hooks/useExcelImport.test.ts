/**
 * useExcelImport Hook Tests
 *
 * Tests for Excel import React Query hooks.
 * FF-M01: Verifies useExecuteImport invalidates correct query keys after successful import.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { createElement, type ReactNode } from 'react'
import {
  useExecuteImport,
  useUploadFile,
  useCancelImport,
} from './useExcelImport'
import { excelImportService } from '../services/excelImportService'
import type { ImportResult, FileAnalysisResult } from '../types'

// Mock the Excel import service
vi.mock('../services/excelImportService', () => ({
  excelImportService: {
    uploadFile: vi.fn(),
    executeImport: vi.fn(),
    cancelImport: vi.fn(),
  },
}))

// Helper to create a wrapper with React Query provider
const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  const Wrapper = ({ children }: { children: ReactNode }) =>
    createElement(QueryClientProvider, { client: queryClient }, children)

  return { Wrapper, queryClient }
}

describe('useExecuteImport', () => {
  const mockImportResult: ImportResult = {
    success: true,
    injectsCreated: 5,
    injectsUpdated: 0,
    rowsSkipped: 0,
    phasesCreated: 1,
    objectivesCreated: 2,
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(excelImportService.executeImport).mockResolvedValue(mockImportResult)
  })

  it('calls executeImport service function', async () => {
    const { Wrapper } = createWrapper()
    const { result } = renderHook(() => useExecuteImport(), { wrapper: Wrapper })

    await act(async () => {
      await result.current.mutateAsync({
        sessionId: 'session-1',
        exerciseId: 'exercise-123',
        strategy: 'Append',
      })
    })

    expect(excelImportService.executeImport).toHaveBeenCalledWith({
      sessionId: 'session-1',
      exerciseId: 'exercise-123',
      strategy: 'Append',
    })
  })

  it('invalidates injects query key on success (FF-M01)', async () => {
    const { Wrapper, queryClient } = createWrapper()
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries')

    const { result } = renderHook(() => useExecuteImport(), { wrapper: Wrapper })

    await act(async () => {
      await result.current.mutateAsync({
        sessionId: 'session-1',
        exerciseId: 'exercise-123',
        strategy: 'Append',
      })
    })

    // FF-M01: Must invalidate ['exercises', exerciseId, 'injects']
    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: ['exercises', 'exercise-123', 'injects'],
    })
  })

  it('invalidates MSEL query key on success (FF-M01)', async () => {
    const { Wrapper, queryClient } = createWrapper()
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries')

    const { result } = renderHook(() => useExecuteImport(), { wrapper: Wrapper })

    await act(async () => {
      await result.current.mutateAsync({
        sessionId: 'session-1',
        exerciseId: 'exercise-123',
        strategy: 'Append',
      })
    })

    // FF-M01: Must also invalidate ['exercises', exerciseId, 'msel']
    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: ['exercises', 'exercise-123', 'msel'],
    })
  })

  it('uses exercise-specific keys, not generic ones (FF-M01)', async () => {
    const { Wrapper, queryClient } = createWrapper()
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries')

    const { result } = renderHook(() => useExecuteImport(), { wrapper: Wrapper })

    await act(async () => {
      await result.current.mutateAsync({
        sessionId: 'session-1',
        exerciseId: 'exercise-456',
        strategy: 'Replace',
      })
    })

    // Should use the specific exerciseId from the request variables
    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: ['exercises', 'exercise-456', 'injects'],
    })
    expect(invalidateSpy).toHaveBeenCalledWith({
      queryKey: ['exercises', 'exercise-456', 'msel'],
    })
  })
})

describe('useUploadFile', () => {
  const mockAnalysis: FileAnalysisResult = {
    sessionId: 'session-abc',
    fileName: 'test.xlsx',
    fileSize: 1024,
    fileFormat: 'xlsx',
    worksheets: [],
    isPasswordProtected: false,
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(excelImportService.uploadFile).mockResolvedValue(mockAnalysis)
  })

  it('caches session state on success', async () => {
    const { Wrapper, queryClient } = createWrapper()
    const setDataSpy = vi.spyOn(queryClient, 'setQueryData')

    const { result } = renderHook(() => useUploadFile(), { wrapper: Wrapper })

    await act(async () => {
      await result.current.mutateAsync(new File([''], 'test.xlsx'))
    })

    expect(setDataSpy).toHaveBeenCalledWith(
      ['excelImport', 'session', 'session-abc'],
      mockAnalysis,
    )
  })
})

describe('useCancelImport', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(excelImportService.cancelImport).mockResolvedValue(undefined)
  })

  it('clears all cached data for the session on cancel', async () => {
    const { Wrapper, queryClient } = createWrapper()
    const removeSpy = vi.spyOn(queryClient, 'removeQueries')

    const { result } = renderHook(() => useCancelImport(), { wrapper: Wrapper })

    await act(async () => {
      await result.current.mutateAsync('session-xyz')
    })

    expect(removeSpy).toHaveBeenCalledWith({ queryKey: ['excelImport', 'session', 'session-xyz'] })
    expect(removeSpy).toHaveBeenCalledWith({ queryKey: ['excelImport', 'worksheet', 'session-xyz'] })
    expect(removeSpy).toHaveBeenCalledWith({ queryKey: ['excelImport', 'mappings', 'session-xyz'] })
    expect(removeSpy).toHaveBeenCalledWith({ queryKey: ['excelImport', 'validation', 'session-xyz'] })
  })
})
