/**
 * Delivery Method Service
 *
 * API client for delivery method lookup operations.
 */

import api from '@/core/services/api'
import type { DeliveryMethodDto } from '../types'

const BASE_URL = '/api/delivery-methods'

/**
 * Get all active delivery methods
 */
export const getDeliveryMethods = async (): Promise<DeliveryMethodDto[]> => {
  const response = await api.get<DeliveryMethodDto[]>(BASE_URL)
  return response.data
}

/**
 * Get a single delivery method by ID
 */
export const getDeliveryMethod = async (id: string): Promise<DeliveryMethodDto> => {
  const response = await api.get<DeliveryMethodDto>(`${BASE_URL}/${id}`)
  return response.data
}

export const deliveryMethodService = {
  getDeliveryMethods,
  getDeliveryMethod,
}

export default deliveryMethodService
