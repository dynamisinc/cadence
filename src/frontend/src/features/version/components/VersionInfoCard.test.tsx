import { render, screen, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { BrowserRouter } from 'react-router-dom'
import { VersionInfoCard } from './VersionInfoCard'

// Mock the version module
vi.mock('@/config/version', () => ({
  appVersion: {
    version: '1.0.0',
    buildDate: '2026-01-30T00:00:00.000Z',
    commitSha: 'abc1234',
  },
}))

// Mock the API client
vi.mock('@/core/services/api', () => ({
  apiClient: {
    get: vi.fn(),
  },
}))

import { apiClient } from '@/core/services/api'

const renderCard = () => {
  return render(
    <BrowserRouter>
      <VersionInfoCard />
    </BrowserRouter>,
  )
}

describe('VersionInfoCard', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders app version', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({
      data: { version: '1.0.0', environment: 'Development' },
    })

    renderCard()

    expect(screen.getByText('App Version')).toBeInTheDocument()
    expect(screen.getByText('1.0.0')).toBeInTheDocument()
  })

  it('shows connected status when API is reachable', async () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({
      data: { version: '1.0.0', environment: 'Development' },
    })

    renderCard()

    await waitFor(() => {
      expect(screen.getByText('Connected')).toBeInTheDocument()
    })
  })

  it('shows unavailable status when API fails', async () => {
    vi.mocked(apiClient.get).mockRejectedValueOnce(new Error('Network error'))

    renderCard()

    await waitFor(() => {
      expect(screen.getByText('Unavailable')).toBeInTheDocument()
    })
  })

  it('renders version information title', () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({
      data: { version: '1.0.0', environment: 'Development' },
    })

    renderCard()

    expect(screen.getByText('Version Information')).toBeInTheDocument()
  })

  it('renders view release notes button', () => {
    vi.mocked(apiClient.get).mockResolvedValueOnce({
      data: { version: '1.0.0', environment: 'Development' },
    })

    renderCard()

    expect(screen.getByText('View release notes')).toBeInTheDocument()
  })
})
