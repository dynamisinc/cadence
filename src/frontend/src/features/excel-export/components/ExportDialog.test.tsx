/**
 * ExportDialog Component Tests
 *
 * Tests for the MSEL export options dialog.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, waitFor, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { render } from '../../../test/test-utils'
import { ExportDialog } from './ExportDialog'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { excelExportService, downloadBlob } from '../services/excelExportService'

// Mock the service
vi.mock('../services/excelExportService', () => ({
  excelExportService: {
    exportMsel: vi.fn(),
  },
  downloadBlob: vi.fn(),
}))

// Custom render with QueryClient
const renderWithQueryClient = (ui: React.ReactElement) => {
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
  return render(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>,
  )
}

describe('ExportDialog', () => {
  const defaultProps = {
    open: true,
    onClose: vi.fn(),
    exerciseId: 'exercise-1',
    exerciseName: 'Hurricane Exercise 2025',
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(excelExportService.exportMsel).mockResolvedValue({
      blob: new Blob(['test']),
      info: {
        filename: 'Test_MSEL.xlsx',
        injectCount: 10,
        phaseCount: 3,
        objectiveCount: 5,
      },
    })
  })

  describe('rendering', () => {
    it('renders dialog when open is true', async () => {
      renderWithQueryClient(<ExportDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })
    })

    it('does not render when open is false', () => {
      renderWithQueryClient(<ExportDialog {...defaultProps} open={false} />)

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })

    it('displays exercise name in dialog', async () => {
      renderWithQueryClient(<ExportDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByText(/Hurricane Exercise 2025/)).toBeInTheDocument()
      })
    })

    it('displays Export MSEL title', async () => {
      renderWithQueryClient(<ExportDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByText('Export MSEL')).toBeInTheDocument()
      })
    })
  })

  describe('format selection', () => {
    it('defaults to xlsx format', async () => {
      renderWithQueryClient(<ExportDialog {...defaultProps} />)

      await waitFor(() => {
        const xlsxRadio = screen.getByLabelText(/Excel/i)
        expect(xlsxRadio).toBeChecked()
      })
    })

    it('allows selecting csv format', async () => {
      const user = userEvent.setup()
      renderWithQueryClient(<ExportDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const csvRadio = screen.getByLabelText(/CSV/i)
      await user.click(csvRadio)

      expect(csvRadio).toBeChecked()
    })

    it('hides formatting options when csv selected', async () => {
      const user = userEvent.setup()
      renderWithQueryClient(<ExportDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      // Initially formatting checkbox should be visible
      expect(screen.getByLabelText(/formatting/i)).toBeInTheDocument()

      // Select CSV
      const csvRadio = screen.getByLabelText(/CSV/i)
      await user.click(csvRadio)

      // Formatting option should be hidden for CSV
      expect(screen.queryByLabelText(/Include formatting/i)).not.toBeInTheDocument()
    })
  })

  describe('export options', () => {
    it('has formatting checkbox enabled by default', async () => {
      renderWithQueryClient(<ExportDialog {...defaultProps} />)

      await waitFor(() => {
        const checkbox = screen.getByLabelText(/formatting/i)
        expect(checkbox).toBeChecked()
      })
    })

    it('has phases checkbox enabled by default', async () => {
      renderWithQueryClient(<ExportDialog {...defaultProps} />)

      await waitFor(() => {
        const checkbox = screen.getByLabelText(/Phases worksheet/i)
        expect(checkbox).toBeChecked()
      })
    })

    it('has objectives checkbox enabled by default', async () => {
      renderWithQueryClient(<ExportDialog {...defaultProps} />)

      await waitFor(() => {
        const checkbox = screen.getByLabelText(/Objectives worksheet/i)
        expect(checkbox).toBeChecked()
      })
    })

    it('has conduct data checkbox disabled by default', async () => {
      renderWithQueryClient(<ExportDialog {...defaultProps} />)

      await waitFor(() => {
        const checkbox = screen.getByLabelText(/conduct data/i)
        expect(checkbox).not.toBeChecked()
      })
    })
  })

  describe('export action', () => {
    it('calls export mutation with correct options on Export click', async () => {
      const user = userEvent.setup()
      renderWithQueryClient(<ExportDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const exportButton = screen.getByRole('button', { name: /export/i })
      await user.click(exportButton)

      await waitFor(() => {
        expect(excelExportService.exportMsel).toHaveBeenCalledWith({
          exerciseId: 'exercise-1',
          format: 'xlsx',
          includeFormatting: true,
          includePhases: true,
          includeObjectives: true,
          includeConductData: false,
        })
      })
    })

    it('shows loading state during export', async () => {
      // Make the export take time
      vi.mocked(excelExportService.exportMsel).mockImplementation(
        () => new Promise(resolve => setTimeout(() => resolve({
          blob: new Blob(['test']),
          info: { filename: 'test.xlsx', injectCount: 0, phaseCount: 0, objectiveCount: 0 },
        }), 100)),
      )

      const user = userEvent.setup()
      renderWithQueryClient(<ExportDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const exportButton = screen.getByRole('button', { name: /export/i })
      await user.click(exportButton)

      // Should show loading text
      await waitFor(() => {
        expect(screen.getByText(/Exporting/i)).toBeInTheDocument()
      })
    })

    it('closes dialog on successful export', async () => {
      const onClose = vi.fn()
      const user = userEvent.setup()
      renderWithQueryClient(<ExportDialog {...defaultProps} onClose={onClose} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const exportButton = screen.getByRole('button', { name: /export/i })
      await user.click(exportButton)

      await waitFor(() => {
        expect(onClose).toHaveBeenCalled()
      })
    })

    it('shows error alert on export failure', async () => {
      vi.mocked(excelExportService.exportMsel).mockRejectedValue(new Error('Export failed'))

      const user = userEvent.setup()
      renderWithQueryClient(<ExportDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const exportButton = screen.getByRole('button', { name: /export/i })
      await user.click(exportButton)

      await waitFor(() => {
        expect(screen.getByRole('alert')).toBeInTheDocument()
      })
    })
  })

  describe('cancel action', () => {
    it('calls onClose when Cancel clicked', async () => {
      const onClose = vi.fn()
      const user = userEvent.setup()
      renderWithQueryClient(<ExportDialog {...defaultProps} onClose={onClose} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const cancelButton = screen.getByRole('button', { name: /cancel/i })
      await user.click(cancelButton)

      expect(onClose).toHaveBeenCalled()
    })

    it('disables Cancel during export', async () => {
      // Make the export take time
      vi.mocked(excelExportService.exportMsel).mockImplementation(
        () => new Promise(resolve => setTimeout(() => resolve({
          blob: new Blob(['test']),
          info: { filename: 'test.xlsx', injectCount: 0, phaseCount: 0, objectiveCount: 0 },
        }), 500)),
      )

      const user = userEvent.setup()
      renderWithQueryClient(<ExportDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const exportButton = screen.getByRole('button', { name: /export/i })
      await user.click(exportButton)

      // Cancel button should be disabled during export
      await waitFor(() => {
        const cancelButton = screen.getByRole('button', { name: /cancel/i })
        expect(cancelButton).toBeDisabled()
      })
    })
  })
})
