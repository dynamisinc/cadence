/**
 * AuthLayout Component Tests
 *
 * @module features/auth
 */
import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { AuthLayout } from './AuthLayout'

describe('AuthLayout', () => {
  it('renders the Cadence branding', () => {
    render(
      <AuthLayout title="Sign In">
        <div>Form content</div>
      </AuthLayout>,
    )

    expect(screen.getByText('CADENCE')).toBeInTheDocument()
  })

  it('renders the title', () => {
    render(
      <AuthLayout title="Sign In">
        <div>Form content</div>
      </AuthLayout>,
    )

    expect(screen.getByRole('heading', { name: 'Sign In' })).toBeInTheDocument()
  })

  it('renders children content', () => {
    render(
      <AuthLayout title="Sign In">
        <div data-testid="form-content">Form content</div>
      </AuthLayout>,
    )

    expect(screen.getByTestId('form-content')).toBeInTheDocument()
    expect(screen.getByText('Form content')).toBeInTheDocument()
  })

  it('hides offline indicator by default', () => {
    render(
      <AuthLayout title="Sign In">
        <div>Form content</div>
      </AuthLayout>,
    )

    expect(screen.queryByText(/offline/i)).not.toBeInTheDocument()
  })

  it('shows offline indicator when enabled', () => {
    render(
      <AuthLayout title="Sign In" showOfflineIndicator>
        <div>Form content</div>
      </AuthLayout>,
    )

    expect(screen.getByText(/offline/i)).toBeInTheDocument()
    expect(screen.getByRole('alert')).toBeInTheDocument()
  })

  it('renders with different titles', () => {
    const { rerender } = render(
      <AuthLayout title="Create Account">
        <div>Form content</div>
      </AuthLayout>,
    )

    expect(screen.getByRole('heading', { name: 'Create Account' })).toBeInTheDocument()

    rerender(
      <AuthLayout title="Reset Password">
        <div>Form content</div>
      </AuthLayout>,
    )

    expect(screen.getByRole('heading', { name: 'Reset Password' })).toBeInTheDocument()
  })
})
