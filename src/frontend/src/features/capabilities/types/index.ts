/**
 * Capabilities Feature Types
 *
 * TypeScript types for organizational capability management.
 * Matches backend DTOs in Cadence.Core.Features.Capabilities.Models.DTOs
 */

/**
 * Capability DTO - Full response from API
 */
export interface CapabilityDto {
  id: string
  organizationId: string
  name: string
  description: string | null
  category: string | null
  sortOrder: number
  isActive: boolean
  sourceLibrary: string | null
  createdAt: string
  updatedAt: string
}

/**
 * Lightweight DTO for capability selection dropdowns
 */
export interface CapabilitySummaryDto {
  id: string
  name: string
  category: string | null
  isActive: boolean
}

/**
 * Request DTO for creating a new capability
 */
export interface CreateCapabilityRequest {
  name: string
  description?: string | null
  category?: string | null
  sortOrder?: number
  sourceLibrary?: string | null
}

/**
 * Request DTO for updating an existing capability
 */
export interface UpdateCapabilityRequest {
  name: string
  description?: string | null
  category?: string | null
  sortOrder?: number
  isActive?: boolean
}

/**
 * Available capability library frameworks
 */
export type CapabilityLibrary = 'FEMA' | 'NATO' | 'NIST' | 'ISO' | 'Custom'

/**
 * Capability library metadata
 */
export interface CapabilityLibraryInfo {
  id: CapabilityLibrary
  name: string
  description: string
  count?: number
}

/**
 * Available predefined capability libraries
 */
export const CAPABILITY_LIBRARIES: CapabilityLibraryInfo[] = [
  {
    id: 'FEMA',
    name: 'FEMA Core Capabilities',
    description: '32 core capabilities from the National Preparedness Goal',
  },
  {
    id: 'NATO',
    name: 'NATO Baseline Requirements',
    description: '7 baseline requirements for collective defense',
  },
  {
    id: 'NIST',
    name: 'NIST Cybersecurity Framework',
    description: '6 functions for managing cybersecurity risk',
  },
  {
    id: 'ISO',
    name: 'ISO 22301 Process Areas',
    description: '10 process areas for business continuity management',
  },
]

/**
 * Group capabilities by category
 */
export const groupCapabilitiesByCategory = (
  capabilities: CapabilityDto[],
): Map<string, CapabilityDto[]> => {
  const grouped = new Map<string, CapabilityDto[]>()

  for (const cap of capabilities) {
    const category = cap.category || 'Uncategorized'
    const existing = grouped.get(category) || []
    grouped.set(category, [...existing, cap])
  }

  return grouped
}

/**
 * Get unique categories from capabilities
 */
export const getUniqueCategories = (capabilities: CapabilityDto[]): string[] => {
  const categories = new Set<string>()
  for (const cap of capabilities) {
    categories.add(cap.category || 'Uncategorized')
  }
  return Array.from(categories).sort()
}

/**
 * Information about a predefined capability library
 */
export interface PredefinedLibraryInfo {
  id: string
  name: string
  description: string
  capabilityCount: number
}

/**
 * Result of importing a predefined library
 */
export interface ImportLibraryResult {
  totalInLibrary: number
  imported: number
  skippedDuplicates: number
  importedNames: string[]
}
