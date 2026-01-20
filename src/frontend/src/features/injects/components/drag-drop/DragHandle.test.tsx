/**
 * Tests for DragHandle component
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { DragHandle } from './DragHandle'

describe('DragHandle', () => {
  it('renders grip icon when not disabled', () => {
    render(<DragHandle />)

    const handle = screen.getByRole('button', { name: /drag to reorder/i })
    expect(handle).toBeInTheDocument()
  })

  it('hides when disabled', () => {
    const { container } = render(<DragHandle disabled />)

    expect(container.firstChild).toBeNull()
  })

  it('applies attributes and listeners from dnd-kit', () => {
    const mockAttributes = {
      role: 'button',
      'aria-describedby': 'dnd-description',
      tabIndex: 0,
    }
    const mockListeners = {
      onPointerDown: vi.fn(),
      onKeyDown: vi.fn(),
    }

    render(<DragHandle attributes={mockAttributes} listeners={mockListeners} />)

    const handle = screen.getByRole('button', { name: /drag to reorder/i })
    expect(handle).toHaveAttribute('aria-describedby', 'dnd-description')
  })

  it('has grab cursor when not disabled', () => {
    render(<DragHandle />)

    const handle = screen.getByRole('button', { name: /drag to reorder/i })
    expect(handle).toHaveStyle({ cursor: 'grab' })
  })
})
