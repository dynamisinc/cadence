/**
 * Capability Service
 *
 * API client for organizational capability management.
 * Handles CRUD operations for capabilities.
 */

import { apiClient } from '@/core/services/api'
import type {
  CapabilityDto,
  CreateCapabilityRequest,
  UpdateCapabilityRequest,
  PredefinedLibraryInfo,
  ImportLibraryResult,
} from '../types'

/**
 * Default organization ID (single-tenant architecture)
 * Matches SystemConstants.DefaultOrganizationId in backend
 */
const DEFAULT_ORGANIZATION_ID = '00000000-0000-0000-0000-000000000001'

/**
 * Build the base URL for capabilities API
 */
const getBaseUrl = (organizationId: string = DEFAULT_ORGANIZATION_ID) =>
  `/organizations/${organizationId}/capabilities`

/**
 * Capability management API client
 */
export const capabilityService = {
  /**
   * Get all capabilities for an organization
   * @param includeInactive Include inactive capabilities (default: false)
   * @param organizationId Organization ID (defaults to system organization)
   */
  async getCapabilities(
    includeInactive = false,
    organizationId?: string,
  ): Promise<CapabilityDto[]> {
    const params = includeInactive ? { includeInactive: 'true' } : undefined
    const response = await apiClient.get<CapabilityDto[]>(getBaseUrl(organizationId), { params })
    return response.data
  },

  /**
   * Get a single capability by ID
   * @param id Capability ID
   * @param organizationId Organization ID (defaults to system organization)
   */
  async getCapability(id: string, organizationId?: string): Promise<CapabilityDto> {
    const response = await apiClient.get<CapabilityDto>(`${getBaseUrl(organizationId)}/${id}`)
    return response.data
  },

  /**
   * Create a new capability
   * @param request Create capability request
   * @param organizationId Organization ID (defaults to system organization)
   */
  async createCapability(
    request: CreateCapabilityRequest,
    organizationId?: string,
  ): Promise<CapabilityDto> {
    const response = await apiClient.post<CapabilityDto>(getBaseUrl(organizationId), request)
    return response.data
  },

  /**
   * Update an existing capability
   * @param id Capability ID
   * @param request Update capability request
   * @param organizationId Organization ID (defaults to system organization)
   */
  async updateCapability(
    id: string,
    request: UpdateCapabilityRequest,
    organizationId?: string,
  ): Promise<CapabilityDto> {
    const response = await apiClient.put<CapabilityDto>(
      `${getBaseUrl(organizationId)}/${id}`,
      request,
    )
    return response.data
  },

  /**
   * Deactivate (soft delete) a capability
   * @param id Capability ID
   * @param organizationId Organization ID (defaults to system organization)
   */
  async deleteCapability(id: string, organizationId?: string): Promise<void> {
    await apiClient.delete(`${getBaseUrl(organizationId)}/${id}`)
  },

  /**
   * Check if a capability name is available
   * @param name Name to check
   * @param excludeId Optional ID to exclude (for updates)
   * @param organizationId Organization ID (defaults to system organization)
   */
  async checkNameAvailability(
    name: string,
    excludeId?: string,
    organizationId?: string,
  ): Promise<boolean> {
    const params: Record<string, string> = { name }
    if (excludeId) {
      params.excludeId = excludeId
    }
    const response = await apiClient.get<{ isAvailable: boolean }>(
      `${getBaseUrl(organizationId)}/check-name`,
      { params },
    )
    return response.data.isAvailable
  },

  /**
   * Get available predefined capability libraries
   * @param organizationId Organization ID (defaults to system organization)
   */
  async getAvailableLibraries(organizationId?: string): Promise<PredefinedLibraryInfo[]> {
    const response = await apiClient.get<PredefinedLibraryInfo[]>(
      `${getBaseUrl(organizationId)}/libraries`,
    )
    return response.data
  },

  /**
   * Import a predefined capability library
   * @param libraryName Name/ID of the library to import (e.g., "FEMA", "NATO")
   * @param organizationId Organization ID (defaults to system organization)
   */
  async importLibrary(
    libraryName: string,
    organizationId?: string,
  ): Promise<ImportLibraryResult> {
    const response = await apiClient.post<ImportLibraryResult>(
      `${getBaseUrl(organizationId)}/import`,
      { libraryName },
    )
    return response.data
  },
}

export default capabilityService
