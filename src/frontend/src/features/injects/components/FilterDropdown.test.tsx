/**
 * FilterDropdown Tests
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThemeProvider } from '@mui/material'
import { cobraTheme } from '../../../theme/cobraTheme'
import { FilterDropdown, type FilterOption } from './FilterDropdown'

// Wrap with theme provider for COBRA styled components
const renderWithTheme = (ui: React.ReactElement) => {
  return render(<ThemeProvider theme={cobraTheme}>{ui}</ThemeProvider>)
}

const statusOptions: FilterOption<string>[] = [
  { value: 'Pending', label: 'Pending' },
  { value: 'Fired', label: 'Fired' },
  { value: 'Skipped', label: 'Skipped' },
]

describe('FilterDropdown', () => {
  describe('Rendering', () => {
    it('renders the dropdown button with label', () => {
      renderWithTheme(
        <FilterDropdown
          label="Status"
          options={statusOptions}
          selected={[]}
          onChange={() => {}}
        />,
      )

      expect(screen.getByText('Status')).toBeInTheDocument()
    })

    it('shows badge count when options are selected', () => {
      renderWithTheme(
        <FilterDropdown
          label="Status"
          options={statusOptions}
          selected={['Pending', 'Fired']}
          onChange={() => {}}
        />,
      )

      expect(screen.getByText('2')).toBeInTheDocument()
    })

    it('does not show badge when no selections', () => {
      renderWithTheme(
        <FilterDropdown
          label="Status"
          options={statusOptions}
          selected={[]}
          onChange={() => {}}
        />,
      )

      expect(screen.queryByText('0')).not.toBeInTheDocument()
    })
  })

  describe('Dropdown Behavior', () => {
    it('opens popover on click', async () => {
      const user = userEvent.setup()

      renderWithTheme(
        <FilterDropdown
          label="Status"
          options={statusOptions}
          selected={[]}
          onChange={() => {}}
        />,
      )

      await user.click(screen.getByText('Status'))

      await waitFor(() => {
        expect(screen.getByText('Pending')).toBeInTheDocument()
        expect(screen.getByText('Fired')).toBeInTheDocument()
        expect(screen.getByText('Skipped')).toBeInTheDocument()
      })
    })

    it('shows all options in the dropdown', async () => {
      const user = userEvent.setup()

      renderWithTheme(
        <FilterDropdown
          label="Status"
          options={statusOptions}
          selected={[]}
          onChange={() => {}}
        />,
      )

      await user.click(screen.getByRole('button'))

      await waitFor(() => {
        statusOptions.forEach(option => {
          expect(screen.getByText(option.label)).toBeInTheDocument()
        })
      })
    })

    it('shows checkboxes for each option', async () => {
      const user = userEvent.setup()

      renderWithTheme(
        <FilterDropdown
          label="Status"
          options={statusOptions}
          selected={[]}
          onChange={() => {}}
        />,
      )

      await user.click(screen.getByRole('button'))

      await waitFor(() => {
        const checkboxes = screen.getAllByRole('checkbox')
        expect(checkboxes).toHaveLength(3)
      })
    })
  })

  describe('Selection', () => {
    it('calls onChange when option is clicked', async () => {
      const user = userEvent.setup()
      const onChange = vi.fn()

      renderWithTheme(
        <FilterDropdown
          label="Status"
          options={statusOptions}
          selected={[]}
          onChange={onChange}
        />,
      )

      await user.click(screen.getByRole('button'))

      await waitFor(async () => {
        await user.click(screen.getByText('Pending'))
      })

      expect(onChange).toHaveBeenCalledWith(['Pending'])
    })

    it('removes option from selection when clicked again', async () => {
      const user = userEvent.setup()
      const onChange = vi.fn()

      renderWithTheme(
        <FilterDropdown
          label="Status"
          options={statusOptions}
          selected={['Pending', 'Fired']}
          onChange={onChange}
        />,
      )

      await user.click(screen.getByRole('button'))

      await waitFor(async () => {
        await user.click(screen.getByText('Pending'))
      })

      expect(onChange).toHaveBeenCalledWith(['Fired'])
    })

    it('shows selected options as checked', async () => {
      const user = userEvent.setup()

      renderWithTheme(
        <FilterDropdown
          label="Status"
          options={statusOptions}
          selected={['Pending']}
          onChange={() => {}}
        />,
      )

      await user.click(screen.getByRole('button'))

      await waitFor(() => {
        const checkboxes = screen.getAllByRole('checkbox')
        const pendingCheckbox = checkboxes[0]
        expect(pendingCheckbox).toBeChecked()
      })
    })
  })

  describe('Quick Actions', () => {
    it('shows Select all and Clear buttons', async () => {
      const user = userEvent.setup()

      renderWithTheme(
        <FilterDropdown
          label="Status"
          options={statusOptions}
          selected={[]}
          onChange={() => {}}
        />,
      )

      await user.click(screen.getByRole('button'))

      await waitFor(() => {
        expect(screen.getByText('Select all')).toBeInTheDocument()
        expect(screen.getByText('Clear')).toBeInTheDocument()
      })
    })

    it('selects all options when Select all is clicked', async () => {
      const user = userEvent.setup()
      const onChange = vi.fn()

      renderWithTheme(
        <FilterDropdown
          label="Status"
          options={statusOptions}
          selected={[]}
          onChange={onChange}
        />,
      )

      await user.click(screen.getByRole('button'))

      await waitFor(async () => {
        await user.click(screen.getByText('Select all'))
      })

      expect(onChange).toHaveBeenCalledWith(['Pending', 'Fired', 'Skipped'])
    })

    it('clears all selections when Clear is clicked', async () => {
      const user = userEvent.setup()
      const onChange = vi.fn()

      renderWithTheme(
        <FilterDropdown
          label="Status"
          options={statusOptions}
          selected={['Pending', 'Fired']}
          onChange={onChange}
        />,
      )

      await user.click(screen.getByRole('button'))

      await waitFor(async () => {
        await user.click(screen.getByText('Clear'))
      })

      expect(onChange).toHaveBeenCalledWith([])
    })

    it('disables Select all when all are selected', async () => {
      const user = userEvent.setup()

      renderWithTheme(
        <FilterDropdown
          label="Status"
          options={statusOptions}
          selected={['Pending', 'Fired', 'Skipped']}
          onChange={() => {}}
        />,
      )

      await user.click(screen.getByRole('button'))

      await waitFor(() => {
        expect(screen.getByText('Select all')).toBeDisabled()
      })
    })

    it('disables Clear when none are selected', async () => {
      const user = userEvent.setup()

      renderWithTheme(
        <FilterDropdown
          label="Status"
          options={statusOptions}
          selected={[]}
          onChange={() => {}}
        />,
      )

      await user.click(screen.getByRole('button'))

      await waitFor(() => {
        expect(screen.getByText('Clear')).toBeDisabled()
      })
    })
  })

  describe('Accessibility', () => {
    it('has correct aria attributes', () => {
      renderWithTheme(
        <FilterDropdown
          label="Status"
          options={statusOptions}
          selected={[]}
          onChange={() => {}}
        />,
      )

      const button = screen.getByRole('button')
      expect(button).toHaveAttribute('aria-haspopup', 'listbox')
      expect(button).toHaveAttribute('aria-expanded', 'false')
      expect(button).toHaveAttribute('aria-label', 'Filter by Status')
    })
  })
})
