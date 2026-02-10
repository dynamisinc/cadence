/**
 * useParticipantImport Hook Tests
 *
 * Tests the bulk participant import flow state management.
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook, act, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useParticipantImport } from './useParticipantImport'
import { bulkImportService } from '../services/bulkImportService'
import type { ReactNode } from 'react'
import type {
  FileParseResult,
  ImportPreviewResult,
  BulkImportResult,
} from '../types/bulkImport'

// Mock the bulk import service
vi.mock('../services/bulkImportService', () => ({
  bulkImportService: {
    uploadFile: vi.fn(),
    getPreview: vi.fn(),
    confirmImport: vi.fn(),
  },
}))

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
}

describe('useParticipantImport', () => {
  const exerciseId = 'test-exercise-id'

  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('starts in idle state with no data', () => {
    const { result } = renderHook(() => useParticipantImport(exerciseId), {
      wrapper: createWrapper(),
    })

    expect(result.current.step).toBe('idle')
    expect(result.current.parseResult).toBeNull()
    expect(result.current.previewResult).toBeNull()
    expect(result.current.importResult).toBeNull()
    expect(result.current.error).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })

  it('transitions through upload → preview on successful file upload', async () => {
    const mockParseResult: FileParseResult = {
      sessionId: 'session-123',
      fileName: 'participants.csv',
      totalRows: 2,
      columnMappings: [],
      rows: [],
      warnings: [],
      errors: [],
      isValid: true,
    }

    const mockPreviewResult: ImportPreviewResult = {
      sessionId: 'session-123',
      totalRows: 2,
      assignCount: 1,
      updateCount: 0,
      inviteCount: 1,
      errorCount: 0,
      rows: [],
      hasProcessableRows: true,
    }

    vi.mocked(bulkImportService.uploadFile).mockResolvedValue(mockParseResult)
    vi.mocked(bulkImportService.getPreview).mockResolvedValue(mockPreviewResult)

    const { result } = renderHook(() => useParticipantImport(exerciseId), {
      wrapper: createWrapper(),
    })

    const file = new File(['email,role\ntest@example.com,Controller'], 'participants.csv', {
      type: 'text/csv',
    })

    await act(async () => {
      await result.current.uploadFile(file)
    })

    await waitFor(() => {
      expect(result.current.step).toBe('preview')
    })

    expect(result.current.parseResult).toEqual(mockParseResult)
    expect(result.current.previewResult).toEqual(mockPreviewResult)
    expect(result.current.error).toBeNull()
    expect(result.current.isLoading).toBe(false)
    expect(bulkImportService.uploadFile).toHaveBeenCalledWith(exerciseId, file)
    expect(bulkImportService.getPreview).toHaveBeenCalledWith(exerciseId, 'session-123')
  })

  it('returns to idle with error when file upload fails validation', async () => {
    const mockParseResult: FileParseResult = {
      sessionId: 'session-123',
      fileName: 'bad.csv',
      totalRows: 0,
      columnMappings: [],
      rows: [],
      warnings: [],
      errors: ['Missing required column: Email', 'File is empty'],
      isValid: false,
    }

    vi.mocked(bulkImportService.uploadFile).mockResolvedValue(mockParseResult)

    const { result } = renderHook(() => useParticipantImport(exerciseId), {
      wrapper: createWrapper(),
    })

    const file = new File(['invalid'], 'bad.csv', { type: 'text/csv' })

    await act(async () => {
      await result.current.uploadFile(file)
    })

    await waitFor(() => {
      expect(result.current.step).toBe('idle')
    })

    expect(result.current.parseResult).toEqual(mockParseResult)
    expect(result.current.previewResult).toBeNull()
    expect(result.current.error).toBe('Missing required column: Email; File is empty')
    expect(result.current.isLoading).toBe(false)
  })

  it('handles API errors during upload', async () => {
    const mockError = new Error('Network error')
    vi.mocked(bulkImportService.uploadFile).mockRejectedValue(mockError)

    const { result } = renderHook(() => useParticipantImport(exerciseId), {
      wrapper: createWrapper(),
    })

    const file = new File(['test'], 'test.csv', { type: 'text/csv' })

    await act(async () => {
      await result.current.uploadFile(file)
    })

    await waitFor(() => {
      expect(result.current.step).toBe('idle')
    })

    expect(result.current.error).toBe('Network error')
    expect(result.current.isLoading).toBe(false)
  })

  it('transitions through confirming → results on successful import', async () => {
    const mockParseResult: FileParseResult = {
      sessionId: 'session-123',
      fileName: 'participants.csv',
      totalRows: 2,
      columnMappings: [],
      rows: [],
      warnings: [],
      errors: [],
      isValid: true,
    }

    const mockPreviewResult: ImportPreviewResult = {
      sessionId: 'session-123',
      totalRows: 2,
      assignCount: 1,
      updateCount: 0,
      inviteCount: 1,
      errorCount: 0,
      rows: [],
      hasProcessableRows: true,
    }

    const mockImportResult: BulkImportResult = {
      importRecordId: 'record-456',
      assignedCount: 1,
      updatedCount: 0,
      invitedCount: 1,
      errorCount: 0,
      skippedCount: 0,
      rowOutcomes: [],
    }

    vi.mocked(bulkImportService.uploadFile).mockResolvedValue(mockParseResult)
    vi.mocked(bulkImportService.getPreview).mockResolvedValue(mockPreviewResult)
    vi.mocked(bulkImportService.confirmImport).mockResolvedValue(mockImportResult)

    const { result } = renderHook(() => useParticipantImport(exerciseId), {
      wrapper: createWrapper(),
    })

    // Upload file to reach preview state
    const file = new File(['test'], 'participants.csv', { type: 'text/csv' })
    await act(async () => {
      await result.current.uploadFile(file)
    })

    await waitFor(() => {
      expect(result.current.step).toBe('preview')
    })

    await act(async () => {
      await result.current.confirmImport()
    })

    await waitFor(() => {
      expect(result.current.step).toBe('results')
    })

    expect(result.current.importResult).toEqual(mockImportResult)
    expect(result.current.error).toBeNull()
    expect(result.current.isLoading).toBe(false)
    expect(bulkImportService.confirmImport).toHaveBeenCalledWith(exerciseId, 'session-123')
  })

  it('handles API errors during confirm', async () => {
    const mockParseResult: FileParseResult = {
      sessionId: 'session-123',
      fileName: 'participants.csv',
      totalRows: 1,
      columnMappings: [],
      rows: [],
      warnings: [],
      errors: [],
      isValid: true,
    }

    const mockPreviewResult: ImportPreviewResult = {
      sessionId: 'session-123',
      totalRows: 1,
      assignCount: 1,
      updateCount: 0,
      inviteCount: 0,
      errorCount: 0,
      rows: [],
      hasProcessableRows: true,
    }

    vi.mocked(bulkImportService.uploadFile).mockResolvedValue(mockParseResult)
    vi.mocked(bulkImportService.getPreview).mockResolvedValue(mockPreviewResult)
    vi.mocked(bulkImportService.confirmImport).mockRejectedValue(new Error('Database error'))

    const { result } = renderHook(() => useParticipantImport(exerciseId), {
      wrapper: createWrapper(),
    })

    // Upload file to reach preview state
    const file = new File(['test'], 'participants.csv', { type: 'text/csv' })
    await act(async () => {
      await result.current.uploadFile(file)
    })

    await waitFor(() => {
      expect(result.current.step).toBe('preview')
    })

    await act(async () => {
      await result.current.confirmImport()
    })

    await waitFor(() => {
      expect(result.current.step).toBe('preview')
    })

    expect(result.current.error).toBe('Database error')
    expect(result.current.isLoading).toBe(false)
  })

  it('resets all state when reset is called', async () => {
    const mockParseResult: FileParseResult = {
      sessionId: 'session-123',
      fileName: 'participants.csv',
      totalRows: 1,
      columnMappings: [],
      rows: [],
      warnings: [],
      errors: [],
      isValid: true,
    }

    const mockPreviewResult: ImportPreviewResult = {
      sessionId: 'session-123',
      totalRows: 1,
      assignCount: 1,
      updateCount: 0,
      inviteCount: 0,
      errorCount: 0,
      rows: [],
      hasProcessableRows: true,
    }

    vi.mocked(bulkImportService.uploadFile).mockResolvedValue(mockParseResult)
    vi.mocked(bulkImportService.getPreview).mockResolvedValue(mockPreviewResult)

    const { result } = renderHook(() => useParticipantImport(exerciseId), {
      wrapper: createWrapper(),
    })

    // Upload file to reach preview state
    const file = new File(['test'], 'participants.csv', { type: 'text/csv' })
    await act(async () => {
      await result.current.uploadFile(file)
    })

    await waitFor(() => {
      expect(result.current.step).toBe('preview')
    })

    act(() => {
      result.current.reset()
    })

    expect(result.current.step).toBe('idle')
    expect(result.current.parseResult).toBeNull()
    expect(result.current.previewResult).toBeNull()
    expect(result.current.importResult).toBeNull()
    expect(result.current.error).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })

  it('goBackToUpload clears preview and returns to idle', async () => {
    const mockParseResult: FileParseResult = {
      sessionId: 'session-123',
      fileName: 'test.csv',
      totalRows: 1,
      columnMappings: [],
      rows: [],
      warnings: [],
      errors: [],
      isValid: true,
    }

    const mockPreviewResult: ImportPreviewResult = {
      sessionId: 'session-123',
      totalRows: 1,
      assignCount: 1,
      updateCount: 0,
      inviteCount: 0,
      errorCount: 0,
      rows: [],
      hasProcessableRows: true,
    }

    vi.mocked(bulkImportService.uploadFile).mockResolvedValue(mockParseResult)
    vi.mocked(bulkImportService.getPreview).mockResolvedValue(mockPreviewResult)

    const { result } = renderHook(() => useParticipantImport(exerciseId), {
      wrapper: createWrapper(),
    })

    // Upload file to reach preview state
    const file = new File(['test'], 'test.csv', { type: 'text/csv' })
    await act(async () => {
      await result.current.uploadFile(file)
    })

    await waitFor(() => {
      expect(result.current.step).toBe('preview')
    })

    act(() => {
      result.current.goBackToUpload()
    })

    expect(result.current.step).toBe('idle')
    expect(result.current.parseResult).toBeNull()
    expect(result.current.previewResult).toBeNull()
    expect(result.current.error).toBeNull()
  })

  it('handles confirmImport when no preview result is available', async () => {
    const { result } = renderHook(() => useParticipantImport(exerciseId), {
      wrapper: createWrapper(),
    })

    await act(async () => {
      await result.current.confirmImport()
    })

    expect(result.current.error).toBe('No preview result available')
    expect(bulkImportService.confirmImport).not.toHaveBeenCalled()
  })
})
