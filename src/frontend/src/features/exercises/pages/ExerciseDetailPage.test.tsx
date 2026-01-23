/* eslint-disable @typescript-eslint/no-explicit-any */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, waitFor, render } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ThemeProvider } from '@mui/material/styles'
import { ExerciseDetailPage } from './ExerciseDetailPage'
import { cobraTheme } from '../../../theme/cobraTheme'
import { ExerciseStatus, ExerciseType, DeliveryMode, TimelineMode } from '../../../types'
import type { ExerciseDto } from '../types'

// Mock the hooks
vi.mock('../hooks', () => ({
  useExercise: vi.fn(),
  useSetupProgress: vi.fn(),
  useDuplicateExercise: vi.fn(),
  useExerciseStatus: vi.fn(),
  useMselSummary: vi.fn(),
  useExerciseTransitions: vi.fn(() => ({ availableTransitions: [] })),
  useExerciseParticipants: vi.fn(() => ({ participants: [], isLoading: false })),
}))

vi.mock('../../objectives', () => ({
  ObjectiveList: () => <div>Objectives List</div>,
}))

vi.mock('./ExerciseParticipantsPage', () => ({
  ExerciseParticipantsPage: () => <div>Participants Page</div>,
}))

vi.mock('../components', async () => {
  const actual = await vi.importActual('../components')
  return {
    ...actual,
    ExerciseStatusActions: () => null,
  }
})

vi.mock('../../../shared/hooks', () => ({
  usePermissions: vi.fn(() => ({ canManage: true })),
  useUnsavedChangesWarning: vi.fn(() => ({
    UnsavedChangesDialog: () => null,
  })),
}))

vi.mock('../../../core/contexts', () => ({
  useBreadcrumbs: vi.fn(),
}))

vi.mock('@/features/auth', () => ({
  EffectiveRoleBadge: () => <div data-testid="role-badge">Controller</div>,
  PermissionGate: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  useExerciseRole: vi.fn(() => ({
    effectiveRole: 'Controller',
    systemRole: 'User',
    exerciseRole: 'Controller',
    can: vi.fn(() => true),
    isLoading: false,
  })),
}))

// Helper function to render with all necessary providers
const renderExerciseDetailPage = (exerciseId = 'exercise-123') => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={cobraTheme}>
        <MemoryRouter initialEntries={[`/exercises/${exerciseId}`]}>
          <Routes>
            <Route path="/exercises/:id" element={<ExerciseDetailPage />} />
          </Routes>
        </MemoryRouter>
      </ThemeProvider>
    </QueryClientProvider>,
  )
}

const mockExercise: ExerciseDto = {
  id: 'exercise-123',
  name: 'Test Exercise',
  description: 'Test description',
  exerciseType: ExerciseType.TTX,
  status: ExerciseStatus.Draft,
  isPracticeMode: false,
  scheduledDate: '2025-02-01',
  startTime: '09:00:00',
  endTime: '17:00:00',
  timeZoneId: 'America/New_York',
  location: 'Test Location',
  organizationId: 'org-123',
  activeMselId: null,
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

describe('ExerciseDetailPage - Tabs Integration', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('displays Details tab by default', async () => {
    const { useExercise, useSetupProgress, useDuplicateExercise, useExerciseStatus, useMselSummary } = await import('../hooks')

    vi.mocked(useExercise).mockReturnValue({
      exercise: mockExercise,
      loading: false,
      error: null,
      updateExercise: vi.fn(),
    } as any)

    vi.mocked(useSetupProgress).mockReturnValue({
      data: null,
      isLoading: false,
      error: null,
    } as any)

    vi.mocked(useDuplicateExercise).mockReturnValue({
      duplicate: vi.fn(),
      isDuplicating: false,
    } as any)

    vi.mocked(useExerciseStatus).mockReturnValue({
      archive: vi.fn(),
      isTransitioning: false,
    } as any)

    vi.mocked(useMselSummary).mockReturnValue({
      data: null,
    } as any)

    renderExerciseDetailPage()

    // Should show Details tab as active
    await waitFor(() => {
      expect(screen.getByRole('tab', { name: /details/i })).toHaveAttribute('aria-selected', 'true')
    })

    // Should show exercise details content
    expect(screen.getByText('Exercise Details')).toBeInTheDocument()
  })

  it('displays Participants tab when clicked', async () => {
    const { useExercise, useSetupProgress, useDuplicateExercise, useExerciseStatus, useMselSummary } = await import('../hooks')

    vi.mocked(useExercise).mockReturnValue({
      exercise: mockExercise,
      loading: false,
      error: null,
      updateExercise: vi.fn(),
    } as any)

    vi.mocked(useSetupProgress).mockReturnValue({
      data: null,
      isLoading: false,
      error: null,
    } as any)

    vi.mocked(useDuplicateExercise).mockReturnValue({
      duplicate: vi.fn(),
      isDuplicating: false,
    } as any)

    vi.mocked(useExerciseStatus).mockReturnValue({
      archive: vi.fn(),
      isTransitioning: false,
    } as any)

    vi.mocked(useMselSummary).mockReturnValue({
      data: null,
    } as any)

    const user = userEvent.setup()

    renderExerciseDetailPage()

    // Click Participants tab
    const participantsTab = screen.getByRole('tab', { name: /participants/i })
    await user.click(participantsTab)

    // Should show Participants tab as active
    await waitFor(() => {
      expect(participantsTab).toHaveAttribute('aria-selected', 'true')
    })

    // Should show participants content
    expect(screen.getByText('Participants Page')).toBeInTheDocument()
  })

  it('displays Objectives tab when clicked', async () => {
    const { useExercise, useSetupProgress, useDuplicateExercise, useExerciseStatus, useMselSummary } = await import('../hooks')

    vi.mocked(useExercise).mockReturnValue({
      exercise: mockExercise,
      loading: false,
      error: null,
      updateExercise: vi.fn(),
    } as any)

    vi.mocked(useSetupProgress).mockReturnValue({
      data: null,
      isLoading: false,
      error: null,
    } as any)

    vi.mocked(useDuplicateExercise).mockReturnValue({
      duplicate: vi.fn(),
      isDuplicating: false,
    } as any)

    vi.mocked(useExerciseStatus).mockReturnValue({
      archive: vi.fn(),
      isTransitioning: false,
    } as any)

    vi.mocked(useMselSummary).mockReturnValue({
      data: null,
    } as any)

    const user = userEvent.setup()

    renderExerciseDetailPage()

    // Click Objectives tab
    const objectivesTab = screen.getByRole('tab', { name: /objectives/i })
    await user.click(objectivesTab)

    // Should show Objectives tab as active
    await waitFor(() => {
      expect(objectivesTab).toHaveAttribute('aria-selected', 'true')
    })

    // Should show objectives content
    expect(screen.getByText('Objectives List')).toBeInTheDocument()
  })

  it('persists tab selection when navigating within tabs', async () => {
    const { useExercise, useSetupProgress, useDuplicateExercise, useExerciseStatus, useMselSummary } = await import('../hooks')

    vi.mocked(useExercise).mockReturnValue({
      exercise: mockExercise,
      loading: false,
      error: null,
      updateExercise: vi.fn(),
    } as any)

    vi.mocked(useSetupProgress).mockReturnValue({
      data: null,
      isLoading: false,
      error: null,
    } as any)

    vi.mocked(useDuplicateExercise).mockReturnValue({
      duplicate: vi.fn(),
      isDuplicating: false,
    } as any)

    vi.mocked(useExerciseStatus).mockReturnValue({
      archive: vi.fn(),
      isTransitioning: false,
    } as any)

    vi.mocked(useMselSummary).mockReturnValue({
      data: null,
    } as any)

    const user = userEvent.setup()

    renderExerciseDetailPage()

    // Click Participants tab
    await user.click(screen.getByRole('tab', { name: /participants/i }))

    await waitFor(() => {
      expect(screen.getByRole('tab', { name: /participants/i })).toHaveAttribute('aria-selected', 'true')
    })

    // Click Details tab to return
    await user.click(screen.getByRole('tab', { name: /details/i }))

    await waitFor(() => {
      expect(screen.getByRole('tab', { name: /details/i })).toHaveAttribute('aria-selected', 'true')
    })
  })
})

describe('ExerciseDetailPage - Role Badge Integration', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('displays EffectiveRoleBadge in the header', async () => {
    const { useExercise, useSetupProgress, useDuplicateExercise, useExerciseStatus, useMselSummary } = await import('../hooks')

    vi.mocked(useExercise).mockReturnValue({
      exercise: mockExercise,
      loading: false,
      error: null,
      updateExercise: vi.fn(),
    } as any)

    vi.mocked(useSetupProgress).mockReturnValue({
      data: null,
      isLoading: false,
      error: null,
    } as any)

    vi.mocked(useDuplicateExercise).mockReturnValue({
      duplicate: vi.fn(),
      isDuplicating: false,
    } as any)

    vi.mocked(useExerciseStatus).mockReturnValue({
      archive: vi.fn(),
      isTransitioning: false,
    } as any)

    vi.mocked(useMselSummary).mockReturnValue({
      data: null,
    } as any)

    renderExerciseDetailPage()

    // Should display the role badge in the header
    await waitFor(() => {
      expect(screen.getByTestId('role-badge')).toBeInTheDocument()
    })
  })
})
