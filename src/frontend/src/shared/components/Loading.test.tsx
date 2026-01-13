import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { ThemeProvider } from '@mui/material/styles'
import { Loading } from './Loading'
import { cobraTheme } from '../../theme/cobraTheme'

const renderWithTheme = (component: React.ReactElement) => {
  return render(
    <ThemeProvider theme={cobraTheme}>
      {component}
    </ThemeProvider>,
  )
}

describe('Loading', () => {
  it('renders the loading spinner', () => {
    renderWithTheme(<Loading />)

    expect(screen.getByTestId('loading')).toBeInTheDocument()
    expect(screen.getByRole('progressbar')).toBeInTheDocument()
  })

  it('renders with a message', () => {
    renderWithTheme(<Loading message="Loading data..." />)

    expect(screen.getByText('Loading data...')).toBeInTheDocument()
  })

  it('renders without message when not provided', () => {
    renderWithTheme(<Loading />)

    // Should not render any Typography message elements (p tags with message text)
    // Note: The CircularProgress may contain internal span elements
    expect(screen.queryByText(/loading/i)).not.toBeInTheDocument()
    expect(screen.queryByText(/please wait/i)).not.toBeInTheDocument()
  })

  describe('sizes', () => {
    it('renders small size', () => {
      renderWithTheme(<Loading size="small" />)

      const progressbar = screen.getByRole('progressbar')
      expect(progressbar).toBeInTheDocument()
    })

    it('renders medium size (default)', () => {
      renderWithTheme(<Loading />)

      const progressbar = screen.getByRole('progressbar')
      expect(progressbar).toBeInTheDocument()
    })

    it('renders large size', () => {
      renderWithTheme(<Loading size="large" />)

      const progressbar = screen.getByRole('progressbar')
      expect(progressbar).toBeInTheDocument()
    })
  })

  describe('overlay mode', () => {
    it('renders overlay when overlay prop is true', () => {
      renderWithTheme(<Loading overlay />)

      expect(screen.getByTestId('loading-overlay')).toBeInTheDocument()
      expect(screen.getByTestId('loading')).toBeInTheDocument()
    })

    it('does not render overlay by default', () => {
      renderWithTheme(<Loading />)

      expect(screen.queryByTestId('loading-overlay')).not.toBeInTheDocument()
    })

    it('renders overlay with message', () => {
      renderWithTheme(<Loading overlay message="Please wait..." />)

      expect(screen.getByTestId('loading-overlay')).toBeInTheDocument()
      expect(screen.getByText('Please wait...')).toBeInTheDocument()
    })
  })

  describe('fullPage mode', () => {
    it('applies fullPage styling when prop is true', () => {
      renderWithTheme(<Loading fullPage />)

      const loading = screen.getByTestId('loading')
      expect(loading).toBeInTheDocument()
      // The component should have min-height: 100vh when fullPage is true
      expect(loading).toHaveStyle({ minHeight: '100vh' })
    })
  })
})
