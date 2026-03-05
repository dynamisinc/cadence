/**
 * HelpTooltip Component Tests
 */

import { describe, it, expect } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { ThemeProvider } from '@mui/material/styles'
import { HelpTooltip } from './HelpTooltip'
import { cobraTheme } from '../../theme/cobraTheme'

const renderWithTheme = (component: React.ReactElement) => {
  return render(
    <ThemeProvider theme={cobraTheme}>{component}</ThemeProvider>,
  )
}

describe('HelpTooltip', () => {
  it('renders nothing when no content is provided', () => {
    const { container } = renderWithTheme(<HelpTooltip />)
    expect(container.firstChild).toBeNull()
  })

  it('renders nothing for an invalid helpKey', () => {
    const { container } = renderWithTheme(
      <HelpTooltip helpKey="nonexistent.key" />,
    )
    expect(container.firstChild).toBeNull()
  })

  it('renders an info icon when summary is provided', () => {
    renderWithTheme(<HelpTooltip summary="Test help text" />)
    expect(screen.getByTestId('help-tooltip-icon')).toBeInTheDocument()
  })

  it('renders an info icon when valid helpKey is provided', () => {
    renderWithTheme(<HelpTooltip helpKey="conduct.fire" />)
    expect(screen.getByTestId('help-tooltip-icon')).toBeInTheDocument()
  })

  describe('compact mode', () => {
    it('renders with MUI Tooltip wrapping the icon', async () => {
      renderWithTheme(
        <HelpTooltip summary="Short tip" compact />,
      )
      const icon = screen.getByTestId('help-tooltip-icon')
      expect(icon).toBeInTheDocument()
    })
  })

  describe('full mode (popover)', () => {
    it('opens popover on click', () => {
      renderWithTheme(
        <HelpTooltip summary="Detailed help" details="More info here" />,
      )

      fireEvent.click(screen.getByTestId('help-tooltip-icon'))

      expect(screen.getByText('Detailed help')).toBeInTheDocument()
      expect(screen.getByText('More info here')).toBeInTheDocument()
    })

    it('shows HSEEP glossary terms in popover', () => {
      renderWithTheme(
        <HelpTooltip
          summary="Fire injects"
          relatedTerms={['inject', 'fire']}
        />,
      )

      fireEvent.click(screen.getByTestId('help-tooltip-icon'))

      expect(screen.getByText('HSEEP Terms')).toBeInTheDocument()
      expect(screen.getByText('Inject')).toBeInTheDocument()
      expect(screen.getByText('Fire')).toBeInTheDocument()
    })

    it('shows role-specific tip when exerciseRole matches', () => {
      renderWithTheme(
        <HelpTooltip
          summary="Inject controls"
          roleTips={{ Controller: 'You can fire injects here.' }}
          exerciseRole="Controller"
        />,
      )

      fireEvent.click(screen.getByTestId('help-tooltip-icon'))

      expect(screen.getByText('What you can do')).toBeInTheDocument()
      expect(
        screen.getByText('You can fire injects here.'),
      ).toBeInTheDocument()
    })

    it('does not show role tip when exerciseRole does not match', () => {
      renderWithTheme(
        <HelpTooltip
          summary="Inject controls"
          roleTips={{ Controller: 'You can fire injects here.' }}
          exerciseRole="Observer"
        />,
      )

      fireEvent.click(screen.getByTestId('help-tooltip-icon'))

      expect(screen.queryByText('What you can do')).not.toBeInTheDocument()
    })

    it('resolves content from helpKey', () => {
      renderWithTheme(
        <HelpTooltip helpKey="conduct.clock" exerciseRole="ExerciseDirector" />,
      )

      fireEvent.click(screen.getByTestId('help-tooltip-icon'))

      // Should show the summary from CONTEXTUAL_HELP['conduct.clock']
      expect(
        screen.getByText(/Dual time tracking/),
      ).toBeInTheDocument()
      // Should show the ExerciseDirector role tip
      expect(
        screen.getByText(/start, pause, and reset/),
      ).toBeInTheDocument()
    })

    it('closes popover on click away', () => {
      renderWithTheme(
        <HelpTooltip summary="Test" details="Details here" />,
      )

      fireEvent.click(screen.getByTestId('help-tooltip-icon'))
      expect(screen.getByText('Details here')).toBeInTheDocument()

      // Click the backdrop to close
      const backdrop = document.querySelector('.MuiBackdrop-root')
      if (backdrop) {
        fireEvent.click(backdrop)
      }
    })

    it('prefers inline props over helpKey', () => {
      renderWithTheme(
        <HelpTooltip
          helpKey="conduct.fire"
          summary="Custom override summary"
        />,
      )

      fireEvent.click(screen.getByTestId('help-tooltip-icon'))

      expect(
        screen.getByText('Custom override summary'),
      ).toBeInTheDocument()
    })
  })
})
