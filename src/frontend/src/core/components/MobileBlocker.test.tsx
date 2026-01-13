import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { ThemeProvider } from '@mui/material/styles'
import { MobileBlocker } from './MobileBlocker'
import { cobraTheme } from '../../theme/cobraTheme'

// Mock useMediaQuery
const mockUseMediaQuery = vi.fn()
vi.mock('@mui/material', async () => {
  const actual = await vi.importActual('@mui/material')
  return {
    ...actual,
    useMediaQuery: () => mockUseMediaQuery(),
  }
})

const renderWithTheme = (component: React.ReactElement) => {
  return render(
    <ThemeProvider theme={cobraTheme}>
      {component}
    </ThemeProvider>,
  )
}

describe('MobileBlocker', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('when viewport is mobile (< 768px)', () => {
    beforeEach(() => {
      mockUseMediaQuery.mockReturnValue(true)
    })

    it('renders the blocker screen', () => {
      renderWithTheme(
        <MobileBlocker>
          <div data-testid="child-content">App Content</div>
        </MobileBlocker>,
      )

      expect(screen.getByTestId('mobile-blocker')).toBeInTheDocument()
    })

    it('does not render children', () => {
      renderWithTheme(
        <MobileBlocker>
          <div data-testid="child-content">App Content</div>
        </MobileBlocker>,
      )

      expect(screen.queryByTestId('child-content')).not.toBeInTheDocument()
    })

    it('displays the correct heading', () => {
      renderWithTheme(
        <MobileBlocker>
          <div>Content</div>
        </MobileBlocker>,
      )

      expect(screen.getByText('Larger Screen Required')).toBeInTheDocument()
    })

    it('displays the explanation message', () => {
      renderWithTheme(
        <MobileBlocker>
          <div>Content</div>
        </MobileBlocker>,
      )

      expect(
        screen.getByText(/Cadence works best on tablets and desktops/),
      ).toBeInTheDocument()
      expect(
        screen.getByText(/screen width of at least 768 pixels/),
      ).toBeInTheDocument()
    })

    it('displays minimum required width', () => {
      renderWithTheme(
        <MobileBlocker>
          <div>Content</div>
        </MobileBlocker>,
      )

      expect(screen.getByText('Minimum required:')).toBeInTheDocument()
      expect(screen.getByText('768px')).toBeInTheDocument()
    })

    it('displays current viewport width', () => {
      renderWithTheme(
        <MobileBlocker>
          <div>Content</div>
        </MobileBlocker>,
      )

      expect(screen.getByText('Current width:')).toBeInTheDocument()
    })
  })

  describe('when viewport is tablet or larger (>= 768px)', () => {
    beforeEach(() => {
      mockUseMediaQuery.mockReturnValue(false)
    })

    it('does not render the blocker screen', () => {
      renderWithTheme(
        <MobileBlocker>
          <div data-testid="child-content">App Content</div>
        </MobileBlocker>,
      )

      expect(screen.queryByTestId('mobile-blocker')).not.toBeInTheDocument()
    })

    it('renders children', () => {
      renderWithTheme(
        <MobileBlocker>
          <div data-testid="child-content">App Content</div>
        </MobileBlocker>,
      )

      expect(screen.getByTestId('child-content')).toBeInTheDocument()
      expect(screen.getByText('App Content')).toBeInTheDocument()
    })
  })
})
