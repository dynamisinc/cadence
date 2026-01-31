import { renderHook, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { useApiVersion } from './useApiVersion'

// Mock the API client
vi.mock('@/core/services/api', () => ({
  apiClient: {
    get: vi.fn(),
  },
}))

import { apiClient } from '@/core/services/api'

describe('useApiVersion', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should fetch API version successfully', async () => {
    const mockVersionInfo = {
      version: '1.0.0',
      commitSha: 'abc1234',
      buildDate: '2026-01-30T00:00:00.000Z',
      environment: 'Development',
    }

    vi.mocked(apiClient.get).mockResolvedValueOnce({ data: mockVersionInfo })

    const { result } = renderHook(() => useApiVersion())

    // Initially loading
    expect(result.current.isLoading).toBe(true)

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    expect(result.current.apiVersion).toEqual(mockVersionInfo)
    expect(result.current.isConnected).toBe(true)
    expect(result.current.error).toBeNull()
  })

  it('should handle API error', async () => {
    vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Network error'))

    const { result } = renderHook(() => useApiVersion())

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false)
    })

    expect(result.current.apiVersion).toBeNull()
    expect(result.current.isConnected).toBe(false)
    expect(result.current.error).toBeDefined()
  })

  it('should call the correct endpoint', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({
      data: { version: '1.0.0', environment: 'Test' },
    })

    renderHook(() => useApiVersion())

    await waitFor(() => {
      expect(apiClient.get).toHaveBeenCalledWith('/version')
    })
  })
})
