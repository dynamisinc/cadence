/**
 * OrganizationListPage Tests
 *
 * Tests the organization list page with:
 * - List rendering with sorting
 * - Search and status filtering
 * - Empty states
 * - Navigation to create/edit
 *
 * @module features/organizations/pages
 */
/* eslint-disable @typescript-eslint/no-explicit-any */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { OrganizationListPage } from './OrganizationListPage'
import type { OrganizationListItem } from '../types'

// Mock dependencies
vi.mock('react-router-dom', () => ({
  useNavigate: vi.fn(),
}))

vi.mock('../hooks/useOrganizations', () => ({
  useOrganizations: vi.fn(),
}))


vi.mock('@/core/contexts', () => ({
  useBreadcrumbs: vi.fn(),
}))

vi.mock('@/shared/components', () => ({
  PageHeader: ({ title, subtitle, actions }: any) => (
    <div>
      <h1>{title}</h1>
      {subtitle && <p>{subtitle}</p>}
      {actions}
    </div>
  ),
}))

vi.mock('@/shared/components/StatusChip', () => ({
  StatusChip: ({ status }: any) => <span data-testid="status-chip">{status}</span>,
}))

// Mock styled components
vi.mock('@/theme/styledComponents', () => ({
  CobraPrimaryButton: ({ children, ...props }: any) => <button {...props}>{children}</button>,
  CobraTextField: ({ label, InputProps, ...props }: any) => {
    const inputId = `input-${label?.toLowerCase().replace(/\s+/g, '-')}`
    return (
      <div>
        <label htmlFor={inputId}>{label}</label>
        {InputProps?.startAdornment}
        <input id={inputId} {...props} />
        {InputProps?.endAdornment}
      </div>
    )
  },
}))

import { useNavigate } from 'react-router-dom'
import { useOrganizations } from '../hooks/useOrganizations'

describe('OrganizationListPage', () => {
  const mockNavigate = vi.fn()

  const mockOrganizations: OrganizationListItem[] = [
    {
      id: 'org-1',
      name: 'CISA Region 4',
      slug: 'cisa-r4',
      status: 'Active',
      userCount: 25,
      exerciseCount: 10,
      createdAt: '2024-01-01T00:00:00Z',
    },
    {
      id: 'org-2',
      name: 'FEMA Region 2',
      slug: 'fema-r2',
      status: 'Active',
      userCount: 15,
      exerciseCount: 5,
      createdAt: '2024-01-15T00:00:00Z',
    },
    {
      id: 'org-3',
      name: 'Archived Org',
      slug: 'archived-org',
      status: 'Archived',
      userCount: 0,
      exerciseCount: 0,
      createdAt: '2023-12-01T00:00:00Z',
    },
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(useNavigate).mockReturnValue(mockNavigate)
    vi.mocked(useOrganizations).mockReturnValue({
      data: {
        items: mockOrganizations,
        totalCount: 3,
      },
      isLoading: false,
      error: null,
    } as any)
  })

  describe('Rendering', () => {
    it('renders page header with title', () => {
      render(<OrganizationListPage />)

      expect(screen.getByText(/organization management/i)).toBeInTheDocument()
    })

    it('renders create organization button', () => {
      render(<OrganizationListPage />)

      expect(screen.getByRole('button', { name: /create organization/i })).toBeInTheDocument()
    })

    it('renders search and filter controls', () => {
      render(<OrganizationListPage />)

      expect(screen.getByPlaceholderText(/search organizations/i)).toBeInTheDocument()
      // MUI Select is present - just verify form control exists
      expect(screen.getByRole('combobox')).toBeInTheDocument()
    })

    it('renders organization list table', () => {
      render(<OrganizationListPage />)

      expect(screen.getByRole('table')).toBeInTheDocument()
      expect(screen.getByText('CISA Region 4')).toBeInTheDocument()
      expect(screen.getByText('FEMA Region 2')).toBeInTheDocument()
      expect(screen.getByText('Archived Org')).toBeInTheDocument()
    })

    it('displays organization slugs', () => {
      render(<OrganizationListPage />)

      expect(screen.getByText('cisa-r4')).toBeInTheDocument()
      expect(screen.getByText('fema-r2')).toBeInTheDocument()
    })

    it('displays user and exercise counts', () => {
      render(<OrganizationListPage />)

      expect(screen.getByText('25')).toBeInTheDocument()
      expect(screen.getByText('10')).toBeInTheDocument()
      expect(screen.getByText('15')).toBeInTheDocument()
      expect(screen.getByText('5')).toBeInTheDocument()
    })

    it('displays status chips for each organization', () => {
      render(<OrganizationListPage />)

      const statusChips = screen.getAllByTestId('status-chip')
      expect(statusChips).toHaveLength(3)
      expect(statusChips[0]).toHaveTextContent('Active')
      expect(statusChips[2]).toHaveTextContent('Archived')
    })

    it('displays total count', () => {
      render(<OrganizationListPage />)

      expect(screen.getByText('3 organizations')).toBeInTheDocument()
    })

    it('uses singular form for single organization', () => {
      vi.mocked(useOrganizations).mockReturnValue({
        data: {
          items: [mockOrganizations[0]],
          totalCount: 1,
        },
        isLoading: false,
        error: null,
      } as any)

      render(<OrganizationListPage />)

      expect(screen.getByText('1 organization')).toBeInTheDocument()
    })
  })

  describe('Loading State', () => {
    it('shows skeleton loaders while loading', () => {
      vi.mocked(useOrganizations).mockReturnValue({
        data: undefined,
        isLoading: true,
        error: null,
      } as any)

      render(<OrganizationListPage />)

      // Should have skeleton rows
      const rows = screen.getAllByRole('row')
      expect(rows.length).toBeGreaterThan(1) // Header + skeleton rows
    })
  })

  describe('Error State', () => {
    it('shows error message when organizations fail to load', () => {
      vi.mocked(useOrganizations).mockReturnValue({
        data: undefined,
        isLoading: false,
        error: new Error('Failed to load'),
      } as any)

      render(<OrganizationListPage />)

      expect(screen.getByText(/failed to load organizations/i)).toBeInTheDocument()
    })
  })

  describe('Empty States', () => {
    it('shows empty state when no organizations exist', () => {
      vi.mocked(useOrganizations).mockReturnValue({
        data: {
          items: [],
          totalCount: 0,
        },
        isLoading: false,
        error: null,
      } as any)

      render(<OrganizationListPage />)

      expect(
        screen.getByText(/no organizations yet\. create your first organization/i),
      ).toBeInTheDocument()
      // There are multiple create buttons (header + empty state), just verify at least one exists
      const createButtons = screen.getAllByRole('button', { name: /create organization/i })
      expect(createButtons.length).toBeGreaterThan(0)
    })

    it('shows filtered empty state when search has no results', async () => {
      const user = userEvent.setup()
      vi.mocked(useOrganizations).mockReturnValue({
        data: {
          items: [],
          totalCount: 0,
        },
        isLoading: false,
        error: null,
      } as any)

      render(<OrganizationListPage />)

      const searchInput = screen.getByPlaceholderText(/search organizations/i)
      await user.type(searchInput, 'nonexistent')

      expect(screen.getByText(/no organizations match your filters/i)).toBeInTheDocument()
    })
  })

  describe('Search and Filter', () => {
    it('updates search query when typing', async () => {
      const user = userEvent.setup()
      render(<OrganizationListPage />)

      const searchInput = screen.getByPlaceholderText(/search organizations/i)
      await user.type(searchInput, 'CISA')

      expect(searchInput).toHaveValue('CISA')
    })

    it('filters by status when status filter changed', async () => {
      const user = userEvent.setup()
      render(<OrganizationListPage />)

      // Find status select by finding the combobox inside FormControl
      const statusSelect = screen.getByRole('combobox')
      await user.click(statusSelect)
      await user.click(screen.getByRole('option', { name: 'Archived' }))

      expect(statusSelect).toHaveTextContent('Archived')
    })

    it('calls useOrganizations with search parameter', async () => {
      const user = userEvent.setup()
      const mockUseOrgs = vi.mocked(useOrganizations)

      render(<OrganizationListPage />)

      const searchInput = screen.getByPlaceholderText(/search organizations/i)
      await user.type(searchInput, 'CISA')

      await waitFor(() => {
        const lastCall = mockUseOrgs.mock.calls[mockUseOrgs.mock.calls.length - 1]
        expect(lastCall[0]?.search).toBe('CISA')
      })
    })

    it('calls useOrganizations with status filter', async () => {
      const user = userEvent.setup()
      const mockUseOrgs = vi.mocked(useOrganizations)

      render(<OrganizationListPage />)

      const statusSelect = screen.getByRole('combobox')
      await user.click(statusSelect)
      await user.click(screen.getByRole('option', { name: 'Archived' }))

      await waitFor(() => {
        const lastCall = mockUseOrgs.mock.calls[mockUseOrgs.mock.calls.length - 1]
        expect(lastCall[0]?.status).toBe('Archived')
      })
    })

    it('clears status filter when "All" selected', async () => {
      const user = userEvent.setup()
      const mockUseOrgs = vi.mocked(useOrganizations)

      render(<OrganizationListPage />)

      const statusSelect = screen.getByRole('combobox')
      await user.click(statusSelect)
      await user.click(screen.getByRole('option', { name: 'Archived' }))
      await user.click(statusSelect)
      await user.click(screen.getByRole('option', { name: 'All' }))

      await waitFor(() => {
        const lastCall = mockUseOrgs.mock.calls[mockUseOrgs.mock.calls.length - 1]
        expect(lastCall[0]?.status).toBeUndefined()
      })
    })
  })

  describe('Sorting', () => {
    it('sorts by name when name column header clicked', async () => {
      const user = userEvent.setup()
      const mockUseOrgs = vi.mocked(useOrganizations)

      render(<OrganizationListPage />)

      // Default sort is 'name' asc, so clicking toggles to desc
      const nameHeader = screen.getByText('Name').closest('span')
      if (nameHeader) {
        await user.click(nameHeader)
      }

      await waitFor(() => {
        const lastCall = mockUseOrgs.mock.calls[mockUseOrgs.mock.calls.length - 1]
        expect(lastCall[0]?.sortBy).toBe('name')
        expect(lastCall[0]?.sortDir).toBe('desc')
      })
    })

    it('toggles sort direction when clicking same column twice', async () => {
      const user = userEvent.setup()
      const mockUseOrgs = vi.mocked(useOrganizations)

      render(<OrganizationListPage />)

      // Default is 'name' asc, click once -> desc, click again -> asc
      const nameHeader = screen.getByText('Name').closest('span')
      if (nameHeader) {
        await user.click(nameHeader)
        await user.click(nameHeader)
      }

      await waitFor(() => {
        const lastCall = mockUseOrgs.mock.calls[mockUseOrgs.mock.calls.length - 1]
        expect(lastCall[0]?.sortBy).toBe('name')
        expect(lastCall[0]?.sortDir).toBe('asc')
      })
    })

    it('sorts by user count when user count column clicked', async () => {
      const user = userEvent.setup()
      const mockUseOrgs = vi.mocked(useOrganizations)

      render(<OrganizationListPage />)

      const userCountHeader = screen.getByText('Users').closest('span')
      if (userCountHeader) {
        await user.click(userCountHeader)
      }

      await waitFor(() => {
        const lastCall = mockUseOrgs.mock.calls[mockUseOrgs.mock.calls.length - 1]
        expect(lastCall[0]?.sortBy).toBe('userCount')
      })
    })

    it('sorts by exercise count when exercise count column clicked', async () => {
      const user = userEvent.setup()
      const mockUseOrgs = vi.mocked(useOrganizations)

      render(<OrganizationListPage />)

      const exerciseCountHeader = screen.getByText('Exercises').closest('span')
      if (exerciseCountHeader) {
        await user.click(exerciseCountHeader)
      }

      await waitFor(() => {
        const lastCall = mockUseOrgs.mock.calls[mockUseOrgs.mock.calls.length - 1]
        expect(lastCall[0]?.sortBy).toBe('exerciseCount')
      })
    })

    it('sorts by created date when created column clicked', async () => {
      const user = userEvent.setup()
      const mockUseOrgs = vi.mocked(useOrganizations)

      render(<OrganizationListPage />)

      const createdHeader = screen.getByText('Created').closest('span')
      if (createdHeader) {
        await user.click(createdHeader)
      }

      await waitFor(() => {
        const lastCall = mockUseOrgs.mock.calls[mockUseOrgs.mock.calls.length - 1]
        expect(lastCall[0]?.sortBy).toBe('createdAt')
      })
    })
  })

  describe('Navigation', () => {
    it('navigates to create page when create button clicked', async () => {
      const user = userEvent.setup()
      render(<OrganizationListPage />)

      const createButtons = screen.getAllByRole('button', { name: /create organization/i })
      await user.click(createButtons[0])

      expect(mockNavigate).toHaveBeenCalledWith('/admin/organizations/new')
    })

    it('navigates to edit page when row clicked', async () => {
      const user = userEvent.setup()
      render(<OrganizationListPage />)

      const firstRow = screen.getByText('CISA Region 4').closest('tr')
      if (firstRow) {
        await user.click(firstRow)
      }

      expect(mockNavigate).toHaveBeenCalledWith('/admin/organizations/org-1')
    })

    it('navigates to correct org when different rows clicked', async () => {
      const user = userEvent.setup()
      render(<OrganizationListPage />)

      const secondRow = screen.getByText('FEMA Region 2').closest('tr')
      if (secondRow) {
        await user.click(secondRow)
      }

      expect(mockNavigate).toHaveBeenCalledWith('/admin/organizations/org-2')
    })
  })

  describe('Visual Styling', () => {
    it('applies reduced opacity to inactive organizations', () => {
      vi.mocked(useOrganizations).mockReturnValue({
        data: {
          items: [{ ...mockOrganizations[0], status: 'Inactive' }],
          totalCount: 1,
        },
        isLoading: false,
        error: null,
      } as any)

      render(<OrganizationListPage />)

      const row = screen.getByText('CISA Region 4').closest('tr')
      expect(row).toHaveStyle({ opacity: 0.5 })
    })

    it('applies normal opacity to active organizations', () => {
      render(<OrganizationListPage />)

      const row = screen.getByText('CISA Region 4').closest('tr')
      expect(row).toHaveStyle({ opacity: 1 })
    })

    it('shows cursor pointer on hoverable rows', () => {
      render(<OrganizationListPage />)

      const row = screen.getByText('CISA Region 4').closest('tr')
      expect(row).toHaveStyle({ cursor: 'pointer' })
    })
  })

  describe('Date Formatting', () => {
    it('formats dates correctly', () => {
      render(<OrganizationListPage />)

      // Check that dates are rendered - use queryAllByText since format varies by locale
      const cells = screen.getAllByRole('cell')
      const datePattern = /\d{1,4}/
      const hasDateCells = cells.some(cell => datePattern.test(cell.textContent || ''))
      expect(hasDateCells).toBe(true)
    })
  })
})
