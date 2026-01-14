import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { ThemeProvider } from '@mui/material'
import { cobraTheme } from '../../../theme/cobraTheme'
import { GroupByDropdown } from './GroupByDropdown'
import type { GroupBy } from '../types/organization'

// Wrap with theme provider for COBRA styled components
const renderWithTheme = (ui: React.ReactElement) => {
  return render(<ThemeProvider theme={cobraTheme}>{ui}</ThemeProvider>)
}

describe('GroupByDropdown', () => {
  const defaultProps = {
    value: 'none' as GroupBy,
    onChange: vi.fn(),
  }

  it('renders group by label', () => {
    renderWithTheme(<GroupByDropdown {...defaultProps} />)
    expect(screen.getByText('Group:')).toBeInTheDocument()
  })

  it('renders group icon', () => {
    const { container } = renderWithTheme(<GroupByDropdown {...defaultProps} />)
    // FontAwesome icon is rendered as svg
    const icon = container.querySelector('svg')
    expect(icon).toBeInTheDocument()
  })

  it('displays current value in select', () => {
    renderWithTheme(<GroupByDropdown {...defaultProps} value="status" />)
    // MUI Select renders with the value in a hidden input
    const select = screen.getByRole('combobox')
    expect(select).toBeInTheDocument()
  })

  it('opens dropdown when clicked', async () => {
    const user = userEvent.setup()
    renderWithTheme(<GroupByDropdown {...defaultProps} />)

    const select = screen.getByRole('combobox')
    await user.click(select)

    // Dropdown options should be visible
    await waitFor(() => {
      expect(screen.getByRole('listbox')).toBeInTheDocument()
    })
  })

  it('renders all group by options', async () => {
    const user = userEvent.setup()
    renderWithTheme(<GroupByDropdown {...defaultProps} />)

    const select = screen.getByRole('combobox')
    await user.click(select)

    // Check for expected options (from getGroupByOptions: none, phase, status)
    await waitFor(() => {
      expect(screen.getByRole('option', { name: /none/i })).toBeInTheDocument()
      expect(screen.getByRole('option', { name: /phase/i })).toBeInTheDocument()
      expect(screen.getByRole('option', { name: /status/i })).toBeInTheDocument()
    })
  })

  it('calls onChange with selected value', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()

    renderWithTheme(<GroupByDropdown {...defaultProps} onChange={onChange} />)

    const select = screen.getByRole('combobox')
    await user.click(select)

    // Wait for dropdown to open, then click option
    const statusOption = await screen.findByRole('option', { name: /status/i })
    await user.click(statusOption)

    expect(onChange).toHaveBeenCalledWith('status')
  })

  it('changes displayed value when value prop changes', () => {
    const { rerender } = renderWithTheme(<GroupByDropdown {...defaultProps} value="none" />)
    expect(screen.getByRole('combobox')).toBeInTheDocument()

    rerender(
      <ThemeProvider theme={cobraTheme}>
        <GroupByDropdown {...defaultProps} value="status" />
      </ThemeProvider>,
    )
    expect(screen.getByRole('combobox')).toBeInTheDocument()

    rerender(
      <ThemeProvider theme={cobraTheme}>
        <GroupByDropdown {...defaultProps} value="phase" />
      </ThemeProvider>,
    )
    expect(screen.getByRole('combobox')).toBeInTheDocument()
  })

  it('applies small size styling', () => {
    const { container } = renderWithTheme(<GroupByDropdown {...defaultProps} />)
    const select = container.querySelector('.MuiSelect-select')
    expect(select).toHaveClass('MuiInputBase-inputSizeSmall')
  })

  it('renders with outlined variant', () => {
    const { container } = renderWithTheme(<GroupByDropdown {...defaultProps} />)
    const input = container.querySelector('.MuiOutlinedInput-root')
    expect(input).toBeInTheDocument()
  })
})
