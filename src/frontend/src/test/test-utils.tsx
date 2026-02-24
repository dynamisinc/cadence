import type { ReactElement, ReactNode } from 'react'
import { render, type RenderOptions } from '@testing-library/react'
import { ThemeProvider } from '@mui/material/styles'
import { cobraTheme } from '../theme/cobraTheme'
import { BreadcrumbProvider } from '../core/contexts/BreadcrumbContext'

interface WrapperProps {
  children: ReactNode
}

const AllTheProviders = ({ children }: WrapperProps) => {
  return (
    <ThemeProvider theme={cobraTheme}>
      <BreadcrumbProvider>
        {children}
      </BreadcrumbProvider>
    </ThemeProvider>
  )
}

const customRender = (
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>,
) => render(ui, { wrapper: AllTheProviders, ...options })

// re-export everything
export * from '@testing-library/react'

// override render method
export { customRender as render }
