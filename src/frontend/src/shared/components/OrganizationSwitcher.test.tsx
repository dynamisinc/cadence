/**
 * OrganizationSwitcher Component Tests
 *
 * @module shared/components
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThemeProvider } from '@mui/material/styles'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { OrganizationSwitcher } from './OrganizationSwitcher'
import { useOrganization } from '@/contexts/OrganizationContext'
import { useAuth } from '@/contexts/AuthContext'
import { cobraTheme } from '@/theme/cobraTheme'

// Mock the contexts
vi.mock('@/contexts/OrganizationContext')
vi.mock('@/contexts/AuthContext')

// Mock organizationService to prevent actual API calls
vi.mock('@/features/organizations/services/organizationService', () => ({
  organizationService: {
    getAll: vi.fn().mockResolvedValue({ items: [], totalCount: 0 }),
  },
}))

const createTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
      },
    },
  })

const renderWithProviders = (ui: React.ReactElement) => {
  const testQueryClient = createTestQueryClient()
  return render(
    <QueryClientProvider client={testQueryClient}>
      <ThemeProvider theme={cobraTheme}>{ui}</ThemeProvider>
    </QueryClientProvider>,
  )
}

describe('OrganizationSwitcher', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    // Default mock for useAuth - non-admin user
    const mockUseAuth = vi.mocked(useAuth)
    mockUseAuth.mockReturnValue({
      user: { id: 'u1', email: 'test@test.com', displayName: 'Test User', role: 'User', status: 'Active' },
      isAuthenticated: true,
      isLoading: false,
      accessToken: 'mock-token',
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      refreshAccessToken: vi.fn(),
    })
  })

  it('renders current organization name for single-org users', () => {
    const mockUseOrganization = vi.mocked(useOrganization)
    mockUseOrganization.mockReturnValue({
      currentOrg: { id: '1', name: 'CISA Region 4', slug: 'cisa-r4', role: 'OrgAdmin' },
      memberships: [
        {
          id: 'm1',
          userId: 'u1',
          organizationId: '1',
          organizationName: 'CISA Region 4',
          organizationSlug: 'cisa-r4',
          role: 'OrgAdmin',
          joinedAt: '2024-01-01T00:00:00Z',
          isCurrent: true,
        },
      ],
      isLoading: false,
      isPending: false,
      switchOrganization: vi.fn(),
      refreshMemberships: vi.fn(),
    })

    renderWithProviders(<OrganizationSwitcher />)

    // Should show org name
    expect(screen.getByText('CISA Region 4')).toBeInTheDocument()
    // Should NOT show dropdown arrow (single org, non-admin)
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('renders dropdown button for multi-org users', () => {
    const mockUseOrganization = vi.mocked(useOrganization)
    mockUseOrganization.mockReturnValue({
      currentOrg: { id: '1', name: 'CISA Region 4', slug: 'cisa-r4', role: 'OrgAdmin' },
      memberships: [
        {
          id: 'm1',
          userId: 'u1',
          organizationId: '1',
          organizationName: 'CISA Region 4',
          organizationSlug: 'cisa-r4',
          role: 'OrgAdmin',
          joinedAt: '2024-01-01T00:00:00Z',
          isCurrent: true,
        },
        {
          id: 'm2',
          userId: 'u1',
          organizationId: '2',
          organizationName: 'State EMA',
          organizationSlug: 'state-ema',
          role: 'OrgManager',
          joinedAt: '2024-01-02T00:00:00Z',
          isCurrent: false,
        },
      ],
      isLoading: false,
      isPending: false,
      switchOrganization: vi.fn(),
      refreshMemberships: vi.fn(),
    })

    renderWithProviders(<OrganizationSwitcher />)

    // Should show org name in a button
    const button = screen.getByRole('button')
    expect(button).toHaveTextContent('CISA Region 4')
  })

  it('shows dropdown menu when clicked', async () => {
    const user = userEvent.setup()
    const mockUseOrganization = vi.mocked(useOrganization)
    mockUseOrganization.mockReturnValue({
      currentOrg: { id: '1', name: 'CISA Region 4', slug: 'cisa-r4', role: 'OrgAdmin' },
      memberships: [
        {
          id: 'm1',
          userId: 'u1',
          organizationId: '1',
          organizationName: 'CISA Region 4',
          organizationSlug: 'cisa-r4',
          role: 'OrgAdmin',
          joinedAt: '2024-01-01T00:00:00Z',
          isCurrent: true,
        },
        {
          id: 'm2',
          userId: 'u1',
          organizationId: '2',
          organizationName: 'State EMA',
          organizationSlug: 'state-ema',
          role: 'OrgManager',
          joinedAt: '2024-01-02T00:00:00Z',
          isCurrent: false,
        },
      ],
      isLoading: false,
      isPending: false,
      switchOrganization: vi.fn(),
      refreshMemberships: vi.fn(),
    })

    renderWithProviders(<OrganizationSwitcher />)

    const button = screen.getByRole('button')
    await user.click(button)

    // Should show both organizations in menu
    await waitFor(() => {
      expect(screen.getByRole('menu')).toBeInTheDocument()
    })

    // CISA Region 4 appears in button and menu, so use getAllByText
    expect(screen.getAllByText('CISA Region 4').length).toBeGreaterThanOrEqual(1)
    expect(screen.getByText('State EMA')).toBeInTheDocument()
    expect(screen.getByText('Admin')).toBeInTheDocument()
    expect(screen.getByText('Manager')).toBeInTheDocument()
  })

  it('calls switchOrganization when clicking different org', async () => {
    const user = userEvent.setup()
    const mockSwitchOrganization = vi.fn()
    const mockUseOrganization = vi.mocked(useOrganization)
    mockUseOrganization.mockReturnValue({
      currentOrg: { id: '1', name: 'CISA Region 4', slug: 'cisa-r4', role: 'OrgAdmin' },
      memberships: [
        {
          id: 'm1',
          userId: 'u1',
          organizationId: '1',
          organizationName: 'CISA Region 4',
          organizationSlug: 'cisa-r4',
          role: 'OrgAdmin',
          joinedAt: '2024-01-01T00:00:00Z',
          isCurrent: true,
        },
        {
          id: 'm2',
          userId: 'u1',
          organizationId: '2',
          organizationName: 'State EMA',
          organizationSlug: 'state-ema',
          role: 'OrgManager',
          joinedAt: '2024-01-02T00:00:00Z',
          isCurrent: false,
        },
      ],
      isLoading: false,
      isPending: false,
      switchOrganization: mockSwitchOrganization,
      refreshMemberships: vi.fn(),
    })

    renderWithProviders(<OrganizationSwitcher />)

    const button = screen.getByRole('button')
    await user.click(button)

    // Click on "State EMA" menu item
    const stateEmaItem = await screen.findByText('State EMA')
    await user.click(stateEmaItem)

    expect(mockSwitchOrganization).toHaveBeenCalledWith('2')
  })

  it('shows loading state while switching', async () => {
    const user = userEvent.setup()
    let resolveSwitchPromise: () => void
    const switchPromise = new Promise<void>(resolve => {
      resolveSwitchPromise = resolve
    })

    const mockSwitchOrganization = vi.fn(() => switchPromise)
    const mockUseOrganization = vi.mocked(useOrganization)
    mockUseOrganization.mockReturnValue({
      currentOrg: { id: '1', name: 'CISA Region 4', slug: 'cisa-r4', role: 'OrgAdmin' },
      memberships: [
        {
          id: 'm1',
          userId: 'u1',
          organizationId: '1',
          organizationName: 'CISA Region 4',
          organizationSlug: 'cisa-r4',
          role: 'OrgAdmin',
          joinedAt: '2024-01-01T00:00:00Z',
          isCurrent: true,
        },
        {
          id: 'm2',
          userId: 'u1',
          organizationId: '2',
          organizationName: 'State EMA',
          organizationSlug: 'state-ema',
          role: 'OrgManager',
          joinedAt: '2024-01-02T00:00:00Z',
          isCurrent: false,
        },
      ],
      isLoading: false,
      isPending: false,
      switchOrganization: mockSwitchOrganization,
      refreshMemberships: vi.fn(),
    })

    renderWithProviders(<OrganizationSwitcher />)

    const button = screen.getByRole('button')
    await user.click(button)

    const stateEmaItem = await screen.findByText('State EMA')
    await user.click(stateEmaItem)

    // Should show loading state
    await waitFor(() => {
      expect(screen.getByText(/switching/i)).toBeInTheDocument()
    })

    // Resolve the promise
    resolveSwitchPromise!()
  })

  it('returns null when loading', () => {
    const mockUseOrganization = vi.mocked(useOrganization)
    mockUseOrganization.mockReturnValue({
      currentOrg: null,
      memberships: [],
      isLoading: true,
      isPending: false,
      switchOrganization: vi.fn(),
      refreshMemberships: vi.fn(),
    })

    const { container } = renderWithProviders(<OrganizationSwitcher />)
    expect(container.firstChild).toBeNull()
  })

  it('returns null when user is pending', () => {
    const mockUseOrganization = vi.mocked(useOrganization)
    mockUseOrganization.mockReturnValue({
      currentOrg: null,
      memberships: [],
      isLoading: false,
      isPending: true,
      switchOrganization: vi.fn(),
      refreshMemberships: vi.fn(),
    })

    const { container } = renderWithProviders(<OrganizationSwitcher />)
    expect(container.firstChild).toBeNull()
  })
})
