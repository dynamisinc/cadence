import type { ReactElement, ReactNode } from 'react'
import { render, type RenderOptions } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import { ThemeProvider } from '@mui/material/styles'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cobraTheme } from '../theme/cobraTheme'
import { FeatureFlagsProvider } from '../admin'
import { ConnectivityProvider, OfflineSyncProvider } from '../core/contexts'

/**
 * Custom render function that wraps components with necessary providers
 */
interface WrapperProps {
  children: ReactNode;
}

// Create a fresh QueryClient for each test to avoid state leaking
const createTestQueryClient = () => new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
    },
  },
})

const AllProviders = ({ children }: WrapperProps) => {
  const testQueryClient = createTestQueryClient()

  return (
    <QueryClientProvider client={testQueryClient}>
      <ThemeProvider theme={cobraTheme}>
        <ConnectivityProvider>
          <OfflineSyncProvider>
            <FeatureFlagsProvider>
              <BrowserRouter>{children}</BrowserRouter>
            </FeatureFlagsProvider>
          </OfflineSyncProvider>
        </ConnectivityProvider>
      </ThemeProvider>
    </QueryClientProvider>
  )
}

const customRender = (
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>,
) => render(ui, { wrapper: AllProviders, ...options })

// Re-export everything from testing-library
export * from '@testing-library/react'

// Override render with custom render
export { customRender as render }

/**
 * Mock data factories for tests
 */
export const mockNote = (overrides = {}) => ({
  id: 'test-note-1',
  title: 'Test Note',
  content: 'Test content',
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  ...overrides,
})

export const mockNotes = (count = 3) =>
  Array.from({ length: count }, (_, i) =>
    mockNote({
      id: `note-${i + 1}`,
      title: `Note ${i + 1}`,
      content: `Content ${i + 1}`,
    }),
  )
