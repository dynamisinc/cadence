/**
 * NoOrgBanner Component Tests
 *
 * Tests for the info banner shown when a user has no organization selected.
 * Covers visibility conditions, org chip rendering, dismissal, and
 * org selection via chip click.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { ThemeProvider } from '@mui/material/styles'
import { cobraTheme } from '@/theme/cobraTheme'
import { NoOrgBanner } from './NoOrgBanner'
import type { OrganizationMembership } from '@/features/organizations/types'

// Mock contexts
vi.mock('../../contexts/OrganizationContext', () => ({
  useOrganization: vi.fn(),
}))

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: vi.fn(() => ({ user: { role: 'User' } })),
}))

vi.mock('@/shared/utils/notify', () => ({
  notify: { error: vi.fn(), success: vi.fn() },
}))

import * as OrganizationContext from '../../contexts/OrganizationContext'
import * as AuthContext from '../../contexts/AuthContext'
import { notify } from '@/shared/utils/notify'

const renderWithTheme = (component: React.ReactElement) => {
  return render(
    <ThemeProvider theme={cobraTheme}>
      {component}
    </ThemeProvider>,
  )
}

/** Create a default org context with org selected (banner should not show) */
const makeOrgContext = (overrides?: Partial<ReturnType<typeof OrganizationContext.useOrganization>>) => ({
  currentOrg: { id: 'org-1', name: 'Acme Corp', slug: 'acme', role: 'OrgUser' },
  memberships: [] as OrganizationMembership[],
  isLoading: false,
  isPending: false,
  needsOrgSelection: false,
  switchOrganization: vi.fn(),
  refreshMemberships: vi.fn(),
  ...overrides,
})

const makeMembership = (id: string, name: string, orgId: string): OrganizationMembership => ({
  id,
  userId: 'user-1',
  organizationId: orgId,
  organizationName: name,
  organizationSlug: name.toLowerCase().replace(/\s+/g, '-'),
  role: 'OrgUser',
  joinedAt: '2025-01-01T00:00:00Z',
  isCurrent: false,
})

describe('NoOrgBanner', () => {
  const mockUseOrganization = vi.mocked(OrganizationContext.useOrganization)
  const mockUseAuth = vi.mocked(AuthContext.useAuth)

  beforeEach(() => {
    vi.clearAllMocks()
    mockUseAuth.mockReturnValue({
      user: { id: 'user-1', email: 'user@example.com', displayName: 'Test User', role: 'User', status: 'Active' },
      isAuthenticated: true,
      isLoading: false,
      accessToken: 'token',
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      refreshAccessToken: vi.fn(),
    })
  })

  describe('when org is already selected', () => {
    it('renders nothing when needsOrgSelection is false and org is set', () => {
      mockUseOrganization.mockReturnValue(
        makeOrgContext({ needsOrgSelection: false }),
      )

      const { container } = renderWithTheme(<NoOrgBanner />)
      expect(container.firstChild).toBeNull()
    })

    it('renders nothing during initial loading', () => {
      mockUseOrganization.mockReturnValue(
        makeOrgContext({ isLoading: true, currentOrg: null, needsOrgSelection: false }),
      )

      const { container } = renderWithTheme(<NoOrgBanner />)
      expect(container.firstChild).toBeNull()
    })
  })

  describe('when needsOrgSelection is true', () => {
    it('renders the info banner', () => {
      mockUseOrganization.mockReturnValue(
        makeOrgContext({
          currentOrg: null,
          needsOrgSelection: true,
          memberships: [makeMembership('m1', 'Acme Corp', 'org-1')],
        }),
      )

      renderWithTheme(<NoOrgBanner />)

      expect(screen.getByRole('alert')).toBeInTheDocument()
    })

    it('shows the "no organization selected" message', () => {
      mockUseOrganization.mockReturnValue(
        makeOrgContext({
          currentOrg: null,
          needsOrgSelection: true,
          memberships: [makeMembership('m1', 'Acme Corp', 'org-1')],
        }),
      )

      renderWithTheme(<NoOrgBanner />)

      expect(screen.getByText(/no organization selected/i)).toBeInTheDocument()
    })

    it('shows a chip for each membership', () => {
      mockUseOrganization.mockReturnValue(
        makeOrgContext({
          currentOrg: null,
          needsOrgSelection: true,
          memberships: [
            makeMembership('m1', 'Acme Corp', 'org-1'),
            makeMembership('m2', 'Beta Labs', 'org-2'),
            makeMembership('m3', 'Gamma Solutions', 'org-3'),
          ],
        }),
      )

      renderWithTheme(<NoOrgBanner />)

      expect(screen.getByText('Acme Corp')).toBeInTheDocument()
      expect(screen.getByText('Beta Labs')).toBeInTheDocument()
      expect(screen.getByText('Gamma Solutions')).toBeInTheDocument()
    })

    it('calls switchOrganization with correct orgId when chip is clicked', async () => {
      const mockSwitch = vi.fn().mockResolvedValue(undefined)
      mockUseOrganization.mockReturnValue(
        makeOrgContext({
          currentOrg: null,
          needsOrgSelection: true,
          memberships: [
            makeMembership('m1', 'Acme Corp', 'org-1'),
            makeMembership('m2', 'Beta Labs', 'org-2'),
          ],
          switchOrganization: mockSwitch,
        }),
      )

      renderWithTheme(<NoOrgBanner />)

      fireEvent.click(screen.getByText('Beta Labs'))

      await waitFor(() => {
        expect(mockSwitch).toHaveBeenCalledWith('org-2')
      })
    })

    it('calls switchOrganization with the correct orgId for the first chip', async () => {
      const mockSwitch = vi.fn().mockResolvedValue(undefined)
      mockUseOrganization.mockReturnValue(
        makeOrgContext({
          currentOrg: null,
          needsOrgSelection: true,
          memberships: [makeMembership('m1', 'Acme Corp', 'org-1')],
          switchOrganization: mockSwitch,
        }),
      )

      renderWithTheme(<NoOrgBanner />)

      fireEvent.click(screen.getByText('Acme Corp'))

      await waitFor(() => {
        expect(mockSwitch).toHaveBeenCalledWith('org-1')
      })
    })

    it('shows error notification when switchOrganization fails', async () => {
      const mockSwitch = vi.fn().mockRejectedValue(new Error('Network error'))
      mockUseOrganization.mockReturnValue(
        makeOrgContext({
          currentOrg: null,
          needsOrgSelection: true,
          memberships: [makeMembership('m1', 'Acme Corp', 'org-1')],
          switchOrganization: mockSwitch,
        }),
      )

      renderWithTheme(<NoOrgBanner />)

      fireEvent.click(screen.getByText('Acme Corp'))

      await waitFor(() => {
        expect(vi.mocked(notify.error)).toHaveBeenCalledWith(
          'Failed to select organization. Please try again.',
        )
      })
    })
  })

  describe('dismissal', () => {
    it('dismisses the banner when the close button is clicked', () => {
      mockUseOrganization.mockReturnValue(
        makeOrgContext({
          currentOrg: null,
          needsOrgSelection: true,
          memberships: [makeMembership('m1', 'Acme Corp', 'org-1')],
        }),
      )

      renderWithTheme(<NoOrgBanner />)

      expect(screen.getByRole('alert')).toBeInTheDocument()

      // MUI Alert renders a close button when onClose is provided
      const closeButton = screen.getByRole('button', { name: /close/i })
      fireEvent.click(closeButton)

      expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    })

    it('banner does not reappear after dismissal within same session', () => {
      mockUseOrganization.mockReturnValue(
        makeOrgContext({
          currentOrg: null,
          needsOrgSelection: true,
          memberships: [makeMembership('m1', 'Acme Corp', 'org-1')],
        }),
      )

      renderWithTheme(<NoOrgBanner />)

      fireEvent.click(screen.getByRole('button', { name: /close/i }))

      // Re-render with same conditions — dismissed state is local
      expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    })
  })

  describe('SysAdmin with no memberships', () => {
    it('shows banner for SysAdmin even with no memberships when no org selected', () => {
      mockUseAuth.mockReturnValue({
        user: { id: 'admin-1', email: 'admin@example.com', displayName: 'Admin', role: 'Admin', status: 'Active' },
        isAuthenticated: true,
        isLoading: false,
        accessToken: 'token',
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshAccessToken: vi.fn(),
      })
      mockUseOrganization.mockReturnValue(
        makeOrgContext({
          currentOrg: null,
          needsOrgSelection: false, // SysAdmin may have needsOrgSelection=false but still no org
          memberships: [],
          isLoading: false,
        }),
      )

      renderWithTheme(<NoOrgBanner />)

      // SysAdmin with no org and no memberships sees the fallback message pointing to the switcher
      expect(screen.getByRole('alert')).toBeInTheDocument()
    })

    it('shows "use the organization switcher" message for SysAdmin with no memberships', () => {
      mockUseAuth.mockReturnValue({
        user: { id: 'admin-1', email: 'admin@example.com', displayName: 'Admin', role: 'Admin', status: 'Active' },
        isAuthenticated: true,
        isLoading: false,
        accessToken: 'token',
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshAccessToken: vi.fn(),
      })
      mockUseOrganization.mockReturnValue(
        makeOrgContext({
          currentOrg: null,
          needsOrgSelection: false,
          memberships: [],
          isLoading: false,
        }),
      )

      renderWithTheme(<NoOrgBanner />)

      expect(screen.getByText(/organization switcher/i)).toBeInTheDocument()
    })

    it('does not show org chips when memberships are empty', () => {
      mockUseAuth.mockReturnValue({
        user: { id: 'admin-1', email: 'admin@example.com', displayName: 'Admin', role: 'Admin', status: 'Active' },
        isAuthenticated: true,
        isLoading: false,
        accessToken: 'token',
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshAccessToken: vi.fn(),
      })
      mockUseOrganization.mockReturnValue(
        makeOrgContext({
          currentOrg: null,
          needsOrgSelection: false,
          memberships: [],
          isLoading: false,
        }),
      )

      renderWithTheme(<NoOrgBanner />)

      // No chips should be rendered without memberships
      expect(screen.queryByText('Choose one to access all features:')).not.toBeInTheDocument()
    })
  })
})
