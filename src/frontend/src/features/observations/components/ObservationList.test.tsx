/**
 * ObservationList Component Tests
 *
 * Tests for the observation list display with inject linking.
 */

import { describe, it, expect, vi } from 'vitest'
import { screen, fireEvent } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { ObservationList } from './ObservationList'
import { ObservationRating } from '../../../types'
import type { ObservationDto } from '../types'

// Helper to create mock observation
const createMockObservation = (
  overrides: Partial<ObservationDto> = {},
): ObservationDto => ({
  id: 'obs-1',
  exerciseId: 'ex-1',
  injectId: null,
  objectiveId: null,
  content: 'Test observation content',
  rating: ObservationRating.Satisfactory,
  recommendation: null,
  observedAt: '2025-01-01T10:00:00Z',
  location: null,
  createdAt: '2025-01-01T10:00:00Z',
  updatedAt: '2025-01-01T10:00:00Z',
  createdBy: 'user-1',
  createdByName: 'Test User',
  injectTitle: null,
  injectNumber: null,
  capabilities: [],
  ...overrides,
})

describe('ObservationList', () => {
  describe('Inject Reference Display', () => {
    it('shows "Re: #X Title" format when observation has linked inject', () => {
      const observations = [
        createMockObservation({
          id: 'obs-1',
          injectId: 'inject-1',
          injectTitle: 'Media Inquiry',
          injectNumber: 3,
        }),
      ]

      render(<ObservationList observations={observations} />)

      expect(screen.getByText(/Re: #3 Media Inquiry/)).toBeInTheDocument()
    })

    it('shows "General observation" when no inject linked', () => {
      const observations = [
        createMockObservation({
          id: 'obs-1',
          injectId: null,
          injectTitle: null,
        }),
      ]

      render(<ObservationList observations={observations} />)

      expect(screen.getByText('General observation')).toBeInTheDocument()
    })

    it('makes inject reference clickable', () => {
      const onInjectClick = vi.fn()
      const observations = [
        createMockObservation({
          id: 'obs-1',
          injectId: 'inject-1',
          injectTitle: 'Media Inquiry',
          injectNumber: 5,
        }),
      ]

      render(
        <ObservationList
          observations={observations}
          onInjectClick={onInjectClick}
        />,
      )

      const injectLink = screen.getByRole('button', { name: /Media Inquiry/i })
      fireEvent.click(injectLink)

      expect(onInjectClick).toHaveBeenCalledWith('inject-1')
    })

    it('does not show inject link button when no onInjectClick handler', () => {
      const observations = [
        createMockObservation({
          id: 'obs-1',
          injectId: 'inject-1',
          injectTitle: 'Media Inquiry',
          injectNumber: 7,
        }),
      ]

      render(<ObservationList observations={observations} />)

      // Should show text but not as a button
      expect(screen.getByText(/Re: #7 Media Inquiry/)).toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /Media Inquiry/i })).not.toBeInTheDocument()
    })
  })

  describe('Basic Rendering', () => {
    it('renders observation content', () => {
      const observations = [
        createMockObservation({ content: 'EOC communication was excellent' }),
      ]

      render(<ObservationList observations={observations} />)

      expect(screen.getByText('EOC communication was excellent')).toBeInTheDocument()
    })

    it('renders rating badge', () => {
      const observations = [
        createMockObservation({ rating: ObservationRating.Satisfactory }),
      ]

      render(<ObservationList observations={observations} />)

      expect(screen.getByText('S')).toBeInTheDocument()
    })

    it('renders recommendation when present', () => {
      const observations = [
        createMockObservation({ recommendation: 'Consider additional training' }),
      ]

      render(<ObservationList observations={observations} />)

      expect(screen.getByText('Consider additional training')).toBeInTheDocument()
      expect(screen.getByText('Recommendation')).toBeInTheDocument()
    })

    it('renders empty state when no observations', () => {
      render(<ObservationList observations={[]} />)

      expect(screen.getByText('No observations recorded yet.')).toBeInTheDocument()
    })

    it('renders loading state', () => {
      render(<ObservationList observations={[]} loading />)

      expect(screen.getByRole('progressbar')).toBeInTheDocument()
    })

    it('renders error state', () => {
      render(<ObservationList observations={[]} error="Failed to load" />)

      expect(screen.getByText('Failed to load')).toBeInTheDocument()
    })
  })

  describe('Edit/Delete Actions', () => {
    it('shows edit and delete buttons when canEdit is true', () => {
      const observations = [createMockObservation()]

      render(
        <ObservationList
          observations={observations}
          canEdit
          onEdit={vi.fn()}
          onDelete={vi.fn()}
        />,
      )

      expect(screen.getByLabelText('Edit')).toBeInTheDocument()
      expect(screen.getByLabelText('Delete')).toBeInTheDocument()
    })

    it('hides edit and delete buttons when canEdit is false', () => {
      const observations = [createMockObservation()]

      render(<ObservationList observations={observations} canEdit={false} />)

      expect(screen.queryByLabelText('Edit')).not.toBeInTheDocument()
      expect(screen.queryByLabelText('Delete')).not.toBeInTheDocument()
    })
  })

  describe('Filtering', () => {
    const observations: ObservationDto[] = [
      createMockObservation({
        id: 'obs-1',
        content: 'Excellent response time',
        rating: ObservationRating.Performed,
      }),
      createMockObservation({
        id: 'obs-2',
        content: 'Communication needs improvement',
        rating: ObservationRating.Marginal,
      }),
      createMockObservation({
        id: 'obs-3',
        content: 'Adequate performance',
        rating: ObservationRating.Satisfactory,
      }),
      createMockObservation({
        id: 'obs-4',
        content: 'Failed to follow protocol',
        rating: ObservationRating.Unsatisfactory,
      }),
      createMockObservation({
        id: 'obs-5',
        content: 'General observation',
        rating: null,
      }),
    ]

    it('shows all observations by default', () => {
      render(<ObservationList observations={observations} />)

      expect(screen.getByText('Excellent response time')).toBeInTheDocument()
      expect(screen.getByText('Communication needs improvement')).toBeInTheDocument()
      expect(screen.getByText('Adequate performance')).toBeInTheDocument()
      expect(screen.getByText('Failed to follow protocol')).toBeInTheDocument()
      expect(screen.getAllByText('General observation').length).toBeGreaterThan(0)
    })

    it('filters by rating P', () => {
      render(<ObservationList observations={observations} />)

      // Select P rating filter
      const ratingFilter = screen.getByLabelText('Filter by Rating')
      fireEvent.mouseDown(ratingFilter)
      fireEvent.click(screen.getByRole('option', { name: /P - Performed/ }))

      // Should only show P-rated observation
      expect(screen.getByText('Excellent response time')).toBeInTheDocument()
      expect(screen.queryByText('Communication needs improvement')).not.toBeInTheDocument()
      expect(screen.queryByText('Adequate performance')).not.toBeInTheDocument()
    })

    it('filters by rating M', () => {
      render(<ObservationList observations={observations} />)

      // Select M rating filter
      const ratingFilter = screen.getByLabelText('Filter by Rating')
      fireEvent.mouseDown(ratingFilter)
      fireEvent.click(screen.getByRole('option', { name: /M - Marginal/ }))

      // Should only show M-rated observation
      expect(screen.getByText('Communication needs improvement')).toBeInTheDocument()
      expect(screen.queryByText('Excellent response time')).not.toBeInTheDocument()
      expect(screen.queryByText('Adequate performance')).not.toBeInTheDocument()
    })

    it('filters by unrated observations', () => {
      render(<ObservationList observations={observations} />)

      // Select Unrated filter
      const ratingFilter = screen.getByLabelText('Filter by Rating')
      fireEvent.mouseDown(ratingFilter)
      fireEvent.click(screen.getByRole('option', { name: /Unrated/ }))

      // Should only show unrated observation (content is "General observation" too)
      expect(screen.getAllByText(/General observation/).length).toBeGreaterThan(0)
      expect(screen.queryByText('Excellent response time')).not.toBeInTheDocument()
      expect(screen.queryByText('Communication needs improvement')).not.toBeInTheDocument()
      expect(screen.queryByText('Adequate performance')).not.toBeInTheDocument()
    })

    it('shows filter count when filtering is active', () => {
      render(<ObservationList observations={observations} />)

      // Select P rating filter
      const ratingFilter = screen.getByLabelText('Filter by Rating')
      fireEvent.mouseDown(ratingFilter)
      fireEvent.click(screen.getByRole('option', { name: /P - Performed/ }))

      // Should show filtered count
      expect(screen.getByText(/Showing 1 of 5 observations/)).toBeInTheDocument()
    })

    it('clears filters when Clear Filters button clicked', () => {
      render(<ObservationList observations={observations} />)

      // Apply filter
      const ratingFilter = screen.getByLabelText('Filter by Rating')
      fireEvent.mouseDown(ratingFilter)
      fireEvent.click(screen.getByRole('option', { name: /P - Performed/ }))

      // Verify filter is applied
      expect(screen.queryByText('Adequate performance')).not.toBeInTheDocument()

      // Clear filters
      const clearButton = screen.getByRole('button', { name: /Clear Filters/i })
      fireEvent.click(clearButton)

      // All observations should be visible again
      expect(screen.getByText('Excellent response time')).toBeInTheDocument()
      expect(screen.getByText('Communication needs improvement')).toBeInTheDocument()
      expect(screen.getByText('Adequate performance')).toBeInTheDocument()
    })

    it('shows empty state when no observations match filter', () => {
      const singleObservation = [observations[0]] // Only P-rated

      render(<ObservationList observations={singleObservation} />)

      // Filter by U rating
      const ratingFilter = screen.getByLabelText('Filter by Rating')
      fireEvent.mouseDown(ratingFilter)
      fireEvent.click(screen.getByRole('option', { name: /U - Unsatisfactory/ }))

      // Should show no matches message
      expect(screen.getByText(/No observations match your filters/)).toBeInTheDocument()
    })
  })

  describe('Capability Tags (S05)', () => {
    it('displays capability tags when present', () => {
      const observationWithCapabilities: ObservationDto = {
        id: 'obs-with-caps',
        exerciseId: 'ex-1',
        content: 'Test observation with capabilities',
        rating: ObservationRating.Satisfactory,
        recommendation: null,
        observedAt: '2024-03-15T10:30:00Z',
        createdAt: '2024-03-15T10:30:00Z',
        updatedAt: '2024-03-15T10:30:00Z',
        createdBy: 'user-1',
        createdByName: 'Test Evaluator',
        injectId: null,
        injectTitle: null,
        injectNumber: null,
        objectiveId: null,
        location: null,
        capabilities: [
          {
            id: 'cap-1',
            name: 'Mass Care Services',
            category: 'Response',
          },
          {
            id: 'cap-2',
            name: 'Public Health',
            category: 'Response',
          },
        ],
      }

      render(<ObservationList observations={[observationWithCapabilities]} />)

      // Check that capability chips are displayed
      expect(screen.getByText('Mass Care Services')).toBeInTheDocument()
      expect(screen.getByText('Public Health')).toBeInTheDocument()
    })

    it('does not display capability section when no tags present', () => {
      const observationWithoutCapabilities: ObservationDto = {
        id: 'obs-no-caps',
        exerciseId: 'ex-1',
        content: 'Test observation without capabilities',
        rating: ObservationRating.Performed,
        recommendation: null,
        observedAt: '2024-03-15T10:30:00Z',
        createdAt: '2024-03-15T10:30:00Z',
        updatedAt: '2024-03-15T10:30:00Z',
        createdBy: 'user-1',
        createdByName: 'Test Evaluator',
        injectId: null,
        injectTitle: null,
        injectNumber: null,
        objectiveId: null,
        location: null,
        capabilities: [],
      }

      render(<ObservationList observations={[observationWithoutCapabilities]} />)

      // Check that no capability chips are displayed
      expect(screen.queryByText('Mass Care Services')).not.toBeInTheDocument()
      expect(screen.queryByText('Public Health')).not.toBeInTheDocument()
    })
  })
})
