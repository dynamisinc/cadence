/**
 * SetClockTimeDialog Component Tests
 */

import { render, screen, fireEvent } from '@testing-library/react'
import { ThemeProvider } from '@mui/material/styles'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { cobraTheme } from '../../../theme/cobraTheme'
import { SetClockTimeDialog } from './SetClockTimeDialog'

const renderWithTheme = (ui: React.ReactElement) => {
  return render(<ThemeProvider theme={cobraTheme}>{ui}</ThemeProvider>)
}

describe('SetClockTimeDialog', () => {
  const defaultProps = {
    open: true,
    currentTime: '01:30:45',
    maxDurationHours: 72,
    onConfirm: vi.fn(),
    onCancel: vi.fn(),
    isLoading: false,
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  // =========================================================================
  // Rendering Tests
  // =========================================================================

  it('renders dialog title and current time', () => {
    renderWithTheme(<SetClockTimeDialog {...defaultProps} />)

    expect(screen.getByText('Set Clock Time')).toBeInTheDocument()
    expect(screen.getByText('01:30:45')).toBeInTheDocument()
  })

  it('initializes inputs from current time', () => {
    renderWithTheme(<SetClockTimeDialog {...defaultProps} />)

    const hoursInput = screen.getByLabelText('Hours') as HTMLInputElement
    const minutesInput = screen.getByLabelText('Minutes') as HTMLInputElement
    const secondsInput = screen.getByLabelText('Seconds') as HTMLInputElement

    expect(hoursInput.value).toBe('1')
    expect(minutesInput.value).toBe('30')
    expect(secondsInput.value).toBe('45')
  })

  it('displays max duration info', () => {
    renderWithTheme(<SetClockTimeDialog {...defaultProps} />)

    expect(screen.getByText(/Maximum duration: 72 hours/)).toBeInTheDocument()
  })

  it('does not render when closed', () => {
    renderWithTheme(<SetClockTimeDialog {...defaultProps} open={false} />)

    expect(screen.queryByText('Set Clock Time')).not.toBeInTheDocument()
  })

  // =========================================================================
  // Interaction Tests
  // =========================================================================

  it('calls onConfirm with formatted time string', () => {
    const onConfirm = vi.fn()
    renderWithTheme(<SetClockTimeDialog {...defaultProps} onConfirm={onConfirm} />)

    fireEvent.click(screen.getByRole('button', { name: /Set Time/i }))

    expect(onConfirm).toHaveBeenCalledWith('01:30:45')
  })

  it('calls onCancel when cancel button is clicked', () => {
    const onCancel = vi.fn()
    renderWithTheme(<SetClockTimeDialog {...defaultProps} onCancel={onCancel} />)

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    expect(onCancel).toHaveBeenCalledTimes(1)
  })

  it('pads hours to two digits in output', () => {
    const onConfirm = vi.fn()
    renderWithTheme(
      <SetClockTimeDialog {...defaultProps} currentTime="02:05:03" onConfirm={onConfirm} />,
    )

    fireEvent.click(screen.getByRole('button', { name: /Set Time/i }))

    expect(onConfirm).toHaveBeenCalledWith('02:05:03')
  })

  // =========================================================================
  // Validation Tests
  // =========================================================================

  it('shows error when time exceeds max duration', () => {
    renderWithTheme(
      <SetClockTimeDialog {...defaultProps} maxDurationHours={2} currentTime="00:00:00" />,
    )

    // Set hours to 3 (exceeds max of 2)
    const hoursInput = screen.getByLabelText('Hours')
    fireEvent.change(hoursInput, { target: { value: '3' } })

    // Click confirm to trigger validation
    fireEvent.click(screen.getByRole('button', { name: /Set Time/i }))

    expect(screen.getByText(/cannot exceed the maximum duration/)).toBeInTheDocument()
  })

  it('does not call onConfirm when validation fails', () => {
    const onConfirm = vi.fn()
    renderWithTheme(
      <SetClockTimeDialog
        {...defaultProps}
        maxDurationHours={2}
        currentTime="00:00:00"
        onConfirm={onConfirm}
      />,
    )

    const hoursInput = screen.getByLabelText('Hours')
    fireEvent.change(hoursInput, { target: { value: '3' } })
    fireEvent.click(screen.getByRole('button', { name: /Set Time/i }))

    expect(onConfirm).not.toHaveBeenCalled()
  })

  // =========================================================================
  // Loading State Tests
  // =========================================================================

  it('shows loading text when isLoading is true', () => {
    renderWithTheme(<SetClockTimeDialog {...defaultProps} isLoading={true} />)

    expect(screen.getByRole('button', { name: /Setting.../i })).toBeInTheDocument()
  })

  it('disables buttons when loading', () => {
    renderWithTheme(<SetClockTimeDialog {...defaultProps} isLoading={true} />)

    expect(screen.getByRole('button', { name: /Setting.../i })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled()
  })

  // =========================================================================
  // Accessibility Tests
  // =========================================================================

  it('has proper aria attributes', () => {
    renderWithTheme(<SetClockTimeDialog {...defaultProps} />)

    const dialog = screen.getByRole('dialog')
    expect(dialog).toHaveAttribute('aria-labelledby', 'set-clock-time-title')
  })
})
