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
      isInExercise: false,
      isSignalRJoined: false,
      pendingCount: 0,
    })

    const { container } = render(<ConnectionStatusIndicator compact />)
    expect(container.firstChild).toBeNull()
  })

  it('shows indicator when offline', () => {
    mockUseConnectivity.mockReturnValue({
      connectivityState: 'offline',
      isInExercise: false,
      isSignalRJoined: false,
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
      isInExercise: true,
      isSignalRJoined: false,
      pendingCount: 0,
    })

    render(<ConnectionStatusIndicator />)

    expect(screen.getByText('Connecting')).toBeInTheDocument()
  })

  it('shows reconnecting state', () => {
    mockUseConnectivity.mockReturnValue({
      connectivityState: 'reconnecting',
      isInExercise: true,
      isSignalRJoined: false,
      pendingCount: 0,
    })

    render(<ConnectionStatusIndicator />)

    expect(screen.getByText('Reconnecting')).toBeInTheDocument()
  })

  it('shows pending count when there are pending actions', () => {
    mockUseConnectivity.mockReturnValue({
      connectivityState: 'offline',
      isInExercise: false,
      isSignalRJoined: false,
      pendingCount: 3,
    })

    render(<ConnectionStatusIndicator />)

    expect(screen.getByText('(3 pending)')).toBeInTheDocument()
  })

  it('shows pending count even when online', () => {
    mockUseConnectivity.mockReturnValue({
      connectivityState: 'online',
      isInExercise: false,
      isSignalRJoined: false,
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
      isInExercise: false,
      isSignalRJoined: false,
      pendingCount: 0,
    })

    render(<ConnectionStatusIndicator compact={false} />)

    const indicator = screen.getByTestId('connection-status-indicator')
    expect(indicator).toBeInTheDocument()
  })

  it('shows Live state when in exercise, connected, and joined', () => {
    mockUseConnectivity.mockReturnValue({
      connectivityState: 'online',
      isInExercise: true,
      isSignalRJoined: true,
      pendingCount: 0,
    })

    render(<ConnectionStatusIndicator compact />)

    const indicator = screen.getByTestId('connection-status-indicator')
    expect(indicator).toBeInTheDocument()
    expect(indicator).toHaveAttribute('data-status', 'live')
    expect(screen.getByText('Live')).toBeInTheDocument()
  })

  it('does not show Live when connected but not joined', () => {
    mockUseConnectivity.mockReturnValue({
      connectivityState: 'online',
      isInExercise: true,
      isSignalRJoined: false,
      pendingCount: 0,
    })

    // In compact mode, online without pending should be hidden
    const { container } = render(<ConnectionStatusIndicator compact />)
    expect(container.firstChild).toBeNull()
  })
})
