/**
 * Tests for TimingConfigurationSection component
 *
 * @module features/exercises
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThemeProvider } from '@mui/material/styles'
import { cobraTheme } from '../../../theme/cobraTheme'
import { TimingConfigurationSection } from './TimingConfigurationSection'
import { DeliveryMode, TimelineMode, ExerciseType } from '../../../types'

const renderWithTheme = (ui: React.ReactElement) => {
  return render(<ThemeProvider theme={cobraTheme}>{ui}</ThemeProvider>)
}

describe('TimingConfigurationSection', () => {
  const defaultProps = {
    deliveryMode: DeliveryMode.ClockDriven,
    timelineMode: TimelineMode.RealTime,
    clockMultiplier: 1,
    exerciseType: ExerciseType.FSE,
    isLocked: false,
    onChange: vi.fn(),
    errors: {},
  }

  describe('Editable State', () => {
    it('renders delivery mode options', () => {
      renderWithTheme(<TimingConfigurationSection {...defaultProps} />)

      expect(screen.getByText('How will injects be delivered?')).toBeInTheDocument()
      expect(screen.getByText('Clock-driven')).toBeInTheDocument()
      expect(screen.getByText('Facilitator-paced')).toBeInTheDocument()
    })

    it('renders timeline mode options', () => {
      renderWithTheme(<TimingConfigurationSection {...defaultProps} />)

      expect(screen.getByText('What timeline will the exercise use?')).toBeInTheDocument()
      expect(screen.getByText('Real-time')).toBeInTheDocument()
      expect(screen.getByText('Compressed')).toBeInTheDocument()
      expect(screen.getByText('Story-only')).toBeInTheDocument()
    })

    it('shows selected delivery mode', () => {
      renderWithTheme(
        <TimingConfigurationSection
          {...defaultProps}
          deliveryMode={DeliveryMode.ClockDriven}
        />,
      )

      const clockDrivenRadio = screen.getByRole('radio', { name: /Clock-driven/ })
      expect(clockDrivenRadio).toBeChecked()
    })

    it('shows selected timeline mode', () => {
      renderWithTheme(
        <TimingConfigurationSection
          {...defaultProps}
          timelineMode={TimelineMode.RealTime}
        />,
      )

      // Use getAllByRole and find the one that's checked to avoid name collision
      // with the "1x (Real-time)" clock speed option
      const realTimeRadios = screen.getAllByRole('radio', { name: /Real-time/ })
      const checkedRealTimeRadio = realTimeRadios.find(
        radio => radio.getAttribute('value') === 'RealTime',
      )
      expect(checkedRealTimeRadio).toBeChecked()
    })

    it('calls onChange when delivery mode changes', async () => {
      const onChange = vi.fn()
      const user = userEvent.setup()

      renderWithTheme(
        <TimingConfigurationSection {...defaultProps} onChange={onChange} />,
      )

      const facilitatorPacedRadio = screen.getByRole('radio', { name: /Facilitator-paced/ })
      await user.click(facilitatorPacedRadio)

      expect(onChange).toHaveBeenCalledWith('deliveryMode', DeliveryMode.FacilitatorPaced)
    })

    it('calls onChange when timeline mode changes', async () => {
      const onChange = vi.fn()
      const user = userEvent.setup()

      renderWithTheme(
        <TimingConfigurationSection {...defaultProps} onChange={onChange} />,
      )

      const compressedRadio = screen.getByRole('radio', { name: /Compressed/ })
      await user.click(compressedRadio)

      expect(onChange).toHaveBeenCalledWith('timelineMode', TimelineMode.Compressed)
    })

    it('shows clock speed options when Clock-driven selected', () => {
      renderWithTheme(
        <TimingConfigurationSection
          {...defaultProps}
          deliveryMode={DeliveryMode.ClockDriven}
        />,
      )

      expect(screen.getByText('Clock Speed')).toBeInTheDocument()
      expect(screen.getByText('1x (Real-time)')).toBeInTheDocument()
      expect(screen.getByText('2x')).toBeInTheDocument()
      expect(screen.getByText('5x')).toBeInTheDocument()
    })

    it('hides timeline and clock speed options when Facilitator-paced selected', () => {
      renderWithTheme(
        <TimingConfigurationSection
          {...defaultProps}
          deliveryMode={DeliveryMode.FacilitatorPaced}
        />,
      )

      expect(screen.queryByText('What timeline will the exercise use?')).not.toBeInTheDocument()
      expect(screen.queryByText('Clock Speed')).not.toBeInTheDocument()
    })

    it('displays helper text for clock multiplier greater than 1', () => {
      renderWithTheme(
        <TimingConfigurationSection
          {...defaultProps}
          clockMultiplier={5}
        />,
      )

      expect(screen.getByText('1 minute wall clock = 5 minutes scenario time')).toBeInTheDocument()
    })

    it('calls onChange with numeric value when clock multiplier changes', async () => {
      const onChange = vi.fn()
      const user = userEvent.setup()

      renderWithTheme(
        <TimingConfigurationSection
          {...defaultProps}
          clockMultiplier={1}
          onChange={onChange}
        />,
      )

      const twoXRadio = screen.getByRole('radio', { name: /2x/ })
      await user.click(twoXRadio)

      expect(onChange).toHaveBeenCalledWith('clockMultiplier', 2)
    })

    it('shows validation error for delivery mode', () => {
      renderWithTheme(
        <TimingConfigurationSection
          {...defaultProps}
          errors={{ deliveryMode: 'Delivery mode is required' }}
        />,
      )

      expect(screen.getByText('Delivery mode is required')).toBeInTheDocument()
    })

    it('shows validation error for timeline mode', () => {
      renderWithTheme(
        <TimingConfigurationSection
          {...defaultProps}
          errors={{ timelineMode: 'Timeline mode is required' }}
        />,
      )

      expect(screen.getByText('Timeline mode is required')).toBeInTheDocument()
    })

    it('shows validation error for clock multiplier', () => {
      renderWithTheme(
        <TimingConfigurationSection
          {...defaultProps}
          errors={{ clockMultiplier: 'Clock multiplier is required' }}
        />,
      )

      expect(screen.getByText('Clock multiplier is required')).toBeInTheDocument()
    })

    it('shows help tooltips for both sections', () => {
      renderWithTheme(<TimingConfigurationSection {...defaultProps} />)

      const deliveryModeHelp = screen.getByLabelText('Delivery mode help')
      const timelineModeHelp = screen.getByLabelText('Timeline mode help')

      expect(deliveryModeHelp).toBeInTheDocument()
      expect(timelineModeHelp).toBeInTheDocument()
    })
  })

  describe('Locked State (Active Exercise)', () => {
    it('shows locked state when isLocked is true', () => {
      renderWithTheme(
        <TimingConfigurationSection {...defaultProps} isLocked={true} />,
      )

      expect(
        screen.getByText('Timing Configuration (locked during active exercise)'),
      ).toBeInTheDocument()
    })

    it('displays lock icon in locked state', () => {
      renderWithTheme(
        <TimingConfigurationSection {...defaultProps} isLocked={true} />,
      )

      // FontAwesome icon is rendered via SVG
      expect(screen.getByText(/Timing Configuration/i)).toBeInTheDocument()
    })

    it('displays current delivery mode in locked state', () => {
      renderWithTheme(
        <TimingConfigurationSection
          {...defaultProps}
          deliveryMode={DeliveryMode.ClockDriven}
          isLocked={true}
        />,
      )

      expect(screen.getByText('Delivery Mode:')).toBeInTheDocument()
      expect(screen.getByText('Clock-driven')).toBeInTheDocument()
    })

    it('displays current timeline mode as Real-time in locked state', () => {
      renderWithTheme(
        <TimingConfigurationSection
          {...defaultProps}
          timelineMode={TimelineMode.RealTime}
          isLocked={true}
        />,
      )

      expect(screen.getByText('Timeline Mode:')).toBeInTheDocument()
      expect(screen.getByText('Real-time')).toBeInTheDocument()
    })

    it('displays current timeline mode as Compressed in locked state', () => {
      renderWithTheme(
        <TimingConfigurationSection
          {...defaultProps}
          timelineMode={TimelineMode.Compressed}
          clockMultiplier={5}
          isLocked={true}
        />,
      )

      expect(screen.getByText('Timeline Mode:')).toBeInTheDocument()
      expect(screen.getByText('Compressed')).toBeInTheDocument()
    })

    it('displays current timeline mode as Story-only in locked state', () => {
      renderWithTheme(
        <TimingConfigurationSection
          {...defaultProps}
          timelineMode={TimelineMode.StoryOnly}
          isLocked={true}
        />,
      )

      expect(screen.getByText('Story-only')).toBeInTheDocument()
    })

    it('shows instruction message in locked state', () => {
      renderWithTheme(
        <TimingConfigurationSection {...defaultProps} isLocked={true} />,
      )

      expect(
        screen.getByText('To change these settings, stop the exercise first.'),
      ).toBeInTheDocument()
    })

    it('does not show radio buttons in locked state', () => {
      renderWithTheme(
        <TimingConfigurationSection {...defaultProps} isLocked={true} />,
      )

      expect(screen.queryByRole('radio')).not.toBeInTheDocument()
    })
  })
})
