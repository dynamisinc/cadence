/**
 * ExportButton Component Tests
 *
 * Tests for the export dropdown button component.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, fireEvent, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '../../../test/test-utils'
import { ExportButton } from './ExportButton'

// Mock the hooks
vi.mock('../hooks/useExcelExport', () => ({
  useExportMsel: vi.fn(() => ({
    mutateAsync: vi.fn(),
    isPending: false,
  })),
  useExportObservations: vi.fn(() => ({
    mutateAsync: vi.fn(),
    isPending: false,
  })),
  useExportFullPackage: vi.fn(() => ({
    mutateAsync: vi.fn(),
    isPending: false,
  })),
}))

import {
  useExportMsel,
  useExportObservations,
  useExportFullPackage,
} from '../hooks/useExcelExport'

// Custom render with QueryClient
const renderWithQueryClient = (ui: React.ReactElement) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>)
}

describe('ExportButton', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders export button with dropdown icon', () => {
    renderWithQueryClient(<ExportButton exerciseId="test-exercise-id" />)

    expect(screen.getByRole('button', { name: /export/i })).toBeInTheDocument()
  })

  it('opens menu when clicked', async () => {
    renderWithQueryClient(<ExportButton exerciseId="test-exercise-id" />)

    const button = screen.getByRole('button', { name: /export/i })
    fireEvent.click(button)

    await waitFor(() => {
      expect(screen.getByText('Export MSEL')).toBeInTheDocument()
      expect(screen.getByText('Export Observations')).toBeInTheDocument()
      expect(screen.getByText('Export Full Package')).toBeInTheDocument()
    })
  })

  it('calls exportMsel when MSEL option is clicked', async () => {
    const mockMutateAsync = vi.fn().mockResolvedValue({})
    vi.mocked(useExportMsel).mockReturnValue({
      mutateAsync: mockMutateAsync,
      isPending: false,
    } as unknown as ReturnType<typeof useExportMsel>)

    renderWithQueryClient(<ExportButton exerciseId="test-exercise-id" />)

    const button = screen.getByRole('button', { name: /export/i })
    fireEvent.click(button)

    await waitFor(() => {
      expect(screen.getByText('Export MSEL')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByText('Export MSEL'))

    await waitFor(() => {
      expect(mockMutateAsync).toHaveBeenCalledWith({
        exerciseId: 'test-exercise-id',
        format: 'xlsx',
        includeFormatting: true,
        includeConductData: true,
      })
    })
  })

  it('calls exportObservations when Observations option is clicked', async () => {
    const mockMutateAsync = vi.fn().mockResolvedValue({})
    vi.mocked(useExportObservations).mockReturnValue({
      mutateAsync: mockMutateAsync,
      isPending: false,
    } as unknown as ReturnType<typeof useExportObservations>)

    renderWithQueryClient(<ExportButton exerciseId="test-exercise-id" />)

    const button = screen.getByRole('button', { name: /export/i })
    fireEvent.click(button)

    await waitFor(() => {
      expect(screen.getByText('Export Observations')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByText('Export Observations'))

    await waitFor(() => {
      expect(mockMutateAsync).toHaveBeenCalledWith({
        exerciseId: 'test-exercise-id',
        includeFormatting: true,
      })
    })
  })

  it('calls exportFullPackage when Full Package option is clicked', async () => {
    const mockMutateAsync = vi.fn().mockResolvedValue({})
    vi.mocked(useExportFullPackage).mockReturnValue({
      mutateAsync: mockMutateAsync,
      isPending: false,
    } as unknown as ReturnType<typeof useExportFullPackage>)

    renderWithQueryClient(<ExportButton exerciseId="test-exercise-id" />)

    const button = screen.getByRole('button', { name: /export/i })
    fireEvent.click(button)

    await waitFor(() => {
      expect(screen.getByText('Export Full Package')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByText('Export Full Package'))

    await waitFor(() => {
      expect(mockMutateAsync).toHaveBeenCalledWith({
        exerciseId: 'test-exercise-id',
        includeFormatting: true,
      })
    })
  })

  it('shows loading state when export is in progress', () => {
    vi.mocked(useExportMsel).mockReturnValue({
      mutateAsync: vi.fn(),
      isPending: true,
    } as unknown as ReturnType<typeof useExportMsel>)

    renderWithQueryClient(<ExportButton exerciseId="test-exercise-id" />)

    expect(screen.getByText('Exporting...')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /exporting/i })).toBeDisabled()
  })

  it('is disabled when disabled prop is true', () => {
    renderWithQueryClient(<ExportButton exerciseId="test-exercise-id" disabled />)

    expect(screen.getByRole('button', { name: /export/i })).toBeDisabled()
  })

  it('shows error alert when export fails', async () => {
    const mockMutateAsync = vi.fn().mockRejectedValue(new Error('Export failed'))
    vi.mocked(useExportMsel).mockReturnValue({
      mutateAsync: mockMutateAsync,
      isPending: false,
    } as unknown as ReturnType<typeof useExportMsel>)

    renderWithQueryClient(<ExportButton exerciseId="test-exercise-id" />)

    const button = screen.getByRole('button', { name: /export/i })
    fireEvent.click(button)

    await waitFor(() => {
      expect(screen.getByText('Export MSEL')).toBeInTheDocument()
    })

    fireEvent.click(screen.getByText('Export MSEL'))

    await waitFor(() => {
      expect(screen.getByText('Export failed. Please try again.')).toBeInTheDocument()
    })
  })
})
