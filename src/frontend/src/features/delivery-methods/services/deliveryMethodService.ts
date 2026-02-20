/**
 * Delivery Method Service
 *
 * API client for delivery method lookup operations.
 */

import api from '@/core/services/api'
import type {
  DeliveryMethodDto,
  CreateDeliveryMethodRequest,
  UpdateDeliveryMethodRequest,
} from '../types'

const BASE_URL = '/delivery-methods'

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

/**
 * Get all delivery methods including inactive (admin only)
 */
export const getAllDeliveryMethods = async (): Promise<DeliveryMethodDto[]> => {
  const response = await api.get<DeliveryMethodDto[]>(`${BASE_URL}/all`)
  return response.data
}

/**
 * Create a new delivery method (admin only)
 */
export const createDeliveryMethod = async (
  request: CreateDeliveryMethodRequest,
): Promise<DeliveryMethodDto> => {
  const response = await api.post<DeliveryMethodDto>(BASE_URL, request)
  return response.data
}

/**
 * Update a delivery method (admin only)
 */
export const updateDeliveryMethod = async (
  id: string,
  request: UpdateDeliveryMethodRequest,
): Promise<DeliveryMethodDto> => {
  const response = await api.put<DeliveryMethodDto>(`${BASE_URL}/${id}`, request)
  return response.data
}

/**
 * Delete a delivery method (admin only)
 */
export const deleteDeliveryMethod = async (id: string): Promise<void> => {
  await api.delete(`${BASE_URL}/${id}`)
}

/**
 * Reorder delivery methods (admin only)
 */
export const reorderDeliveryMethods = async (orderedIds: string[]): Promise<void> => {
  await api.put(`${BASE_URL}/reorder`, orderedIds)
}

export const deliveryMethodService = {
  getDeliveryMethods,
  getDeliveryMethod,
  getAllDeliveryMethods,
  createDeliveryMethod,
  updateDeliveryMethod,
  deleteDeliveryMethod,
  reorderDeliveryMethods,
}

export default deliveryMethodService
