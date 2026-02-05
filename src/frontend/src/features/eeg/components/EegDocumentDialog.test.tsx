/**
 * EegDocumentDialog Component Tests
 *
 * Tests for the EEG document generation dialog.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { render } from '../../../test/test-utils'
import { EegDocumentDialog } from './EegDocumentDialog'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { eegDocumentService } from '../services/eegService'
import type { EegCoverageDto } from '../types'

// Mock the service
vi.mock('../services/eegService', () => ({
  eegDocumentService: {
    download: vi.fn(),
    generate: vi.fn(),
  },
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
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>)
}

describe('EegDocumentDialog', () => {
  const mockCoverage: EegCoverageDto = {
    capabilityTargetCount: 3,
    totalTasks: 12,
    evaluatedTasks: 8,
    coveragePercentage: 67,
    tasksWithMultipleEvaluators: 2,
    ratingDistribution: {
      P: 5,
      S: 2,
      M: 1,
      U: 0,
    },
    byCapability: [],
  }

  const defaultProps = {
    open: true,
    onClose: vi.fn(),
    exerciseId: 'exercise-1',
    exerciseName: 'Hurricane Response TTX',
    coverage: mockCoverage,
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(eegDocumentService.download).mockResolvedValue()
  })

  describe('rendering', () => {
    it('renders dialog when open is true', async () => {
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })
    })

    it('does not render when open is false', () => {
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} open={false} />)

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })

    it('displays exercise name in dialog', async () => {
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByText(/Hurricane Response TTX/)).toBeInTheDocument()
      })
    })

    it('displays Generate Exercise Evaluation Guide title', async () => {
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByText(/Generate EEG Document/i)).toBeInTheDocument()
      })
    })

    it('displays coverage information when provided', async () => {
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByText(/Capability Targets: 3/)).toBeInTheDocument()
        expect(screen.getByText(/Critical Tasks: 12/)).toBeInTheDocument()
      })
    })
  })

  describe('document mode selection', () => {
    it('defaults to blank mode', async () => {
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        const blankRadio = screen.getByLabelText(/Blank EEG/i)
        expect(blankRadio).toBeChecked()
      })
    })

    it('allows selecting completed mode when entries exist', async () => {
      const user = userEvent.setup()
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const completedRadio = screen.getByLabelText(/Completed EEG/i)
      await user.click(completedRadio)

      expect(completedRadio).toBeChecked()
    })

    it('disables completed mode when no entries exist', async () => {
      const coverageNoEntries: EegCoverageDto = {
        ...mockCoverage,
        ratingDistribution: { P: 0, S: 0, M: 0, U: 0 },
      }

      renderWithQueryClient(
        <EegDocumentDialog {...defaultProps} coverage={coverageNoEntries} />,
      )

      await waitFor(() => {
        const completedRadio = screen.getByLabelText(/Completed EEG/i)
        expect(completedRadio).toBeDisabled()
      })
    })

    it('uses defaultMode prop when provided', async () => {
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} defaultMode="completed" />)

      await waitFor(() => {
        const completedRadio = screen.getByLabelText(/Completed EEG/i)
        expect(completedRadio).toBeChecked()
      })
    })
  })

  describe('output format selection', () => {
    it('defaults to single document format', async () => {
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        const singleRadio = screen.getByLabelText(/Single Document/i)
        expect(singleRadio).toBeChecked()
      })
    })

    it('allows selecting per capability format', async () => {
      const user = userEvent.setup()
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const perCapabilityRadio = screen.getByLabelText(/Per Capability/i)
      await user.click(perCapabilityRadio)

      expect(perCapabilityRadio).toBeChecked()
    })
  })

  describe('evaluator names option', () => {
    it('shows evaluator names checkbox only in completed mode', async () => {
      const user = userEvent.setup()
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      // Should not be visible in blank mode
      expect(screen.queryByLabelText(/Include evaluator names/i)).not.toBeInTheDocument()

      // Switch to completed mode
      const completedRadio = screen.getByLabelText(/Completed EEG/i)
      await user.click(completedRadio)

      // Should now be visible
      await waitFor(() => {
        expect(screen.getByLabelText(/Include evaluator names/i)).toBeInTheDocument()
      })
    })

    it('evaluator names checkbox is checked by default', async () => {
      const user = userEvent.setup()
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      // Switch to completed mode
      const completedRadio = screen.getByLabelText(/Completed EEG/i)
      await user.click(completedRadio)

      await waitFor(() => {
        const checkbox = screen.getByLabelText(/Include evaluator names/i)
        expect(checkbox).toBeChecked()
      })
    })
  })

  describe('generate action', () => {
    it('calls download service with correct options on Generate click', async () => {
      const user = userEvent.setup()
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const generateButton = screen.getByRole('button', { name: /generate/i })
      await user.click(generateButton)

      await waitFor(() => {
        expect(eegDocumentService.download).toHaveBeenCalledWith(
          'exercise-1',
          'Hurricane Response TTX',
          expect.objectContaining({
            mode: 'blank',
            outputFormat: 'single',
          }),
        )
      })
    })

    it('passes completed mode when selected', async () => {
      const user = userEvent.setup()
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      // Switch to completed mode
      const completedRadio = screen.getByLabelText(/Completed EEG/i)
      await user.click(completedRadio)

      const generateButton = screen.getByRole('button', { name: /generate/i })
      await user.click(generateButton)

      await waitFor(() => {
        expect(eegDocumentService.download).toHaveBeenCalledWith(
          'exercise-1',
          'Hurricane Response TTX',
          expect.objectContaining({
            mode: 'completed',
            includeEvaluatorNames: true,
          }),
        )
      })
    })

    it('passes perCapability format when selected', async () => {
      const user = userEvent.setup()
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      // Switch to per-capability format
      const perCapabilityRadio = screen.getByLabelText(/Per Capability/i)
      await user.click(perCapabilityRadio)

      const generateButton = screen.getByRole('button', { name: /generate/i })
      await user.click(generateButton)

      await waitFor(() => {
        expect(eegDocumentService.download).toHaveBeenCalledWith(
          'exercise-1',
          'Hurricane Response TTX',
          expect.objectContaining({
            outputFormat: 'perCapability',
          }),
        )
      })
    })

    it('shows loading state during generation', async () => {
      vi.mocked(eegDocumentService.download).mockImplementation(
        () => new Promise(resolve => setTimeout(resolve, 100)),
      )

      const user = userEvent.setup()
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const generateButton = screen.getByRole('button', { name: /generate/i })
      await user.click(generateButton)

      await waitFor(() => {
        expect(screen.getByText(/Generating/i)).toBeInTheDocument()
      })
    })

    it('closes dialog on successful generation', async () => {
      const onClose = vi.fn()
      const user = userEvent.setup()
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} onClose={onClose} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const generateButton = screen.getByRole('button', { name: /generate/i })
      await user.click(generateButton)

      await waitFor(() => {
        expect(onClose).toHaveBeenCalled()
      })
    })

    it('shows error alert on generation failure', async () => {
      vi.mocked(eegDocumentService.download).mockRejectedValue(
        new Error('Failed to generate document'),
      )

      const user = userEvent.setup()
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const generateButton = screen.getByRole('button', { name: /generate/i })
      await user.click(generateButton)

      await waitFor(() => {
        expect(screen.getByRole('alert')).toBeInTheDocument()
      })
    })
  })

  describe('no targets warning', () => {
    it('shows warning when no capability targets defined', async () => {
      const coverageNoTargets: EegCoverageDto = {
        ...mockCoverage,
        capabilityTargetCount: 0,
        totalTasks: 0,
      }

      renderWithQueryClient(
        <EegDocumentDialog {...defaultProps} coverage={coverageNoTargets} />,
      )

      await waitFor(() => {
        expect(screen.getByText(/No capability targets defined/i)).toBeInTheDocument()
      })
    })

    it('disables generate button when no targets defined', async () => {
      const coverageNoTargets: EegCoverageDto = {
        ...mockCoverage,
        capabilityTargetCount: 0,
        totalTasks: 0,
      }

      renderWithQueryClient(
        <EegDocumentDialog {...defaultProps} coverage={coverageNoTargets} />,
      )

      await waitFor(() => {
        const generateButton = screen.getByRole('button', { name: /generate/i })
        expect(generateButton).toBeDisabled()
      })
    })
  })

  describe('cancel action', () => {
    it('calls onClose when Cancel clicked', async () => {
      const onClose = vi.fn()
      const user = userEvent.setup()
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} onClose={onClose} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const cancelButton = screen.getByRole('button', { name: /cancel/i })
      await user.click(cancelButton)

      expect(onClose).toHaveBeenCalled()
    })

    it('disables Cancel during generation', async () => {
      vi.mocked(eegDocumentService.download).mockImplementation(
        () => new Promise(resolve => setTimeout(resolve, 500)),
      )

      const user = userEvent.setup()
      renderWithQueryClient(<EegDocumentDialog {...defaultProps} />)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const generateButton = screen.getByRole('button', { name: /generate/i })
      await user.click(generateButton)

      await waitFor(() => {
        const cancelButton = screen.getByRole('button', { name: /cancel/i })
        expect(cancelButton).toBeDisabled()
      })
    })
  })
})
