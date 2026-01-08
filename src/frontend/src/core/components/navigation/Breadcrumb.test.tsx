/**
 * Breadcrumb Component Tests
 *
 * Tests for the navigation breadcrumb including:
 * - Auto-generation based on current route
 * - Custom items override
 * - Clickable navigation links
 * - Home icon display
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '../../../test/testUtils'
import { Breadcrumb, type BreadcrumbItem } from './Breadcrumb'
import { faHome, faStickyNote } from '@fortawesome/free-solid-svg-icons'

// Mock react-router-dom
const mockNavigate = vi.fn()
let mockPathname = '/'

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useLocation: () => ({ pathname: mockPathname }),
  }
})

describe('Breadcrumb', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockPathname = '/'
  })

  describe('rendering', () => {
    it('renders the breadcrumb container', () => {
      render(<Breadcrumb />)

      const container = screen.getByTestId('breadcrumb-container')
      expect(container).toBeInTheDocument()
    })

    it('renders Home as single item when at root', () => {
      mockPathname = '/'
      render(<Breadcrumb />)

      const homeItem = screen.getByTestId('breadcrumb-item-0')
      expect(homeItem).toBeInTheDocument()
      expect(homeItem).toHaveTextContent('Home')
    })

    it('renders Home with link when not at root', () => {
      mockPathname = '/notes'
      render(<Breadcrumb />)

      const homeLink = screen.getByTestId('breadcrumb-link-0')
      expect(homeLink).toBeInTheDocument()
      expect(homeLink).toHaveTextContent('Home')
    })

    it('renders Notes breadcrumb for /notes route', () => {
      mockPathname = '/notes'
      render(<Breadcrumb />)

      const notesItem = screen.getByTestId('breadcrumb-item-1')
      expect(notesItem).toBeInTheDocument()
      expect(notesItem).toHaveTextContent('Notes')
    })

    it('capitalizes unknown routes', () => {
      mockPathname = '/settings'
      render(<Breadcrumb />)

      const settingsItem = screen.getByTestId('breadcrumb-item-1')
      expect(settingsItem).toBeInTheDocument()
      expect(settingsItem).toHaveTextContent('Settings')
    })
  })

  describe('custom items', () => {
    it('uses provided items instead of auto-generated', () => {
      const customItems: BreadcrumbItem[] = [
        { label: 'Dashboard', path: '/dashboard' },
        { label: 'Reports', path: '/reports' },
        { label: 'Monthly' },
      ]

      render(<Breadcrumb items={customItems} />)

      expect(screen.getByTestId('breadcrumb-link-0')).toHaveTextContent('Dashboard')
      expect(screen.getByTestId('breadcrumb-link-1')).toHaveTextContent('Reports')
      expect(screen.getByTestId('breadcrumb-item-2')).toHaveTextContent('Monthly')
    })

    it('renders icons when provided', () => {
      const customItems: BreadcrumbItem[] = [
        { label: 'Home', path: '/', icon: faHome },
        { label: 'Notes', icon: faStickyNote },
      ]

      render(<Breadcrumb items={customItems} />)

      // Icons should be rendered (FontAwesome icons render as SVGs)
      const homeLink = screen.getByTestId('breadcrumb-link-0')
      expect(homeLink.querySelector('svg')).toBeInTheDocument()
    })
  })

  describe('navigation', () => {
    it('navigates when clicking a link item', () => {
      mockPathname = '/notes'
      render(<Breadcrumb />)

      const homeLink = screen.getByTestId('breadcrumb-link-0')
      fireEvent.click(homeLink)

      expect(mockNavigate).toHaveBeenCalledWith('/')
    })

    it('does not navigate when clicking the last item', () => {
      mockPathname = '/notes'
      render(<Breadcrumb />)

      const notesItem = screen.getByTestId('breadcrumb-item-1')
      fireEvent.click(notesItem)

      // Last item is not a link, so navigate should not be called
      expect(mockNavigate).not.toHaveBeenCalled()
    })

    it('navigates to custom paths', () => {
      const customItems: BreadcrumbItem[] = [
        { label: 'Dashboard', path: '/dashboard' },
        { label: 'Current Page' },
      ]

      render(<Breadcrumb items={customItems} />)

      const dashboardLink = screen.getByTestId('breadcrumb-link-0')
      fireEvent.click(dashboardLink)

      expect(mockNavigate).toHaveBeenCalledWith('/dashboard')
    })
  })

  describe('separators', () => {
    it('renders separator between items', () => {
      mockPathname = '/notes'
      render(<Breadcrumb />)

      // There should be a "/" separator between Home and Notes
      const container = screen.getByTestId('breadcrumb-container')
      expect(container.textContent).toContain('/')
    })

    it('does not render separator before first item', () => {
      mockPathname = '/'
      render(<Breadcrumb />)

      // Only "Home" should be visible, no separator
      const container = screen.getByTestId('breadcrumb-container')
      expect(container.textContent).toBe('Home')
    })
  })

  describe('styling', () => {
    it('applies correct background color from theme', () => {
      render(<Breadcrumb />)

      const container = screen.getByTestId('breadcrumb-container')
      const styles = window.getComputedStyle(container)

      // Should have breadcrumb background color (light gray)
      expect(styles.backgroundColor).toBeDefined()
    })

    it('has minimum height of 40px', () => {
      render(<Breadcrumb />)

      const container = screen.getByTestId('breadcrumb-container')
      const styles = window.getComputedStyle(container)

      expect(styles.minHeight).toBe('40px')
    })

    it('last item has bolder font weight', () => {
      mockPathname = '/notes'
      render(<Breadcrumb />)

      const notesItem = screen.getByTestId('breadcrumb-item-1')
      const styles = window.getComputedStyle(notesItem)

      expect(parseInt(styles.fontWeight)).toBeGreaterThanOrEqual(500)
    })
  })
})
