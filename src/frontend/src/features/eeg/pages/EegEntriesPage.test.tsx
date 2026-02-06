/**
 * EegEntriesPage Component Tests
 *
 * Tests for the main EEG entries page with tab navigation, filtering, and CRUD operations.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { render } from '../../../test/test-utils'
import { EegEntriesPage } from './EegEntriesPage'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import type { EegEntryDto, EegCoverageDto, PerformanceRating } from '../types'
import type { ExerciseDto } from '../../exercises/types'
import type { InjectDto } from '../../injects/types'
import { ExerciseStatus, ExerciseClockState } from '../../../types'

// Mock hooks
vi.mock('../../exercises/hooks', () => ({
  useExercise: vi.fn(),
}))

vi.mock('../../auth', () => ({
  useExerciseRole: vi.fn(),
}))

vi.mock('../../../contexts/AuthContext', () => ({
  useAuth: vi.fn(),
}))

vi.mock('../hooks/useEegEntries', () => ({
  useEegEntries: vi.fn(),
  useEegCoverage: vi.fn(),
  eegEntryKeys: {
    byExercise: (exerciseId: string) => ['eeg-entries', 'exercise', exerciseId],
    coverage: (exerciseId: string) => ['eeg-entries', 'coverage', exerciseId],
  },
}))

vi.mock('../../injects/hooks', () => ({
  useInjects: vi.fn(),
}))

vi.mock('../../../core/contexts', () => ({
  useBreadcrumbs: vi.fn(),
}))

vi.mock('../services/eegService', () => ({
  eegEntryService: {
    delete: vi.fn(),
  },
}))

// Mock child components
vi.mock('../components/EegEntriesList', () => ({
  EegEntriesList: ({ entries }: { entries: EegEntryDto[] }) => (
    <div data-testid="eeg-entries-list">
      {entries.map(entry => (
        <div key={entry.id} data-testid={`entry-${entry.id}`}>
          {entry.observationText}
        </div>
      ))}
    </div>
  ),
}))

vi.mock('../components/EegCoverageDashboard', () => ({
  EegCoverageDashboard: ({ compact }: { compact?: boolean }) => (
    <div data-testid="eeg-coverage-dashboard">
      {compact ? 'Compact Coverage' : 'Full Coverage'}
    </div>
  ),
}))

vi.mock('../components/EegEntryForm', () => ({
  EegEntryForm: ({ onClose }: { onClose: () => void }) => (
    <div data-testid="eeg-entry-form">
      <button onClick={onClose}>Close Form</button>
    </div>
  ),
}))

vi.mock('../components/EegExportDialog', () => ({
  EegExportDialog: ({ open, onClose }: { open: boolean; onClose: () => void }) =>
    open ? (
      <div data-testid="eeg-export-dialog">
        <button onClick={onClose}>Close Export</button>
      </div>
    ) : null,
}))

vi.mock('../components/EegDocumentDialog', () => ({
  EegDocumentDialog: ({ open, onClose }: { open: boolean; onClose: () => void }) =>
    open ? (
      <div data-testid="eeg-document-dialog">
        <button onClick={onClose}>Close Document</button>
      </div>
    ) : null,
}))

// Import mocked modules
import { useExercise } from '../../exercises/hooks'
import { useExerciseRole } from '../../auth'
import { useAuth } from '../../../contexts/AuthContext'
import { useEegEntries, useEegCoverage } from '../hooks/useEegEntries'
import { useInjects } from '../../injects/hooks'

// Custom render with routing and query client
const renderWithRouter = (
  ui: React.ReactElement,
  { initialEntries = ['/exercises/ex-1/eeg-entries'] } = {},
) => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={initialEntries}>
        <Routes>
          <Route path="/exercises/:id/eeg-entries" element={ui} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

describe('EegEntriesPage', () => {
  const mockExercise: ExerciseDto = {
    id: 'ex-1',
    name: 'Hurricane Response TTX',
    type: 'TabletopExercise',
    status: ExerciseStatus.Active,
    organizationId: 'org-1',
    description: null,
    startDate: null,
    endDate: null,
    location: null,
    clockState: ExerciseClockState.Paused,
    clockScenarioTime: null,
    clockRealStartTime: null,
    clockElapsedSeconds: 0,
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z',
  }

  const mockEntries: EegEntryDto[] = [
    {
      id: 'entry-1',
      criticalTaskId: 'task-1',
      criticalTask: {
        id: 'task-1',
        taskDescription: 'Activate EOC',
        standard: null,
        capabilityTargetId: 'cap-1',
        capabilityTargetDescription: 'Establish command',
        capabilityTargetSources: null,
        capabilityName: 'Operational Coordination',
      },
      observationText: 'EOC activated promptly',
      rating: 'Performed' as PerformanceRating,
      ratingDisplay: 'P - Performed without Challenges',
      observedAt: '2025-01-15T10:00:00Z',
      recordedAt: '2025-01-15T10:05:00Z',
      evaluatorId: 'user-1',
      evaluatorName: 'Jane Smith',
      triggeringInjectId: null,
      triggeringInject: null,
      createdAt: '2025-01-15T10:05:00Z',
      updatedAt: '2025-01-15T10:05:00Z',
      wasEdited: false,
      updatedBy: null,
    },
    {
      id: 'entry-2',
      criticalTaskId: 'task-2',
      criticalTask: {
        id: 'task-2',
        taskDescription: 'Establish communications',
        standard: null,
        capabilityTargetId: 'cap-1',
        capabilityTargetDescription: 'Establish command',
        capabilityTargetSources: null,
        capabilityName: 'Operational Coordination',
      },
      observationText: 'Radio communications had delays',
      rating: 'SomeChallenges' as PerformanceRating,
      ratingDisplay: 'S - Performed with Some Challenges',
      observedAt: '2025-01-15T11:00:00Z',
      recordedAt: '2025-01-15T11:10:00Z',
      evaluatorId: 'user-2',
      evaluatorName: 'John Doe',
      triggeringInjectId: null,
      triggeringInject: null,
      createdAt: '2025-01-15T11:10:00Z',
      updatedAt: '2025-01-15T11:10:00Z',
      wasEdited: false,
      updatedBy: null,
    },
  ]

  const mockCoverage: EegCoverageDto = {
    totalTasks: 10,
    evaluatedTasks: 2,
    coveragePercentage: 20,
    ratingDistribution: {
      Performed: 1,
      SomeChallenges: 1,
      MajorChallenges: 0,
      UnableToPerform: 0,
    },
    byCapabilityTarget: [],
    unevaluatedTasks: [],
  }

  const mockInjects: InjectDto[] = []

  beforeEach(() => {
    vi.clearAllMocks()

    vi.mocked(useExercise).mockReturnValue({
      exercise: mockExercise,
      loading: false,
      error: null,
      refetch: vi.fn(),
    })

    vi.mocked(useExerciseRole).mockReturnValue({
      can: vi.fn().mockReturnValue(true),
      role: 'Evaluator',
      isLoading: false,
    })

    vi.mocked(useAuth).mockReturnValue({
      user: { id: 'user-1', name: 'Jane Smith', email: 'jane@example.com' },
      isAuthenticated: true,
      login: vi.fn(),
      logout: vi.fn(),
      register: vi.fn(),
      loading: false,
    } as ReturnType<typeof useAuth>)

    vi.mocked(useEegEntries).mockReturnValue({
      eegEntries: mockEntries,
      totalCount: 2,
      page: 1,
      pageSize: 20,
      totalPages: 1,
      loading: false,
      error: null,
      fetchEegEntries: vi.fn(),
      createEntry: vi.fn(),
      isCreating: false,
    })

    vi.mocked(useEegCoverage).mockReturnValue({
      coverage: mockCoverage,
      loading: false,
      error: null,
      refetch: vi.fn(),
    })

    vi.mocked(useInjects).mockReturnValue({
      injects: mockInjects,
      loading: false,
      error: null,
      refetch: vi.fn(),
    } as ReturnType<typeof useInjects>)
  })

  describe('rendering', () => {
    it('renders page header with exercise name', async () => {
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByText('EEG Entries')).toBeInTheDocument()
        expect(
          screen.getByText(/Exercise Evaluation Guide entries for Hurricane Response TTX/),
        ).toBeInTheDocument()
      })
    })

    it('shows loading state while fetching exercise', () => {
      vi.mocked(useExercise).mockReturnValue({
        exercise: null,
        loading: true,
        error: null,
        refetch: vi.fn(),
      })

      renderWithRouter(<EegEntriesPage />)

      expect(screen.getByRole('progressbar')).toBeInTheDocument()
    })

    it('shows error alert when exercise fails to load', async () => {
      vi.mocked(useExercise).mockReturnValue({
        exercise: null,
        loading: false,
        error: 'Exercise not found',
        refetch: vi.fn(),
      })

      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByRole('alert')).toBeInTheDocument()
        expect(screen.getByText(/Exercise not found/)).toBeInTheDocument()
      })
    })
  })

  describe('tab navigation', () => {
    it('defaults to Entries tab', async () => {
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        const entriesTab = screen.getByRole('tab', { name: /Entries/i })
        expect(entriesTab).toHaveAttribute('aria-selected', 'true')
      })
    })

    it('switches to Coverage tab when clicked', async () => {
      const user = userEvent.setup()
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: /Entries/i })).toBeInTheDocument()
      })

      const coverageTab = screen.getByRole('tab', { name: /Coverage/i })
      await user.click(coverageTab)

      await waitFor(() => {
        expect(coverageTab).toHaveAttribute('aria-selected', 'true')
      })
    })

    it('syncs tab state with URL parameter', async () => {
      renderWithRouter(<EegEntriesPage />, {
        initialEntries: ['/exercises/ex-1/eeg-entries?tab=coverage'],
      })

      await waitFor(() => {
        const coverageTab = screen.getByRole('tab', { name: /Coverage/i })
        expect(coverageTab).toHaveAttribute('aria-selected', 'true')
      })
    })

    it('has proper ARIA labels on tabs', async () => {
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByLabelText('Entries tab')).toBeInTheDocument()
        expect(screen.getByLabelText('Coverage tab')).toBeInTheDocument()
      })
    })

    it('shows tabpanel role for active content', async () => {
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        const tabpanels = screen.getAllByRole('tabpanel')
        expect(tabpanels).toHaveLength(1)
      })
    })
  })

  describe('Entries tab content', () => {
    it('renders entries list when on Entries tab', async () => {
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByTestId('eeg-entries-list')).toBeInTheDocument()
        expect(screen.getByTestId('entry-entry-1')).toBeInTheDocument()
        expect(screen.getByTestId('entry-entry-2')).toBeInTheDocument()
      })
    })

    it('shows compact coverage summary on Entries tab', async () => {
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        const dashboard = screen.getByTestId('eeg-coverage-dashboard')
        expect(dashboard).toHaveTextContent('Compact Coverage')
      })
    })

    it('shows Add Entry button only on Entries tab with permissions', async () => {
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Add Entry/i })).toBeInTheDocument()
      })

      // Switch to Coverage tab
      const user = userEvent.setup()
      const coverageTab = screen.getByRole('tab', { name: /Coverage/i })
      await user.click(coverageTab)

      await waitFor(() => {
        expect(screen.queryByRole('button', { name: /Add Entry/i })).not.toBeInTheDocument()
      })
    })

    it('hides Add Entry button when user lacks permissions', async () => {
      vi.mocked(useExerciseRole).mockReturnValue({
        can: vi.fn(permission => permission !== 'add_observation'),
        role: 'Observer',
        isLoading: false,
      })

      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.queryByRole('button', { name: /Add Entry/i })).not.toBeInTheDocument()
      })
    })

    it('shows empty state when no entries exist', async () => {
      vi.mocked(useEegEntries).mockReturnValue({
        eegEntries: [],
        totalCount: 0,
        page: 1,
        pageSize: 20,
        totalPages: 1,
        loading: false,
        error: null,
        fetchEegEntries: vi.fn(),
        createEntry: vi.fn(),
        isCreating: false,
      })

      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByText(/No EEG entries yet/i)).toBeInTheDocument()
      })
    })

    it('shows no tasks alert when no critical tasks configured', async () => {
      vi.mocked(useEegCoverage).mockReturnValue({
        coverage: { ...mockCoverage, totalTasks: 0 },
        loading: false,
        error: null,
        refetch: vi.fn(),
      })

      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByText(/No critical tasks configured/i)).toBeInTheDocument()
      })
    })
  })

  describe('Coverage tab content', () => {
    it('renders full coverage dashboard when on Coverage tab', async () => {
      const user = userEvent.setup()
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: /Coverage/i })).toBeInTheDocument()
      })

      const coverageTab = screen.getByRole('tab', { name: /Coverage/i })
      await user.click(coverageTab)

      await waitFor(() => {
        const dashboard = screen.getByTestId('eeg-coverage-dashboard')
        expect(dashboard).toHaveTextContent('Full Coverage')
      })
    })

    it('shows Generate EEG button only on Coverage tab', async () => {
      const user = userEvent.setup()
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.queryByRole('button', { name: /Generate EEG/i })).not.toBeInTheDocument()
      })

      const coverageTab = screen.getByRole('tab', { name: /Coverage/i })
      await user.click(coverageTab)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Generate EEG/i })).toBeInTheDocument()
      })
    })
  })

  describe('filtering', () => {
    it('displays search input', async () => {
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByPlaceholderText(/Search entries/i)).toBeInTheDocument()
      })
    })

    it('filters entries by search query', async () => {
      const user = userEvent.setup()
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByTestId('entry-entry-1')).toBeInTheDocument()
        expect(screen.getByTestId('entry-entry-2')).toBeInTheDocument()
      })

      const searchInput = screen.getByPlaceholderText(/Search entries/i)
      await user.type(searchInput, 'promptly')

      await waitFor(() => {
        expect(screen.getByTestId('entry-entry-1')).toBeInTheDocument()
        expect(screen.queryByTestId('entry-entry-2')).not.toBeInTheDocument()
      })
    })

    it('displays rating filter dropdown', async () => {
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByLabelText(/Rating/i)).toBeInTheDocument()
      })
    })

    it('filters entries by rating', async () => {
      const user = userEvent.setup()
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByTestId('entry-entry-1')).toBeInTheDocument()
        expect(screen.getByTestId('entry-entry-2')).toBeInTheDocument()
      })

      const ratingFilter = screen.getByLabelText(/Rating/i)
      await user.click(ratingFilter)

      const performedOption = screen.getByRole('option', { name: /P - Performed/i })
      await user.click(performedOption)

      await waitFor(() => {
        expect(screen.getByTestId('entry-entry-1')).toBeInTheDocument()
        expect(screen.queryByTestId('entry-entry-2')).not.toBeInTheDocument()
      })
    })

    it('shows filter results count', async () => {
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByText('2 of 2 entries')).toBeInTheDocument()
      })
    })

    it('updates results count when filtering', async () => {
      const user = userEvent.setup()
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByText('2 of 2 entries')).toBeInTheDocument()
      })

      const searchInput = screen.getByPlaceholderText(/Search entries/i)
      await user.type(searchInput, 'promptly')

      await waitFor(() => {
        expect(screen.getByText('1 of 2 entries')).toBeInTheDocument()
      })
    })

    it('shows active filter chips', async () => {
      const user = userEvent.setup()
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByLabelText(/Rating/i)).toBeInTheDocument()
      })

      const ratingFilter = screen.getByLabelText(/Rating/i)
      await user.click(ratingFilter)

      const performedOption = screen.getByRole('option', { name: /P - Performed/i })
      await user.click(performedOption)

      await waitFor(() => {
        expect(screen.getByText(/Rating: P/i)).toBeInTheDocument()
      })
    })

    it('shows Clear Filters button when filters are active', async () => {
      const user = userEvent.setup()
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.queryByRole('button', { name: /Clear Filters/i })).not.toBeInTheDocument()
      })

      const searchInput = screen.getByPlaceholderText(/Search entries/i)
      await user.type(searchInput, 'test')

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Clear Filters/i })).toBeInTheDocument()
      })
    })

    it('clears all filters when Clear Filters clicked', async () => {
      const user = userEvent.setup()
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByPlaceholderText(/Search entries/i)).toBeInTheDocument()
      })

      const searchInput = screen.getByPlaceholderText(/Search entries/i)
      await user.type(searchInput, 'test')

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Clear Filters/i })).toBeInTheDocument()
      })

      const clearButton = screen.getByRole('button', { name: /Clear Filters/i })
      await user.click(clearButton)

      await waitFor(() => {
        expect(searchInput).toHaveValue('')
      })
    })

    it('has live region for results count accessibility', async () => {
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        const resultsCount = screen.getByText('2 of 2 entries')
        expect(resultsCount).toHaveAttribute('aria-live', 'polite')
        expect(resultsCount).toHaveAttribute('aria-atomic', 'true')
      })
    })
  })

  describe('create entry dialog', () => {
    it('opens create dialog when Add Entry clicked', async () => {
      const user = userEvent.setup()
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Add Entry/i })).toBeInTheDocument()
      })

      const addButton = screen.getByRole('button', { name: /Add Entry/i })
      await user.click(addButton)

      await waitFor(() => {
        expect(screen.getByTestId('eeg-entry-form')).toBeInTheDocument()
      })
    })

    it('closes create dialog when form close is triggered', async () => {
      const user = userEvent.setup()
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Add Entry/i })).toBeInTheDocument()
      })

      const addButton = screen.getByRole('button', { name: /Add Entry/i })
      await user.click(addButton)

      await waitFor(() => {
        expect(screen.getByTestId('eeg-entry-form')).toBeInTheDocument()
      })

      const closeButton = screen.getByRole('button', { name: /Close Form/i })
      await user.click(closeButton)

      await waitFor(() => {
        expect(screen.queryByTestId('eeg-entry-form')).not.toBeInTheDocument()
      })
    })
  })

  describe('export functionality', () => {
    it('shows Export button when user has permissions', async () => {
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Export/i })).toBeInTheDocument()
      })
    })

    it('hides Export button when user lacks permissions', async () => {
      vi.mocked(useExerciseRole).mockReturnValue({
        can: vi.fn(permission => permission !== 'delete_observation'),
        role: 'Evaluator',
        isLoading: false,
      })

      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.queryByRole('button', { name: /Export/i })).not.toBeInTheDocument()
      })
    })

    it('opens export dialog when Export clicked', async () => {
      const user = userEvent.setup()
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Export/i })).toBeInTheDocument()
      })

      const exportButton = screen.getByRole('button', { name: /Export/i })
      await user.click(exportButton)

      await waitFor(() => {
        expect(screen.getByTestId('eeg-export-dialog')).toBeInTheDocument()
      })
    })
  })

  describe('document generation', () => {
    it('opens document dialog when Generate EEG clicked', async () => {
      const user = userEvent.setup()
      renderWithRouter(<EegEntriesPage />)

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: /Coverage/i })).toBeInTheDocument()
      })

      const coverageTab = screen.getByRole('tab', { name: /Coverage/i })
      await user.click(coverageTab)

      await waitFor(() => {
        expect(screen.getByRole('button', { name: /Generate EEG/i })).toBeInTheDocument()
      })

      const generateButton = screen.getByRole('button', { name: /Generate EEG/i })
      await user.click(generateButton)

      await waitFor(() => {
        expect(screen.getByTestId('eeg-document-dialog')).toBeInTheDocument()
      })
    })
  })
})
