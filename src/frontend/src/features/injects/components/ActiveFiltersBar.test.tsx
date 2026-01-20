import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi } from 'vitest'
import { ThemeProvider } from '@mui/material'
import { cobraTheme } from '../../../theme/cobraTheme'
import { ActiveFiltersBar, type ActiveFilter } from './ActiveFiltersBar'

// Wrap with theme provider for COBRA styled components
const renderWithTheme = (ui: React.ReactElement) => {
  return render(<ThemeProvider theme={cobraTheme}>{ui}</ThemeProvider>)
}

describe('ActiveFiltersBar', () => {
  const mockFilters: ActiveFilter[] = [
    { type: 'status', label: 'Status', value: 'Pending' },
    { type: 'phase', label: 'Phase', value: 'Phase 1' },
  ]

  const defaultProps = {
    filters: mockFilters,
    totalCount: 100,
    filteredCount: 25,
    onRemoveFilter: vi.fn(),
    onClearAll: vi.fn(),
  }

  it('renders nothing when no filters are active', () => {
    const { container } = renderWithTheme(
      <ActiveFiltersBar {...defaultProps} filters={[]} />,
    )
    expect(container).toBeEmptyDOMElement()
  })

  it('renders active filters label', () => {
    renderWithTheme(<ActiveFiltersBar {...defaultProps} />)
    expect(screen.getByText('Active filters:')).toBeInTheDocument()
  })

  it('renders all filter chips', () => {
    renderWithTheme(<ActiveFiltersBar {...defaultProps} />)
    expect(screen.getByText('Status:')).toBeInTheDocument()
    expect(screen.getByText('Pending')).toBeInTheDocument()
    expect(screen.getByText('Phase:')).toBeInTheDocument()
    expect(screen.getByText('Phase 1')).toBeInTheDocument()
  })

  it('renders clear all button', () => {
    renderWithTheme(<ActiveFiltersBar {...defaultProps} />)
    expect(screen.getByRole('button', { name: /clear all/i })).toBeInTheDocument()
  })

  it('displays filtered count', () => {
    renderWithTheme(<ActiveFiltersBar {...defaultProps} />)
    expect(screen.getByText(/showing/i)).toBeInTheDocument()
    expect(screen.getByText('25')).toBeInTheDocument()
    expect(screen.getByText(/of 100 injects/i)).toBeInTheDocument()
  })

  it('uses singular "inject" when total count is 1', () => {
    renderWithTheme(<ActiveFiltersBar {...defaultProps} totalCount={1} filteredCount={1} />)
    expect(screen.getByText(/1 inject$/i)).toBeInTheDocument()
  })

  it('uses plural "injects" when total count is not 1', () => {
    renderWithTheme(<ActiveFiltersBar {...defaultProps} totalCount={5} filteredCount={5} />)
    expect(screen.getByText(/5 injects$/i)).toBeInTheDocument()
  })

  it('highlights filtered count when results differ from total', () => {
    const { rerender } = renderWithTheme(
      <ActiveFiltersBar {...defaultProps} totalCount={100} filteredCount={25} />,
    )

    // When filtered, count should be bold (fontWeight 600)
    const countElement = screen.getByText('25')
    expect(countElement).toHaveStyle({ fontWeight: 600 })

    // When not filtered, count should be normal (fontWeight 400)
    rerender(
      <ThemeProvider theme={cobraTheme}>
        <ActiveFiltersBar {...defaultProps} totalCount={100} filteredCount={100} />
      </ThemeProvider>,
    )
    const sameCountElement = screen.getByText('100')
    expect(sameCountElement).toHaveStyle({ fontWeight: 400 })
  })

  it('calls onClearAll when clear all button is clicked', async () => {
    const user = userEvent.setup()
    const onClearAll = vi.fn()

    renderWithTheme(<ActiveFiltersBar {...defaultProps} onClearAll={onClearAll} />)

    await user.click(screen.getByRole('button', { name: /clear all/i }))

    expect(onClearAll).toHaveBeenCalledTimes(1)
  })

  it('calls onRemoveFilter with correct type when filter chip is removed', async () => {
    const user = userEvent.setup()
    const onRemoveFilter = vi.fn()

    const { container } = renderWithTheme(
      <ActiveFiltersBar {...defaultProps} onRemoveFilter={onRemoveFilter} />,
    )

    // FilterChip uses Chip with onDelete, which renders a delete icon
    const deleteIcons = container.querySelectorAll('.MuiChip-deleteIcon')
    expect(deleteIcons.length).toBeGreaterThan(0)

    // First delete icon should be for the first filter (Status)
    await user.click(deleteIcons[0] as HTMLElement)

    expect(onRemoveFilter).toHaveBeenCalledWith('status')
  })

  it('renders multiple filters of different types', () => {
    const multipleFilters: ActiveFilter[] = [
      { type: 'status', label: 'Status', value: 'Pending' },
      { type: 'phase', label: 'Phase', value: 'Phase 1' },
      { type: 'method', label: 'Method', value: 'Email' },
    ]

    renderWithTheme(<ActiveFiltersBar {...defaultProps} filters={multipleFilters} />)

    expect(screen.getByText('Status:')).toBeInTheDocument()
    expect(screen.getByText('Phase:')).toBeInTheDocument()
    expect(screen.getByText('Method:')).toBeInTheDocument()
  })
})
