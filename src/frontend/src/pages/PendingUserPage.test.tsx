/**
 * PendingUserPage Component Tests
 *
 * @module pages
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThemeProvider } from '@mui/material/styles'
import { PendingUserPage } from './PendingUserPage'
import { BrowserRouter } from 'react-router-dom'
import { cobraTheme } from '@/theme/cobraTheme'

// Mock react-router-dom
const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  }
})

describe('PendingUserPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  const renderPage = () => {
    return render(
      <ThemeProvider theme={cobraTheme}>
        <BrowserRouter>
          <PendingUserPage />
        </BrowserRouter>
      </ThemeProvider>,
    )
  }

  it('renders pending message', () => {
    renderPage()

    expect(screen.getByText(/waiting for organization assignment/i)).toBeInTheDocument()
    expect(
      screen.getByText(/your account has been created/i),
    ).toBeInTheDocument()
  })

  it('renders organization code input', () => {
    renderPage()

    expect(screen.getByLabelText(/organization code/i)).toBeInTheDocument()
  })

  it('renders join button', () => {
    renderPage()

    expect(screen.getByRole('button', { name: /join organization/i })).toBeInTheDocument()
  })

  it('disables join button when code is empty', () => {
    renderPage()

    const button = screen.getByRole('button', { name: /join organization/i })
    expect(button).toBeDisabled()
  })

  it('enables join button when code is entered', async () => {
    const user = userEvent.setup()
    renderPage()

    const input = screen.getByLabelText(/organization code/i)
    const button = screen.getByRole('button', { name: /join organization/i })

    expect(button).toBeDisabled()

    await user.type(input, 'ABC12345')

    expect(button).toBeEnabled()
  })

  it('shows contact information', () => {
    renderPage()

    expect(screen.getByText(/contact your administrator/i)).toBeInTheDocument()
  })
})
