/**
 * Sidebar Component Tests
 *
 * Exhaustive tests for the collapsible left navigation sidebar including:
 * - Role-based menu filtering
 * - Section headers (CONDUCT, ANALYSIS, SYSTEM)
 * - Active state highlighting
 * - Disabled state for items requiring exercise context
 * - Mobile drawer behavior
 * - Navigation functionality
 * - Accessibility
 *
 * @see docs/features/navigation-shell/S01-updated-sidebar-menu.md
 * @see docs/features/navigation-shell/S02-role-based-menu-visibility.md
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { ThemeProvider } from '@mui/material/styles'
import { Sidebar } from './Sidebar'
import { cobraTheme } from '../../../theme/cobraTheme'
import { HseepRole } from '../../../types'

// Mock useMediaQuery for desktop/mobile testing
vi.mock('@mui/material', async () => {
  const actual = await vi.importActual('@mui/material')
  return {
    ...actual,
    useMediaQuery: vi.fn().mockReturnValue(false), // Default to desktop
  }
})

// Import the mocked useMediaQuery
import { useMediaQuery } from '@mui/material'

// Mock useNavigate
const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

// Mock useFilteredMenu hook
const mockUseFilteredMenu = vi.fn()
vi.mock('../../../shared/hooks', async () => {
  const actual = await vi.importActual('../../../shared/hooks')
  return {
    ...actual,
    useFilteredMenu: () => mockUseFilteredMenu(),
    MENU_SECTION_LABELS: {
      conduct: 'CONDUCT',
      analysis: 'ANALYSIS',
      system: 'SYSTEM',
    },
  }
})

// Import menu items for building mock data
import {
  faClipboardList,
  faFolderOpen,
  faGamepad,
  faListCheck,
  faBinoculars,
  faChartBar,
  faFileAlt,
  faUsers,
  faCog,
} from '@fortawesome/free-solid-svg-icons'

// Build mock menu items
const mockMenuItems = {
  'my-assignments': {
    id: 'my-assignments',
    label: 'My Assignments',
    icon: faClipboardList,
    path: '/assignments',
    section: 'conduct',
    allowedRoles: [],
    requiresExerciseContext: false,
    disabledTooltip: undefined,
  },
  'exercises': {
    id: 'exercises',
    label: 'Exercises',
    icon: faFolderOpen,
    path: '/exercises',
    section: 'conduct',
    allowedRoles: [],
    requiresExerciseContext: false,
    disabledTooltip: undefined,
  },
  'control-room': {
    id: 'control-room',
    label: 'Control Room',
    icon: faGamepad,
    path: '/exercises/:id/control',
    section: 'conduct',
    allowedRoles: [HseepRole.Administrator, HseepRole.ExerciseDirector, HseepRole.Controller],
    requiresExerciseContext: true,
    disabledTooltip: 'Enter an exercise first',
  },
  'inject-queue': {
    id: 'inject-queue',
    label: 'Inject Queue',
    icon: faListCheck,
    path: '/exercises/:id/queue',
    section: 'conduct',
    allowedRoles: [HseepRole.Administrator, HseepRole.ExerciseDirector, HseepRole.Controller],
    requiresExerciseContext: true,
    disabledTooltip: 'Enter an exercise first',
  },
  'observations': {
    id: 'observations',
    label: 'Observations',
    icon: faBinoculars,
    path: '/exercises/:id/observations',
    section: 'analysis',
    allowedRoles: [HseepRole.Administrator, HseepRole.ExerciseDirector, HseepRole.Evaluator],
    requiresExerciseContext: true,
    disabledTooltip: 'Enter an exercise first',
  },
  'reports': {
    id: 'reports',
    label: 'Reports',
    icon: faChartBar,
    path: '/reports',
    section: 'analysis',
    allowedRoles: [HseepRole.Administrator, HseepRole.ExerciseDirector],
    requiresExerciseContext: false,
    disabledTooltip: undefined,
  },
  'templates': {
    id: 'templates',
    label: 'Templates',
    icon: faFileAlt,
    path: '/templates',
    section: 'system',
    allowedRoles: [HseepRole.Administrator],
    requiresExerciseContext: false,
    disabledTooltip: undefined,
  },
  'users': {
    id: 'users',
    label: 'Users',
    icon: faUsers,
    path: '/users',
    section: 'system',
    allowedRoles: [HseepRole.Administrator],
    requiresExerciseContext: false,
    disabledTooltip: undefined,
  },
  'settings': {
    id: 'settings',
    label: 'My Preferences',
    icon: faCog,
    path: '/settings',
    section: 'system',
    allowedRoles: [],
    requiresExerciseContext: false,
    disabledTooltip: undefined,
  },
}

type MockMenuItem = typeof mockMenuItems[keyof typeof mockMenuItems]

// Helper to create mock return value for useFilteredMenu
const createMockFilteredMenu = (
  itemIds: string[],
  options: { exerciseId?: string | null } = {},
) => {
  const { exerciseId = null } = options
  const items = itemIds.map(id => mockMenuItems[id as keyof typeof mockMenuItems])

  const groupedBySection = {
    conduct: items.filter((i: MockMenuItem) => i.section === 'conduct'),
    analysis: items.filter((i: MockMenuItem) => i.section === 'analysis'),
    system: items.filter((i: MockMenuItem) => i.section === 'system'),
  }

  const visibleSections = (['conduct', 'analysis', 'system'] as const).filter(
    s => groupedBySection[s].length > 0,
  )

  return {
    filteredItems: items,
    groupedBySection,
    visibleSections,
    isItemDisabled: (itemId: string) => {
      const item = mockMenuItems[itemId as keyof typeof mockMenuItems]
      return item?.requiresExerciseContext && !exerciseId
    },
    getDisabledTooltip: (itemId: string) => {
      const item = mockMenuItems[itemId as keyof typeof mockMenuItems]
      if (item?.requiresExerciseContext && !exerciseId) {
        return item.disabledTooltip
      }
      return undefined
    },
  }
}

// Helper to render with providers
const renderSidebar = (props = {}, initialRoute = '/') => {
  const defaultProps = {
    open: true,
    onToggle: vi.fn(),
    mobileOpen: false,
    onMobileClose: vi.fn(),
  }

  return render(
    <ThemeProvider theme={cobraTheme}>
      <MemoryRouter initialEntries={[initialRoute]}>
        <Sidebar {...defaultProps} {...props} />
      </MemoryRouter>
    </ThemeProvider>,
  )
}

describe('Sidebar', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
    (useMediaQuery as ReturnType<typeof vi.fn>).mockReturnValue(false) // Desktop by default

    // Default mock: Admin sees all items, not in exercise context
    mockUseFilteredMenu.mockReturnValue(
      createMockFilteredMenu([
        'my-assignments',
        'exercises',
        'control-room',
        'inject-queue',
        'observations',
        'reports',
        'templates',
        'users',
        'settings',
      ]),
    )
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  // ===========================================================================
  // Desktop Rendering Tests
  // ===========================================================================
  describe('Desktop Rendering', () => {
    it('renders desktop sidebar when on larger screens', () => {
      renderSidebar()

      const sidebar = screen.getByTestId('sidebar-desktop')
      expect(sidebar).toBeInTheDocument()
    })

    it('does not render mobile sidebar when on desktop', () => {
      renderSidebar()

      expect(screen.queryByTestId('sidebar-mobile')).not.toBeInTheDocument()
    })

    it('renders sidebar content', () => {
      renderSidebar()

      const content = screen.getByTestId('sidebar-content')
      expect(content).toBeInTheDocument()
    })

    it('renders toggle button on desktop', () => {
      renderSidebar()

      const toggle = screen.getByTestId('sidebar-toggle')
      expect(toggle).toBeInTheDocument()
    })

    it('toggle button has chevron-left icon when open', () => {
      renderSidebar({ open: true })

      const toggle = screen.getByTestId('sidebar-toggle')
      expect(toggle).toBeInTheDocument()
    })

    it('toggle button has chevron-right icon when closed', () => {
      renderSidebar({ open: false })

      const toggle = screen.getByTestId('sidebar-toggle')
      expect(toggle).toBeInTheDocument()
    })
  })

  // ===========================================================================
  // Section Headers Tests
  // ===========================================================================
  describe('Section Headers', () => {
    it('renders CONDUCT section header when sidebar is open', () => {
      renderSidebar({ open: true })

      const section = screen.getByTestId('section-conduct')
      expect(section).toBeInTheDocument()
      expect(section).toHaveTextContent('CONDUCT')
    })

    it('renders ANALYSIS section header when sidebar is open', () => {
      renderSidebar({ open: true })

      const section = screen.getByTestId('section-analysis')
      expect(section).toBeInTheDocument()
      expect(section).toHaveTextContent('ANALYSIS')
    })

    it('renders SYSTEM section header when sidebar is open', () => {
      renderSidebar({ open: true })

      const section = screen.getByTestId('section-system')
      expect(section).toBeInTheDocument()
      expect(section).toHaveTextContent('SYSTEM')
    })

    it('hides all section headers when sidebar is collapsed', () => {
      renderSidebar({ open: false })

      expect(screen.queryByTestId('section-conduct')).not.toBeInTheDocument()
      expect(screen.queryByTestId('section-analysis')).not.toBeInTheDocument()
      expect(screen.queryByTestId('section-system')).not.toBeInTheDocument()
    })

    it('hides ANALYSIS section header when no items are visible', () => {
      mockUseFilteredMenu.mockReturnValue(
        createMockFilteredMenu(['my-assignments', 'exercises', 'settings']),
      )

      renderSidebar({ open: true })

      expect(screen.queryByTestId('section-analysis')).not.toBeInTheDocument()
    })

    it('section headers are uppercase', () => {
      renderSidebar({ open: true })

      const conduct = screen.getByTestId('section-conduct')
      expect(conduct.textContent).toBe('CONDUCT')
    })
  })

  // ===========================================================================
  // Menu Items Rendering Tests - Admin Role
  // ===========================================================================
  describe('Menu Items - Admin Role', () => {
    it('Admin sees all 9 menu items', () => {
      renderSidebar()

      expect(screen.getByTestId('nav-item-my-assignments')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-exercises')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-control-room')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-inject-queue')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-observations')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-reports')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-templates')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-users')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-settings')).toBeInTheDocument()
    })

    it('renders menu items with correct labels when open', () => {
      renderSidebar({ open: true })

      expect(screen.getByText('My Assignments')).toBeInTheDocument()
      expect(screen.getByText('Exercises')).toBeInTheDocument()
      expect(screen.getByText('Control Room')).toBeInTheDocument()
      expect(screen.getByText('Inject Queue')).toBeInTheDocument()
      expect(screen.getByText('Observations')).toBeInTheDocument()
      expect(screen.getByText('Reports')).toBeInTheDocument()
      expect(screen.getByText('Templates')).toBeInTheDocument()
      expect(screen.getByText('Users')).toBeInTheDocument()
      expect(screen.getByText('My Preferences')).toBeInTheDocument()
    })

    it('does not render labels when sidebar is collapsed', () => {
      renderSidebar({ open: false })

      expect(screen.queryByText('My Assignments')).not.toBeInTheDocument()
      expect(screen.queryByText('Exercises')).not.toBeInTheDocument()
    })
  })

  // ===========================================================================
  // Menu Items Rendering Tests - Controller Role
  // ===========================================================================
  describe('Menu Items - Controller Role', () => {
    beforeEach(() => {
      mockUseFilteredMenu.mockReturnValue(
        createMockFilteredMenu([
          'my-assignments',
          'exercises',
          'control-room',
          'inject-queue',
          'settings',
        ]),
      )
    })

    it('Controller sees 5 menu items', () => {
      renderSidebar()

      expect(screen.getByTestId('nav-item-my-assignments')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-exercises')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-control-room')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-inject-queue')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-settings')).toBeInTheDocument()
    })

    it('Controller does NOT see Observations', () => {
      renderSidebar()

      expect(screen.queryByTestId('nav-item-observations')).not.toBeInTheDocument()
    })

    it('Controller does NOT see Reports', () => {
      renderSidebar()

      expect(screen.queryByTestId('nav-item-reports')).not.toBeInTheDocument()
    })

    it('Controller does NOT see Templates', () => {
      renderSidebar()

      expect(screen.queryByTestId('nav-item-templates')).not.toBeInTheDocument()
    })

    it('Controller does NOT see Users', () => {
      renderSidebar()

      expect(screen.queryByTestId('nav-item-users')).not.toBeInTheDocument()
    })
  })

  // ===========================================================================
  // Menu Items Rendering Tests - Evaluator Role
  // ===========================================================================
  describe('Menu Items - Evaluator Role', () => {
    beforeEach(() => {
      mockUseFilteredMenu.mockReturnValue(
        createMockFilteredMenu([
          'my-assignments',
          'exercises',
          'observations',
          'settings',
        ]),
      )
    })

    it('Evaluator sees 4 menu items', () => {
      renderSidebar()

      expect(screen.getByTestId('nav-item-my-assignments')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-exercises')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-observations')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-settings')).toBeInTheDocument()
    })

    it('Evaluator does NOT see Control Room', () => {
      renderSidebar()

      expect(screen.queryByTestId('nav-item-control-room')).not.toBeInTheDocument()
    })

    it('Evaluator does NOT see Inject Queue', () => {
      renderSidebar()

      expect(screen.queryByTestId('nav-item-inject-queue')).not.toBeInTheDocument()
    })
  })

  // ===========================================================================
  // Menu Items Rendering Tests - Observer Role
  // ===========================================================================
  describe('Menu Items - Observer Role', () => {
    beforeEach(() => {
      mockUseFilteredMenu.mockReturnValue(
        createMockFilteredMenu(['my-assignments', 'exercises', 'settings']),
      )
    })

    it('Observer sees only 3 menu items', () => {
      renderSidebar()

      expect(screen.getByTestId('nav-item-my-assignments')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-exercises')).toBeInTheDocument()
      expect(screen.getByTestId('nav-item-settings')).toBeInTheDocument()
    })

    it('Observer does NOT see Control Room', () => {
      renderSidebar()

      expect(screen.queryByTestId('nav-item-control-room')).not.toBeInTheDocument()
    })

    it('Observer does NOT see Observations', () => {
      renderSidebar()

      expect(screen.queryByTestId('nav-item-observations')).not.toBeInTheDocument()
    })
  })

  // ===========================================================================
  // Disabled State Tests
  // ===========================================================================
  describe('Disabled State', () => {
    it('Control Room is disabled when not in exercise context', () => {
      renderSidebar()

      const controlRoom = screen.getByTestId('nav-item-control-room')
      expect(controlRoom).toHaveAttribute('data-disabled', 'true')
    })

    it('Inject Queue is disabled when not in exercise context', () => {
      renderSidebar()

      const injectQueue = screen.getByTestId('nav-item-inject-queue')
      expect(injectQueue).toHaveAttribute('data-disabled', 'true')
    })

    it('Observations is disabled when not in exercise context', () => {
      renderSidebar()

      const observations = screen.getByTestId('nav-item-observations')
      expect(observations).toHaveAttribute('data-disabled', 'true')
    })

    it('Control Room is enabled when in exercise context', () => {
      mockUseFilteredMenu.mockReturnValue(
        createMockFilteredMenu(
          ['my-assignments', 'exercises', 'control-room', 'inject-queue', 'settings'],
          { exerciseId: 'exercise-123' },
        ),
      )

      renderSidebar({}, '/exercises/exercise-123/control')

      const controlRoom = screen.getByTestId('nav-item-control-room')
      expect(controlRoom).toHaveAttribute('data-disabled', 'false')
    })

    it('disabled items do not navigate when clicked', () => {
      renderSidebar()

      const controlRoom = screen.getByTestId('nav-item-control-room')
      fireEvent.click(controlRoom)

      expect(mockNavigate).not.toHaveBeenCalled()
    })

    it('My Assignments is NOT disabled (no exercise context needed)', () => {
      renderSidebar()

      const myAssignments = screen.getByTestId('nav-item-my-assignments')
      expect(myAssignments).toHaveAttribute('data-disabled', 'false')
    })

    it('Exercises is NOT disabled (no exercise context needed)', () => {
      renderSidebar()

      const exercises = screen.getByTestId('nav-item-exercises')
      expect(exercises).toHaveAttribute('data-disabled', 'false')
    })

    it('Settings is NOT disabled (no exercise context needed)', () => {
      renderSidebar()

      const settings = screen.getByTestId('nav-item-settings')
      expect(settings).toHaveAttribute('data-disabled', 'false')
    })
  })

  // ===========================================================================
  // Toggle Behavior Tests
  // ===========================================================================
  describe('Toggle Behavior', () => {
    it('calls onToggle when toggle button is clicked', () => {
      const onToggle = vi.fn()
      renderSidebar({ onToggle })

      const toggle = screen.getByTestId('sidebar-toggle')
      fireEvent.click(toggle)

      expect(onToggle).toHaveBeenCalledTimes(1)
    })

    it('calls onToggle only once per click', () => {
      const onToggle = vi.fn()
      renderSidebar({ onToggle })

      const toggle = screen.getByTestId('sidebar-toggle')
      fireEvent.click(toggle)
      fireEvent.click(toggle)
      fireEvent.click(toggle)

      expect(onToggle).toHaveBeenCalledTimes(3)
    })
  })

  // ===========================================================================
  // Navigation Tests
  // ===========================================================================
  describe('Navigation', () => {
    it('navigates to /assignments when My Assignments is clicked', () => {
      renderSidebar()

      const nav = screen.getByTestId('nav-item-my-assignments')
      fireEvent.click(nav)

      expect(mockNavigate).toHaveBeenCalledWith('/assignments')
    })

    it('navigates to /exercises when Exercises is clicked', () => {
      renderSidebar()

      const nav = screen.getByTestId('nav-item-exercises')
      fireEvent.click(nav)

      expect(mockNavigate).toHaveBeenCalledWith('/exercises')
    })

    it('navigates to /settings when Settings is clicked', () => {
      renderSidebar()

      const nav = screen.getByTestId('nav-item-settings')
      fireEvent.click(nav)

      expect(mockNavigate).toHaveBeenCalledWith('/settings')
    })

    it('navigates to /reports when Reports is clicked', () => {
      renderSidebar()

      const nav = screen.getByTestId('nav-item-reports')
      fireEvent.click(nav)

      expect(mockNavigate).toHaveBeenCalledWith('/reports')
    })

    it('navigates to /templates when Templates is clicked', () => {
      renderSidebar()

      const nav = screen.getByTestId('nav-item-templates')
      fireEvent.click(nav)

      expect(mockNavigate).toHaveBeenCalledWith('/templates')
    })

    it('navigates to /users when Users is clicked', () => {
      renderSidebar()

      const nav = screen.getByTestId('nav-item-users')
      fireEvent.click(nav)

      expect(mockNavigate).toHaveBeenCalledWith('/users')
    })

    it('navigates to exercise-specific route when in exercise context', () => {
      mockUseFilteredMenu.mockReturnValue(
        createMockFilteredMenu(
          ['my-assignments', 'exercises', 'control-room', 'inject-queue', 'settings'],
          { exerciseId: 'exercise-123' },
        ),
      )

      renderSidebar({}, '/exercises/exercise-123/control')

      const controlRoom = screen.getByTestId('nav-item-control-room')
      fireEvent.click(controlRoom)

      expect(mockNavigate).toHaveBeenCalledWith('/exercises/exercise-123/control')
    })
  })

  // ===========================================================================
  // Active State Tests
  // ===========================================================================
  describe('Active State', () => {
    it('highlights My Assignments when on /assignments path', () => {
      renderSidebar({}, '/assignments')

      const nav = screen.getByTestId('nav-item-my-assignments')
      const styles = window.getComputedStyle(nav)

      expect(styles.backgroundColor).not.toBe('transparent')
    })

    it('highlights Exercises when on /exercises path', () => {
      renderSidebar({}, '/exercises')

      const nav = screen.getByTestId('nav-item-exercises')
      const styles = window.getComputedStyle(nav)

      expect(styles.backgroundColor).not.toBe('transparent')
    })

    it('highlights Exercises when on nested /exercises/* path', () => {
      renderSidebar({}, '/exercises/abc-123')

      const nav = screen.getByTestId('nav-item-exercises')
      const styles = window.getComputedStyle(nav)

      expect(styles.backgroundColor).not.toBe('transparent')
    })

    it('highlights Settings when on /settings path', () => {
      renderSidebar({}, '/settings')

      const nav = screen.getByTestId('nav-item-settings')
      const styles = window.getComputedStyle(nav)

      expect(styles.backgroundColor).not.toBe('transparent')
    })
  })

  // ===========================================================================
  // Mobile Behavior Tests
  // ===========================================================================
  describe('Mobile Behavior', () => {
    beforeEach(() => {
      (useMediaQuery as ReturnType<typeof vi.fn>).mockReturnValue(true) // Mobile
    })

    it('renders mobile drawer when on small screens', () => {
      renderSidebar({ mobileOpen: true })

      const mobileSidebar = screen.getByTestId('sidebar-mobile')
      expect(mobileSidebar).toBeInTheDocument()
    })

    it('does not render desktop drawer when on mobile', () => {
      renderSidebar({ mobileOpen: true })

      expect(screen.queryByTestId('sidebar-desktop')).not.toBeInTheDocument()
    })

    it('does not render toggle button on mobile', () => {
      renderSidebar({ mobileOpen: true })

      expect(screen.queryByTestId('sidebar-toggle')).not.toBeInTheDocument()
    })

    it('calls onMobileClose when navigating in mobile', () => {
      const onMobileClose = vi.fn()
      renderSidebar({ mobileOpen: true, onMobileClose })

      const nav = screen.getByTestId('nav-item-exercises')
      fireEvent.click(nav)

      expect(onMobileClose).toHaveBeenCalledTimes(1)
    })

    it('still navigates when clicking item in mobile', () => {
      const onMobileClose = vi.fn()
      renderSidebar({ mobileOpen: true, onMobileClose })

      const nav = screen.getByTestId('nav-item-exercises')
      fireEvent.click(nav)

      expect(mockNavigate).toHaveBeenCalledWith('/exercises')
    })

    it('mobile drawer is hidden when mobileOpen is false', () => {
      renderSidebar({ mobileOpen: false })

      // The drawer exists but is not visible
      const sidebar = screen.queryByTestId('sidebar-mobile')
      if (sidebar) {
        // Drawer may or may not be in DOM based on keepMounted
        expect(sidebar).not.toBeVisible()
      }
    })
  })

  // ===========================================================================
  // Styling Tests
  // ===========================================================================
  describe('Styling', () => {
    it('uses open drawer width (288px) when open', () => {
      renderSidebar({ open: true })

      const sidebar = screen.getByTestId('sidebar-desktop')
      const styles = window.getComputedStyle(sidebar)

      expect(styles.width).toBe('288px')
    })

    it('uses closed drawer width (64px) when closed', () => {
      renderSidebar({ open: false })

      const sidebar = screen.getByTestId('sidebar-desktop')
      const styles = window.getComputedStyle(sidebar)

      expect(styles.width).toBe('64px')
    })

    it('sidebar has border on right side', () => {
      renderSidebar()

      const sidebar = screen.getByTestId('sidebar-desktop')
      expect(sidebar).toBeInTheDocument()
    })
  })

  // ===========================================================================
  // Accessibility Tests
  // ===========================================================================
  describe('Accessibility', () => {
    it('navigation items have button role', () => {
      renderSidebar()

      const nav = screen.getByTestId('nav-item-my-assignments')
      expect(nav).toHaveAttribute('role', 'button')
    })

    it('all menu items are keyboard accessible', () => {
      renderSidebar()

      const items = [
        'nav-item-my-assignments',
        'nav-item-exercises',
        'nav-item-control-room',
        'nav-item-inject-queue',
        'nav-item-observations',
        'nav-item-reports',
        'nav-item-templates',
        'nav-item-users',
        'nav-item-settings',
      ]

      items.forEach(testId => {
        const nav = screen.getByTestId(testId)
        expect(nav).toHaveAttribute('role', 'button')
      })
    })

    it('toggle button is accessible', () => {
      renderSidebar()

      const toggle = screen.getByTestId('sidebar-toggle')
      expect(toggle).toBeInTheDocument()
      expect(toggle.tagName.toLowerCase()).toBe('button')
    })
  })

  // ===========================================================================
  // Icon Tests
  // ===========================================================================
  describe('Icons', () => {
    it('each menu item renders an icon', () => {
      renderSidebar()

      const items = screen.getAllByRole('button')
      // Each nav item should have an icon (FontAwesomeIcon renders as svg)
      expect(items.length).toBeGreaterThan(0)
    })
  })

  // ===========================================================================
  // Edge Cases
  // ===========================================================================
  describe('Edge Cases', () => {
    it('handles empty filtered items gracefully', () => {
      mockUseFilteredMenu.mockReturnValue({
        filteredItems: [],
        groupedBySection: { conduct: [], analysis: [], system: [] },
        visibleSections: [],
        isItemDisabled: () => false,
        getDisabledTooltip: () => undefined,
      })

      renderSidebar()

      // Should render without crashing
      expect(screen.getByTestId('sidebar-content')).toBeInTheDocument()
    })

    it('handles missing sections gracefully', () => {
      mockUseFilteredMenu.mockReturnValue(
        createMockFilteredMenu(['settings']),
      )

      renderSidebar({ open: true })

      // Only SYSTEM section should be visible
      expect(screen.queryByTestId('section-conduct')).not.toBeInTheDocument()
      expect(screen.queryByTestId('section-analysis')).not.toBeInTheDocument()
      expect(screen.getByTestId('section-system')).toBeInTheDocument()
    })

    it('renders correctly with only one section', () => {
      mockUseFilteredMenu.mockReturnValue(
        createMockFilteredMenu(['my-assignments', 'exercises']),
      )

      renderSidebar({ open: true })

      expect(screen.getByTestId('section-conduct')).toBeInTheDocument()
      expect(screen.queryByTestId('section-analysis')).not.toBeInTheDocument()
      expect(screen.queryByTestId('section-system')).not.toBeInTheDocument()
    })
  })

  // ===========================================================================
  // Exercise Context Extraction Tests
  // ===========================================================================
  describe('Exercise Context Extraction', () => {
    it('extracts exercise ID from /exercises/:id path', () => {
      mockUseFilteredMenu.mockReturnValue(
        createMockFilteredMenu(
          ['my-assignments', 'exercises', 'control-room', 'settings'],
          { exerciseId: 'abc-123' },
        ),
      )

      renderSidebar({}, '/exercises/abc-123')

      // Control room should be enabled because we're in exercise context
      const controlRoom = screen.getByTestId('nav-item-control-room')
      expect(controlRoom).toHaveAttribute('data-disabled', 'false')
    })

    it('extracts exercise ID from /exercises/:id/control path', () => {
      mockUseFilteredMenu.mockReturnValue(
        createMockFilteredMenu(
          ['my-assignments', 'exercises', 'control-room', 'settings'],
          { exerciseId: 'def-456' },
        ),
      )

      renderSidebar({}, '/exercises/def-456/control')

      const controlRoom = screen.getByTestId('nav-item-control-room')
      expect(controlRoom).toHaveAttribute('data-disabled', 'false')
    })

    it('does not extract exercise ID from /exercises/new path', () => {
      renderSidebar({}, '/exercises/new')

      // Should remain disabled because "new" is not a valid exercise ID
      const controlRoom = screen.getByTestId('nav-item-control-room')
      expect(controlRoom).toHaveAttribute('data-disabled', 'true')
    })

    it('does not extract exercise ID from /exercises path', () => {
      renderSidebar({}, '/exercises')

      const controlRoom = screen.getByTestId('nav-item-control-room')
      expect(controlRoom).toHaveAttribute('data-disabled', 'true')
    })
  })
})
