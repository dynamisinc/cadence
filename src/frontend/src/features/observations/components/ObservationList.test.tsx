/**
 * ObservationList Component Tests
 *
 * Tests for the observation list display with inject linking.
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
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
})
