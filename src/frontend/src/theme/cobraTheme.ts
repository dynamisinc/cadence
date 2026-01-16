import { alpha, createTheme } from '@mui/material/styles'

/**
 * COBRA C5 Design System - Material-UI 7 Theme
 *
 * Standardized theme configuration for Cadence applications
 * Based on cobra-styling-package patterns
 *
 * Key Design Principles:
 * - Silver/Cobalt Blue color scheme
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
    grid: {
      light: string;
      main: string;
    };
    linkButton: {
      contrastText: string;
      dark: string;
      light: string;
      main?: string;
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
    grid: {
      light: string;
      main: string;
    };
    linkButton?: {
      contrastText: string;
      dark: string;
      light: string;
      main?: string;
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
      main: '#0020c2',
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
      main: '#0020c2', // cobalt blue
    },
    linkButton: {
      contrastText: '#ffffff',
      dark: '#00008b',
      light: '#DBE9FA',
      main: '#0020c2',
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
    MuiIconButton: {
      styleOverrides: {
        root: {
          color: '#1a1a1a',
        },
      },
    },
    MuiTextField: {
      defaultProps: {
        size: 'small',
      },
    },
    MuiAutocomplete: {
      defaultProps: {
        size: 'small',
      },
    },
    MuiSelect: {
      defaultProps: {
        size: 'small',
      },
    },
    MuiInputLabel: {
      defaultProps: {
        size: 'small',
      },
    },
    MuiButtonGroup: {
      defaultProps: {
        size: 'small',
      },
    },
    MuiToggleButtonGroup: {
      defaultProps: {
        size: 'small',
      },
    },
    MuiButton: {
      defaultProps: {
        size: 'small',
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

export default cobraTheme
