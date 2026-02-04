import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { ThemeProvider } from '@mui/material'
import { cobraTheme } from '../../../theme/cobraTheme'
import { FilterChip } from './FilterChip'

// Wrap with theme provider for COBRA styled components
const renderWithTheme = (ui: React.ReactElement) => {
  return render(<ThemeProvider theme={cobraTheme}>{ui}</ThemeProvider>)
}

describe('FilterChip', () => {
  const defaultProps = {
    label: 'Status',
    value: 'Draft',
    onRemove: vi.fn(),
  }

  it('renders label and value', () => {
    renderWithTheme(<FilterChip {...defaultProps} />)
    expect(screen.getByText('Status:')).toBeInTheDocument()
    expect(screen.getByText('Draft')).toBeInTheDocument()
  })

  it('renders delete icon', () => {
    renderWithTheme(<FilterChip {...defaultProps} />)
    // The delete button should be present (from Chip's onDelete prop)
    const deleteButton = screen.getByRole('button')
    expect(deleteButton).toBeInTheDocument()
  })

  it('calls onRemove when delete icon is clicked', async () => {
    const user = userEvent.setup()
    const onRemove = vi.fn()

    const { container } = renderWithTheme(<FilterChip {...defaultProps} onRemove={onRemove} />)

    // Find the delete icon within the chip
    const deleteIcon = container.querySelector('.MuiChip-deleteIcon')
    expect(deleteIcon).toBeInTheDocument()

    if (deleteIcon) {
      await user.click(deleteIcon as HTMLElement)
    }

    expect(onRemove).toHaveBeenCalledTimes(1)
  })

  it('renders with different label and value combinations', () => {
    const { rerender } = renderWithTheme(<FilterChip {...defaultProps} />)

    expect(screen.getByText('Status:')).toBeInTheDocument()
    expect(screen.getByText('Draft')).toBeInTheDocument()

    rerender(
      <ThemeProvider theme={cobraTheme}>
        <FilterChip label="Phase" value="Phase 1" onRemove={vi.fn()} />
      </ThemeProvider>,
    )

    expect(screen.getByText('Phase:')).toBeInTheDocument()
    expect(screen.getByText('Phase 1')).toBeInTheDocument()
  })

  it('applies small size styling', () => {
    const { container } = renderWithTheme(<FilterChip {...defaultProps} />)
    // MUI Chip with size="small" is rendered
    const chip = container.querySelector('.MuiChip-root')
    expect(chip).toBeInTheDocument()
    expect(chip).toHaveClass('MuiChip-sizeSmall')
  })

  it('displays label with colon separator', () => {
    renderWithTheme(<FilterChip {...defaultProps} />)
    const labelElement = screen.getByText('Status:')
    expect(labelElement).toBeInTheDocument()
  })

  it('handles empty value gracefully', () => {
    renderWithTheme(<FilterChip {...defaultProps} value="" />)
    expect(screen.getByText('Status:')).toBeInTheDocument()
    // Value element should still exist but be empty
    const chip = screen.getByRole('button').closest('.MuiChip-root')
    expect(chip).toBeInTheDocument()
  })
})
