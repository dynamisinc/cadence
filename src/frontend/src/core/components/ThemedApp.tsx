/**
 * ThemedApp Component
 *
 * Wrapper that applies dynamic theme based on user preferences.
 * Uses UserPreferencesContext to get the resolved theme mode.
 *
 * @module core/components
 */

import { useMemo } from 'react'
import type { ReactNode } from 'react'
import { ThemeProvider } from '@mui/material/styles'
import CssBaseline from '@mui/material/CssBaseline'
import { useUserPreferences } from '@/features/settings'
import { createCobraTheme, cobraTheme } from '@/theme/cobraTheme'

interface ThemedAppProps {
  children: ReactNode
}

/**
 * Wrapper component that provides dynamic theme based on user preferences
 */
export function ThemedApp({ children }: ThemedAppProps) {
  const { resolvedTheme, preferences } = useUserPreferences()

  // Create theme based on resolved preference
  // Memoize to prevent unnecessary re-renders
  const theme = useMemo(() => {
    // If preferences haven't loaded yet, use the default light theme
    if (!preferences) {
      return cobraTheme
    }
    return createCobraTheme(resolvedTheme)
  }, [resolvedTheme, preferences])

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      {children}
    </ThemeProvider>
  )
}

export default ThemedApp
