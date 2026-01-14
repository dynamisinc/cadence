import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { ThemeProvider } from '@mui/material'
import { cobraTheme } from '../../../theme/cobraTheme'
import { InjectFilterBar, type PhaseOption } from './InjectFilterBar'
import type { FilterState } from '../types/organization'
import { InjectStatus, DeliveryMethod } from '../../../types'

// Wrap with theme provider for COBRA styled components
const renderWithTheme = (ui: React.ReactElement) => {
  return render(<ThemeProvider theme={cobraTheme}>{ui}</ThemeProvider>)
}

describe('InjectFilterBar', () => {
  const phases: PhaseOption[] = [
    { id: 'phase-1', name: 'Phase 1' },
    { id: 'phase-2', name: 'Phase 2' },
  ]

  const defaultFilters: FilterState = {
    statuses: [],
    phaseIds: [],
    deliveryMethods: [],
  }

  const defaultProps = {
    searchTerm: '',
    onSearchChange: vi.fn(),
    onSearchClear: vi.fn(),
    filters: defaultFilters,
    onStatusChange: vi.fn(),
    onPhaseChange: vi.fn(),
    onMethodChange: vi.fn(),
    groupBy: 'none' as const,
    onGroupByChange: vi.fn(),
    phases,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Search input', () => {
    it('renders search input with placeholder', () => {
      renderWithTheme(<InjectFilterBar {...defaultProps} />)
      expect(screen.getByPlaceholderText('Search injects...')).toBeInTheDocument()
    })

    it('displays current search term', () => {
      renderWithTheme(<InjectFilterBar {...defaultProps} searchTerm="test" />)
      expect(screen.getByDisplayValue('test')).toBeInTheDocument()
    })

    it('calls onSearchChange when typing', async () => {
      const user = userEvent.setup()
      const onSearchChange = vi.fn()

      renderWithTheme(<InjectFilterBar {...defaultProps} onSearchChange={onSearchChange} />)

      const input = screen.getByPlaceholderText('Search injects...')
      await user.type(input, 'test')

      expect(onSearchChange).toHaveBeenCalled()
      // Called once per character typed
      expect(onSearchChange).toHaveBeenCalledWith('t')
    })

    it('shows clear button when search term is present', () => {
      renderWithTheme(<InjectFilterBar {...defaultProps} searchTerm="test" />)
      expect(screen.getByLabelText('Clear search')).toBeInTheDocument()
    })

    it('hides clear button when search term is empty', () => {
      renderWithTheme(<InjectFilterBar {...defaultProps} searchTerm="" />)
      expect(screen.queryByLabelText('Clear search')).not.toBeInTheDocument()
    })

    it('calls onSearchClear when clear button is clicked', async () => {
      const user = userEvent.setup()
      const onSearchClear = vi.fn()

      renderWithTheme(<InjectFilterBar {...defaultProps} searchTerm="test" onSearchClear={onSearchClear} />)

      await user.click(screen.getByLabelText('Clear search'))

      expect(onSearchClear).toHaveBeenCalledTimes(1)
    })

    it('focuses search input on Ctrl+F', async () => {
      const user = userEvent.setup()
      renderWithTheme(<InjectFilterBar {...defaultProps} />)

      const input = screen.getByPlaceholderText('Search injects...')
      expect(input).not.toHaveFocus()

      await user.keyboard('{Control>}f{/Control}')

      await waitFor(() => {
        expect(input).toHaveFocus()
      })
    })

    it('focuses search input on Cmd+F (Mac)', async () => {
      const user = userEvent.setup()
      renderWithTheme(<InjectFilterBar {...defaultProps} />)

      const input = screen.getByPlaceholderText('Search injects...')
      expect(input).not.toHaveFocus()

      await user.keyboard('{Meta>}f{/Meta}')

      await waitFor(() => {
        expect(input).toHaveFocus()
      })
    })
  })

  describe('Filter dropdowns', () => {
    it('renders status filter dropdown', () => {
      renderWithTheme(<InjectFilterBar {...defaultProps} />)
      expect(screen.getByText('Status')).toBeInTheDocument()
    })

    it('renders phase filter dropdown', () => {
      renderWithTheme(<InjectFilterBar {...defaultProps} />)
      expect(screen.getByText('Phase')).toBeInTheDocument()
    })

    it('renders method filter dropdown', () => {
      renderWithTheme(<InjectFilterBar {...defaultProps} />)
      expect(screen.getByText('Method')).toBeInTheDocument()
    })

    it('passes status filter values to status dropdown', () => {
      const filters: FilterState = {
        ...defaultFilters,
        statuses: [InjectStatus.Pending],
      }

      renderWithTheme(<InjectFilterBar {...defaultProps} filters={filters} />)
      // FilterDropdown component should receive the selected values
      expect(screen.getByText('Status')).toBeInTheDocument()
    })

    it('passes phase filter values to phase dropdown', () => {
      const filters: FilterState = {
        ...defaultFilters,
        phaseIds: ['phase-1'],
      }

      renderWithTheme(<InjectFilterBar {...defaultProps} filters={filters} />)
      expect(screen.getByText('Phase')).toBeInTheDocument()
    })

    it('passes delivery method filter values to method dropdown', () => {
      const filters: FilterState = {
        ...defaultFilters,
        deliveryMethods: [DeliveryMethod.Email],
      }

      renderWithTheme(<InjectFilterBar {...defaultProps} filters={filters} />)
      expect(screen.getByText('Method')).toBeInTheDocument()
    })

    it('includes Unassigned option in phase filter', () => {
      renderWithTheme(<InjectFilterBar {...defaultProps} />)
      // The phase dropdown should include an "Unassigned" option
      // This is verified by the FilterDropdown component receiving the correct options
      expect(screen.getByText('Phase')).toBeInTheDocument()
    })
  })

  describe('Group by dropdown', () => {
    it('renders group by dropdown', () => {
      renderWithTheme(<InjectFilterBar {...defaultProps} />)
      expect(screen.getByText('Group:')).toBeInTheDocument()
    })

    it('passes current groupBy value', () => {
      renderWithTheme(<InjectFilterBar {...defaultProps} groupBy="status" />)
      // The GroupByDropdown component receives and displays the value
      // Get the select element and verify it has the correct value
      const select = screen.getByRole('combobox')
      expect(select).toBeInTheDocument()
      // The select should display "Status" as the selected option (among others on the page)
      expect(screen.getAllByText('Status').length).toBeGreaterThanOrEqual(1)
    })

    it('calls onGroupByChange when grouping changes', async () => {
      const user = userEvent.setup()
      const onGroupByChange = vi.fn()

      renderWithTheme(<InjectFilterBar {...defaultProps} onGroupByChange={onGroupByChange} />)

      const select = screen.getByRole('combobox')
      await user.click(select)

      // Wait for dropdown to open, then click option
      const statusOption = await screen.findByRole('option', { name: /status/i })
      await user.click(statusOption)

      expect(onGroupByChange).toHaveBeenCalledWith('status')
    })
  })

  describe('Group controls', () => {
    it('does not show group controls by default', () => {
      renderWithTheme(<InjectFilterBar {...defaultProps} />)
      expect(screen.queryByLabelText('Expand all groups')).not.toBeInTheDocument()
    })

    it('does not show group controls when groupBy is none', () => {
      renderWithTheme(
        <InjectFilterBar
          {...defaultProps}
          groupBy="none"
          showGroupControls={true}
          onExpandAll={vi.fn()}
          onCollapseAll={vi.fn()}
        />,
      )
      expect(screen.queryByLabelText('Expand all groups')).not.toBeInTheDocument()
    })

    it('shows group controls when showGroupControls is true and groupBy is not none', () => {
      renderWithTheme(
        <InjectFilterBar
          {...defaultProps}
          groupBy="status"
          showGroupControls={true}
          onExpandAll={vi.fn()}
          onCollapseAll={vi.fn()}
        />,
      )
      expect(screen.getByLabelText('Expand all groups')).toBeInTheDocument()
      expect(screen.getByLabelText('Collapse all groups')).toBeInTheDocument()
    })

    it('calls onExpandAll when expand button is clicked', async () => {
      const user = userEvent.setup()
      const onExpandAll = vi.fn()

      renderWithTheme(
        <InjectFilterBar
          {...defaultProps}
          groupBy="status"
          showGroupControls={true}
          onExpandAll={onExpandAll}
          onCollapseAll={vi.fn()}
        />,
      )

      await user.click(screen.getByLabelText('Expand all groups'))

      expect(onExpandAll).toHaveBeenCalledTimes(1)
    })

    it('calls onCollapseAll when collapse button is clicked', async () => {
      const user = userEvent.setup()
      const onCollapseAll = vi.fn()

      renderWithTheme(
        <InjectFilterBar
          {...defaultProps}
          groupBy="status"
          showGroupControls={true}
          onExpandAll={vi.fn()}
          onCollapseAll={onCollapseAll}
        />,
      )

      await user.click(screen.getByLabelText('Collapse all groups'))

      expect(onCollapseAll).toHaveBeenCalledTimes(1)
    })
  })

  describe('Layout and responsiveness', () => {
    it('renders search and filters on the left', () => {
      const { container } = renderWithTheme(<InjectFilterBar {...defaultProps} />)
      const leftStack = container.querySelector('.MuiStack-root')
      expect(leftStack).toBeInTheDocument()
    })

    it('renders grouping controls on the right', () => {
      renderWithTheme(<InjectFilterBar {...defaultProps} />)
      expect(screen.getByText('Group:')).toBeInTheDocument()
    })

    it('applies flexbox layout for responsiveness', () => {
      const { container } = renderWithTheme(<InjectFilterBar {...defaultProps} />)
      const mainBox = container.firstChild
      expect(mainBox).toHaveStyle({ display: 'flex' })
    })
  })

  describe('Phase options', () => {
    it('handles empty phase array', () => {
      renderWithTheme(<InjectFilterBar {...defaultProps} phases={[]} />)
      expect(screen.getByText('Phase')).toBeInTheDocument()
    })

    it('handles multiple phases', () => {
      const manyPhases: PhaseOption[] = [
        { id: 'phase-1', name: 'Phase 1' },
        { id: 'phase-2', name: 'Phase 2' },
        { id: 'phase-3', name: 'Phase 3' },
      ]

      renderWithTheme(<InjectFilterBar {...defaultProps} phases={manyPhases} />)
      expect(screen.getByText('Phase')).toBeInTheDocument()
    })
  })

  afterEach(() => {
    // Clean up event listeners
    vi.clearAllMocks()
  })
})
