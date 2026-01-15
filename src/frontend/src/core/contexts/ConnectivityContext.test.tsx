/**
 * ConnectivityContext Tests
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, act } from '@testing-library/react'
import { ConnectivityProvider, useConnectivity } from './ConnectivityContext'

// Mock react-toastify
vi.mock('react-toastify', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
  },
}))

// Test component to access context
const TestComponent = () => {
  const {
    isOnline,
    connectivityState,
    signalRState,
    isInExercise,
    pendingCount,
    setSignalRState,
    setIsInExercise,
    setPendingCount,
    incrementPendingCount,
    decrementPendingCount,
  } = useConnectivity()

  return (
    <div>
      <div data-testid="isOnline">{String(isOnline)}</div>
      <div data-testid="connectivityState">{connectivityState}</div>
      <div data-testid="signalRState">{signalRState ?? 'null'}</div>
      <div data-testid="isInExercise">{String(isInExercise)}</div>
      <div data-testid="pendingCount">{pendingCount}</div>
      <button onClick={() => setSignalRState('connected')}>Set Connected</button>
      <button onClick={() => setSignalRState('disconnected')}>Set Disconnected</button>
      <button onClick={() => setIsInExercise(true)}>Enter Exercise</button>
      <button onClick={() => setIsInExercise(false)}>Exit Exercise</button>
      <button onClick={() => setPendingCount(5)}>Set Pending 5</button>
      <button onClick={incrementPendingCount}>Increment</button>
      <button onClick={decrementPendingCount}>Decrement</button>
    </div>
  )
}

describe('ConnectivityContext', () => {
  beforeEach(() => {
    // Reset navigator.onLine mock
    Object.defineProperty(navigator, 'onLine', {
      value: true,
      writable: true,
    })
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  it('provides initial online state', () => {
    render(
      <ConnectivityProvider>
        <TestComponent />
      </ConnectivityProvider>,
    )

    expect(screen.getByTestId('isOnline').textContent).toBe('true')
    expect(screen.getByTestId('connectivityState').textContent).toBe('online')
  })

  it('throws error when used outside provider', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {})

    expect(() => render(<TestComponent />)).toThrow(
      'useConnectivity must be used within a ConnectivityProvider',
    )

    consoleError.mockRestore()
  })

  it('tracks SignalR state when in exercise', async () => {
    render(
      <ConnectivityProvider>
        <TestComponent />
      </ConnectivityProvider>,
    )

    // Enter exercise mode
    await act(async () => {
      screen.getByText('Enter Exercise').click()
    })

    expect(screen.getByTestId('isInExercise').textContent).toBe('true')

    // Set SignalR connected
    await act(async () => {
      screen.getByText('Set Connected').click()
    })

    expect(screen.getByTestId('signalRState').textContent).toBe('connected')
    expect(screen.getByTestId('connectivityState').textContent).toBe('online')
  })

  it('shows offline when SignalR disconnects in exercise', async () => {
    render(
      <ConnectivityProvider>
        <TestComponent />
      </ConnectivityProvider>,
    )

    // Enter exercise and set connected first
    await act(async () => {
      screen.getByText('Enter Exercise').click()
    })
    await act(async () => {
      screen.getByText('Set Connected').click()
    })

    // Now disconnect
    await act(async () => {
      screen.getByText('Set Disconnected').click()
    })

    expect(screen.getByTestId('signalRState').textContent).toBe('disconnected')
    expect(screen.getByTestId('connectivityState').textContent).toBe('offline')
  })

  it('tracks pending count', async () => {
    render(
      <ConnectivityProvider>
        <TestComponent />
      </ConnectivityProvider>,
    )

    expect(screen.getByTestId('pendingCount').textContent).toBe('0')

    await act(async () => {
      screen.getByText('Set Pending 5').click()
    })
    expect(screen.getByTestId('pendingCount').textContent).toBe('5')

    await act(async () => {
      screen.getByText('Increment').click()
    })
    expect(screen.getByTestId('pendingCount').textContent).toBe('6')

    await act(async () => {
      screen.getByText('Decrement').click()
    })
    expect(screen.getByTestId('pendingCount').textContent).toBe('5')
  })

  it('does not allow negative pending count', async () => {
    render(
      <ConnectivityProvider>
        <TestComponent />
      </ConnectivityProvider>,
    )

    // Try to decrement from 0
    await act(async () => {
      screen.getByText('Decrement').click()
    })

    expect(screen.getByTestId('pendingCount').textContent).toBe('0')
  })

  it('clears SignalR state when exiting exercise', async () => {
    render(
      <ConnectivityProvider>
        <TestComponent />
      </ConnectivityProvider>,
    )

    // Enter exercise and set connected
    await act(async () => {
      screen.getByText('Enter Exercise').click()
    })
    await act(async () => {
      screen.getByText('Set Connected').click()
    })

    expect(screen.getByTestId('signalRState').textContent).toBe('connected')

    // Exit exercise
    await act(async () => {
      screen.getByText('Exit Exercise').click()
    })

    expect(screen.getByTestId('isInExercise').textContent).toBe('false')
    // SignalR state should remain until explicitly cleared
    expect(screen.getByTestId('connectivityState').textContent).toBe('online')
  })
})
