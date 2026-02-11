/**
 * AnnotationToolbar Component Tests
 *
 * Tests for the annotation toolbar:
 * - Tool selection and highlighting
 * - Action button states
 * - Callback invocation
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@/test/testUtils'
import userEvent from '@testing-library/user-event'
import { AnnotationToolbar } from './AnnotationToolbar'

describe('AnnotationToolbar', () => {
  const mockOnToolChange = vi.fn()
  const mockOnUndo = vi.fn()
  const mockOnDone = vi.fn()
  const mockOnCancel = vi.fn()

  const defaultProps = {
    activeTool: null as any,
    onToolChange: mockOnToolChange,
    onUndo: mockOnUndo,
    onDone: mockOnDone,
    onCancel: mockOnCancel,
    canUndo: false,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('rendering', () => {
    it('renders all tool buttons', () => {
      render(<AnnotationToolbar {...defaultProps} />)

      expect(screen.getByLabelText('Circle tool')).toBeInTheDocument()
      expect(screen.getByLabelText('Arrow tool')).toBeInTheDocument()
      expect(screen.getByLabelText('Text tool')).toBeInTheDocument()
    })

    it('renders all action buttons', () => {
      render(<AnnotationToolbar {...defaultProps} />)

      expect(screen.getByLabelText('Undo annotation')).toBeInTheDocument()
      expect(screen.getByLabelText('Cancel annotation')).toBeInTheDocument()
      expect(screen.getByLabelText('Done annotating')).toBeInTheDocument()
    })

    it('renders with active tool', () => {
      const { rerender } = render(<AnnotationToolbar {...defaultProps} activeTool="circle" />)

      const circleButton = screen.getByLabelText('Circle tool')
      expect(circleButton).toBeInTheDocument()

      rerender(<AnnotationToolbar {...defaultProps} activeTool="arrow" />)

      const arrowButton = screen.getByLabelText('Arrow tool')
      expect(arrowButton).toBeInTheDocument()
    })

    it('disables undo button when canUndo is false', () => {
      render(<AnnotationToolbar {...defaultProps} canUndo={false} />)

      expect(screen.getByLabelText('Undo annotation')).toBeDisabled()
    })

    it('enables undo button when canUndo is true', () => {
      render(<AnnotationToolbar {...defaultProps} canUndo={true} />)

      expect(screen.getByLabelText('Undo annotation')).not.toBeDisabled()
    })
  })

  describe('tool selection', () => {
    it('calls onToolChange with circle when circle button clicked', async () => {
      const user = userEvent.setup()
      render(<AnnotationToolbar {...defaultProps} />)

      await user.click(screen.getByLabelText('Circle tool'))

      expect(mockOnToolChange).toHaveBeenCalledWith('circle')
    })

    it('calls onToolChange with arrow when arrow button clicked', async () => {
      const user = userEvent.setup()
      render(<AnnotationToolbar {...defaultProps} />)

      await user.click(screen.getByLabelText('Arrow tool'))

      expect(mockOnToolChange).toHaveBeenCalledWith('arrow')
    })

    it('calls onToolChange with text when text button clicked', async () => {
      const user = userEvent.setup()
      render(<AnnotationToolbar {...defaultProps} />)

      await user.click(screen.getByLabelText('Text tool'))

      expect(mockOnToolChange).toHaveBeenCalledWith('text')
    })

    it('toggles tool off when clicking active tool', async () => {
      const user = userEvent.setup()
      render(<AnnotationToolbar {...defaultProps} activeTool="circle" />)

      await user.click(screen.getByLabelText('Circle tool'))

      expect(mockOnToolChange).toHaveBeenCalledWith(null)
    })
  })

  describe('action buttons', () => {
    it('calls onUndo when undo button clicked', async () => {
      const user = userEvent.setup()
      render(<AnnotationToolbar {...defaultProps} canUndo={true} />)

      await user.click(screen.getByLabelText('Undo annotation'))

      expect(mockOnUndo).toHaveBeenCalled()
    })

    it('calls onCancel when cancel button clicked', async () => {
      const user = userEvent.setup()
      render(<AnnotationToolbar {...defaultProps} />)

      await user.click(screen.getByLabelText('Cancel annotation'))

      expect(mockOnCancel).toHaveBeenCalled()
    })

    it('calls onDone when done button clicked', async () => {
      const user = userEvent.setup()
      render(<AnnotationToolbar {...defaultProps} />)

      await user.click(screen.getByLabelText('Done annotating'))

      expect(mockOnDone).toHaveBeenCalled()
    })
  })
})
