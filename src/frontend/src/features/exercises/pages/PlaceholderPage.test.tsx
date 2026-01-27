/**
 * PlaceholderPage Tests
 *
 * Tests for the temporary placeholder page shown for features under development.
 *
 * Test Coverage:
 * - Component rendering with required props
 * - Default vs custom descriptions
 * - Exercise name display
 * - Icon rendering
 * - Breadcrumb integration
 * - Pre-configured placeholder pages
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { render } from '../../../test/test-utils'
import {
  PlaceholderPage,
  ObservationsPlaceholderPage,
  ParticipantsPlaceholderPage,
  MetricsPlaceholderPage,
  SettingsPlaceholderPage,
  ReportsPlaceholderPage,
  TemplatesPlaceholderPage,
} from './PlaceholderPage'

// Mock hooks
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useParams: vi.fn(),
  }
})

vi.mock('../hooks', () => ({
  useExercise: vi.fn(),
}))

vi.mock('@/core/contexts', () => ({
  useBreadcrumbs: vi.fn(),
}))

describe('PlaceholderPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Basic Rendering', () => {
    it('renders feature name as heading', async () => {
      const { useParams } = await import('react-router-dom')
      const { useExercise } = await import('../hooks')
      const { useBreadcrumbs } = await import('@/core/contexts')

      vi.mocked(useParams).mockReturnValue({ id: 'exercise-123' })
      vi.mocked(useExercise).mockReturnValue({
        exercise: null,
        loading: false,
        error: null,
        fetchExercise: vi.fn(),
        updateExercise: vi.fn(),
        isUpdating: false,
      })
      vi.mocked(useBreadcrumbs).mockReturnValue()

      render(
        <MemoryRouter>
          <PlaceholderPage featureName="Test Feature" />
        </MemoryRouter>,
      )

      expect(screen.getByRole('heading', { name: /test feature/i })).toBeInTheDocument()
    })

    it('renders custom description when provided', async () => {
      const { useParams } = await import('react-router-dom')
      const { useExercise } = await import('../hooks')
      const { useBreadcrumbs } = await import('@/core/contexts')

      vi.mocked(useParams).mockReturnValue({ id: 'exercise-123' })
      vi.mocked(useExercise).mockReturnValue({
        exercise: null,
        loading: false,
        error: null,
        fetchExercise: vi.fn(),
        updateExercise: vi.fn(),
        isUpdating: false,
      })
      vi.mocked(useBreadcrumbs).mockReturnValue()

      render(
        <MemoryRouter>
          <PlaceholderPage
            featureName="Test Feature"
            description="Custom description for this feature."
          />
        </MemoryRouter>,
      )

      expect(screen.getByText('Custom description for this feature.')).toBeInTheDocument()
    })

    it('shows default description when no description provided', async () => {
      const { useParams } = await import('react-router-dom')
      const { useExercise } = await import('../hooks')
      const { useBreadcrumbs } = await import('@/core/contexts')

      vi.mocked(useParams).mockReturnValue({ id: 'exercise-123' })
      vi.mocked(useExercise).mockReturnValue({
        exercise: null,
        loading: false,
        error: null,
        fetchExercise: vi.fn(),
        updateExercise: vi.fn(),
        isUpdating: false,
      })
      vi.mocked(useBreadcrumbs).mockReturnValue()

      render(
        <MemoryRouter>
          <PlaceholderPage featureName="Test Feature" />
        </MemoryRouter>,
      )

      expect(screen.getByText('This feature is coming soon.')).toBeInTheDocument()
    })

    it('shows hard hat icon', async () => {
      const { useParams } = await import('react-router-dom')
      const { useExercise } = await import('../hooks')
      const { useBreadcrumbs } = await import('@/core/contexts')

      vi.mocked(useParams).mockReturnValue({ id: 'exercise-123' })
      vi.mocked(useExercise).mockReturnValue({
        exercise: null,
        loading: false,
        error: null,
        fetchExercise: vi.fn(),
        updateExercise: vi.fn(),
        isUpdating: false,
      })
      vi.mocked(useBreadcrumbs).mockReturnValue()

      const { container } = render(
        <MemoryRouter>
          <PlaceholderPage featureName="Test Feature" />
        </MemoryRouter>,
      )

      // FontAwesome icons render as SVG elements with class attribute
      const icon = container.querySelector('svg.svg-inline--fa')
      expect(icon).toBeInTheDocument()
    })
  })

  describe('Exercise Integration', () => {
    it('shows exercise name when exercise is loaded', async () => {
      const { useParams } = await import('react-router-dom')
      const { useExercise } = await import('../hooks')
      const { useBreadcrumbs } = await import('@/core/contexts')

      vi.mocked(useParams).mockReturnValue({ id: 'exercise-123' })
      vi.mocked(useExercise).mockReturnValue({
        exercise: {
          id: 'exercise-123',
          name: 'Emergency Response Exercise',
          description: 'Test exercise description',
          exerciseType: 'TTX',
          status: 'Draft',
          isPracticeMode: false,
          scheduledDate: '2025-02-01',
          startTime: '09:00:00',
          endTime: '17:00:00',
          timeZoneId: 'America/New_York',
          location: 'Test Location',
          organizationId: 'org-123',
          activeMselId: null,
          deliveryMode: 'ClockDriven',
          timelineMode: 'RealTime',
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
        },
        loading: false,
        error: null,
        fetchExercise: vi.fn(),
        updateExercise: vi.fn(),
        isUpdating: false,
      })
      vi.mocked(useBreadcrumbs).mockReturnValue()

      render(
        <MemoryRouter>
          <PlaceholderPage featureName="Test Feature" />
        </MemoryRouter>,
      )

      expect(screen.getByText(/Exercise: Emergency Response Exercise/i)).toBeInTheDocument()
    })

    it('does not show exercise name when exercise is not loaded', async () => {
      const { useParams } = await import('react-router-dom')
      const { useExercise } = await import('../hooks')
      const { useBreadcrumbs } = await import('@/core/contexts')

      vi.mocked(useParams).mockReturnValue({ id: 'exercise-123' })
      vi.mocked(useExercise).mockReturnValue({
        exercise: null,
        loading: false,
        error: null,
        fetchExercise: vi.fn(),
        updateExercise: vi.fn(),
        isUpdating: false,
      })
      vi.mocked(useBreadcrumbs).mockReturnValue()

      render(
        <MemoryRouter>
          <PlaceholderPage featureName="Test Feature" />
        </MemoryRouter>,
      )

      expect(screen.queryByText(/Exercise:/i)).not.toBeInTheDocument()
    })
  })

  describe('Breadcrumbs Integration', () => {
    it('sets breadcrumbs correctly with exercise name', async () => {
      const { useParams } = await import('react-router-dom')
      const { useExercise } = await import('../hooks')
      const { useBreadcrumbs } = await import('@/core/contexts')

      const mockUseBreadcrumbs = vi.fn()
      vi.mocked(useParams).mockReturnValue({ id: 'exercise-123' })
      vi.mocked(useExercise).mockReturnValue({
        exercise: {
          id: 'exercise-123',
          name: 'Emergency Response Exercise',
          description: 'Test exercise description',
          exerciseType: 'TTX',
          status: 'Draft',
          isPracticeMode: false,
          scheduledDate: '2025-02-01',
          startTime: '09:00:00',
          endTime: '17:00:00',
          timeZoneId: 'America/New_York',
          location: 'Test Location',
          organizationId: 'org-123',
          activeMselId: null,
          deliveryMode: 'ClockDriven',
          timelineMode: 'RealTime',
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
        },
        loading: false,
        error: null,
        fetchExercise: vi.fn(),
        updateExercise: vi.fn(),
        isUpdating: false,
      })
      vi.mocked(useBreadcrumbs).mockImplementation(mockUseBreadcrumbs)

      render(
        <MemoryRouter>
          <PlaceholderPage featureName="Test Feature" />
        </MemoryRouter>,
      )

      expect(mockUseBreadcrumbs).toHaveBeenCalledWith(
        expect.arrayContaining([
          expect.objectContaining({ label: 'Home', path: '/' }),
          expect.objectContaining({ label: 'Exercises', path: '/exercises' }),
          expect.objectContaining({ label: 'Emergency Response Exercise', path: '/exercises/exercise-123' }),
          expect.objectContaining({ label: 'Test Feature' }),
        ]),
      )
    })

    it('sets breadcrumbs to undefined when exercise is not loaded', async () => {
      const { useParams } = await import('react-router-dom')
      const { useExercise } = await import('../hooks')
      const { useBreadcrumbs } = await import('@/core/contexts')

      const mockUseBreadcrumbs = vi.fn()
      vi.mocked(useParams).mockReturnValue({ id: 'exercise-123' })
      vi.mocked(useExercise).mockReturnValue({
        exercise: null,
        loading: false,
        error: null,
        fetchExercise: vi.fn(),
        updateExercise: vi.fn(),
        isUpdating: false,
      })
      vi.mocked(useBreadcrumbs).mockImplementation(mockUseBreadcrumbs)

      render(
        <MemoryRouter>
          <PlaceholderPage featureName="Test Feature" />
        </MemoryRouter>,
      )

      expect(mockUseBreadcrumbs).toHaveBeenCalledWith(undefined)
    })
  })

  describe('Pre-configured Placeholder Pages', () => {
    beforeEach(async () => {
      const { useParams } = await import('react-router-dom')
      const { useExercise } = await import('../hooks')
      const { useBreadcrumbs } = await import('@/core/contexts')

      vi.mocked(useParams).mockReturnValue({ id: 'exercise-123' })
      vi.mocked(useExercise).mockReturnValue({
        exercise: null,
        loading: false,
        error: null,
        fetchExercise: vi.fn(),
        updateExercise: vi.fn(),
        isUpdating: false,
      })
      vi.mocked(useBreadcrumbs).mockReturnValue()
    })

    it('ObservationsPlaceholderPage renders with correct feature name and description', () => {
      render(
        <MemoryRouter>
          <ObservationsPlaceholderPage />
        </MemoryRouter>,
      )

      expect(screen.getByRole('heading', { name: /observations/i })).toBeInTheDocument()
      expect(screen.getByText(/view and record evaluator observations during exercise conduct/i)).toBeInTheDocument()
    })

    it('ParticipantsPlaceholderPage renders with correct feature name and description', () => {
      render(
        <MemoryRouter>
          <ParticipantsPlaceholderPage />
        </MemoryRouter>,
      )

      expect(screen.getByRole('heading', { name: /participants/i })).toBeInTheDocument()
      expect(screen.getByText(/manage exercise participants and role assignments/i)).toBeInTheDocument()
    })

    it('MetricsPlaceholderPage renders with correct feature name and description', () => {
      render(
        <MemoryRouter>
          <MetricsPlaceholderPage />
        </MemoryRouter>,
      )

      expect(screen.getByRole('heading', { name: /metrics/i })).toBeInTheDocument()
      expect(screen.getByText(/view exercise performance metrics and analytics/i)).toBeInTheDocument()
    })

    it('SettingsPlaceholderPage renders with correct feature name and description', () => {
      render(
        <MemoryRouter>
          <SettingsPlaceholderPage />
        </MemoryRouter>,
      )

      expect(screen.getByRole('heading', { name: /settings/i })).toBeInTheDocument()
      expect(screen.getByText(/configure exercise settings and preferences/i)).toBeInTheDocument()
    })

    it('ReportsPlaceholderPage renders with correct feature name and description', () => {
      render(
        <MemoryRouter>
          <ReportsPlaceholderPage />
        </MemoryRouter>,
      )

      expect(screen.getByRole('heading', { name: /reports/i })).toBeInTheDocument()
      expect(screen.getByText(/generate and view exercise reports and after-action documentation/i)).toBeInTheDocument()
    })

    it('TemplatesPlaceholderPage renders with correct feature name and description', () => {
      render(
        <MemoryRouter>
          <TemplatesPlaceholderPage />
        </MemoryRouter>,
      )

      expect(screen.getByRole('heading', { name: /templates/i })).toBeInTheDocument()
      expect(screen.getByText(/manage inject templates and exercise blueprints/i)).toBeInTheDocument()
    })
  })

  describe('URL Parameter Integration', () => {
    it('extracts exercise ID from URL params', async () => {
      const { useParams } = await import('react-router-dom')
      const { useExercise } = await import('../hooks')
      const { useBreadcrumbs } = await import('@/core/contexts')

      const mockUseExercise = vi.fn().mockReturnValue({
        exercise: null,
        loading: false,
        error: null,
        fetchExercise: vi.fn(),
        updateExercise: vi.fn(),
        isUpdating: false,
      })

      vi.mocked(useParams).mockReturnValue({ id: 'exercise-456' })
      vi.mocked(useExercise).mockImplementation(mockUseExercise)
      vi.mocked(useBreadcrumbs).mockReturnValue()

      render(
        <MemoryRouter initialEntries={['/exercises/exercise-456/placeholder']}>
          <Routes>
            <Route path="/exercises/:id/placeholder" element={<PlaceholderPage featureName="Test" />} />
          </Routes>
        </MemoryRouter>,
      )

      expect(mockUseExercise).toHaveBeenCalledWith('exercise-456')
    })
  })
})
