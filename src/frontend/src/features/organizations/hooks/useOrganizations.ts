/**
 * Organization React Query Hooks
 *
 * Provides React Query hooks for organization CRUD operations with
 * automatic cache invalidation and optimistic updates.
 *
 * @module features/organizations/hooks
 */
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { organizationService } from '../services/organizationService'
import type {
  CreateOrganizationRequest,
  UpdateOrganizationRequest,
} from '../types'

/**
 * Query key factory for organizations
 */
export const organizationKeys = {
  all: ['organizations'] as const,
  lists: () => [...organizationKeys.all, 'list'] as const,
  list: (filters: string) => [...organizationKeys.lists(), filters] as const,
  details: () => [...organizationKeys.all, 'detail'] as const,
  detail: (id: string) => [...organizationKeys.details(), id] as const,
  current: () => [...organizationKeys.all, 'current'] as const,
}

/**
 * Fetch all organizations (SysAdmin only)
 */
export function useOrganizations(params?: {
  search?: string;
  status?: string;
  sortBy?: string;
  sortDir?: string;
}) {
  return useQuery({
    queryKey: organizationKeys.list(JSON.stringify(params)),
    queryFn: () => organizationService.getAll(params),
  })
}

/**
 * Fetch single organization by ID (SysAdmin only)
 */
export function useOrganization(id: string) {
  return useQuery({
    queryKey: organizationKeys.detail(id),
    queryFn: () => organizationService.getById(id),
    enabled: !!id,
  })
}

/**
 * Fetch current organization (OrgAdmin)
 */
export function useCurrentOrganization() {
  return useQuery({
    queryKey: organizationKeys.current(),
    queryFn: () => organizationService.getCurrent(),
  })
}

/**
 * Create organization mutation (SysAdmin only)
 */
export function useCreateOrganization() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateOrganizationRequest) => organizationService.create(request),
    onSuccess: () => {
      // Invalidate all organization lists
      queryClient.invalidateQueries({ queryKey: organizationKeys.lists() })
    },
  })
}

/**
 * Update organization mutation (SysAdmin only)
 */
export function useUpdateOrganization() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateOrganizationRequest }) =>
      organizationService.update(id, request),
    onSuccess: data => {
      // Invalidate specific org detail
      queryClient.invalidateQueries({ queryKey: organizationKeys.detail(data.id) })
      // Invalidate all lists
      queryClient.invalidateQueries({ queryKey: organizationKeys.lists() })
    },
  })
}

/**
 * Update current organization mutation (OrgAdmin)
 */
export function useUpdateCurrentOrganization() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: UpdateOrganizationRequest) => organizationService.updateCurrent(request),
    onSuccess: () => {
      // Invalidate current org
      queryClient.invalidateQueries({ queryKey: organizationKeys.current() })
    },
  })
}

/**
 * Update current organization approval policy mutation (OrgAdmin)
 */
export function useUpdateCurrentApprovalPolicy() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (policy: string) => organizationService.updateCurrentApprovalPolicy(policy),
    onSuccess: () => {
      // Invalidate current org to refresh the approval policy
      queryClient.invalidateQueries({ queryKey: organizationKeys.current() })
    },
  })
}

/**
 * Archive organization mutation (SysAdmin only)
 */
export function useArchiveOrganization() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => organizationService.archive(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: organizationKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: organizationKeys.lists() })
    },
  })
}

/**
 * Deactivate organization mutation (SysAdmin only)
 */
export function useDeactivateOrganization() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => organizationService.deactivate(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: organizationKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: organizationKeys.lists() })
    },
  })
}

/**
 * Restore organization mutation (SysAdmin only)
 */
export function useRestoreOrganization() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => organizationService.restore(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: organizationKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: organizationKeys.lists() })
    },
  })
}

/**
 * Check slug availability
 */
export function useCheckSlug(slug: string) {
  return useQuery({
    queryKey: ['organizations', 'slug', slug],
    queryFn: () => organizationService.checkSlug(slug),
    enabled: slug.length > 0,
    // Don't cache slug checks for long
    staleTime: 0,
  })
}
