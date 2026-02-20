/**
 * ClockControls Component Tests
 */

import { render, screen, fireEvent } from '@testing-library/react'
import { ThemeProvider } from '@mui/material/styles'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { cobraTheme } from '../../../theme/cobraTheme'
import { ClockControls } from './ClockControls'
import { ExerciseClockState } from '../../../types'

const renderWithTheme = (ui: React.ReactElement) => {
  return render(<ThemeProvider theme={cobraTheme}>{ui}</ThemeProvider>)
}

describe('ClockControls', () => {
  const defaultProps = {
    state: ExerciseClockState.Stopped,
    onStart: vi.fn(),
    onPause: vi.fn(),
    onStop: vi.fn(),
  }

  beforeEach(() => {
    vi.clearAllMocks()
  })

  // =========================================================================
  // Basic Button Visibility Tests
  // =========================================================================

  it('shows Start button when stopped', () => {
    renderWithTheme(<ClockControls {...defaultProps} />)

    expect(screen.getByRole('button', { name: /Start/i })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Pause/i })).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Stop/i })).not.toBeInTheDocument()
  })

  it('shows Pause and Stop buttons when running', () => {
    renderWithTheme(
      <ClockControls {...defaultProps} state={ExerciseClockState.Running} />,
    )

    expect(screen.queryByRole('button', { name: /Start/i })).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Pause/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Stop/i })).toBeInTheDocument()
  })

  it('shows Resume and Stop buttons when paused', () => {
    renderWithTheme(
      <ClockControls {...defaultProps} state={ExerciseClockState.Paused} />,
    )

    expect(screen.getByRole('button', { name: /Resume/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /Stop/i })).toBeInTheDocument()
  })

  // =========================================================================
  // Button Click Handlers
  // =========================================================================

  it('calls onStart when Start button is clicked', () => {
    const onStart = vi.fn()
    renderWithTheme(<ClockControls {...defaultProps} onStart={onStart} />)

    fireEvent.click(screen.getByRole('button', { name: /Start/i }))
    expect(onStart).toHaveBeenCalledTimes(1)
  })

  it('calls onPause when Pause button is clicked', () => {
    const onPause = vi.fn()
    renderWithTheme(
      <ClockControls
        {...defaultProps}
        state={ExerciseClockState.Running}
        onPause={onPause}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: /Pause/i }))
    expect(onPause).toHaveBeenCalledTimes(1)
  })

  it('calls onStop when Stop button is clicked', () => {
    const onStop = vi.fn()
    renderWithTheme(
      <ClockControls
        {...defaultProps}
        state={ExerciseClockState.Running}
        onStop={onStop}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: /Stop/i }))
    expect(onStop).toHaveBeenCalledTimes(1)
  })

  // =========================================================================
  // Set Time Button Tests
  // =========================================================================

  it('shows Set Time button when paused with canSetTime and onSetTime', () => {
    const onSetTime = vi.fn()
    renderWithTheme(
      <ClockControls
        {...defaultProps}
        state={ExerciseClockState.Paused}
        canSetTime={true}
        onSetTime={onSetTime}
      />,
    )

    expect(screen.getByRole('button', { name: /Set Time/i })).toBeInTheDocument()
  })

  it('hides Set Time button when clock is running', () => {
    const onSetTime = vi.fn()
    renderWithTheme(
      <ClockControls
        {...defaultProps}
        state={ExerciseClockState.Running}
        canSetTime={true}
        onSetTime={onSetTime}
      />,
    )

    expect(screen.queryByRole('button', { name: /Set Time/i })).not.toBeInTheDocument()
  })

  it('hides Set Time button when clock is stopped', () => {
    const onSetTime = vi.fn()
    renderWithTheme(
      <ClockControls
        {...defaultProps}
        state={ExerciseClockState.Stopped}
        canSetTime={true}
        onSetTime={onSetTime}
      />,
    )

    expect(screen.queryByRole('button', { name: /Set Time/i })).not.toBeInTheDocument()
  })

  it('hides Set Time button when canSetTime is false', () => {
    const onSetTime = vi.fn()
    renderWithTheme(
      <ClockControls
        {...defaultProps}
        state={ExerciseClockState.Paused}
        canSetTime={false}
        onSetTime={onSetTime}
      />,
    )

    expect(screen.queryByRole('button', { name: /Set Time/i })).not.toBeInTheDocument()
  })

  it('hides Set Time button when onSetTime is not provided', () => {
    renderWithTheme(
      <ClockControls
        {...defaultProps}
        state={ExerciseClockState.Paused}
        canSetTime={true}
      />,
    )

    expect(screen.queryByRole('button', { name: /Set Time/i })).not.toBeInTheDocument()
  })

  it('calls onSetTime when Set Time button is clicked', () => {
    const onSetTime = vi.fn()
    renderWithTheme(
      <ClockControls
        {...defaultProps}
        state={ExerciseClockState.Paused}
        canSetTime={true}
        onSetTime={onSetTime}
      />,
    )

    fireEvent.click(screen.getByRole('button', { name: /Set Time/i }))
    expect(onSetTime).toHaveBeenCalledTimes(1)
  })

  it('disables Set Time button when disabled prop is true', () => {
    const onSetTime = vi.fn()
    renderWithTheme(
      <ClockControls
        {...defaultProps}
        state={ExerciseClockState.Paused}
        canSetTime={true}
        onSetTime={onSetTime}
        disabled={true}
      />,
    )

    expect(screen.getByRole('button', { name: /Set Time/i })).toBeDisabled()
  })

  // =========================================================================
  // Loading State Tests
  // =========================================================================

  it('shows Starting... text when isStarting', () => {
    renderWithTheme(<ClockControls {...defaultProps} isStarting={true} />)

    expect(screen.getByRole('button', { name: /Starting.../i })).toBeInTheDocument()
  })

  it('shows Pausing... text when isPausing', () => {
    renderWithTheme(
      <ClockControls
        {...defaultProps}
        state={ExerciseClockState.Running}
        isPausing={true}
      />,
    )

    expect(screen.getByRole('button', { name: /Pausing.../i })).toBeInTheDocument()
  })

  // =========================================================================
  // Reset Button Tests
  // =========================================================================

  it('shows Reset button when showReset is true and clock is stopped', () => {
    const onReset = vi.fn()
    renderWithTheme(
      <ClockControls {...defaultProps} showReset={true} onReset={onReset} />,
    )

    expect(screen.getByRole('button', { name: /Reset/i })).toBeInTheDocument()
  })

  it('hides Reset button when showReset is false', () => {
    const onReset = vi.fn()
    renderWithTheme(
      <ClockControls {...defaultProps} showReset={false} onReset={onReset} />,
    )

    expect(screen.queryByRole('button', { name: /Reset/i })).not.toBeInTheDocument()
  })
})
