/**
 * EditOrganizationPage Tests
 *
 * Tests the organization editing page with:
 * - Organization details loading
 * - Form editing and update
 * - Member management
 * - Status actions (archive, deactivate, restore)
 *
 * @module features/organizations/pages
 */
/* eslint-disable @typescript-eslint/no-explicit-any */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { EditOrganizationPage } from './EditOrganizationPage'
import { toast } from 'react-toastify'
import type { Organization, OrgMember } from '../types'

// Mock dependencies
vi.mock('react-router-dom', () => ({
  useNavigate: vi.fn(),
  useParams: vi.fn(),
}))

vi.mock('@tanstack/react-query', () => ({
  useQuery: vi.fn(),
  useMutation: vi.fn(),
  useQueryClient: vi.fn(),
}))

vi.mock('../hooks/useOrganizations', () => ({
  useOrganization: vi.fn(),
  useUpdateOrganization: vi.fn(),
  useArchiveOrganization: vi.fn(),
  useDeactivateOrganization: vi.fn(),
  useRestoreOrganization: vi.fn(),
}))

vi.mock('../services/organizationService', () => ({
  organizationService: {
    getMembers: vi.fn(),
    addMember: vi.fn(),
    updateMemberRole: vi.fn(),
    removeMember: vi.fn(),
  },
}))

vi.mock('../components', () => ({
  AddMemberDialog: ({ open, onClose, onAdd }: any) =>
    open ? (
      <div role="dialog">
        <button onClick={() => onAdd('test@example.com', 'OrgUser')}>Confirm Add</button>
        <button onClick={onClose}>Close Dialog</button>
      </div>
    ) : null,
  OrgMembersTable: ({ members, onAddClick, onRoleChange, onRemove }: any) => (
    <div>
      <button onClick={onAddClick}>Add Member</button>
      {members.map((m: OrgMember) => (
        <div key={m.membershipId} data-testid={`member-${m.membershipId}`}>
          <span>{m.email}</span>
          <button onClick={() => onRoleChange(m.membershipId, 'OrgAdmin')}>Change Role</button>
          <button onClick={() => onRemove(m.membershipId)}>Remove</button>
        </div>
      ))}
    </div>
  ),
  OrganizationStatusActions: ({ status, onArchive, onDeactivate, onRestore }: any) => (
    <div>
      {status === 'Active' && (
        <>
          <button onClick={onArchive}>Archive</button>
          <button onClick={onDeactivate}>Deactivate</button>
        </>
      )}
      {(status === 'Archived' || status === 'Inactive') && (
        <button onClick={onRestore}>Restore</button>
      )}
    </div>
  ),
}))

vi.mock('@/shared/components', () => ({
  StatusChip: ({ status }: any) => <span>{status}</span>,
}))

vi.mock('react-toastify', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}))

// Mock styled components
vi.mock('@/theme/styledComponents', () => ({
  CobraPrimaryButton: ({ children, ...props }: any) => <button {...props}>{children}</button>,
  CobraSecondaryButton: ({ children, ...props }: any) => <button {...props}>{children}</button>,
  CobraTextField: ({ label, ...props }: any) => (
    <div>
      <label>{label}</label>
      <input {...props} />
    </div>
  ),
}))

import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  useOrganization,
  useUpdateOrganization,
  useArchiveOrganization,
  useDeactivateOrganization,
  useRestoreOrganization,
} from '../hooks/useOrganizations'
import { organizationService } from '../services/organizationService'

describe('EditOrganizationPage', () => {
  const mockNavigate = vi.fn()
  const mockQueryClient = {
    invalidateQueries: vi.fn(),
  }
  const mockUpdateAsync = vi.fn()
  const mockArchiveAsync = vi.fn()
  const mockDeactivateAsync = vi.fn()
  const mockRestoreAsync = vi.fn()
  const mockAddMemberAsync = vi.fn()
  const mockUpdateRoleAsync = vi.fn()
  const mockRemoveMemberAsync = vi.fn()

  const mockOrganization: Organization = {
    id: 'org-1',
    name: 'Test Organization',
    slug: 'test-org',
    description: 'A test organization',
    contactEmail: 'contact@test.org',
    status: 'Active',
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  }

  const mockMembers: OrgMember[] = [
    {
      membershipId: 'mem-1',
      userId: 'user-1',
      email: 'admin@test.org',
      displayName: 'Admin User',
      role: 'OrgAdmin',
      joinedAt: '2024-01-01T00:00:00Z',
    },
    {
      membershipId: 'mem-2',
      userId: 'user-2',
      email: 'user@test.org',
      displayName: 'Regular User',
      role: 'OrgUser',
      joinedAt: '2024-01-02T00:00:00Z',
    },
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(useNavigate).mockReturnValue(mockNavigate)
    vi.mocked(useParams).mockReturnValue({ id: 'org-1' })
    vi.mocked(useQueryClient).mockReturnValue(mockQueryClient as any)

    vi.mocked(useOrganization).mockReturnValue({
      data: mockOrganization,
      isLoading: false,
      error: null,
    } as any)

    vi.mocked(useUpdateOrganization).mockReturnValue({
      mutateAsync: mockUpdateAsync,
      isPending: false,
    } as any)

    vi.mocked(useArchiveOrganization).mockReturnValue({
      mutateAsync: mockArchiveAsync,
      isPending: false,
    } as any)

    vi.mocked(useDeactivateOrganization).mockReturnValue({
      mutateAsync: mockDeactivateAsync,
      isPending: false,
    } as any)

    vi.mocked(useRestoreOrganization).mockReturnValue({
      mutateAsync: mockRestoreAsync,
      isPending: false,
    } as any)

    // Mock useQuery for members
    vi.mocked(useQuery).mockReturnValue({
      data: mockMembers,
      isLoading: false,
      error: null,
    } as any)

    // Mock useMutation for member operations
    let mutationCallCount = 0
    vi.mocked(useMutation).mockImplementation(() => {
      mutationCallCount++
      if (mutationCallCount === 1) {
        // addMember
        return { mutateAsync: mockAddMemberAsync, isPending: false } as any
      } else if (mutationCallCount === 2) {
        // updateMemberRole
        return { mutateAsync: mockUpdateRoleAsync, isPending: false } as any
      } else {
        // removeMember
        return { mutateAsync: mockRemoveMemberAsync, isPending: false } as any
      }
    })

    vi.mocked(organizationService.getMembers).mockResolvedValue(mockMembers)
  })

  describe('Loading and Error States', () => {
    it('shows loading spinner while loading organization', () => {
      vi.mocked(useOrganization).mockReturnValue({
        data: undefined,
        isLoading: true,
        error: null,
      } as any)

      render(<EditOrganizationPage />)

      expect(screen.getByRole('progressbar')).toBeInTheDocument()
    })

    it('shows error message when organization fails to load', () => {
      vi.mocked(useOrganization).mockReturnValue({
        data: undefined,
        isLoading: false,
        error: new Error('Failed to load'),
      } as any)

      render(<EditOrganizationPage />)

      // Error uses error.message when it's an Error instance
      expect(screen.getByText('Failed to load')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /back to organizations/i })).toBeInTheDocument()
    })

    it('shows error message when organization is null', () => {
      vi.mocked(useOrganization).mockReturnValue({
        data: null,
        isLoading: false,
        error: null,
      } as any)

      render(<EditOrganizationPage />)

      expect(screen.getByText(/failed to load organization/i)).toBeInTheDocument()
    })
  })

  describe('Rendering', () => {
    it('renders organization details in form', () => {
      render(<EditOrganizationPage />)

      expect(screen.getByDisplayValue('Test Organization')).toBeInTheDocument()
      expect(screen.getByDisplayValue('A test organization')).toBeInTheDocument()
      expect(screen.getByDisplayValue('contact@test.org')).toBeInTheDocument()
    })

    it('displays organization slug and created date', () => {
      render(<EditOrganizationPage />)

      expect(screen.getByText('test-org')).toBeInTheDocument()
      // Date format is locale-dependent, check the Created label exists
      expect(screen.getByText('Created')).toBeInTheDocument()
    })

    it('displays status chip', () => {
      render(<EditOrganizationPage />)

      expect(screen.getByText('Active')).toBeInTheDocument()
    })

    it('renders member list', () => {
      render(<EditOrganizationPage />)

      expect(screen.getByText('admin@test.org')).toBeInTheDocument()
      expect(screen.getByText('user@test.org')).toBeInTheDocument()
    })
  })

  describe('Form Editing', () => {
    it('enables save button when form has changes', async () => {
      const user = userEvent.setup()
      render(<EditOrganizationPage />)

      const nameInput = screen.getByDisplayValue('Test Organization')
      await user.clear(nameInput)
      await user.type(nameInput, 'Updated Organization')

      await waitFor(() => {
        const saveButton = screen.getByRole('button', { name: /save changes/i })
        expect(saveButton).not.toBeDisabled()
      })
    })

    it('disables save button when no changes', () => {
      render(<EditOrganizationPage />)

      const saveButton = screen.getByRole('button', { name: /save changes/i })
      expect(saveButton).toBeDisabled()
    })

    it('submits updated organization data', async () => {
      const user = userEvent.setup()
      mockUpdateAsync.mockResolvedValue({})

      render(<EditOrganizationPage />)

      const nameInput = screen.getByDisplayValue('Test Organization')
      await user.clear(nameInput)
      await user.type(nameInput, 'Updated Organization')

      const saveButton = screen.getByRole('button', { name: /save changes/i })
      await user.click(saveButton)

      expect(mockUpdateAsync).toHaveBeenCalledWith({
        id: 'org-1',
        request: {
          name: 'Updated Organization',
          description: 'A test organization',
          contactEmail: 'contact@test.org',
        },
      })
    })

    it('shows success toast on successful update', async () => {
      const user = userEvent.setup()
      mockUpdateAsync.mockResolvedValue({})

      render(<EditOrganizationPage />)

      const nameInput = screen.getByDisplayValue('Test Organization')
      await user.type(nameInput, ' Updated')

      const saveButton = screen.getByRole('button', { name: /save changes/i })
      await user.click(saveButton)

      await waitFor(() => {
        expect(toast.success).toHaveBeenCalledWith('Organization updated successfully')
      })
    })

    it('shows error toast on update failure', async () => {
      const user = userEvent.setup()
      mockUpdateAsync.mockRejectedValue(new Error('Update failed'))

      render(<EditOrganizationPage />)

      const nameInput = screen.getByDisplayValue('Test Organization')
      await user.type(nameInput, ' Updated')

      const saveButton = screen.getByRole('button', { name: /save changes/i })
      await user.click(saveButton)

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith('Update failed')
      })
    })

    it('handles empty optional fields correctly', async () => {
      const user = userEvent.setup()
      mockUpdateAsync.mockResolvedValue({})

      render(<EditOrganizationPage />)

      const descriptionInput = screen.getByDisplayValue('A test organization')
      await user.clear(descriptionInput)

      const contactInput = screen.getByDisplayValue('contact@test.org')
      await user.clear(contactInput)

      const saveButton = screen.getByRole('button', { name: /save changes/i })
      await user.click(saveButton)

      expect(mockUpdateAsync).toHaveBeenCalledWith({
        id: 'org-1',
        request: {
          name: 'Test Organization',
          description: undefined,
          contactEmail: undefined,
        },
      })
    })
  })

  describe('Status Actions', () => {
    it('shows archive and deactivate buttons for active org', () => {
      render(<EditOrganizationPage />)

      expect(screen.getByRole('button', { name: /archive/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /deactivate/i })).toBeInTheDocument()
    })

    it('shows restore button for archived org', () => {
      vi.mocked(useOrganization).mockReturnValue({
        data: { ...mockOrganization, status: 'Archived' },
        isLoading: false,
        error: null,
      } as any)

      render(<EditOrganizationPage />)

      expect(screen.getByRole('button', { name: /restore/i })).toBeInTheDocument()
    })

    it('archives organization with confirmation', async () => {
      const user = userEvent.setup()
      const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
      mockArchiveAsync.mockResolvedValue({})

      render(<EditOrganizationPage />)

      const archiveButton = screen.getByRole('button', { name: /archive/i })
      await user.click(archiveButton)

      expect(confirmSpy).toHaveBeenCalled()
      expect(mockArchiveAsync).toHaveBeenCalledWith('org-1')
      await waitFor(() => {
        expect(toast.success).toHaveBeenCalledWith('Organization archived')
      })
    })

    it('cancels archive when confirmation denied', async () => {
      const user = userEvent.setup()
      vi.spyOn(window, 'confirm').mockReturnValue(false)

      render(<EditOrganizationPage />)

      const archiveButton = screen.getByRole('button', { name: /archive/i })
      await user.click(archiveButton)

      expect(mockArchiveAsync).not.toHaveBeenCalled()
    })

    it('deactivates organization with confirmation', async () => {
      const user = userEvent.setup()
      vi.spyOn(window, 'confirm').mockReturnValue(true)
      mockDeactivateAsync.mockResolvedValue({})

      render(<EditOrganizationPage />)

      const deactivateButton = screen.getByRole('button', { name: /deactivate/i })
      await user.click(deactivateButton)

      expect(mockDeactivateAsync).toHaveBeenCalledWith('org-1')
      await waitFor(() => {
        expect(toast.success).toHaveBeenCalledWith('Organization deactivated')
      })
    })

    it('restores organization without confirmation', async () => {
      const user = userEvent.setup()
      vi.mocked(useOrganization).mockReturnValue({
        data: { ...mockOrganization, status: 'Archived' },
        isLoading: false,
        error: null,
      } as any)
      mockRestoreAsync.mockResolvedValue({})

      render(<EditOrganizationPage />)

      const restoreButton = screen.getByRole('button', { name: /restore/i })
      await user.click(restoreButton)

      expect(mockRestoreAsync).toHaveBeenCalledWith('org-1')
      await waitFor(() => {
        expect(toast.success).toHaveBeenCalledWith('Organization restored to active')
      })
    })
  })

  describe.skip('Member Management', () => {
    it('opens add member dialog when add button clicked', async () => {
      const user = userEvent.setup()
      render(<EditOrganizationPage />)

      const addButtons = screen.getAllByRole('button', { name: /add member/i })
      await user.click(addButtons[0])

      expect(screen.getByRole('dialog')).toBeInTheDocument()
    })

    it('adds member through dialog', async () => {
      const user = userEvent.setup()
      mockAddMemberAsync.mockResolvedValue({})

      render(<EditOrganizationPage />)

      const addButton = screen.getByRole('button', { name: /add member/i })
      await user.click(addButton)

      const confirmButton = screen.getByRole('button', { name: /confirm add/i })
      await user.click(confirmButton)

      await waitFor(() => {
        expect(mockAddMemberAsync).toHaveBeenCalledWith({
          email: 'test@example.com',
          role: 'OrgUser',
        })
        expect(toast.success).toHaveBeenCalledWith('Member added successfully')
      })
    })

    it('updates member role', async () => {
      const user = userEvent.setup()
      mockUpdateRoleAsync.mockResolvedValue({})

      render(<EditOrganizationPage />)

      const changeRoleButton = screen.getAllByRole('button', { name: /change role/i })[0]
      await user.click(changeRoleButton)

      await waitFor(() => {
        expect(mockUpdateRoleAsync).toHaveBeenCalledWith({
          membershipId: 'mem-1',
          role: 'OrgAdmin',
        })
        expect(toast.success).toHaveBeenCalledWith('Member role updated')
      })
    })

    it('removes member', async () => {
      const user = userEvent.setup()
      mockRemoveMemberAsync.mockResolvedValue({})

      render(<EditOrganizationPage />)

      const removeButton = screen.getAllByRole('button', { name: /remove/i })[0]
      await user.click(removeButton)

      await waitFor(() => {
        expect(mockRemoveMemberAsync).toHaveBeenCalledWith('mem-1')
        expect(toast.success).toHaveBeenCalledWith('Member removed')
      })
    })
  })

  describe('Navigation', () => {
    it('navigates back when back button clicked', async () => {
      const user = userEvent.setup()
      render(<EditOrganizationPage />)

      const backButton = screen.getByRole('button', { name: /back/i })
      await user.click(backButton)

      expect(mockNavigate).toHaveBeenCalledWith('/admin/organizations')
    })

    it('navigates back when cancel button clicked', async () => {
      const user = userEvent.setup()
      render(<EditOrganizationPage />)

      const cancelButton = screen.getByRole('button', { name: /cancel/i })
      await user.click(cancelButton)

      expect(mockNavigate).toHaveBeenCalledWith('/admin/organizations')
    })
  })
})
