/**
 * Delivery Methods Feature Types
 *
 * TypeScript types for delivery method lookup data.
 * Matches backend DTOs in Cadence.Core.Features.DeliveryMethods.Models.DTOs
 */

/**
 * DeliveryMethod DTO - Response from API
 */
export interface DeliveryMethodDto {
  id: string
  name: string
  description: string | null
  isActive: boolean
  sortOrder: number
  isOther: boolean
}

/**
 * Request to create a new delivery method (admin only)
 */
export interface CreateDeliveryMethodRequest {
  name: string
  description?: string | null
  sortOrder?: number
  isOther?: boolean
}

/**
 * Request to update an existing delivery method (admin only)
 */
export interface UpdateDeliveryMethodRequest {
  name: string
  description?: string | null
  sortOrder: number
  isActive: boolean
  isOther: boolean
}

/**
 * Get display name for a delivery method
 * If it's the "Other" option and custom text is provided, show the custom text
 */
export const getDeliveryMethodDisplay = (
  method: DeliveryMethodDto | null,
  customText: string | null,
): string => {
  if (!method) return 'Not specified'
  if (method.isOther && customText) return customText
  return method.name
}
