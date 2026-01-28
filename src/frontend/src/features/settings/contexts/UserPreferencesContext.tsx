/**
 * UserPreferencesContext - User display and behavior preferences
 *
 * Provides user preferences state across the app.
 * Loads preferences on auth and applies them immediately.
 *
 * @module features/settings
 */
import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  useMemo,
} from 'react'
import type { FC, ReactNode } from 'react'
import { useAuth } from '@/contexts/AuthContext'
import { preferencesService } from '../services/preferencesService'
import type {
  UserPreferencesDto,
  UpdateUserPreferencesRequest,
  ThemePreference,
  DisplayDensity,
  TimeFormat,
  ResolvedTheme,
} from '../types'

interface UserPreferencesContextType {
  /** Current user preferences (null if not loaded) */
  preferences: UserPreferencesDto | null
  /** Whether preferences are loading */
  isLoading: boolean
  /** Error message if loading/updating failed */
  error: string | null
  /** Resolved theme mode (light or dark) - accounts for 'System' */
  resolvedTheme: ResolvedTheme
  /** Update preferences (partial update supported) */
  updatePreferences: (request: UpdateUserPreferencesRequest) => Promise<void>
  /** Reset preferences to defaults */
  resetPreferences: () => Promise<void>
  /** Helper: update theme only */
  setTheme: (theme: ThemePreference) => Promise<void>
  /** Helper: update density only */
  setDisplayDensity: (density: DisplayDensity) => Promise<void>
  /** Helper: update time format only */
  setTimeFormat: (format: TimeFormat) => Promise<void>
}

const UserPreferencesContext = createContext<UserPreferencesContextType | undefined>(
  undefined,
)

interface UserPreferencesProviderProps {
  children: ReactNode
}

/**
 * Default preferences used before loading from server
 */
const defaultPreferences: UserPreferencesDto = {
  theme: 'System',
  displayDensity: 'Comfortable',
  timeFormat: 'TwentyFourHour',
  updatedAt: new Date().toISOString(),
}

/**
 * User preferences context provider
 * Loads preferences when user is authenticated
 */
export const UserPreferencesProvider: FC<UserPreferencesProviderProps> = ({
  children,
}) => {
  const { isAuthenticated, isLoading: authLoading } = useAuth()
  const [preferences, setPreferences] = useState<UserPreferencesDto | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [systemTheme, setSystemTheme] = useState<ResolvedTheme>(
    window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light',
  )

  // Listen for system theme changes
  useEffect(() => {
    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
    const handleChange = (e: MediaQueryListEvent) => {
      setSystemTheme(e.matches ? 'dark' : 'light')
    }

    mediaQuery.addEventListener('change', handleChange)
    return () => mediaQuery.removeEventListener('change', handleChange)
  }, [])

  // Load preferences when authenticated
  useEffect(() => {
    if (authLoading) return

    if (!isAuthenticated) {
      // Clear preferences on logout
      setPreferences(null)
      return
    }

    const loadPreferences = async () => {
      setIsLoading(true)
      setError(null)

      try {
        const prefs = await preferencesService.getPreferences()
        setPreferences(prefs)
      } catch (err) {
        console.error('Failed to load user preferences:', err)
        setError('Failed to load preferences')
        // Use defaults if loading fails
        setPreferences(defaultPreferences)
      } finally {
        setIsLoading(false)
      }
    }

    loadPreferences()
  }, [isAuthenticated, authLoading])

  const updatePreferences = useCallback(
    async (request: UpdateUserPreferencesRequest) => {
      if (!isAuthenticated) return

      // Optimistic update
      const previousPreferences = preferences
      setPreferences(prev =>
        prev
          ? {
            ...prev,
            ...request,
            updatedAt: new Date().toISOString(),
          }
          : prev,
      )

      try {
        const updated = await preferencesService.updatePreferences(request)
        setPreferences(updated)
        setError(null)
      } catch (err) {
        console.error('Failed to update preferences:', err)
        // Rollback on error
        setPreferences(previousPreferences)
        setError('Failed to update preferences')
        throw err
      }
    },
    [isAuthenticated, preferences],
  )

  const resetPreferences = useCallback(async () => {
    if (!isAuthenticated) return

    try {
      const reset = await preferencesService.resetPreferences()
      setPreferences(reset)
      setError(null)
    } catch (err) {
      console.error('Failed to reset preferences:', err)
      setError('Failed to reset preferences')
      throw err
    }
  }, [isAuthenticated])

  // Helper functions for common updates
  const setTheme = useCallback(
    (theme: ThemePreference) => updatePreferences({ theme }),
    [updatePreferences],
  )

  const setDisplayDensity = useCallback(
    (displayDensity: DisplayDensity) => updatePreferences({ displayDensity }),
    [updatePreferences],
  )

  const setTimeFormat = useCallback(
    (timeFormat: TimeFormat) => updatePreferences({ timeFormat }),
    [updatePreferences],
  )

  // Resolve theme based on preference and system setting
  const resolvedTheme = useMemo((): ResolvedTheme => {
    const theme = preferences?.theme || 'System'
    if (theme === 'System') {
      return systemTheme
    }
    return theme.toLowerCase() as ResolvedTheme
  }, [preferences?.theme, systemTheme])

  const value: UserPreferencesContextType = {
    preferences,
    isLoading,
    error,
    resolvedTheme,
    updatePreferences,
    resetPreferences,
    setTheme,
    setDisplayDensity,
    setTimeFormat,
  }

  return (
    <UserPreferencesContext.Provider value={value}>
      {children}
    </UserPreferencesContext.Provider>
  )
}

/**
 * Hook to access user preferences context
 * Must be used within UserPreferencesProvider
 */
export const useUserPreferences = (): UserPreferencesContextType => {
  const context = useContext(UserPreferencesContext)
  if (context === undefined) {
    throw new Error('useUserPreferences must be used within a UserPreferencesProvider')
  }
  return context
}
