/**
 * LoginPage Component Tests
 */
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '../../../test/testUtils';
import { LoginPage } from './LoginPage';

// Mock navigator.onLine
Object.defineProperty(window.navigator, 'onLine', {
  writable: true,
  value: true,
});

describe('LoginPage', () => {
  it('renders login form', () => {
    render(<LoginPage />);

    expect(screen.getByRole('heading', { name: 'Sign In' })).toBeInTheDocument();
    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument();
    expect(screen.getByLabelText('Password *')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
  });

  it('shows password when visibility toggle clicked', async () => {
    render(<LoginPage />);

    const passwordInput = screen.getByLabelText('Password *') as HTMLInputElement;
    const toggleButton = screen.getByLabelText(/show password/i);

    // Initially password should be hidden
    expect(passwordInput.type).toBe('password');

    // Click toggle to show password
    fireEvent.click(toggleButton);

    await waitFor(() => {
      expect(passwordInput.type).toBe('text');
    });
  });

  it('validates email format on blur', async () => {
    render(<LoginPage />);

    const emailInput = screen.getByLabelText(/email address/i);

    // Enter invalid email
    fireEvent.change(emailInput, { target: { value: 'invalid-email' } });
    fireEvent.blur(emailInput);

    await waitFor(() => {
      expect(screen.getByText(/please enter a valid email address/i)).toBeInTheDocument();
    });
  });

  it('shows remember me checkbox', () => {
    render(<LoginPage />);

    expect(screen.getByLabelText(/remember me/i)).toBeInTheDocument();
  });

  it('shows forgot password link', () => {
    render(<LoginPage />);

    expect(screen.getByText(/forgot your password/i)).toBeInTheDocument();
  });

  it('shows create account link', () => {
    render(<LoginPage />);

    expect(screen.getByText(/create one/i)).toBeInTheDocument();
  });

  it('disables submit button when submitting', async () => {
    render(<LoginPage />);

    const emailInput = screen.getByLabelText(/email address/i);
    const passwordInput = screen.getByLabelText('Password *');
    const submitButton = screen.getByRole('button', { name: /sign in/i });

    // Fill in valid credentials
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(passwordInput, { target: { value: 'Password123' } });

    // Submit form
    fireEvent.click(submitButton);

    // Button should be disabled during submission
    await waitFor(() => {
      expect(submitButton).toBeDisabled();
    });
  });
});
