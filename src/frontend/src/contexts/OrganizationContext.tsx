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
} from 'react';
import apiClient from '../core/services/api';
import { useAuth } from './AuthContext';
import type { OrganizationMembership } from '@/features/organizations/types';

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
  /** Switch to a different organization */
  switchOrganization: (orgId: string) => Promise<void>;
  /** Refresh the list of memberships */
  refreshMemberships: () => Promise<void>;
}

const OrganizationContext = createContext<OrganizationContextValue | null>(null);

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
    const payload = JSON.parse(atob(token.split('.')[1]));

    // Extract org claims from JWT
    const orgId = payload.org_id;
    const orgName = payload.org_name;
    const orgSlug = payload.org_slug;
    const orgRole = payload.org_role;

    if (!orgId || !orgName) {
      return null;
    }

    return {
      id: orgId,
      name: orgName,
      slug: orgSlug || '',
      role: orgRole || 'OrgUser',
    };
  } catch (err) {
    console.error('[OrganizationContext] Failed to parse org from token:', err);
    return null;
  }
}

/**
 * Organization context provider
 */
export const OrganizationProvider: FC<OrganizationProviderProps> = ({ children }) => {
  const { accessToken, isAuthenticated } = useAuth();
  const [currentOrg, setCurrentOrg] = useState<CurrentOrganization | null>(null);
  const [memberships, setMemberships] = useState<OrganizationMembership[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isPending, setIsPending] = useState(false);

  // Track if we're currently switching to prevent duplicate requests
  const isSwitchingRef = useRef(false);

  /**
   * Fetch user's organization memberships
   */
  const refreshMemberships = useCallback(async () => {
    if (!isAuthenticated) {
      console.log('[OrganizationContext] Not authenticated, skipping membership fetch');
      setMemberships([]);
      setCurrentOrg(null);
      setIsPending(true);
      setIsLoading(false);
      return;
    }

    try {
      console.log('[OrganizationContext] Fetching user memberships');
      const response = await apiClient.get<GetMembershipsResponse>('/users/me/organizations');

      console.log('[OrganizationContext] Memberships fetched:', {
        currentOrgId: response.data.currentOrganizationId,
        membershipCount: response.data.memberships.length,
      });

      setMemberships(response.data.memberships);

      // User has no organizations - they're pending assignment
      if (response.data.memberships.length === 0) {
        console.log('[OrganizationContext] User has no memberships - pending status');
        setIsPending(true);
        setCurrentOrg(null);
      } else {
        setIsPending(false);

        // Extract current org from access token
        if (accessToken) {
          const orgFromToken = parseOrgFromToken(accessToken);
          if (orgFromToken) {
            console.log('[OrganizationContext] Current org from token:', orgFromToken);
            setCurrentOrg(orgFromToken);
          }
        }
      }
    } catch (error) {
      console.error('[OrganizationContext] Failed to fetch memberships:', error);
      setMemberships([]);
      setCurrentOrg(null);
    } finally {
      setIsLoading(false);
    }
  }, [isAuthenticated, accessToken]);

  /**
   * Switch to a different organization
   */
  const switchOrganization = useCallback(
    async (orgId: string) => {
      if (isSwitchingRef.current) {
        console.log('[OrganizationContext] Switch already in progress, ignoring');
        return;
      }

      isSwitchingRef.current = true;

      try {
        console.log('[OrganizationContext] Switching to organization:', orgId);

        // Call backend to switch organization
        const response = await apiClient.post<SwitchOrganizationResponse>(
          '/users/current-organization',
          { organizationId: orgId }
        );

        console.log('[OrganizationContext] Switch successful:', {
          orgId: response.data.organizationId,
          orgName: response.data.organizationName,
        });

        // The backend returns a new JWT with updated org_id claim
        // The AuthContext will automatically refresh with the new token
        // We'll reload the page to clear all cached org-scoped data
        window.location.reload();
      } catch (error) {
        console.error('[OrganizationContext] Failed to switch organization:', error);
        throw error;
      } finally {
        isSwitchingRef.current = false;
      }
    },
    []
  );

  /**
   * Initialize: Fetch memberships when authenticated
   */
  useEffect(() => {
    if (isAuthenticated) {
      refreshMemberships();
    } else {
      setIsLoading(false);
      setIsPending(true);
      setMemberships([]);
      setCurrentOrg(null);
    }
  }, [isAuthenticated, refreshMemberships]);

  const value: OrganizationContextValue = {
    currentOrg,
    memberships,
    isLoading,
    isPending,
    switchOrganization,
    refreshMemberships,
  };

  return <OrganizationContext.Provider value={value}>{children}</OrganizationContext.Provider>;
};

/**
 * Hook to access organization context
 * Must be used within OrganizationProvider
 */
export const useOrganization = (): OrganizationContextValue => {
  const context = useContext(OrganizationContext);
  if (context === null) {
    throw new Error('useOrganization must be used within OrganizationProvider');
  }
  return context;
};
