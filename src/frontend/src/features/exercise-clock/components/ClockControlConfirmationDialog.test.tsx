/**
 * ClockControlConfirmationDialog Component Tests
 */

import { render, screen, fireEvent } from '@testing-library/react'
import { ThemeProvider } from '@mui/material/styles'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { cobraTheme } from '../../../theme/cobraTheme'
import {
  ClockControlConfirmationDialog,
  type ClockAction,
} from './ClockControlConfirmationDialog'

const renderWithTheme = (ui: React.ReactElement) => {
  return render(<ThemeProvider theme={cobraTheme}>{ui}</ThemeProvider>)
}

describe('ClockControlConfirmationDialog', () => {
  const defaultProps = {
    open: true,
    action: 'start' as ClockAction,
    currentTime: '00:15:30',
    onConfirm: vi.fn(),
    onCancel: vi.fn(),
    onDontAskAgain: vi.fn(),
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  // =========================================================================
  // Rendering Tests
  // =========================================================================

  it('renders start action dialog correctly', () => {
    renderWithTheme(<ClockControlConfirmationDialog {...defaultProps} />)

    expect(screen.getByText('Start Exercise Clock?')).toBeInTheDocument()
    expect(
      screen.getByText(/Starting the clock will begin the exercise/),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Start Clock' }),
    ).toBeInTheDocument()
  })

  it('renders pause action dialog correctly', () => {
    renderWithTheme(
      <ClockControlConfirmationDialog {...defaultProps} action="pause" />,
    )

    expect(screen.getByText('Pause Exercise Clock?')).toBeInTheDocument()
    expect(
      screen.getByText(/Pausing the clock will temporarily halt the exercise/),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Pause Clock' }),
    ).toBeInTheDocument()
  })

  it('renders stop action dialog with warning', () => {
    renderWithTheme(
      <ClockControlConfirmationDialog {...defaultProps} action="stop" />,
    )

    expect(screen.getByText('Stop Exercise Clock?')).toBeInTheDocument()
    expect(
      screen.getByText(/This action will complete the exercise/),
    ).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Stop Clock' }),
    ).toBeInTheDocument()
  })

  it('renders resume action dialog correctly', () => {
    renderWithTheme(
      <ClockControlConfirmationDialog {...defaultProps} action="resume" />,
    )

    expect(screen.getByText('Resume Exercise Clock?')).toBeInTheDocument()
    expect(
      screen.getByRole('button', { name: 'Resume Clock' }),
    ).toBeInTheDocument()
  })

  it('renders current time when provided', () => {
    renderWithTheme(<ClockControlConfirmationDialog {...defaultProps} />)

    expect(screen.getByText('00:15:30')).toBeInTheDocument()
  })

  it('does not render when action is null', () => {
    renderWithTheme(
      <ClockControlConfirmationDialog {...defaultProps} action={null} />,
    )

    expect(
      screen.queryByText('Start Exercise Clock?'),
    ).not.toBeInTheDocument()
  })

  // =========================================================================
  // Interaction Tests
  // =========================================================================

  it('calls onConfirm when confirm button is clicked', () => {
    const onConfirm = vi.fn()
    renderWithTheme(
      <ClockControlConfirmationDialog {...defaultProps} onConfirm={onConfirm} />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Start Clock' }))
    expect(onConfirm).toHaveBeenCalledTimes(1)
  })

  it('calls onCancel when cancel button is clicked', () => {
    const onCancel = vi.fn()
    renderWithTheme(
      <ClockControlConfirmationDialog {...defaultProps} onCancel={onCancel} />,
    )

    fireEvent.click(screen.getByRole('button', { name: 'Cancel' }))
    expect(onCancel).toHaveBeenCalledTimes(1)
  })

  // =========================================================================
  // "Don't ask again" Tests
  // =========================================================================

  it('renders "don\'t ask again" checkbox', () => {
    renderWithTheme(<ClockControlConfirmationDialog {...defaultProps} />)

    expect(
      screen.getByLabelText(/Don't ask again for this exercise/),
    ).toBeInTheDocument()
  })

  it('calls onDontAskAgain when checkbox is checked and confirmed', () => {
    const onDontAskAgain = vi.fn()
    const onConfirm = vi.fn()
    renderWithTheme(
      <ClockControlConfirmationDialog
        {...defaultProps}
        onConfirm={onConfirm}
        onDontAskAgain={onDontAskAgain}
      />,
    )

    // Check the checkbox
    const checkbox = screen.getByLabelText(/Don't ask again for this exercise/)
    fireEvent.click(checkbox)

    // Confirm the action
    fireEvent.click(screen.getByRole('button', { name: 'Start Clock' }))

    expect(onDontAskAgain).toHaveBeenCalledWith(true)
    expect(onConfirm).toHaveBeenCalledTimes(1)
  })

  it('does not call onDontAskAgain when checkbox is not checked', () => {
    const onDontAskAgain = vi.fn()
    const onConfirm = vi.fn()
    renderWithTheme(
      <ClockControlConfirmationDialog
        {...defaultProps}
        onConfirm={onConfirm}
        onDontAskAgain={onDontAskAgain}
      />,
    )

    // Confirm without checking the checkbox
    fireEvent.click(screen.getByRole('button', { name: 'Start Clock' }))

    expect(onDontAskAgain).not.toHaveBeenCalled()
    expect(onConfirm).toHaveBeenCalledTimes(1)
  })

  // =========================================================================
  // Accessibility Tests
  // =========================================================================

  it('has proper aria attributes', () => {
    renderWithTheme(<ClockControlConfirmationDialog {...defaultProps} />)

    const dialog = screen.getByRole('dialog')
    expect(dialog).toHaveAttribute('aria-labelledby', 'clock-confirmation-title')
    expect(dialog).toHaveAttribute(
      'aria-describedby',
      'clock-confirmation-description',
    )
  })
})
