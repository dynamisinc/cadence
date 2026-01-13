/**
 * AppLayout Component Tests
 *
 * Tests for the main application layout including:
 * - Header rendering
 * - Sidebar integration
 * - Breadcrumb display
 * - Content area
 * - Sidebar state persistence
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { ThemeProvider } from '@mui/material/styles'
import { AppLayout } from './AppLayout'
import { cobraTheme } from '../../../theme/cobraTheme'
import { FeatureFlagsProvider } from '../../../admin'
import { BreadcrumbProvider } from '../../contexts'

// Mock useMediaQuery for desktop behavior
vi.mock('@mui/material', async () => {
  const actual = await vi.importActual('@mui/material')
  return {
    ...actual,
    useMediaQuery: vi.fn().mockReturnValue(false), // Desktop
  }
})

// Helper to render with providers
const renderAppLayout = (
  children: React.ReactNode = <div>Test Content</div>,
  props = {},
) => {
  return render(
    <ThemeProvider theme={cobraTheme}>
      <FeatureFlagsProvider>
        <MemoryRouter>
          <BreadcrumbProvider>
            <AppLayout {...props}>{children}</AppLayout>
          </BreadcrumbProvider>
        </MemoryRouter>
      </FeatureFlagsProvider>
    </ThemeProvider>,
  )
}

describe('AppLayout', () => {
  const mockLocalStorage: Record<string, string> = {}

  beforeEach(() => {
    Object.keys(mockLocalStorage).forEach(key => delete mockLocalStorage[key])

    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(key => {
      return mockLocalStorage[key] || null
    })

    vi.spyOn(Storage.prototype, 'setItem').mockImplementation((key, value) => {
      mockLocalStorage[key] = value
    })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  describe('rendering', () => {
    it('renders the layout container', () => {
      renderAppLayout()

      const layout = screen.getByTestId('app-layout')
      expect(layout).toBeInTheDocument()
    })

    it('renders AppHeader', () => {
      renderAppLayout()

      const header = screen.getByTestId('app-header')
      expect(header).toBeInTheDocument()
    })

    it('renders main content area', () => {
      renderAppLayout()

      const main = screen.getByTestId('main-content')
      expect(main).toBeInTheDocument()
    })

    it('renders workspace content area', () => {
      renderAppLayout()

      const workspace = screen.getByTestId('workspace-content')
      expect(workspace).toBeInTheDocument()
    })

    it('renders children in workspace', () => {
      renderAppLayout(<div data-testid="child-content">Child Content</div>)

      const child = screen.getByTestId('child-content')
      expect(child).toBeInTheDocument()
      expect(child).toHaveTextContent('Child Content')
    })
  })

  describe('breadcrumb', () => {
    it('shows breadcrumb by default', () => {
      renderAppLayout()

      const breadcrumb = screen.getByTestId('breadcrumb-container')
      expect(breadcrumb).toBeInTheDocument()
    })

    it('hides breadcrumb when hideBreadcrumb is true', () => {
      renderAppLayout(<div>Content</div>, { hideBreadcrumb: true })

      expect(screen.queryByTestId('breadcrumb-container')).not.toBeInTheDocument()
    })
  })

  describe('sidebar state persistence', () => {
    it('saves sidebar state to localStorage when toggled', async () => {
      renderAppLayout()

      // There may be multiple sidebar toggles (desktop and mobile), get the first one
      const toggles = screen.getAllByTestId('sidebar-toggle')
      fireEvent.click(toggles[0])

      await waitFor(() => {
        expect(mockLocalStorage['cadence-sidebar-open']).toBe('false')
      })
    })

    it('loads sidebar state from localStorage on mount', () => {
      mockLocalStorage['cadence-sidebar-open'] = 'false'

      renderAppLayout()

      const sidebar = screen.getByTestId('sidebar-desktop')
      const styles = window.getComputedStyle(sidebar)

      // Sidebar should be closed (64px width)
      expect(styles.width).toBe('64px')
    })
  })

  describe('mobile behavior', () => {
    it('mobile menu toggle exists', () => {
      renderAppLayout()

      const mobileToggle = screen.getByTestId('mobile-menu-toggle')
      expect(mobileToggle).toBeInTheDocument()
    })
  })

  describe('layout structure', () => {
    it('renders children inside workspace area', () => {
      renderAppLayout(<div data-testid="test-child">Test</div>)

      const child = screen.getByTestId('test-child')
      const workspace = screen.getByTestId('workspace-content')

      expect(workspace).toContainElement(child)
    })
  })
})
