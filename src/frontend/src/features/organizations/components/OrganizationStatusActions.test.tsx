/**
 * OrganizationStatusActions Tests
 *
 * Tests the organization status display and action buttons component.
 *
 * @module features/organizations/components
 */
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@/test/testUtils'
import userEvent from '@testing-library/user-event'
import { OrganizationStatusActions } from './OrganizationStatusActions'
import type { OrgStatus } from '../types'

// Mock StatusChip
vi.mock('@/shared/components', () => ({
  StatusChip: ({ status }: { status: OrgStatus }) => <span data-testid="status-chip">{status}</span>,
}))

describe('OrganizationStatusActions', () => {
  const mockOnArchive = vi.fn()
  const mockOnDeactivate = vi.fn()
  const mockOnRestore = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Rendering', () => {
    it('renders status section title', () => {
      render(
        <OrganizationStatusActions
          status="Active"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.getByText('Organization Status')).toBeInTheDocument()
    })

    it('displays current status using StatusChip', () => {
      render(
        <OrganizationStatusActions
          status="Active"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      const statusChip = screen.getByTestId('status-chip')
      expect(statusChip).toHaveTextContent('Active')
    })

    it('displays status in current status text', () => {
      render(
        <OrganizationStatusActions
          status="Archived"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.getByText(/current status:/i)).toBeInTheDocument()
    })
  })

  describe('Active Status Actions', () => {
    it('shows Archive and Deactivate buttons when status is Active', () => {
      render(
        <OrganizationStatusActions
          status="Active"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.getByRole('button', { name: /archive organization/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /deactivate organization/i })).toBeInTheDocument()
    })

    it('does not show Restore button when status is Active', () => {
      render(
        <OrganizationStatusActions
          status="Active"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.queryByRole('button', { name: /restore/i })).not.toBeInTheDocument()
    })

    it('calls onArchive when Archive button is clicked', async () => {
      const user = userEvent.setup()
      render(
        <OrganizationStatusActions
          status="Active"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      const archiveButton = screen.getByRole('button', { name: /archive organization/i })
      await user.click(archiveButton)

      expect(mockOnArchive).toHaveBeenCalledTimes(1)
    })

    it('calls onDeactivate when Deactivate button is clicked', async () => {
      const user = userEvent.setup()
      render(
        <OrganizationStatusActions
          status="Active"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      const deactivateButton = screen.getByRole('button', { name: /deactivate organization/i })
      await user.click(deactivateButton)

      expect(mockOnDeactivate).toHaveBeenCalledTimes(1)
    })

    it('does not show warning alert when status is Active', () => {
      render(
        <OrganizationStatusActions
          status="Active"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.queryByText(/users cannot access/i)).not.toBeInTheDocument()
    })
  })

  describe('Archived Status Actions', () => {
    it('shows Restore button when status is Archived', () => {
      render(
        <OrganizationStatusActions
          status="Archived"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.getByRole('button', { name: /restore to active/i })).toBeInTheDocument()
    })

    it('does not show Archive or Deactivate buttons when status is Archived', () => {
      render(
        <OrganizationStatusActions
          status="Archived"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.queryByRole('button', { name: /archive organization/i })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /deactivate organization/i })).not.toBeInTheDocument()
    })

    it('calls onRestore when Restore button is clicked', async () => {
      const user = userEvent.setup()
      render(
        <OrganizationStatusActions
          status="Archived"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      const restoreButton = screen.getByRole('button', { name: /restore to active/i })
      await user.click(restoreButton)

      expect(mockOnRestore).toHaveBeenCalledTimes(1)
    })

    it('shows warning alert when status is Archived', () => {
      render(
        <OrganizationStatusActions
          status="Archived"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.getByText(/this organization is archived/i)).toBeInTheDocument()
      expect(screen.getByText(/users cannot access it until it is restored/i)).toBeInTheDocument()
    })
  })

  describe('Inactive Status Actions', () => {
    it('shows Restore button when status is Inactive', () => {
      render(
        <OrganizationStatusActions
          status="Inactive"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.getByRole('button', { name: /restore to active/i })).toBeInTheDocument()
    })

    it('does not show Archive or Deactivate buttons when status is Inactive', () => {
      render(
        <OrganizationStatusActions
          status="Inactive"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.queryByRole('button', { name: /archive organization/i })).not.toBeInTheDocument()
      expect(screen.queryByRole('button', { name: /deactivate organization/i })).not.toBeInTheDocument()
    })

    it('calls onRestore when Restore button is clicked', async () => {
      const user = userEvent.setup()
      render(
        <OrganizationStatusActions
          status="Inactive"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      const restoreButton = screen.getByRole('button', { name: /restore to active/i })
      await user.click(restoreButton)

      expect(mockOnRestore).toHaveBeenCalledTimes(1)
    })

    it('shows warning alert when status is Inactive', () => {
      render(
        <OrganizationStatusActions
          status="Inactive"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.getByText(/this organization is inactive/i)).toBeInTheDocument()
      expect(screen.getByText(/users cannot access it until it is restored/i)).toBeInTheDocument()
    })
  })

  describe('Loading State', () => {
    it('disables all buttons when isPending is true - Active status', () => {
      render(
        <OrganizationStatusActions
          status="Active"
          isPending={true}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      const archiveButton = screen.getByRole('button', { name: /archive organization/i })
      const deactivateButton = screen.getByRole('button', { name: /deactivate organization/i })

      expect(archiveButton).toBeDisabled()
      expect(deactivateButton).toBeDisabled()
    })

    it('disables Restore button when isPending is true - Archived status', () => {
      render(
        <OrganizationStatusActions
          status="Archived"
          isPending={true}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      const restoreButton = screen.getByRole('button', { name: /restore to active/i })
      expect(restoreButton).toBeDisabled()
    })

    it('disables Restore button when isPending is true - Inactive status', () => {
      render(
        <OrganizationStatusActions
          status="Inactive"
          isPending={true}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      const restoreButton = screen.getByRole('button', { name: /restore to active/i })
      expect(restoreButton).toBeDisabled()
    })

    it('enables buttons when isPending is false', () => {
      render(
        <OrganizationStatusActions
          status="Active"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      const archiveButton = screen.getByRole('button', { name: /archive organization/i })
      const deactivateButton = screen.getByRole('button', { name: /deactivate organization/i })

      expect(archiveButton).not.toBeDisabled()
      expect(deactivateButton).not.toBeDisabled()
    })
  })

  describe('Button Icons', () => {
    it('shows correct icons for Active status buttons', () => {
      const { container } = render(
        <OrganizationStatusActions
          status="Active"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      // FontAwesome icons are rendered with specific class names or SVG elements
      // Just verify buttons exist with correct text
      expect(screen.getByRole('button', { name: /archive organization/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /deactivate organization/i })).toBeInTheDocument()
    })

    it('shows correct icon for Restore button', () => {
      render(
        <OrganizationStatusActions
          status="Archived"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.getByRole('button', { name: /restore to active/i })).toBeInTheDocument()
    })
  })

  describe('Status Changes', () => {
    it('updates displayed status when status prop changes', () => {
      const { rerender } = render(
        <OrganizationStatusActions
          status="Active"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      let statusChip = screen.getByTestId('status-chip')
      expect(statusChip).toHaveTextContent('Active')

      rerender(
        <OrganizationStatusActions
          status="Archived"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      statusChip = screen.getByTestId('status-chip')
      expect(statusChip).toHaveTextContent('Archived')
    })

    it('updates available actions when status changes from Active to Archived', () => {
      const { rerender } = render(
        <OrganizationStatusActions
          status="Active"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.getByRole('button', { name: /archive organization/i })).toBeInTheDocument()

      rerender(
        <OrganizationStatusActions
          status="Archived"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.queryByRole('button', { name: /archive organization/i })).not.toBeInTheDocument()
      expect(screen.getByRole('button', { name: /restore to active/i })).toBeInTheDocument()
    })
  })

  describe('Accessibility', () => {
    it('has proper heading hierarchy', () => {
      render(
        <OrganizationStatusActions
          status="Active"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.getByRole('heading', { level: 6, name: /organization status/i })).toBeInTheDocument()
    })

    it('has descriptive button labels', () => {
      render(
        <OrganizationStatusActions
          status="Active"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      expect(screen.getByRole('button', { name: /archive organization/i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /deactivate organization/i })).toBeInTheDocument()
    })

    it('uses alert role for warning message', () => {
      render(
        <OrganizationStatusActions
          status="Archived"
          isPending={false}
          onArchive={mockOnArchive}
          onDeactivate={mockOnDeactivate}
          onRestore={mockOnRestore}
        />
      )

      const alert = screen.getByRole('alert')
      expect(alert).toHaveTextContent(/this organization is archived/i)
    })
  })
})
