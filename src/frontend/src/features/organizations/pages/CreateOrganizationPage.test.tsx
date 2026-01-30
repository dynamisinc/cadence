/**
 * CreateOrganizationPage Tests
 *
 * Tests the organization creation form with:
 * - Form rendering and validation
 * - Auto-generated slug from name
 * - Slug availability checking
 * - Form submission and navigation
 *
 * @module features/organizations/pages
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { CreateOrganizationPage } from './CreateOrganizationPage'
import { toast } from 'react-toastify'

// Mock dependencies
vi.mock('react-router-dom', () => ({
  useNavigate: vi.fn(),
}))

vi.mock('../hooks/useOrganizations', () => ({
  useCreateOrganization: vi.fn(),
  useCheckSlug: vi.fn(),
}))

vi.mock('react-toastify', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}))

// Mock styled components to avoid theme provider issues
const inputIdCounter = 0
vi.mock('@/theme/styledComponents', () => ({
  CobraPrimaryButton: ({ children, ...props }: any) => <button {...props}>{children}</button>,
  CobraSecondaryButton: ({ children, ...props }: any) => <button {...props}>{children}</button>,
  CobraTextField: ({ label, helperText, error, InputProps, ...props }: any) => {
    const inputId = `input-${label?.toLowerCase().replace(/\s+/g, '-')}`
    return (
      <div>
        <label htmlFor={inputId}>{label}</label>
        <input id={inputId} {...props} />
        {InputProps?.endAdornment}
        {helperText && <span>{helperText}</span>}
        {error && <span role="alert">Error</span>}
      </div>
    )
  },
}))

import { useNavigate } from 'react-router-dom'
import { useCreateOrganization, useCheckSlug } from '../hooks/useOrganizations'

describe('CreateOrganizationPage', () => {
  const mockNavigate = vi.fn()
  const mockMutateAsync = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(useNavigate).mockReturnValue(mockNavigate)
    vi.mocked(useCreateOrganization).mockReturnValue({
      mutateAsync: mockMutateAsync,
      isPending: false,
    } as any)
    vi.mocked(useCheckSlug).mockReturnValue({
      data: undefined,
      isLoading: false,
    } as any)
  })

  describe('Rendering', () => {
    it('renders create organization form with all fields', () => {
      render(<CreateOrganizationPage />)

      expect(screen.getByLabelText(/organization name/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/slug/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/description/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/contact email/i)).toBeInTheDocument()
      expect(screen.getByLabelText(/admin email/i)).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /create organization/i })).toBeInTheDocument()
    })

    it('renders header with back button', () => {
      render(<CreateOrganizationPage />)

      // Title and submit button both contain "Create Organization"
      expect(screen.getByRole('heading', { name: /create organization/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /back/i })).toBeInTheDocument()
    })

    it('renders alert about first administrator requirement', () => {
      render(<CreateOrganizationPage />)

      expect(
        screen.getByText(/every organization needs at least one administrator/i),
      ).toBeInTheDocument()
    })
  })

  describe('Slug Auto-generation', () => {
    it('auto-generates slug from organization name', async () => {
      const user = userEvent.setup()
      render(<CreateOrganizationPage />)

      const nameInput = screen.getByLabelText(/organization name/i)
      const slugInput = screen.getByLabelText(/slug/i)

      await user.type(nameInput, 'CISA Region 4')

      await waitFor(() => {
        expect(slugInput).toHaveValue('cisa-region-4')
      })
    })

    it('converts uppercase to lowercase in slug', async () => {
      const user = userEvent.setup()
      render(<CreateOrganizationPage />)

      const nameInput = screen.getByLabelText(/organization name/i)
      const slugInput = screen.getByLabelText(/slug/i)

      await user.type(nameInput, 'MY ORGANIZATION')

      await waitFor(() => {
        expect(slugInput).toHaveValue('my-organization')
      })
    })

    it('removes special characters from slug', async () => {
      const user = userEvent.setup()
      render(<CreateOrganizationPage />)

      const nameInput = screen.getByLabelText(/organization name/i)
      const slugInput = screen.getByLabelText(/slug/i)

      await user.type(nameInput, 'Org@#$Name!')

      await waitFor(() => {
        expect(slugInput).toHaveValue('orgname')
      })
    })

    it('replaces spaces with hyphens in slug', async () => {
      const user = userEvent.setup()
      render(<CreateOrganizationPage />)

      const nameInput = screen.getByLabelText(/organization name/i)
      const slugInput = screen.getByLabelText(/slug/i)

      await user.type(nameInput, 'My Test Org')

      await waitFor(() => {
        expect(slugInput).toHaveValue('my-test-org')
      })
    })

    it('allows manual slug editing', async () => {
      const user = userEvent.setup()
      render(<CreateOrganizationPage />)

      const nameInput = screen.getByLabelText(/organization name/i)
      const slugInput = screen.getByLabelText(/slug/i)

      // Auto-generate first
      await user.type(nameInput, 'CISA')
      await waitFor(() => {
        expect(slugInput).toHaveValue('cisa')
      })

      // Manually edit slug
      await user.clear(slugInput)
      await user.type(slugInput, 'custom-slug')

      expect(slugInput).toHaveValue('custom-slug')

      // Further name changes should not override manual slug
      await user.type(nameInput, ' Region 4')
      await waitFor(() => {
        expect(slugInput).toHaveValue('custom-slug')
      })
    })
  })

  describe('Slug Availability Check', () => {
    it.skip('shows loading indicator while checking slug', () => {
      vi.mocked(useCheckSlug).mockReturnValue({
        data: undefined,
        isLoading: true,
      } as any)

      render(<CreateOrganizationPage />)

      // Type a slug to trigger check
      const slugInput = screen.getByLabelText(/slug/i) as HTMLInputElement
      slugInput.value = 'test-slug'

      expect(screen.getByRole('progressbar')).toBeInTheDocument()
    })

    it.skip('shows success icon when slug is available', () => {
      vi.mocked(useCheckSlug).mockReturnValue({
        data: { available: true },
        isLoading: false,
      } as any)

      render(<CreateOrganizationPage />)

      const slugInput = screen.getByLabelText(/slug/i) as HTMLInputElement
      slugInput.value = 'available-slug'

      // Icon should be green checkmark
      const icon = document.querySelector('[style*="green"]')
      expect(icon).toBeInTheDocument()
    })

    it.skip('shows error icon when slug is taken', () => {
      vi.mocked(useCheckSlug).mockReturnValue({
        data: { available: false, suggestion: 'test-slug-1' },
        isLoading: false,
      } as any)

      render(<CreateOrganizationPage />)

      const slugInput = screen.getByLabelText(/slug/i) as HTMLInputElement
      slugInput.value = 'taken-slug'

      // Icon should be red X
      const icon = document.querySelector('[style*="red"]')
      expect(icon).toBeInTheDocument()
    })

    it('displays suggestion when slug is taken', () => {
      vi.mocked(useCheckSlug).mockReturnValue({
        data: { available: false, suggestion: 'my-org-1' },
        isLoading: false,
      } as any)

      render(<CreateOrganizationPage />)

      expect(screen.getByText(/suggestion: my-org-1/i)).toBeInTheDocument()
    })
  })

  describe('Form Validation', () => {
    it('disables submit button when required fields are empty', () => {
      render(<CreateOrganizationPage />)

      const submitButton = screen.getByRole('button', { name: /create organization/i })
      expect(submitButton).toBeDisabled()
    })

    it('enables submit button when all required fields are filled and slug is valid', async () => {
      const user = userEvent.setup()
      vi.mocked(useCheckSlug).mockReturnValue({
        data: { available: true },
        isLoading: false,
      } as any)

      render(<CreateOrganizationPage />)

      await user.type(screen.getByLabelText(/organization name/i), 'Test Org')
      await user.type(screen.getByLabelText(/slug/i), 'test-org')
      await user.type(screen.getByLabelText(/admin email/i), 'admin@test.com')

      await waitFor(() => {
        const submitButton = screen.getByRole('button', { name: /create organization/i })
        expect(submitButton).not.toBeDisabled()
      })
    })

    it('disables submit button when slug is not available', async () => {
      const user = userEvent.setup()
      vi.mocked(useCheckSlug).mockReturnValue({
        data: { available: false },
        isLoading: false,
      } as any)

      render(<CreateOrganizationPage />)

      await user.type(screen.getByLabelText(/organization name/i), 'Test Org')
      await user.type(screen.getByLabelText(/slug/i), 'taken-slug')
      await user.type(screen.getByLabelText(/admin email/i), 'admin@test.com')

      const submitButton = screen.getByRole('button', { name: /create organization/i })
      expect(submitButton).toBeDisabled()
    })

    it.skip('shows error toast when submitting with empty required fields', async () => {
      const user = userEvent.setup()
      render(<CreateOrganizationPage />)

      const submitButton = screen.getByRole('button', { name: /create organization/i })
      // Forcefully enable button to test validation
      submitButton.removeAttribute('disabled')

      await user.click(submitButton)

      expect(toast.error).toHaveBeenCalledWith('Please fill in all required fields')
    })

    it('shows error toast when submitting with unavailable slug', async () => {
      const user = userEvent.setup()
      vi.mocked(useCheckSlug).mockReturnValue({
        data: { available: false },
        isLoading: false,
      } as any)

      render(<CreateOrganizationPage />)

      await user.type(screen.getByLabelText(/organization name/i), 'Test Org')
      await user.type(screen.getByLabelText(/slug/i), 'taken-slug')
      await user.type(screen.getByLabelText(/admin email/i), 'admin@test.com')

      const submitButton = screen.getByRole('button', { name: /create organization/i })
      submitButton.removeAttribute('disabled')

      await user.click(submitButton)

      expect(toast.error).toHaveBeenCalledWith('Please choose a different slug')
    })
  })

  describe('Form Submission', () => {
    it.skip('submits form with all required fields', async () => {
      const user = userEvent.setup()
      vi.mocked(useCheckSlug).mockReturnValue({
        data: { available: true },
        isLoading: false,
      } as any)

      mockMutateAsync.mockResolvedValue({})

      render(<CreateOrganizationPage />)

      await user.type(screen.getByLabelText(/organization name/i), 'Test Org')
      await user.type(screen.getByLabelText(/slug/i), 'test-org')
      await user.type(screen.getByLabelText(/admin email/i), 'admin@test.com')

      const submitButton = screen.getByRole('button', { name: /create organization/i })
      await user.click(submitButton)

      expect(mockMutateAsync).toHaveBeenCalledWith({
        name: 'Test Org',
        slug: 'test-org',
        description: undefined,
        contactEmail: undefined,
        firstAdminEmail: 'admin@test.com',
      })
    })

    it.skip('submits form with optional fields', async () => {
      const user = userEvent.setup()
      vi.mocked(useCheckSlug).mockReturnValue({
        data: { available: true },
        isLoading: false,
      } as any)

      mockMutateAsync.mockResolvedValue({})

      render(<CreateOrganizationPage />)

      await user.type(screen.getByLabelText(/organization name/i), 'Test Org')
      await user.type(screen.getByLabelText(/slug/i), 'test-org')
      await user.type(screen.getByLabelText(/description/i), 'A test organization')
      await user.type(screen.getByLabelText(/contact email/i), 'contact@test.com')
      await user.type(screen.getByLabelText(/admin email/i), 'admin@test.com')

      const submitButton = screen.getByRole('button', { name: /create organization/i })
      await user.click(submitButton)

      expect(mockMutateAsync).toHaveBeenCalledWith({
        name: 'Test Org',
        slug: 'test-org',
        description: 'A test organization',
        contactEmail: 'contact@test.com',
        firstAdminEmail: 'admin@test.com',
      })
    })

    it('shows success toast and navigates on successful creation', async () => {
      const user = userEvent.setup()
      vi.mocked(useCheckSlug).mockReturnValue({
        data: { available: true },
        isLoading: false,
      } as any)

      mockMutateAsync.mockResolvedValue({})

      render(<CreateOrganizationPage />)

      await user.type(screen.getByLabelText(/organization name/i), 'Test Org')
      await user.type(screen.getByLabelText(/slug/i), 'test-org')
      await user.type(screen.getByLabelText(/admin email/i), 'admin@test.com')

      const submitButton = screen.getByRole('button', { name: /create organization/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(toast.success).toHaveBeenCalledWith('Organization created successfully')
        expect(mockNavigate).toHaveBeenCalledWith('/admin/organizations')
      })
    })

    it('shows error toast on creation failure', async () => {
      const user = userEvent.setup()
      vi.mocked(useCheckSlug).mockReturnValue({
        data: { available: true },
        isLoading: false,
      } as any)

      const error = {
        response: {
          data: {
            message: 'Organization already exists',
          },
        },
      }
      mockMutateAsync.mockRejectedValue(error)

      render(<CreateOrganizationPage />)

      await user.type(screen.getByLabelText(/organization name/i), 'Test Org')
      await user.type(screen.getByLabelText(/slug/i), 'test-org')
      await user.type(screen.getByLabelText(/admin email/i), 'admin@test.com')

      const submitButton = screen.getByRole('button', { name: /create organization/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith('Organization already exists')
      })
    })

    it('shows generic error message when API error has no message', async () => {
      const user = userEvent.setup()
      vi.mocked(useCheckSlug).mockReturnValue({
        data: { available: true },
        isLoading: false,
      } as any)

      mockMutateAsync.mockRejectedValue(new Error())

      render(<CreateOrganizationPage />)

      await user.type(screen.getByLabelText(/organization name/i), 'Test Org')
      await user.type(screen.getByLabelText(/slug/i), 'test-org')
      await user.type(screen.getByLabelText(/admin email/i), 'admin@test.com')

      const submitButton = screen.getByRole('button', { name: /create organization/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith('Failed to create organization')
      })
    })

    it('shows loading state during submission', async () => {
      const user = userEvent.setup()
      vi.mocked(useCheckSlug).mockReturnValue({
        data: { available: true },
        isLoading: false,
      } as any)
      vi.mocked(useCreateOrganization).mockReturnValue({
        mutateAsync: mockMutateAsync,
        isPending: true,
      } as any)

      render(<CreateOrganizationPage />)

      expect(screen.getByText(/creating\.\.\./i)).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /creating\.\.\./i })).toBeDisabled()
    })
  })

  describe('Navigation', () => {
    it('navigates back when back button clicked', async () => {
      const user = userEvent.setup()
      render(<CreateOrganizationPage />)

      const backButton = screen.getByRole('button', { name: /back/i })
      await user.click(backButton)

      expect(mockNavigate).toHaveBeenCalledWith('/admin/organizations')
    })

    it('navigates back when cancel button clicked', async () => {
      const user = userEvent.setup()
      render(<CreateOrganizationPage />)

      const cancelButton = screen.getByRole('button', { name: /cancel/i })
      await user.click(cancelButton)

      expect(mockNavigate).toHaveBeenCalledWith('/admin/organizations')
    })
  })
})
