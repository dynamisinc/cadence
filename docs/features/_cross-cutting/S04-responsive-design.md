# Story: Responsive Design

> **Status**: 📋 Ready for Development  
> **Priority**: P1 (Important)  
> **Epic**: E2 - Infrastructure  
> **Sprint Points**: 8 (ongoing effort)

## User Story

**As a** user accessing Cadence from different devices,  
**I want** the interface to adapt appropriately to my screen size,  
**So that** I can effectively use Cadence on desktop workstations and tablets during exercise conduct.

## Context

Cadence targets desktop and tablet form factors. Mobile phones are explicitly out of scope for MVP due to the data-intensive nature of MSEL management. However, Controllers and Evaluators often use tablets during field exercises, making responsive design essential.

### Target Devices

| Device Type | Screen Width | Priority | Use Case |
|-------------|--------------|----------|----------|
| Desktop | 1440px+ | Primary | MSEL authoring, administration |
| Laptop | 1024-1439px | Primary | Most common usage |
| Tablet Landscape | 1024-1199px | Secondary | Field exercise conduct |
| Tablet Portrait | 768-1023px | Secondary | Quick reference during conduct |
| Mobile | < 768px | Out of scope | Not supported in MVP |

## Acceptance Criteria

### Breakpoint Behavior

- [ ] **Given** screen width ≥ 1440px, **when** viewing any page, **then** the full desktop layout displays with sidebar navigation

- [ ] **Given** screen width 1024-1439px, **when** viewing any page, **then** the layout adjusts with condensed sidebar or top navigation

- [ ] **Given** screen width 768-1023px, **when** viewing any page, **then** navigation collapses to hamburger menu and content fills width

- [ ] **Given** screen width < 768px, **when** attempting to access, **then** user sees message "Cadence is optimized for tablets and desktops. Please use a larger screen."

### Core Components

- [ ] **Given** any list view (exercises, injects), **when** on tablet, **then** the list remains functional with touch-friendly row heights (minimum 48px)

- [ ] **Given** any form, **when** on tablet, **then** form fields are appropriately sized for touch input (minimum 44px touch targets)

- [ ] **Given** any modal, **when** on tablet, **then** modal width adjusts to not exceed viewport and remains dismissible

- [ ] **Given** data tables, **when** on tablet, **then** horizontal scrolling is enabled for wide tables with sticky first column

### Navigation

- [ ] **Given** desktop width, **when** viewing navigation, **then** sidebar displays with full labels and icons

- [ ] **Given** tablet width, **when** viewing navigation, **then** sidebar collapses to icons only or hamburger menu

- [ ] **Given** hamburger menu is used, **when** I tap it, **then** a full-screen overlay menu appears with all navigation options

### Typography and Spacing

- [ ] **Given** any device, **when** viewing text, **then** body text is minimum 16px for readability

- [ ] **Given** tablet device, **when** viewing content, **then** spacing increases slightly for touch accuracy

- [ ] **Given** any device, **when** viewing content, **then** line lengths do not exceed 75 characters for readability

### Touch Interactions

- [ ] **Given** tablet device, **when** interacting with buttons, **then** touch targets are minimum 44×44px

- [ ] **Given** tablet device, **when** interacting with lists, **then** swipe gestures are not required (buttons visible)

- [ ] **Given** tablet device, **when** scrolling, **then** momentum scrolling is enabled

### Print Support

- [ ] **Given** any page, **when** printing, **then** navigation and non-essential UI elements are hidden

- [ ] **Given** inject list, **when** printing, **then** all visible injects print in a readable table format

## Out of Scope

- Mobile phone support (< 768px)
- Native mobile apps
- Offline-first PWA features (separate story)
- Dark mode (future consideration)
- High contrast mode (future accessibility story)

## Dependencies

- Component library selection (Material UI configured)
- CSS architecture decisions (CSS-in-JS vs CSS Modules)
- Design system/tokens definition

## Open Questions

- [ ] Should we support landscape-only on tablets, or adapt to portrait?
- [ ] Should touch detection change any UI elements (larger buttons)?
- [ ] Should there be a "Request Desktop Site" option on tablets?

## Domain Terms

| Term | Definition |
|------|------------|
| Breakpoint | Screen width at which layout changes |
| Touch target | Minimum tappable area for touch devices |
| Viewport | Visible area of the browser window |
| Responsive | Layout that adapts to screen size |

## UI/UX Notes

### Breakpoint System

Using standard Material UI breakpoints with Cadence-specific adjustments:

| Breakpoint | Width | Cadence Usage |
|------------|-------|---------------|
| `xs` | 0-599px | Unsupported (show message) |
| `sm` | 600-767px | Unsupported (show message) |
| `md` | 768-1023px | Tablet portrait |
| `lg` | 1024-1439px | Tablet landscape / Laptop |
| `xl` | 1440px+ | Desktop |

### Layout Patterns

**Desktop (xl)**
```
┌─────────────────────────────────────────────────────────────┐
│ Header with breadcrumbs                              [User] │
├──────────┬──────────────────────────────────────────────────┤
│          │                                                  │
│ Sidebar  │                Main Content Area                 │
│ Nav      │                                                  │
│          │                                                  │
│          │                                                  │
└──────────┴──────────────────────────────────────────────────┘
```

**Tablet Landscape (lg)**
```
┌─────────────────────────────────────────────────────────────┐
│ ☰  Cadence                                           [User] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│                     Main Content Area                       │
│                     (Full Width)                            │
│                                                             │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**Tablet Portrait (md)**
```
┌─────────────────────────────────────┐
│ ☰  Cadence                  [User]  │
├─────────────────────────────────────┤
│                                     │
│       Main Content Area             │
│       (Stacked Layout)              │
│                                     │
│                                     │
│                                     │
└─────────────────────────────────────┘
```

### Component Adaptations

| Component | Desktop | Tablet |
|-----------|---------|--------|
| Data Table | Horizontal scroll | Horizontal scroll + sticky column |
| Form | 2-column layout | Single column |
| Cards | Grid (3-4 columns) | Grid (2 columns) |
| Modals | Centered (600px max) | Near full-width |
| Buttons | Standard size | Larger touch targets |

### Mobile Block Screen

```
┌─────────────────────────────────────┐
│                                     │
│          📱 → 💻                    │
│                                     │
│    Cadence works best on            │
│    tablets and desktops.            │
│                                     │
│    Please use a device with         │
│    a screen width of at least       │
│    768 pixels.                       │
│                                     │
│    Current width: 375px             │
│                                     │
└─────────────────────────────────────┘
```

## Technical Notes

### CSS Architecture

Using Material UI's responsive utilities:

```typescript
// Theme breakpoints
const theme = createTheme({
  breakpoints: {
    values: {
      xs: 0,
      sm: 600,
      md: 768,    // Tablet portrait
      lg: 1024,   // Tablet landscape / Laptop
      xl: 1440,   // Desktop
    },
  },
});

// Component usage
<Box
  sx={{
    display: { xs: 'none', md: 'block' },  // Hidden on mobile
    padding: { md: 2, lg: 3 },              // Responsive spacing
  }}
>
  Content
</Box>
```

### Container Widths

```typescript
// Max content widths
const contentMaxWidths = {
  form: '800px',      // Forms shouldn't be too wide
  table: '100%',      // Tables fill available space
  text: '75ch',       // Reading content limited
  modal: '600px',     // Modals centered
};
```

### Touch Detection

```typescript
// Detect touch device for UI adjustments
const isTouchDevice = () => {
  return 'ontouchstart' in window || navigator.maxTouchPoints > 0;
};

// Provide to components via context
<TouchContext.Provider value={isTouchDevice()}>
  <App />
</TouchContext.Provider>
```

### Mobile Block Component

```typescript
function MobileBlocker({ children }: { children: React.ReactNode }) {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [width, setWidth] = useState(window.innerWidth);
  
  useEffect(() => {
    const handler = () => setWidth(window.innerWidth);
    window.addEventListener('resize', handler);
    return () => window.removeEventListener('resize', handler);
  }, []);
  
  if (isMobile) {
    return <MobileBlockScreen currentWidth={width} />;
  }
  
  return children;
}
```

### Print Styles

```css
@media print {
  /* Hide non-essential elements */
  .sidebar,
  .header-actions,
  .pagination,
  button:not(.print-include) {
    display: none !important;
  }
  
  /* Reset colors for printing */
  * {
    color: black !important;
    background: white !important;
  }
  
  /* Ensure tables don't break awkwardly */
  table {
    page-break-inside: auto;
  }
  tr {
    page-break-inside: avoid;
  }
}
```

---

## INVEST Checklist

- [x] **I**ndependent - Can be developed alongside other features
- [x] **N**egotiable - Specific breakpoints and behaviors adjustable
- [x] **V**aluable - Essential for tablet usage in field
- [x] **E**stimable - Ongoing effort, ~8 points initial setup
- [x] **S**mall - Foundation only; components adapt individually
- [x] **T**estable - Each breakpoint behavior verifiable

## Test Scenarios

### Visual Regression Tests
- Screenshot comparison at each breakpoint
- Component rendering at boundary widths

### Functional Tests
- Navigation works at each breakpoint
- Forms submittable on tablet
- Tables scrollable and readable

### Manual Tests
- Real device testing on iPad
- Touch interaction verification
- Print preview verification

---

*Related Stories*: All UI stories depend on responsive foundation

*Last updated: 2025-01-08*
