/**
 * SortableTableHeader Tests
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { SortableTableHeader } from './SortableTableHeader'

// Wrap in table for valid DOM structure
const renderInTable = (ui: React.ReactElement) => {
  return render(
    <table>
      <thead>
        <tr>{ui}</tr>
      </thead>
    </table>,
  )
}

describe('SortableTableHeader', () => {
  describe('Rendering', () => {
    it('renders the label', () => {
      renderInTable(
        <SortableTableHeader
          column="title"
          label="Title"
          activeColumn={null}
          direction={null}
          onSort={() => {}}
        />,
      )

      expect(screen.getByText('Title')).toBeInTheDocument()
    })

    it('renders sort icon', () => {
      renderInTable(
        <SortableTableHeader
          column="title"
          label="Title"
          activeColumn={null}
          direction={null}
          onSort={() => {}}
        />,
      )

      // FontAwesome renders an svg element
      expect(document.querySelector('svg')).toBeInTheDocument()
    })

    it('applies custom width', () => {
      renderInTable(
        <SortableTableHeader
          column="injectNumber"
          label="#"
          activeColumn={null}
          direction={null}
          onSort={() => {}}
          width={60}
        />,
      )

      const cell = screen.getByRole('columnheader')
      expect(cell).toHaveAttribute('width', '60')
    })
  })

  describe('Sort Indicator', () => {
    it('shows neutral sort icon when not active', () => {
      renderInTable(
        <SortableTableHeader
          column="title"
          label="Title"
          activeColumn="injectNumber"
          direction="asc"
          onSort={() => {}}
        />,
      )

      const cell = screen.getByRole('columnheader')
      expect(cell).toHaveAttribute('aria-sort', 'none')
    })

    it('shows ascending indicator when sorted asc', () => {
      renderInTable(
        <SortableTableHeader
          column="title"
          label="Title"
          activeColumn="title"
          direction="asc"
          onSort={() => {}}
        />,
      )

      const cell = screen.getByRole('columnheader')
      expect(cell).toHaveAttribute('aria-sort', 'ascending')
    })

    it('shows descending indicator when sorted desc', () => {
      renderInTable(
        <SortableTableHeader
          column="title"
          label="Title"
          activeColumn="title"
          direction="desc"
          onSort={() => {}}
        />,
      )

      const cell = screen.getByRole('columnheader')
      expect(cell).toHaveAttribute('aria-sort', 'descending')
    })
  })

  describe('Interaction', () => {
    it('calls onSort with column when clicked', async () => {
      const onSort = vi.fn()
      const user = userEvent.setup()

      renderInTable(
        <SortableTableHeader
          column="title"
          label="Title"
          activeColumn={null}
          direction={null}
          onSort={onSort}
        />,
      )

      await user.click(screen.getByRole('columnheader'))

      expect(onSort).toHaveBeenCalledWith('title')
      expect(onSort).toHaveBeenCalledTimes(1)
    })

    it('calls onSort when Enter is pressed', () => {
      const onSort = vi.fn()

      renderInTable(
        <SortableTableHeader
          column="scheduledTime"
          label="Time"
          activeColumn={null}
          direction={null}
          onSort={onSort}
        />,
      )

      const cell = screen.getByRole('columnheader')
      fireEvent.keyDown(cell, { key: 'Enter' })

      expect(onSort).toHaveBeenCalledWith('scheduledTime')
    })

    it('calls onSort when Space is pressed', () => {
      const onSort = vi.fn()

      renderInTable(
        <SortableTableHeader
          column="status"
          label="Status"
          activeColumn={null}
          direction={null}
          onSort={onSort}
        />,
      )

      const cell = screen.getByRole('columnheader')
      fireEvent.keyDown(cell, { key: ' ' })

      expect(onSort).toHaveBeenCalledWith('status')
    })

    it('does not call onSort for other keys', () => {
      const onSort = vi.fn()

      renderInTable(
        <SortableTableHeader
          column="title"
          label="Title"
          activeColumn={null}
          direction={null}
          onSort={onSort}
        />,
      )

      const cell = screen.getByRole('columnheader')
      fireEvent.keyDown(cell, { key: 'Tab' })

      expect(onSort).not.toHaveBeenCalled()
    })
  })

  describe('Accessibility', () => {
    it('has columnheader role', () => {
      renderInTable(
        <SortableTableHeader
          column="title"
          label="Title"
          activeColumn={null}
          direction={null}
          onSort={() => {}}
        />,
      )

      expect(screen.getByRole('columnheader')).toBeInTheDocument()
    })

    it('is focusable with tabIndex', () => {
      renderInTable(
        <SortableTableHeader
          column="title"
          label="Title"
          activeColumn={null}
          direction={null}
          onSort={() => {}}
        />,
      )

      const cell = screen.getByRole('columnheader')
      expect(cell).toHaveAttribute('tabIndex', '0')
    })
  })
})
