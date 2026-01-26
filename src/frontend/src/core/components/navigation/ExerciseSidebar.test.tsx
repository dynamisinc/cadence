/**
 * ExerciseSidebar Tests
 *
 * Tests for the exercise-specific sidebar component.
 */

import { render, screen, fireEvent } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import { ThemeProvider } from '@mui/material/styles'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ExerciseSidebar } from './ExerciseSidebar'
import { cobraTheme } from '@/theme/cobraTheme'
import { ExerciseStatus, HseepRole, ExerciseClockState } from '@/types'
import type { ExerciseNavigationData } from '@/shared/contexts'

// Mock the hooks
const mockExitExercise = vi.fn()
const mockCurrentExercise: ExerciseNavigationData = {
  id: 'exercise-123',
  name: 'Hurricane Response 2025',
  status: ExerciseStatus.Active,
  userRole: HseepRole.Controller,
}

vi.mock('@/shared/contexts', () => ({
  useExerciseNavigation: () => ({
    currentExercise: mockCurrentExercise,
    exitExercise: mockExitExercise,
    isInExerciseContext: true,
  }),
}))

vi.mock('@/features/exercise-clock', () => ({
  useExerciseClock: () => ({
    clockState: { state: ExerciseClockState.Running },
    displayTime: '00:32:15',
    loading: false,
  }),
}))

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useLocation: () => ({ pathname: '/exercises/exercise-123/conduct' }),
  }
})

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: false },
  },
})

const renderWithProviders = (ui: React.ReactElement) => {
  return render(
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={cobraTheme}>
        <MemoryRouter initialEntries={['/exercises/exercise-123/conduct']}>
          {ui}
        </MemoryRouter>
      </ThemeProvider>
    </QueryClientProvider>,
  )
}

describe('ExerciseSidebar', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  const defaultProps = {
    open: true,
    onToggle: vi.fn(),
    mobileOpen: false,
    onMobileClose: vi.fn(),
  }

  describe('rendering', () => {
    it('renders exercise name', () => {
      renderWithProviders(<ExerciseSidebar {...defaultProps} />)

      expect(screen.getByTestId('exercise-sidebar-name')).toHaveTextContent(
        'Hurricane Response 2025',
      )
    })

    it('renders clock display', () => {
      renderWithProviders(<ExerciseSidebar {...defaultProps} />)

      expect(screen.getByTestId('exercise-sidebar-clock')).toHaveTextContent('00:32:15')
    })

    it('renders status badge', () => {
      renderWithProviders(<ExerciseSidebar {...defaultProps} />)

      expect(screen.getByTestId('exercise-sidebar-status')).toBeInTheDocument()
    })

    it('renders back button', () => {
      renderWithProviders(<ExerciseSidebar {...defaultProps} />)

      expect(screen.getByTestId('exercise-sidebar-back')).toBeInTheDocument()
    })
  })

  describe('navigation', () => {
    it('calls exitExercise and navigates when back button clicked', () => {
      renderWithProviders(<ExerciseSidebar {...defaultProps} />)

      fireEvent.click(screen.getByTestId('exercise-sidebar-back'))

      expect(mockExitExercise).toHaveBeenCalled()
      expect(mockNavigate).toHaveBeenCalledWith('/exercises')
    })

    it('navigates to menu item path when clicked', () => {
      renderWithProviders(<ExerciseSidebar {...defaultProps} />)

      // Controller should see Hub, MSEL, and Inject Queue
      const hubItem = screen.getByTestId('exercise-nav-item-hub')
      fireEvent.click(hubItem)

      expect(mockNavigate).toHaveBeenCalledWith('/exercises/exercise-123')
    })
  })

  describe('role-based menu filtering', () => {
    it('shows Hub, MSEL, and Inject Queue for Controller', () => {
      renderWithProviders(<ExerciseSidebar {...defaultProps} />)

      expect(screen.getByTestId('exercise-nav-item-hub')).toBeInTheDocument()
      expect(screen.getByTestId('exercise-nav-item-msel')).toBeInTheDocument()
      expect(screen.getByTestId('exercise-nav-item-inject-queue')).toBeInTheDocument()
      expect(screen.queryByTestId('exercise-nav-item-observations')).not.toBeInTheDocument()
    })
  })

  describe('collapsed state', () => {
    it('hides exercise name when collapsed', () => {
      renderWithProviders(<ExerciseSidebar {...defaultProps} open={false} />)

      expect(screen.queryByTestId('exercise-sidebar-name')).not.toBeInTheDocument()
    })

    it('still shows clock when collapsed', () => {
      renderWithProviders(<ExerciseSidebar {...defaultProps} open={false} />)

      expect(screen.getByTestId('exercise-sidebar-clock')).toBeInTheDocument()
    })
  })

  describe('toggle functionality', () => {
    it('calls onToggle when toggle button clicked', () => {
      const onToggle = vi.fn()
      renderWithProviders(<ExerciseSidebar {...defaultProps} onToggle={onToggle} />)

      fireEvent.click(screen.getByTestId('sidebar-toggle'))

      expect(onToggle).toHaveBeenCalled()
    })
  })
})
