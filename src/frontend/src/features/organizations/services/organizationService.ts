/**
 * Organization API Service
 *
 * Handles all HTTP communication with the organization backend.
 * Provides methods for CRUD operations on organizations (SysAdmin) and
 * viewing/updating the current organization (OrgAdmin).
 *
 * @module features/organizations/services
 */
import apiClient from '@/core/services/api'
import type {
  Organization,
  OrganizationListItem,
  CreateOrganizationRequest,
  UpdateOrganizationRequest,
  SlugCheckResponse,
  OrgMember,
  AddMemberRequest,
  UpdateMemberRoleRequest,
} from '../types'
import type {
  ApprovalPermissionsDto,
  UpdateApprovalPermissionsRequest,
} from '@/types'

export const organizationService = {
  /**
   * Get all organizations (SysAdmin only)
   */
  getAll: async (params?: {
    search?: string;
    status?: string;
    sortBy?: string;
    sortDir?: string;
  }): Promise<{ items: OrganizationListItem[]; totalCount: number }> => {
    const response = await apiClient.get<{ items: OrganizationListItem[]; totalCount: number }>(
      '/admin/organizations',
      { params },
    )
    return response.data
  },

  /**
   * Get organization by ID (SysAdmin only)
   */
  getById: async (id: string): Promise<Organization> => {
    const response = await apiClient.get<Organization>(`/admin/organizations/${id}`)
    return response.data
  },

  /**
   * Create new organization (SysAdmin only)
   */
  create: async (request: CreateOrganizationRequest): Promise<Organization> => {
    const response = await apiClient.post<Organization>('/admin/organizations', request)
    return response.data
  },

  /**
   * Update organization (SysAdmin only)
   */
  update: async (id: string, request: UpdateOrganizationRequest): Promise<Organization> => {
    const response = await apiClient.put<Organization>(`/admin/organizations/${id}`, request)
    return response.data
  },

  /**
   * Check if slug is available
   */
  checkSlug: async (slug: string): Promise<SlugCheckResponse> => {
    const response = await apiClient.get<SlugCheckResponse>('/admin/organizations/check-slug', {
      params: { slug },
    })
    return response.data
  },

  /**
   * Archive organization (SysAdmin only)
   */
  archive: async (id: string): Promise<void> => {
    await apiClient.post(`/admin/organizations/${id}/archive`)
  },

  /**
   * Deactivate organization (SysAdmin only)
   */
  deactivate: async (id: string): Promise<void> => {
    await apiClient.post(`/admin/organizations/${id}/deactivate`)
  },

  /**
   * Restore archived/inactive organization (SysAdmin only)
   */
  restore: async (id: string): Promise<void> => {
    await apiClient.post(`/admin/organizations/${id}/restore`)
  },

  /**
   * Get current organization (OrgAdmin)
   */
  getCurrent: async (): Promise<Organization> => {
    const response = await apiClient.get<Organization>('/organizations/current')
    return response.data
  },

  /**
   * Update current organization (OrgAdmin)
   */
  updateCurrent: async (request: UpdateOrganizationRequest): Promise<Organization> => {
    const response = await apiClient.put<Organization>('/organizations/current', request)
    return response.data
  },

  // =========================================================================
  // Current Organization Member Management (OrgAdmin)
  // =========================================================================

  /**
   * Get all members of the current organization
   */
  getCurrentOrgMembers: async (): Promise<OrgMember[]> => {
    const response = await apiClient.get<OrgMember[]>('/organizations/current/members')
    return response.data
  },

  /**
   * Add a user to the current organization by email
   */
  addCurrentOrgMember: async (request: AddMemberRequest): Promise<OrgMember> => {
    const response = await apiClient.post<OrgMember>('/organizations/current/members', request)
    return response.data
  },

  /**
   * Update a member's role in the current organization
   */
  updateCurrentOrgMemberRole: async (
    membershipId: string,
    request: UpdateMemberRoleRequest,
  ): Promise<void> => {
    await apiClient.put(`/organizations/current/members/${membershipId}`, request)
  },

  /**
   * Remove a member from the current organization
   */
  removeCurrentOrgMember: async (membershipId: string): Promise<void> => {
    await apiClient.delete(`/organizations/current/members/${membershipId}`)
  },

  // =========================================================================
  // Current Organization Approval Permissions (OrgAdmin)
  // =========================================================================

  /**
   * Get approval permissions for the current organization
   */
  getCurrentApprovalPermissions: async (): Promise<ApprovalPermissionsDto> => {
    const response = await apiClient.get<ApprovalPermissionsDto>(
      '/organizations/current/settings/approval-permissions',
    )
    return response.data
  },

  /**
   * Update approval permissions for the current organization
   */
  updateCurrentApprovalPermissions: async (
    request: UpdateApprovalPermissionsRequest,
  ): Promise<ApprovalPermissionsDto> => {
    const response = await apiClient.put<ApprovalPermissionsDto>(
      '/organizations/current/settings/approval-permissions',
      request,
    )
    return response.data
  },

  /**
   * Update approval policy for the current organization (OrgAdmin)
   */
  updateCurrentApprovalPolicy: async (policy: string): Promise<Organization> => {
    const response = await apiClient.put<Organization>(
      '/organizations/current/settings/approval-policy',
      { injectApprovalPolicy: policy },
    )
    return response.data
  },

  // =========================================================================
  // Member Management (SysAdmin only)
  // =========================================================================

  /**
   * Get all members of an organization
   */
  getMembers: async (orgId: string): Promise<OrgMember[]> => {
    const response = await apiClient.get<OrgMember[]>(`/admin/organizations/${orgId}/members`)
    return response.data
  },

  /**
   * Add a user to an organization by email
   */
  addMember: async (orgId: string, request: AddMemberRequest): Promise<OrgMember> => {
    const response = await apiClient.post<OrgMember>(
      `/admin/organizations/${orgId}/members`,
      request,
    )
    return response.data
  },

  /**
   * Update a member's role
   */
  updateMemberRole: async (
    orgId: string,
    membershipId: string,
    request: UpdateMemberRoleRequest,
  ): Promise<void> => {
    await apiClient.put(`/admin/organizations/${orgId}/members/${membershipId}`, request)
  },

  /**
   * Remove a member from an organization
   */
  removeMember: async (orgId: string, membershipId: string): Promise<void> => {
    await apiClient.delete(`/admin/organizations/${orgId}/members/${membershipId}`)
  },

  // =========================================================================
  // Approval Permissions (S11 - SysAdmin only)
  // =========================================================================

  /**
   * Get approval permissions for an organization
   */
  getApprovalPermissions: async (orgId: string): Promise<ApprovalPermissionsDto> => {
    const response = await apiClient.get<ApprovalPermissionsDto>(
      `/admin/organizations/${orgId}/settings/approval-permissions`,
    )
    return response.data
  },

  /**
   * Update approval permissions for an organization
   */
  updateApprovalPermissions: async (
    orgId: string,
    request: UpdateApprovalPermissionsRequest,
  ): Promise<ApprovalPermissionsDto> => {
    const response = await apiClient.put<ApprovalPermissionsDto>(
      `/admin/organizations/${orgId}/settings/approval-permissions`,
      request,
    )
    return response.data
  },

  /**
   * Update approval policy for an organization (Disabled/Optional/Required)
   */
  updateApprovalPolicy: async (
    orgId: string,
    policy: string,
  ): Promise<Organization> => {
    const response = await apiClient.put<Organization>(
      `/admin/organizations/${orgId}/settings/approval-policy`,
      { injectApprovalPolicy: policy },
    )
    return response.data
  },
}
