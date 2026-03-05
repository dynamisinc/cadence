/**
 * OrganizationContext - Organization state management
 *
 * Provides organization context and operations across the app.
 * Manages user's organization memberships and current organization selection.
 *
 * Key behaviors:
 * - All data displayed is scoped to the current organization
 * - Switching organizations refreshes the JWT with new org_id claim
 * - User's last-used organization is remembered (via JWT)
 * - Single-org users don't need prominent switcher UI
 *
 * @module contexts
 * @see docs/features/organization-management/OM-06-organization-switcher.md
 */
import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  useRef,
  type FC,
  type ReactNode,
} from 'react'
import apiClient from '../core/services/api'
import { useAuth } from './AuthContext'
import type { OrganizationMembership } from '@/features/organizations/types'

interface CurrentOrganization {
  id: string;
  name: string;
  slug: string;
  role: string;
}

interface OrganizationContextValue {
  /** Current organization the user is working in */
  currentOrg: CurrentOrganization | null;
  /** All organizations this user belongs to */
  memberships: OrganizationMembership[];
  /** Loading state for initial membership fetch */
  isLoading: boolean;
  /** User has no organization assigned (Pending status) */
  isPending: boolean;
  /** User has memberships but no org currently selected — needs to pick one */
  needsOrgSelection: boolean;
  /** Switch to a different organization */
  switchOrganization: (orgId: string) => Promise<void>;
  /** Refresh the list of memberships */
  refreshMemberships: () => Promise<void>;
}

const OrganizationContext = createContext<OrganizationContextValue | null>(null)

interface OrganizationProviderProps {
  children: ReactNode;
}

interface GetMembershipsResponse {
  currentOrganizationId: string | null;
  memberships: OrganizationMembership[];
}

interface SwitchOrganizationResponse {
  organizationId: string;
  organizationName: string;
  role: string;
  newToken: string;
}

/**
 * Parse JWT token to extract organization info
 */
function parseOrgFromToken(token: string): CurrentOrganization | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))

    // Extract org claims from JWT
    const orgId = payload.org_id
    const orgName = payload.org_name
    const orgSlug = payload.org_slug
    const orgRole = payload.org_role

    console.log('[OrganizationContext] parseOrgFromToken - JWT org claims:', {
      org_id: orgId,
      org_name: orgName,
      org_slug: orgSlug,
      org_role: orgRole,
    })

    if (!orgId || !orgName) {
      console.log('[OrganizationContext] parseOrgFromToken - Missing org_id or org_name, returning null')
      return null
    }

    return {
      id: orgId,
      name: orgName,
      slug: orgSlug || '',
      role: orgRole || 'OrgUser',
    }
  } catch (err) {
    console.error('[OrganizationContext] Failed to parse org from token:', err)
    return null
  }
}

/**
 * Organization context provider
 */
export const OrganizationProvider: FC<OrganizationProviderProps> = ({ children }) => {
  const { accessToken, isAuthenticated } = useAuth()
  const [currentOrg, setCurrentOrg] = useState<CurrentOrganization | null>(null)
  const [memberships, setMemberships] = useState<OrganizationMembership[]>([])
  const [isFetching, setIsFetching] = useState(false)
  const [isPending, setIsPending] = useState(false)

  // Track if we're currently switching to prevent duplicate requests
  const isSwitchingRef = useRef(false)

  // Track the token we last completed a membership fetch for.
  // Used to derive loading state synchronously during render,
  // preventing the one-render gap where isPending=true before the useEffect fires.
  const lastFetchedTokenRef = useRef<string | null>(null)

  // Derive loading: true if actively fetching OR haven't fetched for the current token yet.
  // This is computed during render (not in an effect), so PendingUserGuard sees
  // isLoading=true immediately when isAuthenticated transitions to true.
  const isLoading = isFetching
    || (isAuthenticated && lastFetchedTokenRef.current !== (accessToken ?? null))

  /**
   * Fetch user's organization memberships
   */
  const refreshMemberships = useCallback(async () => {
    if (!isAuthenticated) {
      console.log('[OrganizationContext] Not authenticated, skipping membership fetch')
      setMemberships([])
      setCurrentOrg(null)
      setIsPending(false)
      setIsFetching(false)
      lastFetchedTokenRef.current = null
      return
    }

    setIsFetching(true)

    try {
      console.log('[OrganizationContext] Fetching user memberships')
      const response = await apiClient.get<GetMembershipsResponse>('/users/me/organizations')

      console.log('[OrganizationContext] Memberships fetched:', {
        currentOrgId: response.data.currentOrganizationId,
        membershipCount: response.data.memberships.length,
      })

      setMemberships(response.data.memberships)

      // Always try to extract current org from access token first
      // (SysAdmins may have org claims even without memberships)
      let orgSet = false
      if (accessToken) {
        const orgFromToken = parseOrgFromToken(accessToken)
        if (orgFromToken) {
          console.log('[OrganizationContext] Current org from token:', orgFromToken)
          setCurrentOrg(orgFromToken)
          orgSet = true
        }
      }

      // User has no organizations - they're pending assignment (unless org was set from token)
      if (response.data.memberships.length === 0) {
        console.log('[OrganizationContext] User has no memberships')
        if (!orgSet) {
          // Only mark as pending if we couldn't get org from token
          setIsPending(true)
          setCurrentOrg(null)
        } else {
          // SysAdmin with org context but no membership
          setIsPending(false)
        }
      } else {
        setIsPending(false)

        // Fallback: use membership marked as current (if token parsing failed)
        if (!orgSet) {
          const currentMembership = response.data.memberships.find(m => m.isCurrent)
          if (currentMembership) {
            console.log('[OrganizationContext] Current org from membership:', currentMembership.organizationName)
            setCurrentOrg({
              id: currentMembership.organizationId,
              name: currentMembership.organizationName,
              slug: currentMembership.organizationSlug,
              role: currentMembership.role,
            })
          } else if (response.data.memberships.length === 1) {
            // Single membership — auto-switch via backend to get proper JWT claims
            const membership = response.data.memberships[0]
            console.log('[OrganizationContext] Single membership, auto-switching:', membership.organizationName)
            // Don't await — let it reload the page
            apiClient.post('/users/current-organization', {
              organizationId: membership.organizationId,
            }).then(() => {
              window.location.reload()
            }).catch(err => {
              console.error('[OrganizationContext] Auto-switch failed:', err)
              // Fallback: set locally even though JWT won't have claims
              setCurrentOrg({
                id: membership.organizationId,
                name: membership.organizationName,
                slug: membership.organizationSlug,
                role: membership.role,
              })
            })
            // Mark as fetched so isLoading becomes false while reload is pending
            lastFetchedTokenRef.current = accessToken ?? null
          } else {
            // Multiple memberships, none selected — user needs to pick
            console.log('[OrganizationContext] Multiple memberships but none selected, needs org selection')
          }
        }
      }
      // Mark this token as fetched so the derived isLoading becomes false
      lastFetchedTokenRef.current = accessToken ?? null
    } catch (error) {
      console.error('[OrganizationContext] Failed to fetch memberships:', error)
      setMemberships([])
      setCurrentOrg(null)
      // Still mark as fetched so we don't loop
      lastFetchedTokenRef.current = accessToken ?? null
    } finally {
      setIsFetching(false)
    }
  }, [isAuthenticated, accessToken])

  /**
   * Switch to a different organization
   */
  const switchOrganization = useCallback(
    async (orgId: string) => {
      if (isSwitchingRef.current) {
        console.log('[OrganizationContext] Switch already in progress, ignoring')
        return
      }

      isSwitchingRef.current = true

      try {
        console.log('[OrganizationContext] Switching to organization:', orgId)

        // Call backend to switch organization
        const response = await apiClient.post<SwitchOrganizationResponse>(
          '/users/current-organization',
          { organizationId: orgId },
        )

        console.log('[OrganizationContext] Switch successful:', {
          orgId: response.data.organizationId,
          orgName: response.data.organizationName,
        })

        // The backend returns a new JWT with updated org_id claim
        // The AuthContext will automatically refresh with the new token
        // We'll reload the page to clear all cached org-scoped data
        window.location.reload()
      } catch (error) {
        console.error('[OrganizationContext] Failed to switch organization:', error)
        throw error
      } finally {
        isSwitchingRef.current = false
      }
    },
    [],
  )

  /**
   * Initialize: Fetch memberships when authenticated
   */
  useEffect(() => {
    if (isAuthenticated) {
      refreshMemberships()
    } else {
      setIsFetching(false)
      setIsPending(false)
      setMemberships([])
      setCurrentOrg(null)
      lastFetchedTokenRef.current = null
    }
  }, [isAuthenticated, refreshMemberships])

  // User has memberships but no org selected (and not still loading/pending)
  // This covers both multi-org users who need to pick, and single-org users
  // whose auto-switch hasn't completed yet
  const needsOrgSelection = !isLoading && !isPending && !currentOrg && memberships.length > 0

  const value: OrganizationContextValue = {
    currentOrg,
    memberships,
    isLoading,
    isPending,
    needsOrgSelection,
    switchOrganization,
    refreshMemberships,
  }

  return <OrganizationContext.Provider value={value}>{children}</OrganizationContext.Provider>
}

/**
 * Hook to access organization context
 * Must be used within OrganizationProvider
 */
export const useOrganization = (): OrganizationContextValue => {
  const context = useContext(OrganizationContext)
  if (context === null) {
    throw new Error('useOrganization must be used within OrganizationProvider')
  }
  return context
}
