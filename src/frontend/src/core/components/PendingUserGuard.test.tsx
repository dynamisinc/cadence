/**
 * PendingUserGuard Tests
 *
 * Tests for the guard component that redirects users without organization
 * membership to the /pending page.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { BrowserRouter, Routes, Route, MemoryRouter } from 'react-router-dom'
import { PendingUserGuard } from './PendingUserGuard'
import * as OrganizationContext from '../../contexts/OrganizationContext'
import * as AuthContext from '../../contexts/AuthContext'
import type { ReactNode } from 'react'

// Mock the contexts
vi.mock('../../contexts/OrganizationContext', async () => {
  const actual = await vi.importActual('../../contexts/OrganizationContext')
  return {
    ...actual,
    useOrganization: vi.fn(),
  }
})

vi.mock('../../contexts/AuthContext', async () => {
  const actual = await vi.importActual('../../contexts/AuthContext')
  return {
    ...actual,
    useAuth: vi.fn(),
  }
})

// Mock Loading component
vi.mock('../../shared/components/Loading', () => ({
  Loading: () => <div>Loading...</div>,
}))

// Helper to render with router at specific route
const renderWithRouter = (
  ui: ReactNode,
  { route = '/' } = {},
) => {
  return render(
    <MemoryRouter initialEntries={[route]}>
      <Routes>
        <Route path="/" element={ui} />
        <Route path="/pending" element={ui} />
        <Route path="/settings" element={ui} />
        <Route path="/login" element={ui} />
        <Route path="/protected" element={ui} />
      </Routes>
    </MemoryRouter>,
  )
}

describe('PendingUserGuard', () => {
  const mockUseOrganization = vi.mocked(OrganizationContext.useOrganization)
  const mockUseAuth = vi.mocked(AuthContext.useAuth)

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('loading states', () => {
    it('shows loading when auth is loading', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: true,
        accessToken: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshAccessToken: vi.fn(),
      })

      mockUseOrganization.mockReturnValue({
        currentOrg: null,
        memberships: [],
        isLoading: false,
        isPending: false,
        switchOrganization: vi.fn(),
        refreshMemberships: vi.fn(),
      })

      render(
        <BrowserRouter>
          <PendingUserGuard>
            <div>Protected Content</div>
          </PendingUserGuard>
        </BrowserRouter>,
      )

      expect(screen.getByText('Loading...')).toBeInTheDocument()
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
    })

    it('shows loading when authenticated and organization is loading', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: 'user-123',
          email: 'user@example.com',
          displayName: 'Test User',
          role: 'OrgUser',
          status: 'Active',
        },
        isAuthenticated: true,
        isLoading: false,
        accessToken: 'mock-token',
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshAccessToken: vi.fn(),
      })

      mockUseOrganization.mockReturnValue({
        currentOrg: null,
        memberships: [],
        isLoading: true,
        isPending: false,
        switchOrganization: vi.fn(),
        refreshMemberships: vi.fn(),
      })

      render(
        <BrowserRouter>
          <PendingUserGuard>
            <div>Protected Content</div>
          </PendingUserGuard>
        </BrowserRouter>,
      )

      expect(screen.getByText('Loading...')).toBeInTheDocument()
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
    })
  })

  describe('unauthenticated users', () => {
    it('renders children when not authenticated', () => {
      mockUseAuth.mockReturnValue({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        accessToken: null,
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshAccessToken: vi.fn(),
      })

      mockUseOrganization.mockReturnValue({
        currentOrg: null,
        memberships: [],
        isLoading: false,
        isPending: true,
        switchOrganization: vi.fn(),
        refreshMemberships: vi.fn(),
      })

      render(
        <BrowserRouter>
          <PendingUserGuard>
            <div>Protected Content</div>
          </PendingUserGuard>
        </BrowserRouter>,
      )

      expect(screen.getByText('Protected Content')).toBeInTheDocument()
    })
  })

  describe('SysAdmin users', () => {
    it('allows Admin users to bypass pending check', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: 'admin-123',
          email: 'admin@example.com',
          displayName: 'System Admin',
          role: 'Admin',
          status: 'Active',
        },
        isAuthenticated: true,
        isLoading: false,
        accessToken: 'mock-token',
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshAccessToken: vi.fn(),
      })

      mockUseOrganization.mockReturnValue({
        currentOrg: null,
        memberships: [],
        isLoading: false,
        isPending: true,
        switchOrganization: vi.fn(),
        refreshMemberships: vi.fn(),
      })

      render(
        <BrowserRouter>
          <PendingUserGuard>
            <div>Protected Content</div>
          </PendingUserGuard>
        </BrowserRouter>,
      )

      expect(screen.getByText('Protected Content')).toBeInTheDocument()
    })
  })

  describe('pending users', () => {
    it('redirects pending user from restricted route to /pending', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: 'pending-user-123',
          email: 'pending@example.com',
          displayName: 'Pending User',
          role: 'OrgUser',
          status: 'Active',
        },
        isAuthenticated: true,
        isLoading: false,
        accessToken: 'mock-token',
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshAccessToken: vi.fn(),
      })

      mockUseOrganization.mockReturnValue({
        currentOrg: null,
        memberships: [],
        isLoading: false,
        isPending: true,
        switchOrganization: vi.fn(),
        refreshMemberships: vi.fn(),
      })

      // Render at a route that requires a redirect (like /dashboard)
      render(
        <MemoryRouter initialEntries={['/dashboard']}>
          <Routes>
            <Route
              path="/dashboard"
              element={
                <PendingUserGuard>
                  <div>Protected Content</div>
                </PendingUserGuard>
              }
            />
            <Route path="/pending" element={<div>Pending User Page</div>} />
          </Routes>
        </MemoryRouter>,
      )

      // Should redirect to /pending, showing the pending page instead of protected content
      expect(screen.getByText('Pending User Page')).toBeInTheDocument()
      expect(screen.queryByText('Protected Content')).not.toBeInTheDocument()
    })

    it('allows pending user to access /pending route', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: 'pending-user-123',
          email: 'pending@example.com',
          displayName: 'Pending User',
          role: 'OrgUser',
          status: 'Active',
        },
        isAuthenticated: true,
        isLoading: false,
        accessToken: 'mock-token',
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshAccessToken: vi.fn(),
      })

      mockUseOrganization.mockReturnValue({
        currentOrg: null,
        memberships: [],
        isLoading: false,
        isPending: true,
        switchOrganization: vi.fn(),
        refreshMemberships: vi.fn(),
      })

      renderWithRouter(
        <PendingUserGuard>
          <div>Pending Content</div>
        </PendingUserGuard>,
        { route: '/pending' },
      )

      // When on /pending route, the children are rendered (not redirected)
      expect(screen.getByText('Pending Content')).toBeInTheDocument()
    })

    it('allows pending user to access /settings route', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: 'pending-user-123',
          email: 'pending@example.com',
          displayName: 'Pending User',
          role: 'OrgUser',
          status: 'Active',
        },
        isAuthenticated: true,
        isLoading: false,
        accessToken: 'mock-token',
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshAccessToken: vi.fn(),
      })

      mockUseOrganization.mockReturnValue({
        currentOrg: null,
        memberships: [],
        isLoading: false,
        isPending: true,
        switchOrganization: vi.fn(),
        refreshMemberships: vi.fn(),
      })

      renderWithRouter(
        <PendingUserGuard>
          <div>Settings Content</div>
        </PendingUserGuard>,
        { route: '/settings' },
      )

      expect(screen.getByText('Settings Content')).toBeInTheDocument()
    })

    it('allows pending user to access /login route', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: 'pending-user-123',
          email: 'pending@example.com',
          displayName: 'Pending User',
          role: 'OrgUser',
          status: 'Active',
        },
        isAuthenticated: true,
        isLoading: false,
        accessToken: 'mock-token',
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshAccessToken: vi.fn(),
      })

      mockUseOrganization.mockReturnValue({
        currentOrg: null,
        memberships: [],
        isLoading: false,
        isPending: true,
        switchOrganization: vi.fn(),
        refreshMemberships: vi.fn(),
      })

      renderWithRouter(
        <PendingUserGuard>
          <div>Login Content</div>
        </PendingUserGuard>,
        { route: '/login' },
      )

      expect(screen.getByText('Login Content')).toBeInTheDocument()
    })
  })

  describe('users with organization membership', () => {
    it('allows user with organization to access content', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: 'user-123',
          email: 'user@example.com',
          displayName: 'Test User',
          role: 'OrgUser',
          status: 'Active',
        },
        isAuthenticated: true,
        isLoading: false,
        accessToken: 'mock-token',
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshAccessToken: vi.fn(),
      })

      mockUseOrganization.mockReturnValue({
        currentOrg: {
          id: 'org-123',
          name: 'Test Organization',
          slug: 'test-org',
          role: 'OrgUser',
        },
        memberships: [
          {
            id: 'membership-123',
            userId: 'user-123',
            organizationId: 'org-123',
            organizationName: 'Test Organization',
            organizationSlug: 'test-org',
            role: 'OrgUser',
            joinedAt: '2024-01-01T00:00:00Z',
            isCurrent: true,
          },
        ],
        isLoading: false,
        isPending: false,
        switchOrganization: vi.fn(),
        refreshMemberships: vi.fn(),
      })

      render(
        <BrowserRouter>
          <PendingUserGuard>
            <div>Protected Content</div>
          </PendingUserGuard>
        </BrowserRouter>,
      )

      expect(screen.getByText('Protected Content')).toBeInTheDocument()
    })

    it('allows user with organization to access any route', () => {
      mockUseAuth.mockReturnValue({
        user: {
          id: 'user-123',
          email: 'user@example.com',
          displayName: 'Test User',
          role: 'OrgUser',
          status: 'Active',
        },
        isAuthenticated: true,
        isLoading: false,
        accessToken: 'mock-token',
        login: vi.fn(),
        register: vi.fn(),
        logout: vi.fn(),
        refreshAccessToken: vi.fn(),
      })

      mockUseOrganization.mockReturnValue({
        currentOrg: {
          id: 'org-123',
          name: 'Test Organization',
          slug: 'test-org',
          role: 'OrgUser',
        },
        memberships: [
          {
            id: 'membership-123',
            userId: 'user-123',
            organizationId: 'org-123',
            organizationName: 'Test Organization',
            organizationSlug: 'test-org',
            role: 'OrgUser',
            joinedAt: '2024-01-01T00:00:00Z',
            isCurrent: true,
          },
        ],
        isLoading: false,
        isPending: false,
        switchOrganization: vi.fn(),
        refreshMemberships: vi.fn(),
      })

      renderWithRouter(
        <PendingUserGuard>
          <div>Any Protected Content</div>
        </PendingUserGuard>,
        { route: '/protected' },
      )

      expect(screen.getByText('Any Protected Content')).toBeInTheDocument()
    })
  })
})
