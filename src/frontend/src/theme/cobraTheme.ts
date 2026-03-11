import { alpha, createTheme } from '@mui/material/styles'

/**
 * COBRA C5 Design System - Material-UI 7 Theme
 *
 * Standardized theme configuration for Cadence applications
 * Based on cobra-styling-package patterns
 *
 * Key Design Principles:
 * - Silver/Navy Blue color scheme
 * - Small form controls by default
 * - 54px header height
 * - Consistent spacing and padding via CobraStyles
 * - Touch-friendly button sizes (48px minimum)
 */

// Extend MUI Theme interface with custom properties
declare module '@mui/material/styles' {
  interface Theme {
    cssStyling: {
      headerHeight: number;
      drawerClosedWidth: number;
      drawerOpenWidth: number;
    };
  }
  interface ThemeOptions {
    cssStyling?: {
      headerHeight: number;
      drawerClosedWidth: number;
      drawerOpenWidth: number;
    };
  }
  interface StatusColor {
    bg: string;
    text: string;
  }
  interface RatingColor {
    bg: string;
    text: string;
    border: string;
    main: string;
  }
  interface Palette {
    breadcrumb: {
      background: string;
    };
    border: {
      main: string;
    };
    buttonDelete: {
      contrastText: string;
      dark: string;
      light: string;
      main: string;
    };
    buttonPrimary: {
      contrastText: string;
      dark: string;
      light: string;
      main: string;
    };
    clockStatus: {
      running: string;
      paused: string;
      stopped: string;
    };
    grid: {
      light: string;
      main: string;
    };
    injectStatus: {
      draft: StatusColor;
      submitted: StatusColor;
      approved: StatusColor;
      synchronized: StatusColor;
      released: StatusColor;
      complete: StatusColor;
      deferred: StatusColor;
      obsolete: StatusColor;
    };
    linkButton: {
      contrastText: string;
      dark: string;
      light: string;
      main?: string;
    };
    neutral: {
      50: string;
      200: string;
      300: string;
      400: string;
      500: string;
      600: string;
    };
    notifications: {
      error: string;
      errorText: string;
      info: string;
      success: string;
      successText: string;
      warning: string;
      warningText: string;
    };
    rating: {
      performed: RatingColor;
      satisfactory: RatingColor;
      marginal: RatingColor;
      unsatisfactory: RatingColor;
      unrated: RatingColor;
    };
    roleColor: {
      exerciseDirector: string;
      controller: string;
      evaluator: string;
      observer: string;
    };
    semantic: {
      success: string;
      error: string;
      warning: string;
      warningAmber: string;
      info: string;
      purple: string;
      cyan: string;
      gold: string;
      excel: string;
    };
    statusChart: {
      grey: string;
      red: string;
      yellow: string;
      green: string;
      black: string;
    };
  }

  interface PaletteOptions {
    breadcrumb?: {
      background: string;
    };
    border?: {
      main: string;
    };
    buttonDelete?: {
      contrastText: string;
      dark: string;
      light: string;
      main?: string;
    };
    buttonPrimary?: {
      contrastText: string;
      dark: string;
      light: string;
      main?: string;
    };
    clockStatus?: {
      running: string;
      paused: string;
      stopped: string;
    };
    grid: {
      light: string;
      main: string;
    };
    injectStatus?: {
      draft: { bg: string; text: string };
      submitted: { bg: string; text: string };
      approved: { bg: string; text: string };
      synchronized: { bg: string; text: string };
      released: { bg: string; text: string };
      complete: { bg: string; text: string };
      deferred: { bg: string; text: string };
      obsolete: { bg: string; text: string };
    };
    linkButton?: {
      contrastText: string;
      dark: string;
      light: string;
      main?: string;
    };
    neutral?: {
      50: string;
      200: string;
      300: string;
      400: string;
      500: string;
      600: string;
    };
    notifications?: {
      error: string;
      errorText: string;
      info: string;
      success: string;
      successText: string;
      warning: string;
      warningText: string;
    };
    rating?: {
      performed: { bg: string; text: string; border: string; main: string };
      satisfactory: { bg: string; text: string; border: string; main: string };
      marginal: { bg: string; text: string; border: string; main: string };
      unsatisfactory: { bg: string; text: string; border: string; main: string };
      unrated: { bg: string; text: string; border: string; main: string };
    };
    roleColor?: {
      exerciseDirector: string;
      controller: string;
      evaluator: string;
      observer: string;
    };
    semantic?: {
      success: string;
      error: string;
      warning: string;
      warningAmber: string;
      info: string;
      purple: string;
      cyan: string;
      gold: string;
      excel: string;
    };
    statusChart?: {
      grey: string;
      red: string;
      yellow: string;
      green: string;
      black: string;
    };
  }
}

// Color overrides for buttons
declare module '@mui/material/Button' {
  interface ButtonPropsColorOverrides {
    white: true;
  }
}

const primaryContrastText = '#1a1a1a'

/**
 * Cadence Breakpoints (per S04-responsive-design.md)
 *
 * | Breakpoint | Width      | Cadence Usage              |
 * |------------|------------|----------------------------|
 * | xs         | 0-599px    | Unsupported (show message) |
 * | sm         | 600-767px  | Unsupported (show message) |
 * | md         | 768-1023px | Tablet portrait            |
 * | lg         | 1024-1439px| Tablet landscape / Laptop  |
 * | xl         | 1440px+    | Desktop                    |
 */
export const cadenceBreakpoints = {
  values: {
    xs: 0,
    sm: 600,
    md: 768,    // Tablet portrait - minimum supported
    lg: 1024,   // Tablet landscape / Laptop
    xl: 1440,   // Desktop
  },
}

/**
 * Shared component size defaults applied to both light and dark themes.
 * Extracted to avoid duplication between cobraTheme and createCobraTheme.
 */
const sharedComponentSizeDefaults = {
  MuiTextField: {
    defaultProps: {
      size: 'small' as const,
    },
  },
  MuiAutocomplete: {
    defaultProps: {
      size: 'small' as const,
    },
  },
  MuiSelect: {
    defaultProps: {
      size: 'small' as const,
    },
  },
  MuiInputLabel: {
    defaultProps: {
      size: 'small' as const,
    },
  },
  MuiButtonGroup: {
    defaultProps: {
      size: 'small' as const,
    },
  },
  MuiToggleButtonGroup: {
    defaultProps: {
      size: 'small' as const,
    },
  },
  MuiButton: {
    defaultProps: {
      size: 'small' as const,
    },
  },
  MuiTableCell: {
    styleOverrides: {
      head: {
        fontWeight: 600,
      },
    },
  },
}

export const cobraTheme = createTheme({
  breakpoints: cadenceBreakpoints,
  cssStyling: {
    drawerClosedWidth: 64,
    drawerOpenWidth: 288,
    headerHeight: 54,
  },
  palette: {
    mode: 'light',
    primary: {
      dark: '#696969', // dim gray
      main: '#c0c0c0', // silver
      contrastText: primaryContrastText,
      light: '#dadbdd', // silver white
    },
    secondary: {
      main: '#b22222', // firebrick
    },
    background: {
      default: '#f8f8f8',
      paper: '#ffffff',
    },
    border: {
      main: alpha(primaryContrastText, 0.23),
    },
    breadcrumb: {
      background: '#F1F1F1',
    },
    grid: {
      light: '#EAF2FB',
      main: '#DBE9FA',
    },
    text: {
      primary: '#1a1a1a',
      secondary: '#848482',
    },
    error: {
      main: '#e42217', // lava red
    },
    info: {
      main: '#1e3a5f',
      light: '#0000ff',
      dark: '#00008b',
    },
    divider: '#848482',
    success: {
      main: '#08682a',
    },
    buttonDelete: {
      contrastText: '#ffffff',
      dark: '#c11b17', // chilli pepper
      light: '#ff0000',
      main: '#e42217', // lava red
    },
    buttonPrimary: {
      contrastText: '#ffffff',
      dark: '#00008b', // darkblue
      light: '#0000ff',
      main: '#1e3a5f', // navy blue
    },
    linkButton: {
      contrastText: '#ffffff',
      dark: '#00008b',
      light: '#DBE9FA',
      main: '#1e3a5f',
    },
    notifications: {
      error: '#EFB6B6',
      errorText: '#b22222',
      info: '#DBE9FA',
      success: '#AEFBB8',
      successText: '#008000',
      warning: '#F9F9BE',
      warningText: '#6F4E37',
    },
    clockStatus: {
      running: '#4caf50',
      paused: '#ff9800',
      stopped: '#757575',
    },
    injectStatus: {
      draft: { bg: '#E0E0E0', text: '#616161' },
      submitted: { bg: '#FFE0B2', text: '#F57C00' },
      approved: { bg: '#C8E6C9', text: '#388E3C' },
      synchronized: { bg: '#BBDEFB', text: '#1976D2' },
      released: { bg: '#E1BEE7', text: '#7B1FA2' },
      complete: { bg: '#A5D6A7', text: '#1B5E20' },
      deferred: { bg: '#FFCC80', text: '#E65100' },
      obsolete: { bg: '#F5F5F5', text: '#9E9E9E' },
    },
    neutral: {
      50: '#f5f5f5',
      200: '#eeeeee',
      300: '#cccccc',
      400: '#999999',
      500: '#757575',
      600: '#666666',
    },
    rating: {
      performed: { bg: '#e8f5e9', text: '#2e7d32', border: '#4caf50', main: '#4CAF50' },
      satisfactory: { bg: '#e3f2fd', text: '#1565c0', border: '#2196f3', main: '#2196F3' },
      marginal: { bg: '#fff3e0', text: '#e65100', border: '#ff9800', main: '#FFC107' },
      unsatisfactory: { bg: '#ffebee', text: '#c62828', border: '#f44336', main: '#F44336' },
      unrated: { bg: '#f5f5f5', text: '#757575', border: '#9e9e9e', main: '#9E9E9E' },
    },
    roleColor: {
      exerciseDirector: '#d32f2f',
      controller: '#1976d2',
      evaluator: '#2e7d32',
      observer: '#757575',
    },
    semantic: {
      success: '#4caf50',
      error: '#f44336',
      warning: '#ff9800',
      warningAmber: '#f59e0b',
      info: '#1976d2',
      purple: '#9c27b0',
      cyan: '#00bcd4',
      gold: '#FFD700',
      excel: '#217346',
    },
    statusChart: {
      grey: '#C0C0C0',
      red: '#C11B17',
      yellow: '#FFEF00',
      green: '#008000',
      black: '#000000',
    },
  },
  typography: {
    fontFamily: 'Roboto, Arial, sans-serif',
    fontSize: 14,
    button: {
      textTransform: 'none',
    },
  },
  components: {
    ...sharedComponentSizeDefaults,
    MuiIconButton: {
      styleOverrides: {
        root: {
          color: '#1a1a1a',
        },
      },
    },
    MuiTableHead: {
      styleOverrides: {
        root: {
          backgroundColor: 'rgba(0, 0, 0, 0.04)',
        },
      },
    },
    MuiListItemIcon: {
      styleOverrides: {
        root: {
          color: '#3A3B3C',
        },
      },
    },
  },
})

/**
 * Helper function to get progress bar color based on completion percentage
 */
export const getProgressColor = (percentage: number): string => {
  if (percentage === 100) return cobraTheme.palette.success.main
  if (percentage >= 67) return cobraTheme.palette.info.main
  if (percentage >= 34) return cobraTheme.palette.statusChart.yellow
  return cobraTheme.palette.error.main
}

/**
 * Helper function to get status chip color
 */
export const getStatusChipColor = (
  status: string,
): { bg: string; text: string } => {
  const statusLower = status.toLowerCase()

  switch (statusLower) {
    case 'completed':
    case 'complete':
      return {
        bg: cobraTheme.palette.notifications.success,
        text: cobraTheme.palette.text.primary,
      }
    case 'in progress':
    case 'in-progress':
      return {
        bg: cobraTheme.palette.grid.main,
        text: cobraTheme.palette.text.primary,
      }
    case 'not started':
    case 'not-started':
      return {
        bg: cobraTheme.palette.primary.light,
        text: cobraTheme.palette.text.secondary,
      }
    case 'blocked':
      return { bg: cobraTheme.palette.error.main, text: '#ffffff' }
    default:
      return {
        bg: cobraTheme.palette.primary.main,
        text: cobraTheme.palette.text.primary,
      }
  }
}

/**
 * Helper function to get exercise status chip color
 * Per S03-view-exercise-list.md:
 * - Draft: Neutral styling
 * - Active: Success/green styling
 * - Completed: Muted styling
 * - Archived: Hidden from default view (gray if shown)
 */
export const getExerciseStatusChipColor = (
  status: string,
): { bg: string; text: string } => {
  const statusLower = status.toLowerCase()

  switch (statusLower) {
    case 'active':
      return {
        bg: cobraTheme.palette.notifications.success,
        text: cobraTheme.palette.notifications.successText,
      }
    case 'paused':
      return {
        bg: cobraTheme.palette.notifications.warning,
        text: cobraTheme.palette.notifications.warningText,
      }
    case 'draft':
      return {
        bg: cobraTheme.palette.grid.main,
        text: cobraTheme.palette.text.primary,
      }
    case 'completed':
      return {
        bg: cobraTheme.palette.primary.light,
        text: cobraTheme.palette.text.secondary,
      }
    case 'archived':
      return {
        bg: cobraTheme.palette.statusChart.grey,
        text: cobraTheme.palette.text.secondary,
      }
    default:
      return {
        bg: cobraTheme.palette.primary.main,
        text: cobraTheme.palette.text.primary,
      }
  }
}

/**
 * Helper function to get exercise type display text (abbreviation)
 */
export const getExerciseTypeLabel = (type: string): string => {
  const normalized = type.toUpperCase()
  switch (normalized) {
    case 'TTX':
      return 'TTX'
    case 'FE':
      return 'FE'
    case 'FSE':
      return 'FSE'
    case 'CAX':
      return 'CAX'
    case 'HYBRID':
      return 'Hybrid'
    default:
      return type
  }
}

/**
 * Helper function to get exercise type full name
 */
export const getExerciseTypeFullName = (type: string): string => {
  const normalized = type.toUpperCase()
  switch (normalized) {
    case 'TTX':
      return 'Tabletop Exercise'
    case 'FE':
      return 'Functional Exercise'
    case 'FSE':
      return 'Full-Scale Exercise'
    case 'CAX':
      return 'Computer-Aided Exercise'
    case 'HYBRID':
      return 'Hybrid Exercise'
    default:
      return type
  }
}

/**
 * Create a theme with specified mode (light or dark)
 * Used for dynamic theme switching based on user preferences
 */
export function createCobraTheme(mode: 'light' | 'dark') {
  const isDark = mode === 'dark'

  return createTheme({
    breakpoints: cadenceBreakpoints,
    cssStyling: {
      drawerClosedWidth: 64,
      drawerOpenWidth: 288,
      headerHeight: 54,
    },
    palette: {
      mode,
      primary: {
        dark: isDark ? '#a0a0a0' : '#696969',
        main: isDark ? '#c0c0c0' : '#c0c0c0',
        contrastText: isDark ? '#ffffff' : '#1a1a1a',
        light: isDark ? '#e0e0e0' : '#dadbdd',
      },
      secondary: {
        main: '#b22222',
      },
      background: {
        default: isDark ? '#121212' : '#f8f8f8',
        paper: isDark ? '#1e1e1e' : '#ffffff',
      },
      border: {
        main: isDark ? alpha('#ffffff', 0.23) : alpha('#1a1a1a', 0.23),
      },
      breadcrumb: {
        background: isDark ? '#2a2a2a' : '#F1F1F1',
      },
      grid: {
        light: isDark ? '#1a3352' : '#EAF2FB',
        main: isDark ? '#1a4480' : '#DBE9FA',
      },
      text: {
        primary: isDark ? '#e0e0e0' : '#1a1a1a',
        secondary: isDark ? '#a0a0a0' : '#848482',
      },
      error: {
        main: '#e42217',
      },
      info: {
        main: isDark ? '#4a90d9' : '#1e3a5f',
        light: isDark ? '#6ba3e0' : '#0000ff',
        dark: isDark ? '#2a70b9' : '#00008b',
      },
      divider: isDark ? '#404040' : '#848482',
      success: {
        main: isDark ? '#2e8b57' : '#08682a',
      },
      buttonDelete: {
        contrastText: '#ffffff',
        dark: '#c11b17',
        light: '#ff0000',
        main: '#e42217',
      },
      buttonPrimary: {
        contrastText: '#ffffff',
        dark: isDark ? '#1a50a0' : '#00008b',
        light: isDark ? '#4a90d9' : '#0000ff',
        main: isDark ? '#2a70c2' : '#1e3a5f',
      },
      linkButton: {
        contrastText: '#ffffff',
        dark: isDark ? '#1a50a0' : '#00008b',
        light: isDark ? '#1a3352' : '#DBE9FA',
        main: isDark ? '#4a90d9' : '#1e3a5f',
      },
      notifications: {
        error: isDark ? '#5c2020' : '#EFB6B6',
        errorText: isDark ? '#ff6b6b' : '#b22222',
        info: isDark ? '#1a3352' : '#DBE9FA',
        success: isDark ? '#1a4030' : '#AEFBB8',
        successText: isDark ? '#50c878' : '#008000',
        warning: isDark ? '#4a4020' : '#F9F9BE',
        warningText: isDark ? '#d4a574' : '#6F4E37',
      },
      clockStatus: {
        running: isDark ? '#66bb6a' : '#4caf50',
        paused: isDark ? '#ffa726' : '#ff9800',
        stopped: isDark ? '#9e9e9e' : '#757575',
      },
      injectStatus: {
        draft: { bg: isDark ? '#424242' : '#E0E0E0', text: isDark ? '#bdbdbd' : '#616161' },
        submitted: { bg: isDark ? '#4a3000' : '#FFE0B2', text: isDark ? '#ffb74d' : '#F57C00' },
        approved: { bg: isDark ? '#1b3a1b' : '#C8E6C9', text: isDark ? '#81c784' : '#388E3C' },
        synchronized: { bg: isDark ? '#0d2744' : '#BBDEFB', text: isDark ? '#64b5f6' : '#1976D2' },
        released: { bg: isDark ? '#2a1540' : '#E1BEE7', text: isDark ? '#ce93d8' : '#7B1FA2' },
        complete: { bg: isDark ? '#1a4020' : '#A5D6A7', text: isDark ? '#a5d6a7' : '#1B5E20' },
        deferred: { bg: isDark ? '#4a2800' : '#FFCC80', text: isDark ? '#ffb74d' : '#E65100' },
        obsolete: { bg: isDark ? '#303030' : '#F5F5F5', text: isDark ? '#757575' : '#9E9E9E' },
      },
      neutral: {
        50: isDark ? '#303030' : '#f5f5f5',
        200: isDark ? '#404040' : '#eeeeee',
        300: isDark ? '#505050' : '#cccccc',
        400: isDark ? '#707070' : '#999999',
        500: isDark ? '#9e9e9e' : '#757575',
        600: isDark ? '#b0b0b0' : '#666666',
      },
      rating: {
        performed: { bg: isDark ? '#1a3a1a' : '#e8f5e9', text: isDark ? '#81c784' : '#2e7d32', border: isDark ? '#66bb6a' : '#4caf50', main: '#4CAF50' },
        satisfactory: { bg: isDark ? '#0d2744' : '#e3f2fd', text: isDark ? '#64b5f6' : '#1565c0', border: isDark ? '#42a5f5' : '#2196f3', main: '#2196F3' },
        marginal: { bg: isDark ? '#3e2800' : '#fff3e0', text: isDark ? '#ffb74d' : '#e65100', border: isDark ? '#ffa726' : '#ff9800', main: '#FFC107' },
        unsatisfactory: { bg: isDark ? '#4a1a1a' : '#ffebee', text: isDark ? '#ef9a9a' : '#c62828', border: isDark ? '#ef5350' : '#f44336', main: '#F44336' },
        unrated: { bg: isDark ? '#303030' : '#f5f5f5', text: isDark ? '#9e9e9e' : '#757575', border: isDark ? '#616161' : '#9e9e9e', main: '#9E9E9E' },
      },
      roleColor: {
        exerciseDirector: isDark ? '#ef5350' : '#d32f2f',
        controller: isDark ? '#42a5f5' : '#1976d2',
        evaluator: isDark ? '#66bb6a' : '#2e7d32',
        observer: isDark ? '#9e9e9e' : '#757575',
      },
      semantic: {
        success: isDark ? '#66bb6a' : '#4caf50',
        error: isDark ? '#ef5350' : '#f44336',
        warning: isDark ? '#ffa726' : '#ff9800',
        warningAmber: isDark ? '#fbbf24' : '#f59e0b',
        info: isDark ? '#42a5f5' : '#1976d2',
        purple: isDark ? '#ce93d8' : '#9c27b0',
        cyan: isDark ? '#4dd0e1' : '#00bcd4',
        gold: '#FFD700',
        excel: '#217346',
      },
      statusChart: {
        grey: '#C0C0C0',
        red: '#C11B17',
        yellow: '#FFEF00',
        green: '#008000',
        black: isDark ? '#ffffff' : '#000000',
      },
    },
    typography: {
      fontFamily: 'Roboto, Arial, sans-serif',
      fontSize: 14,
      button: {
        textTransform: 'none',
      },
    },
    components: {
      ...sharedComponentSizeDefaults,
      MuiIconButton: {
        styleOverrides: {
          root: {
            color: isDark ? '#e0e0e0' : '#1a1a1a',
          },
        },
      },
      MuiTableHead: {
        styleOverrides: {
          root: {
            backgroundColor: isDark ? 'rgba(255, 255, 255, 0.04)' : 'rgba(0, 0, 0, 0.04)',
          },
        },
      },
      MuiListItemIcon: {
        styleOverrides: {
          root: {
            color: isDark ? '#b0b0b0' : '#3A3B3C',
          },
        },
      },
    },
  })
}

export default cobraTheme
