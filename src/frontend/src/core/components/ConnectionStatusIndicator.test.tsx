/**
 * ConnectionStatusIndicator Tests
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { ConnectionStatusIndicator } from './ConnectionStatusIndicator'

// Mock the useConnectivity hook
const mockUseConnectivity = vi.fn()

vi.mock('../contexts/ConnectivityContext', () => ({
  useConnectivity: () => mockUseConnectivity(),
}))

// Mock PendingActionsPopover to avoid OfflineSyncContext requirement
vi.mock('./PendingActionsPopover', () => ({
  PendingActionsPopover: () => null,
}))

describe('ConnectionStatusIndicator', () => {
  it('renders nothing when online in compact mode with no pending', () => {
    mockUseConnectivity.mockReturnValue({
      connectivityState: 'online',
      pendingCount: 0,
    })

    const { container } = render(<ConnectionStatusIndicator compact />)
    expect(container.firstChild).toBeNull()
  })

  it('shows indicator when offline', () => {
    mockUseConnectivity.mockReturnValue({
      connectivityState: 'offline',
      pendingCount: 0,
    })

    render(<ConnectionStatusIndicator />)

    const indicator = screen.getByTestId('connection-status-indicator')
    expect(indicator).toBeInTheDocument()
    expect(indicator).toHaveAttribute('data-status', 'offline')
    expect(screen.getByText('Offline')).toBeInTheDocument()
  })

  it('shows connecting state', () => {
    mockUseConnectivity.mockReturnValue({
      connectivityState: 'connecting',
      pendingCount: 0,
    })

    render(<ConnectionStatusIndicator />)

    expect(screen.getByText('Connecting')).toBeInTheDocument()
  })

  it('shows reconnecting state', () => {
    mockUseConnectivity.mockReturnValue({
      connectivityState: 'reconnecting',
      pendingCount: 0,
    })

    render(<ConnectionStatusIndicator />)

    expect(screen.getByText('Reconnecting')).toBeInTheDocument()
  })

  it('shows pending count when there are pending actions', () => {
    mockUseConnectivity.mockReturnValue({
      connectivityState: 'offline',
      pendingCount: 3,
    })

    render(<ConnectionStatusIndicator />)

    expect(screen.getByText('(3 pending)')).toBeInTheDocument()
  })

  it('shows pending count even when online', () => {
    mockUseConnectivity.mockReturnValue({
      connectivityState: 'online',
      pendingCount: 2,
    })

    render(<ConnectionStatusIndicator compact />)

    const indicator = screen.getByTestId('connection-status-indicator')
    expect(indicator).toBeInTheDocument()
    expect(screen.getByText('(2 pending)')).toBeInTheDocument()
  })

  it('shows full indicator when not in compact mode', () => {
    mockUseConnectivity.mockReturnValue({
      connectivityState: 'online',
      pendingCount: 0,
    })

    render(<ConnectionStatusIndicator compact={false} />)

    const indicator = screen.getByTestId('connection-status-indicator')
    expect(indicator).toBeInTheDocument()
  })
})
