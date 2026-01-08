import type { ReactElement, ReactNode } from 'react'
import { render, type RenderOptions } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import { ThemeProvider } from '@mui/material/styles'
import { cobraTheme } from '../theme/cobraTheme'
import { FeatureFlagsProvider } from '../admin'

/**
 * Custom render function that wraps components with necessary providers
 */
interface WrapperProps {
  children: ReactNode;
}

const AllProviders = ({ children }: WrapperProps) => {
  return (
    <ThemeProvider theme={cobraTheme}>
      <FeatureFlagsProvider>
        <BrowserRouter>{children}</BrowserRouter>
      </FeatureFlagsProvider>
    </ThemeProvider>
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
