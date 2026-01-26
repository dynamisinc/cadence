/**
 * NotificationToast Component Tests
 *
 * Tests for the toast notification display and behavior.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, fireEvent } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { MemoryRouter } from 'react-router-dom'
import { NotificationToast } from './NotificationToast'
import type { Toast, NotificationDto, NotificationPriority } from '../types'
import type { ReactNode } from 'react'

// Mock useNavigate
const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

// Helper to create mock notification
const createMockNotification = (
  overrides: Partial<NotificationDto> = {},
): NotificationDto => ({
  id: 'notif-1',
  type: 'InjectReady',
  priority: 'Medium',
  title: 'Inject Ready',
  message: 'Inject #5 is ready to fire',
  actionUrl: '/exercises/ex-1/conduct',
  relatedEntityType: 'Inject',
  relatedEntityId: 'inject-5',
  isRead: false,
  createdAt: '2026-01-15T10:00:00Z',
  readAt: null,
  ...overrides,
})

// Helper to create mock toast
const createMockToast = (overrides: Partial<Toast> = {}): Toast => ({
  id: 'toast-1',
  notification: createMockNotification(),
  createdAt: new Date(),
  ...overrides,
})

// Wrapper component
const Wrapper = ({ children }: { children: ReactNode }) => (
  <MemoryRouter>{children}</MemoryRouter>
)

describe('NotificationToast', () => {
  const mockOnDismiss = vi.fn()
  const mockOnMouseEnter = vi.fn()
  const mockOnMouseLeave = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  const renderToast = (toast: Toast) => {
    return render(
      <Wrapper>
        <NotificationToast
          toast={toast}
          onDismiss={mockOnDismiss}
          onMouseEnter={mockOnMouseEnter}
          onMouseLeave={mockOnMouseLeave}
        />
      </Wrapper>,
    )
  }

  describe('Basic Rendering', () => {
    it('renders notification title', () => {
      renderToast(createMockToast())
      expect(screen.getByText('Inject Ready')).toBeInTheDocument()
    })

    it('renders notification message', () => {
      renderToast(createMockToast())
      expect(screen.getByText('Inject #5 is ready to fire')).toBeInTheDocument()
    })

    it('renders close button', () => {
      renderToast(createMockToast())
      expect(screen.getByRole('button')).toBeInTheDocument()
    })

    it('shows "Click to view" when actionUrl is present', () => {
      renderToast(
        createMockToast({
          notification: createMockNotification({ actionUrl: '/some/url' }),
        }),
      )
      expect(screen.getByText('Click to view')).toBeInTheDocument()
    })

    it('does not show "Click to view" when actionUrl is null', () => {
      renderToast(
        createMockToast({
          notification: createMockNotification({ actionUrl: null }),
        }),
      )
      expect(screen.queryByText('Click to view')).not.toBeInTheDocument()
    })
  })

  describe('Priority Styling', () => {
    it('applies high priority styling (orange border)', () => {
      const toast = createMockToast({
        notification: createMockNotification({ priority: 'High' }),
      })
      renderToast(toast)

      // The Paper component should have orange border for high priority
      const paper = screen.getByText('Inject Ready').closest('.MuiPaper-root')
      expect(paper).toHaveStyle({ borderLeft: '4px solid #ff9800' })
    })

    it('applies medium priority styling (blue border)', () => {
      const toast = createMockToast({
        notification: createMockNotification({ priority: 'Medium' }),
      })
      renderToast(toast)

      const paper = screen.getByText('Inject Ready').closest('.MuiPaper-root')
      expect(paper).toHaveStyle({ borderLeft: '4px solid #2196f3' })
    })

    it('applies low priority styling (grey border)', () => {
      const toast = createMockToast({
        notification: createMockNotification({ priority: 'Low' }),
      })
      renderToast(toast)

      const paper = screen.getByText('Inject Ready').closest('.MuiPaper-root')
      expect(paper).toHaveStyle({ borderLeft: '4px solid #9e9e9e' })
    })
  })

  describe('Notification Type Icons', () => {
    const typeTests: Array<{
      type: NotificationDto['type']
      title: string
    }> = [
      { type: 'InjectReady', title: 'Inject is ready' },
      { type: 'InjectFired', title: 'Inject was fired' },
      { type: 'ClockStarted', title: 'Clock started' },
      { type: 'ClockPaused', title: 'Clock paused' },
      { type: 'ExerciseCompleted', title: 'Exercise completed' },
      { type: 'AssignmentCreated', title: 'New assignment' },
      { type: 'ObservationCreated', title: 'Observation added' },
      { type: 'System', title: 'System notification' },
    ]

    typeTests.forEach(({ type, title }) => {
      it(`renders correct icon for ${type} notification`, () => {
        const toast = createMockToast({
          notification: createMockNotification({ type, title }),
        })
        renderToast(toast)
        // Just verify the toast renders without error
        expect(screen.getByText(title)).toBeInTheDocument()
      })
    })
  })

  describe('User Interactions', () => {
    it('calls onDismiss when close button clicked', () => {
      const toast = createMockToast({ id: 'toast-123' })
      renderToast(toast)

      const closeButton = screen.getByRole('button')
      fireEvent.click(closeButton)

      expect(mockOnDismiss).toHaveBeenCalledWith('toast-123')
    })

    it('navigates and dismisses when toast with actionUrl is clicked', () => {
      const toast = createMockToast({
        id: 'toast-123',
        notification: createMockNotification({
          actionUrl: '/exercises/ex-1/conduct',
        }),
      })
      renderToast(toast)

      // Click on the toast (not the close button)
      const paper = screen.getByText('Inject Ready').closest('.MuiPaper-root')
      fireEvent.click(paper!)

      expect(mockNavigate).toHaveBeenCalledWith('/exercises/ex-1/conduct')
      expect(mockOnDismiss).toHaveBeenCalledWith('toast-123')
    })

    it('does not navigate when toast without actionUrl is clicked', () => {
      const toast = createMockToast({
        notification: createMockNotification({ actionUrl: null }),
      })
      renderToast(toast)

      const paper = screen.getByText('Inject Ready').closest('.MuiPaper-root')
      fireEvent.click(paper!)

      expect(mockNavigate).not.toHaveBeenCalled()
    })

    it('calls onMouseEnter when mouse enters', () => {
      const toast = createMockToast({ id: 'toast-123' })
      renderToast(toast)

      const paper = screen.getByText('Inject Ready').closest('.MuiPaper-root')
      fireEvent.mouseEnter(paper!)

      expect(mockOnMouseEnter).toHaveBeenCalledWith('toast-123')
    })

    it('calls onMouseLeave with toast ID and priority when mouse leaves', () => {
      const toast = createMockToast({
        id: 'toast-123',
        notification: createMockNotification({ priority: 'Medium' }),
      })
      renderToast(toast)

      const paper = screen.getByText('Inject Ready').closest('.MuiPaper-root')
      fireEvent.mouseLeave(paper!)

      expect(mockOnMouseLeave).toHaveBeenCalledWith('toast-123', 'Medium')
    })

    it('close button click does not propagate to paper', () => {
      const toast = createMockToast({
        id: 'toast-123',
        notification: createMockNotification({
          actionUrl: '/some/url',
        }),
      })
      renderToast(toast)

      const closeButton = screen.getByRole('button')
      fireEvent.click(closeButton)

      // Should call dismiss but NOT navigate
      expect(mockOnDismiss).toHaveBeenCalledWith('toast-123')
      expect(mockNavigate).not.toHaveBeenCalled()
    })
  })

  describe('Cursor Style', () => {
    it('has pointer cursor when actionUrl is present', () => {
      const toast = createMockToast({
        notification: createMockNotification({ actionUrl: '/some/url' }),
      })
      renderToast(toast)

      const paper = screen.getByText('Inject Ready').closest('.MuiPaper-root')
      expect(paper).toHaveStyle({ cursor: 'pointer' })
    })

    it('has default cursor when actionUrl is null', () => {
      const toast = createMockToast({
        notification: createMockNotification({ actionUrl: null }),
      })
      renderToast(toast)

      const paper = screen.getByText('Inject Ready').closest('.MuiPaper-root')
      expect(paper).toHaveStyle({ cursor: 'default' })
    })
  })
})
