/**
 * Tests for ReportsPage
 *
 * Exercise reports and data export page test coverage.
 */

/* eslint-disable @typescript-eslint/no-explicit-any */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, waitFor, render } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ThemeProvider } from '@mui/material/styles'
import { ReportsPage } from './ReportsPage'
import { cobraTheme } from '../../../theme/cobraTheme'
import type { ReactNode } from 'react'
import type { ExerciseDto, MselSummaryDto } from '../types'
import { ExerciseStatus, ExerciseType, DeliveryMode, TimelineMode } from '../../../types'

// Mock hooks
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useParams: vi.fn(),
    useNavigate: vi.fn(),
  }
})

vi.mock('../hooks', () => ({
  useExercise: vi.fn(),
  useMselSummary: vi.fn(),
}))

vi.mock('@/features/excel-export', () => ({
  useExportMsel: vi.fn(),
  useExportObservations: vi.fn(),
  useExportFullPackage: vi.fn(),
}))

vi.mock('@/core/contexts', () => ({
  useBreadcrumbs: vi.fn(),
  useConnectivity: vi.fn(() => ({
    connectivityState: 'online',
    incrementPendingCount: vi.fn(),
  })),
}))

vi.mock('@/features/observations', () => ({
  useObservations: vi.fn(),
}))

// Mock data
const mockExercise: ExerciseDto = {
  id: 'exercise-123',
  name: 'Test Exercise',
  description: 'Test description',
  exerciseType: ExerciseType.TTX,
  status: ExerciseStatus.Active,
  isPracticeMode: false,
  scheduledDate: '2025-02-01',
  startTime: '09:00:00',
  endTime: '17:00:00',
  timeZoneId: 'America/New_York',
  location: 'Test Location',
  organizationId: 'org-123',
  activeMselId: 'msel-123',
  deliveryMode: DeliveryMode.ClockDriven,
  timelineMode: TimelineMode.RealTime,
  timeScale: null,
  createdAt: '2025-01-20T10:00:00Z',
  updatedAt: '2025-01-20T10:00:00Z',
  createdBy: 'user-123',
  activatedAt: null,
  activatedBy: null,
  completedAt: null,
  completedBy: null,
  archivedAt: null,
  archivedBy: null,
  hasBeenPublished: false,
  previousStatus: null,
}

const mockMselSummary: MselSummaryDto = {
  exerciseId: 'exercise-123',
  mselId: 'msel-123',
  totalInjects: 25,
  pendingCount: 10,
  firedCount: 12,
  skippedCount: 3,
  deferredCount: 0,
  hasInjects: true,
  lastUpdated: '2025-01-20T10:00:00Z',
}

// Helper function to render with all necessary providers
const createWrapper = (exerciseId = 'exercise-123') => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={cobraTheme}>
        <MemoryRouter initialEntries={[`/exercises/${exerciseId}/reports`]}>
          <Routes>
            <Route path="/exercises/:id/reports" element={children} />
          </Routes>
        </MemoryRouter>
      </ThemeProvider>
    </QueryClientProvider>
  )
}

describe('ReportsPage', () => {
  const mockNavigate = vi.fn()
  const mockUseBreadcrumbs = vi.fn()
  const mockExportMselMutateAsync = vi.fn()
  const mockExportObservationsMutateAsync = vi.fn()
  const mockExportFullPackageMutateAsync = vi.fn()

  beforeEach(async () => {
    vi.clearAllMocks()

    const { useParams, useNavigate } = await import('react-router-dom')
    const { useExercise, useMselSummary } = await import('../hooks')
    const { useExportMsel, useExportObservations, useExportFullPackage } =
      await import('@/features/excel-export')
    const { useBreadcrumbs } = await import('@/core/contexts')
    const { useObservations } = await import('@/features/observations')

    vi.mocked(useParams).mockReturnValue({ id: 'exercise-123' })
    vi.mocked(useNavigate).mockReturnValue(mockNavigate)
    vi.mocked(useBreadcrumbs).mockReturnValue(mockUseBreadcrumbs)
    vi.mocked(useObservations).mockReturnValue({
      observations: [
        {
          id: 'obs-1',
          exerciseId: 'exercise-123',
          content: 'Test observation',
          createdAt: '2025-01-20T10:00:00Z',
          updatedAt: '2025-01-20T10:00:00Z',
          createdBy: 'user-123',
          createdByName: 'Test User',
          injectId: null,
          objectiveId: null,
          rating: null,
          recommendation: null,
          observedAt: '2025-01-20T10:00:00Z',
          location: null,
          injectTitle: null,
          injectNumber: null,
        },
      ],
      loading: false,
      error: null,
      fetchObservations: vi.fn(),
      createObservation: vi.fn(),
      updateObservation: vi.fn(),
      deleteObservation: vi.fn(),
      isCreating: false,
      isUpdating: false,
      isDeleting: false,
    } as any)

    // Default mock implementations
    vi.mocked(useExercise).mockReturnValue({
      exercise: mockExercise,
      loading: false,
      error: null,
      updateExercise: vi.fn(),
    } as any)

    vi.mocked(useMselSummary).mockReturnValue({
      data: mockMselSummary,
      isLoading: false,
      error: null,
    } as any)

    vi.mocked(useExportMsel).mockReturnValue({
      mutateAsync: mockExportMselMutateAsync,
      isPending: false,
      isError: false,
      error: null,
    } as any)

    vi.mocked(useExportObservations).mockReturnValue({
      mutateAsync: mockExportObservationsMutateAsync,
      isPending: false,
      isError: false,
      error: null,
    } as any)

    vi.mocked(useExportFullPackage).mockReturnValue({
      mutateAsync: mockExportFullPackageMutateAsync,
      isPending: false,
      isError: false,
      error: null,
    } as any)
  })

  describe('Loading States', () => {
    it('shows loading state while exercise is loading', async () => {
      const { useExercise } = await import('../hooks')

      vi.mocked(useExercise).mockReturnValue({
        exercise: null,
        loading: true,
        error: null,
        updateExercise: vi.fn(),
      } as any)

      render(<ReportsPage />, { wrapper: createWrapper() })

      expect(screen.getByRole('progressbar')).toBeInTheDocument()
    })
  })

  describe('Error States', () => {
    it('shows error state when exercise fails to load', async () => {
      const { useExercise } = await import('../hooks')

      vi.mocked(useExercise).mockReturnValue({
        exercise: null,
        loading: false,
        error: 'Failed to load exercise',
        updateExercise: vi.fn(),
      } as any)

      render(<ReportsPage />, { wrapper: createWrapper() })

      expect(screen.getByText('Failed to load exercise')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /back to exercises/i })).toBeInTheDocument()
    })

    it('shows exercise not found when exercise is null', async () => {
      const { useExercise } = await import('../hooks')

      vi.mocked(useExercise).mockReturnValue({
        exercise: null,
        loading: false,
        error: null,
        updateExercise: vi.fn(),
      } as any)

      render(<ReportsPage />, { wrapper: createWrapper() })

      expect(screen.getByText('Exercise not found')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /back to exercises/i })).toBeInTheDocument()
    })
  })

  describe('Page Layout', () => {
    it('shows page title "Reports & Export"', () => {
      render(<ReportsPage />, { wrapper: createWrapper() })

      expect(screen.getByRole('heading', { name: 'Reports & Export' })).toBeInTheDocument()
    })

    it('shows page description', () => {
      render(<ReportsPage />, { wrapper: createWrapper() })

      expect(screen.getByText('Export exercise data for analysis and documentation')).toBeInTheDocument()
    })

    it('shows "Back to Exercise" link', () => {
      render(<ReportsPage />, { wrapper: createWrapper() })

      expect(screen.getByRole('button', { name: /back to exercise/i })).toBeInTheDocument()
    })

    it('navigates back to exercise when back button clicked', async () => {
      const user = userEvent.setup()
      render(<ReportsPage />, { wrapper: createWrapper() })

      const backButton = screen.getByRole('button', { name: /back to exercise/i })
      await user.click(backButton)

      expect(mockNavigate).toHaveBeenCalledWith('/exercises/exercise-123')
    })
  })

  describe('Breadcrumbs', () => {
    it('sets breadcrumbs correctly', async () => {
      const { useBreadcrumbs } = await import('@/core/contexts')

      render(<ReportsPage />, { wrapper: createWrapper() })

      await waitFor(() => {
        expect(useBreadcrumbs).toHaveBeenCalledWith([
          expect.objectContaining({ label: 'Home', path: '/' }),
          expect.objectContaining({ label: 'Exercises', path: '/exercises' }),
          expect.objectContaining({ label: 'Test Exercise', path: '/exercises/exercise-123' }),
          expect.objectContaining({ label: 'Reports' }),
        ])
      })
    })
  })

  describe('Export Cards', () => {
    it('renders MSEL export card with title and description', () => {
      render(<ReportsPage />, { wrapper: createWrapper() })

      expect(screen.getByRole('heading', { name: 'Export MSEL' })).toBeInTheDocument()
      expect(
        screen.getByText(/download the master scenario events list as an excel file/i),
      ).toBeInTheDocument()
    })

    it('renders Observations export card with title and description', () => {
      render(<ReportsPage />, { wrapper: createWrapper() })

      expect(screen.getByRole('heading', { name: 'Export Observations' })).toBeInTheDocument()
      expect(
        screen.getByText(/download all evaluator observations as an excel file/i),
      ).toBeInTheDocument()
    })

    it('renders Full Package export card with title and description', () => {
      render(<ReportsPage />, { wrapper: createWrapper() })

      expect(screen.getByRole('heading', { name: 'Export Full Package' })).toBeInTheDocument()
      expect(
        screen.getByText(/download a complete exercise package as a zip file/i),
      ).toBeInTheDocument()
    })

    it('shows MSEL summary statistics when available', () => {
      render(<ReportsPage />, { wrapper: createWrapper() })

      expect(screen.getByText(/25 injects \(12 fired, 10 pending, 3 skipped\)/i)).toBeInTheDocument()
    })

    it('does not show MSEL summary when unavailable', async () => {
      const { useMselSummary } = await import('../hooks')

      vi.mocked(useMselSummary).mockReturnValue({
        data: null,
        isLoading: false,
        error: null,
      } as any)

      render(<ReportsPage />, { wrapper: createWrapper() })

      expect(screen.queryByText(/injects \(/i)).not.toBeInTheDocument()
    })
  })

  describe('MSEL Export', () => {
    it('calls exportMsel.mutateAsync when MSEL export button clicked', async () => {
      const user = userEvent.setup()
      mockExportMselMutateAsync.mockResolvedValue({ filename: 'test.xlsx' })

      render(<ReportsPage />, { wrapper: createWrapper() })

      const exportButton = screen.getByRole('button', { name: /export msel/i })
      await user.click(exportButton)

      await waitFor(() => {
        expect(mockExportMselMutateAsync).toHaveBeenCalledWith({
          exerciseId: 'exercise-123',
          format: 'xlsx',
          includeFormatting: true,
          includeConductData: true,
          includePhases: true,
          includeObjectives: true,
        })
      })
    })

    it('shows "Exporting..." text while MSEL export is pending', async () => {
      const { useExportMsel } = await import('@/features/excel-export')

      vi.mocked(useExportMsel).mockReturnValue({
        mutateAsync: mockExportMselMutateAsync,
        isPending: true,
        isError: false,
        error: null,
      } as any)

      render(<ReportsPage />, { wrapper: createWrapper() })

      expect(screen.getByRole('button', { name: /exporting\.\.\./i })).toBeInTheDocument()
    })

    it('disables MSEL export button while export is pending', async () => {
      const { useExportMsel } = await import('@/features/excel-export')

      vi.mocked(useExportMsel).mockReturnValue({
        mutateAsync: mockExportMselMutateAsync,
        isPending: true,
        isError: false,
        error: null,
      } as any)

      render(<ReportsPage />, { wrapper: createWrapper() })

      const exportButton = screen.getByRole('button', { name: /exporting\.\.\./i })
      expect(exportButton).toBeDisabled()
    })

    it('shows error alert when MSEL export fails', async () => {
      const user = userEvent.setup()
      mockExportMselMutateAsync.mockRejectedValue(new Error('Export failed'))

      render(<ReportsPage />, { wrapper: createWrapper() })

      const exportButton = screen.getByRole('button', { name: /export msel/i })
      await user.click(exportButton)

      await waitFor(() => {
        expect(screen.getByText('Failed to export MSEL. Please try again.')).toBeInTheDocument()
      })
    })
  })

  describe('Observations Export', () => {
    it('calls exportObservations.mutateAsync when Observations export button clicked', async () => {
      const user = userEvent.setup()
      mockExportObservationsMutateAsync.mockResolvedValue({ filename: 'observations.xlsx' })

      render(<ReportsPage />, { wrapper: createWrapper() })

      const exportButton = screen.getByRole('button', { name: /export observations/i })
      await user.click(exportButton)

      await waitFor(() => {
        expect(mockExportObservationsMutateAsync).toHaveBeenCalledWith({
          exerciseId: 'exercise-123',
          includeFormatting: true,
        })
      })
    })

    it('shows "Exporting..." text while Observations export is pending', async () => {
      const { useExportObservations } = await import('@/features/excel-export')

      vi.mocked(useExportObservations).mockReturnValue({
        mutateAsync: mockExportObservationsMutateAsync,
        isPending: true,
        isError: false,
        error: null,
      } as any)

      render(<ReportsPage />, { wrapper: createWrapper() })

      const exportButtons = screen.getAllByRole('button', { name: /exporting\.\.\./i })
      // Should have at least one "Exporting..." button for observations
      expect(exportButtons.length).toBeGreaterThan(0)
    })

    it('disables Observations export button while export is pending', async () => {
      const { useExportObservations } = await import('@/features/excel-export')

      vi.mocked(useExportObservations).mockReturnValue({
        mutateAsync: mockExportObservationsMutateAsync,
        isPending: true,
        isError: false,
        error: null,
      } as any)

      render(<ReportsPage />, { wrapper: createWrapper() })

      const exportButtons = screen.getAllByRole('button', { name: /exporting\.\.\./i })
      exportButtons.forEach(button => {
        expect(button).toBeDisabled()
      })
    })

    it('shows error alert when Observations export fails', async () => {
      const user = userEvent.setup()
      mockExportObservationsMutateAsync.mockRejectedValue(new Error('Export failed'))

      render(<ReportsPage />, { wrapper: createWrapper() })

      const exportButton = screen.getByRole('button', { name: /export observations/i })
      await user.click(exportButton)

      await waitFor(() => {
        expect(screen.getByText('Failed to export observations. Please try again.')).toBeInTheDocument()
      })
    })
  })

  describe('Full Package Export', () => {
    it('calls exportFullPackage.mutateAsync when Full Package export button clicked', async () => {
      const user = userEvent.setup()
      mockExportFullPackageMutateAsync.mockResolvedValue({ filename: 'package.zip' })

      render(<ReportsPage />, { wrapper: createWrapper() })

      const exportButton = screen.getByRole('button', { name: /export full package/i })
      await user.click(exportButton)

      await waitFor(() => {
        expect(mockExportFullPackageMutateAsync).toHaveBeenCalledWith({
          exerciseId: 'exercise-123',
          includeFormatting: true,
        })
      })
    })

    it('shows "Exporting..." text while Full Package export is pending', async () => {
      const { useExportFullPackage } = await import('@/features/excel-export')

      vi.mocked(useExportFullPackage).mockReturnValue({
        mutateAsync: mockExportFullPackageMutateAsync,
        isPending: true,
        isError: false,
        error: null,
      } as any)

      render(<ReportsPage />, { wrapper: createWrapper() })

      const exportButtons = screen.getAllByRole('button', { name: /exporting\.\.\./i })
      expect(exportButtons.length).toBeGreaterThan(0)
    })

    it('disables Full Package export button while export is pending', async () => {
      const { useExportFullPackage } = await import('@/features/excel-export')

      vi.mocked(useExportFullPackage).mockReturnValue({
        mutateAsync: mockExportFullPackageMutateAsync,
        isPending: true,
        isError: false,
        error: null,
      } as any)

      render(<ReportsPage />, { wrapper: createWrapper() })

      const exportButtons = screen.getAllByRole('button', { name: /exporting\.\.\./i })
      exportButtons.forEach(button => {
        expect(button).toBeDisabled()
      })
    })

    it('shows error alert when Full Package export fails', async () => {
      const user = userEvent.setup()
      mockExportFullPackageMutateAsync.mockRejectedValue(new Error('Export failed'))

      render(<ReportsPage />, { wrapper: createWrapper() })

      const exportButton = screen.getByRole('button', { name: /export full package/i })
      await user.click(exportButton)

      await waitFor(() => {
        expect(screen.getByText('Failed to export full package. Please try again.')).toBeInTheDocument()
      })
    })
  })

  describe('Error Alert', () => {
    it('can close error alert', async () => {
      const user = userEvent.setup()
      mockExportMselMutateAsync.mockRejectedValue(new Error('Export failed'))

      render(<ReportsPage />, { wrapper: createWrapper() })

      // Trigger error
      const exportButton = screen.getByRole('button', { name: /export msel/i })
      await user.click(exportButton)

      await waitFor(() => {
        expect(screen.getByText('Failed to export MSEL. Please try again.')).toBeInTheDocument()
      })

      // Close alert
      const closeButton = screen.getByRole('button', { name: /close/i })
      await user.click(closeButton)

      await waitFor(() => {
        expect(screen.queryByText('Failed to export MSEL. Please try again.')).not.toBeInTheDocument()
      })
    })

    it('clears previous error when new export is attempted', async () => {
      const user = userEvent.setup()
      mockExportMselMutateAsync.mockRejectedValueOnce(new Error('First error'))
      mockExportMselMutateAsync.mockResolvedValueOnce({ filename: 'test.xlsx' })

      render(<ReportsPage />, { wrapper: createWrapper() })

      // First export fails
      const exportButton = screen.getByRole('button', { name: /export msel/i })
      await user.click(exportButton)

      await waitFor(() => {
        expect(screen.getByText('Failed to export MSEL. Please try again.')).toBeInTheDocument()
      })

      // Second export succeeds
      await user.click(exportButton)

      await waitFor(() => {
        expect(screen.queryByText('Failed to export MSEL. Please try again.')).not.toBeInTheDocument()
      })
    })
  })
})
