import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '../../test/testUtils'
import { SystemSettingsAdmin } from './SystemSettingsAdmin'
import type { SystemSettingsDto } from '../types/systemSettings'

const mockSettings: SystemSettingsDto = {
  id: null,
  supportAddress: null,
  defaultSenderAddress: null,
  defaultSenderName: null,
  effectiveSupportAddress: 'cadence-support@cobrasoftware.com',
  effectiveDefaultSenderAddress: 'noreply@cadence-app.com',
  effectiveDefaultSenderName: 'Cadence',
  updatedAt: null,
  updatedBy: null,
}

const mockSettingsWithOverrides: SystemSettingsDto = {
  id: 'abc-123',
  supportAddress: 'custom-support@org.com',
  defaultSenderAddress: null,
  defaultSenderName: 'Custom Org',
  effectiveSupportAddress: 'custom-support@org.com',
  effectiveDefaultSenderAddress: 'noreply@cadence-app.com',
  effectiveDefaultSenderName: 'Custom Org',
  updatedAt: '2026-02-09T10:00:00Z',
  updatedBy: 'admin@test.com',
}

// Mock the hooks module
vi.mock('../hooks/useSystemSettings', () => ({
  useSystemSettings: vi.fn(),
  useUpdateSystemSettings: vi.fn(),
  useTestGitHubConnection: vi.fn(),
}))

import { useSystemSettings, useUpdateSystemSettings, useTestGitHubConnection } from '../hooks/useSystemSettings'
const mockUseSystemSettings = vi.mocked(useSystemSettings)
const mockUseUpdateSystemSettings = vi.mocked(useUpdateSystemSettings)
const mockUseTestGitHubConnection = vi.mocked(useTestGitHubConnection)

describe('SystemSettingsAdmin', () => {
  const mockMutateAsync = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
    mockMutateAsync.mockResolvedValue(mockSettings)
    mockUseUpdateSystemSettings.mockReturnValue({
      mutateAsync: mockMutateAsync,
      isPending: false,
    } as ReturnType<typeof useUpdateSystemSettings>)
    mockUseTestGitHubConnection.mockReturnValue({
      mutateAsync: vi.fn(),
      isPending: false,
    } as unknown as ReturnType<typeof useTestGitHubConnection>)
  })

  it('renders loading state', () => {
    mockUseSystemSettings.mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
    } as ReturnType<typeof useSystemSettings>)

    render(<SystemSettingsAdmin />)

    expect(screen.getByRole('progressbar')).toBeInTheDocument()
  })

  it('renders error state', () => {
    mockUseSystemSettings.mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error('Failed to load'),
    } as ReturnType<typeof useSystemSettings>)

    render(<SystemSettingsAdmin />)

    expect(screen.getByText('Failed to load')).toBeInTheDocument()
  })

  it('renders form with effective defaults when no overrides', async () => {
    mockUseSystemSettings.mockReturnValue({
      data: mockSettings,
      isLoading: false,
      error: null,
    } as ReturnType<typeof useSystemSettings>)

    render(<SystemSettingsAdmin />)

    expect(screen.getByText('Email Configuration')).toBeInTheDocument()

    await waitFor(() => {
      expect(screen.getByText('Default: cadence-support@cobrasoftware.com')).toBeInTheDocument()
      expect(screen.getByText('Default: Cadence')).toBeInTheDocument()
    })
  })

  it('renders form with override values populated', async () => {
    mockUseSystemSettings.mockReturnValue({
      data: mockSettingsWithOverrides,
      isLoading: false,
      error: null,
    } as ReturnType<typeof useSystemSettings>)

    render(<SystemSettingsAdmin />)

    await waitFor(() => {
      const supportField = screen.getByLabelText('Support Email Address')
      expect(supportField).toHaveValue('custom-support@org.com')

      const senderNameField = screen.getByLabelText('Default Sender Name')
      expect(senderNameField).toHaveValue('Custom Org')
    })
  })

  it('shows last updated info when settings have been modified', async () => {
    mockUseSystemSettings.mockReturnValue({
      data: mockSettingsWithOverrides,
      isLoading: false,
      error: null,
    } as ReturnType<typeof useSystemSettings>)

    render(<SystemSettingsAdmin />)

    await waitFor(() => {
      expect(screen.getByText(/Last updated:/)).toBeInTheDocument()
      expect(screen.getByText(/admin@test.com/)).toBeInTheDocument()
    })
  })

  it('disables save button when no changes', async () => {
    mockUseSystemSettings.mockReturnValue({
      data: mockSettings,
      isLoading: false,
      error: null,
    } as ReturnType<typeof useSystemSettings>)

    render(<SystemSettingsAdmin />)

    await waitFor(() => {
      const saveButton = screen.getByRole('button', { name: /save changes/i })
      expect(saveButton).toBeDisabled()
    })
  })
})
