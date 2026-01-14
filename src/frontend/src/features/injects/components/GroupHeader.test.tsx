import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { ThemeProvider } from '@mui/material'
import { cobraTheme } from '../../../theme/cobraTheme'
import { GroupHeader, type PhaseManagementProps } from './GroupHeader'
import { InjectStatus } from '../../../types'
import type { GroupBy } from '../types/organization'

// Wrap with theme provider for COBRA styled components
const renderWithTheme = (ui: React.ReactElement) => {
  return render(<ThemeProvider theme={cobraTheme}>{ui}</ThemeProvider>)
}

describe('GroupHeader', () => {
  const defaultProps = {
    name: 'Test Group',
    count: 5,
    expanded: false,
    onToggle: vi.fn(),
    groupBy: 'none' as GroupBy,
  }

  it('renders group name', () => {
    renderWithTheme(<GroupHeader {...defaultProps} />)
    expect(screen.getByText('Test Group')).toBeInTheDocument()
  })

  it('renders count badge', () => {
    renderWithTheme(<GroupHeader {...defaultProps} />)
    expect(screen.getByText('5')).toBeInTheDocument()
  })

  it('shows chevron right icon when collapsed', () => {
    const { container } = renderWithTheme(<GroupHeader {...defaultProps} expanded={false} />)
    // FontAwesome renders svg with data-icon attribute
    const chevron = container.querySelector('[data-icon="chevron-right"]')
    expect(chevron).toBeInTheDocument()
  })

  it('shows chevron down icon when expanded', () => {
    const { container } = renderWithTheme(<GroupHeader {...defaultProps} expanded={true} />)
    const chevron = container.querySelector('[data-icon="chevron-down"]')
    expect(chevron).toBeInTheDocument()
  })

  it('calls onToggle when clicked', async () => {
    const user = userEvent.setup()
    const onToggle = vi.fn()

    renderWithTheme(<GroupHeader {...defaultProps} onToggle={onToggle} />)

    const button = screen.getByRole('button', { name: /test group/i })
    await user.click(button)

    expect(onToggle).toHaveBeenCalledTimes(1)
  })

  it('calls onToggle when Enter key is pressed', async () => {
    const user = userEvent.setup()
    const onToggle = vi.fn()

    renderWithTheme(<GroupHeader {...defaultProps} onToggle={onToggle} />)

    const button = screen.getByRole('button', { name: /test group/i })
    button.focus()
    await user.keyboard('{Enter}')

    expect(onToggle).toHaveBeenCalledTimes(1)
  })

  it('calls onToggle when Space key is pressed', async () => {
    const user = userEvent.setup()
    const onToggle = vi.fn()

    renderWithTheme(<GroupHeader {...defaultProps} onToggle={onToggle} />)

    const button = screen.getByRole('button', { name: /test group/i })
    button.focus()
    await user.keyboard(' ')

    expect(onToggle).toHaveBeenCalledTimes(1)
  })

  it('applies correct aria-expanded attribute', () => {
    const { rerender } = renderWithTheme(<GroupHeader {...defaultProps} expanded={false} />)
    let button = screen.getByRole('button', { name: /test group/i })
    expect(button).toHaveAttribute('aria-expanded', 'false')

    rerender(
      <ThemeProvider theme={cobraTheme}>
        <GroupHeader {...defaultProps} expanded={true} />
      </ThemeProvider>,
    )
    button = screen.getByRole('button', { name: /test group/i })
    expect(button).toHaveAttribute('aria-expanded', 'true')
  })

  describe('Status-based styling', () => {
    it('applies success badge color for Fired status', () => {
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          groupBy="status"
          statusValue={InjectStatus.Fired}
        />,
      )
      const badge = container.querySelector('.MuiBadge-colorSuccess')
      expect(badge).toBeInTheDocument()
    })

    it('applies warning badge color for Skipped status', () => {
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          groupBy="status"
          statusValue={InjectStatus.Skipped}
        />,
      )
      const badge = container.querySelector('.MuiBadge-colorWarning')
      expect(badge).toBeInTheDocument()
    })

    it('applies default badge color for Pending status', () => {
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          groupBy="status"
          statusValue={InjectStatus.Pending}
        />,
      )
      // Default color in MUI v7 uses standard variant
      const badge = container.querySelector('.MuiBadge-standard, .MuiBadge-colorDefault')
      expect(badge).toBeInTheDocument()
    })

    it('applies primary badge color for non-status grouping', () => {
      const { container } = renderWithTheme(
        <GroupHeader {...defaultProps} groupBy="phase" />,
      )
      const badge = container.querySelector('.MuiBadge-colorPrimary')
      expect(badge).toBeInTheDocument()
    })
  })

  describe('Phase management controls', () => {
    const phaseManagement: PhaseManagementProps = {
      phaseId: 'phase-1',
      isFirst: false,
      isLast: false,
      onEdit: vi.fn(),
      onDelete: vi.fn(),
      onMoveUp: vi.fn(),
      onMoveDown: vi.fn(),
      isLoading: false,
    }

    it('does not show phase controls when canManagePhases is false', () => {
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          groupBy="phase"
          phaseManagement={phaseManagement}
          canManagePhases={false}
        />,
      )
      // No management buttons should be present (check for icons that indicate phase management)
      const moveUpIcon = container.querySelector('[data-icon="chevron-up"]')
      expect(moveUpIcon).not.toBeInTheDocument()
    })

    it('does not show phase controls for non-phase grouping', () => {
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          groupBy="status"
          phaseManagement={phaseManagement}
          canManagePhases={true}
        />,
      )
      const moveUpIcon = container.querySelector('[data-icon="chevron-up"]')
      expect(moveUpIcon).not.toBeInTheDocument()
    })

    it('does not show phase controls for unassigned phase', () => {
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          groupBy="phase"
          phaseManagement={{ ...phaseManagement, phaseId: null }}
          canManagePhases={true}
        />,
      )
      const moveUpIcon = container.querySelector('[data-icon="chevron-up"]')
      expect(moveUpIcon).not.toBeInTheDocument()
    })

    it('shows phase controls when conditions are met', () => {
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          groupBy="phase"
          phaseManagement={phaseManagement}
          canManagePhases={true}
        />,
      )
      // Check for icons that indicate phase management
      expect(container.querySelector('[data-icon="chevron-up"]')).toBeInTheDocument()
      expect(container.querySelector('[data-icon="pen"]')).toBeInTheDocument()
      expect(container.querySelector('[data-icon="trash"]')).toBeInTheDocument()
    })

    it('disables move up button when isFirst is true', () => {
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          groupBy="phase"
          phaseManagement={{ ...phaseManagement, isFirst: true }}
          canManagePhases={true}
        />,
      )
      // Find the move up button (first button with chevron-up icon)
      const moveUpIcon = container.querySelector('[data-icon="chevron-up"]')
      const moveUpButton = moveUpIcon?.closest('button')
      expect(moveUpButton).toBeDisabled()
    })

    it('disables move down button when isLast is true', () => {
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          groupBy="phase"
          phaseManagement={{ ...phaseManagement, isLast: true }}
          canManagePhases={true}
        />,
      )
      // Find the move down button (second chevron-down icon, first one is for expansion)
      const chevronDownIcons = container.querySelectorAll('[data-icon="chevron-down"]')
      const moveDownButton = chevronDownIcons[chevronDownIcons.length - 1]?.closest('button')
      expect(moveDownButton).toBeDisabled()
    })

    it('disables delete button when count > 0', () => {
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          count={5}
          groupBy="phase"
          phaseManagement={phaseManagement}
          canManagePhases={true}
        />,
      )
      const trashIcon = container.querySelector('[data-icon="trash"]')
      const deleteButton = trashIcon?.closest('button')
      expect(deleteButton).toBeDisabled()
    })

    it('enables delete button when count is 0', () => {
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          count={0}
          groupBy="phase"
          phaseManagement={phaseManagement}
          canManagePhases={true}
        />,
      )
      const trashIcon = container.querySelector('[data-icon="trash"]')
      const deleteButton = trashIcon?.closest('button')
      expect(deleteButton).not.toBeDisabled()
    })

    it('calls onMoveUp when move up button is clicked', async () => {
      const user = userEvent.setup()
      const onMoveUp = vi.fn()
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          groupBy="phase"
          phaseManagement={{ ...phaseManagement, onMoveUp }}
          canManagePhases={true}
        />,
      )

      const moveUpIcon = container.querySelector('[data-icon="chevron-up"]')
      const moveUpButton = moveUpIcon?.closest('button')
      if (moveUpButton) {
        await user.click(moveUpButton)
      }

      expect(onMoveUp).toHaveBeenCalledTimes(1)
    })

    it('calls onMoveDown when move down button is clicked', async () => {
      const user = userEvent.setup()
      const onMoveDown = vi.fn()
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          groupBy="phase"
          phaseManagement={{ ...phaseManagement, onMoveDown }}
          canManagePhases={true}
        />,
      )

      // Get the second chevron-down (first is for expansion)
      const chevronDownIcons = container.querySelectorAll('[data-icon="chevron-down"]')
      const moveDownButton = chevronDownIcons[chevronDownIcons.length - 1]?.closest('button')
      if (moveDownButton) {
        await user.click(moveDownButton)
      }

      expect(onMoveDown).toHaveBeenCalledTimes(1)
    })

    it('calls onEdit when edit button is clicked', async () => {
      const user = userEvent.setup()
      const onEdit = vi.fn()
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          groupBy="phase"
          phaseManagement={{ ...phaseManagement, onEdit }}
          canManagePhases={true}
        />,
      )

      const editIcon = container.querySelector('[data-icon="pen"]')
      const editButton = editIcon?.closest('button')
      if (editButton) {
        await user.click(editButton)
      }

      expect(onEdit).toHaveBeenCalledTimes(1)
    })

    it('calls onDelete when delete button is clicked', async () => {
      const user = userEvent.setup()
      const onDelete = vi.fn()
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          count={0}
          groupBy="phase"
          phaseManagement={{ ...phaseManagement, onDelete }}
          canManagePhases={true}
        />,
      )

      const trashIcon = container.querySelector('[data-icon="trash"]')
      const deleteButton = trashIcon?.closest('button')
      if (deleteButton) {
        await user.click(deleteButton)
      }

      expect(onDelete).toHaveBeenCalledTimes(1)
    })

    it('does not toggle group when management button is clicked', async () => {
      const user = userEvent.setup()
      const onToggle = vi.fn()
      const { container } = renderWithTheme(
        <GroupHeader
          {...defaultProps}
          onToggle={onToggle}
          groupBy="phase"
          phaseManagement={phaseManagement}
          canManagePhases={true}
        />,
      )

      // Click edit button
      const editIcon = container.querySelector('[data-icon="pen"]')
      const editButton = editIcon?.closest('button')
      if (editButton) {
        await user.click(editButton)
      }

      // onToggle should not be called
      expect(onToggle).not.toHaveBeenCalled()
    })
  })

  it('is keyboard accessible with tabIndex', () => {
    renderWithTheme(<GroupHeader {...defaultProps} />)
    const button = screen.getByRole('button', { name: /test group/i })
    expect(button).toHaveAttribute('tabIndex', '0')
  })
})
