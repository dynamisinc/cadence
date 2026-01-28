/**
 * CapabilityList Component Tests
 */
import { describe, it, expect, vi } from 'vitest'
import { render, screen, within, waitFor } from '../../../test/test-utils'
import userEvent from '@testing-library/user-event'
import { CapabilityList } from './CapabilityList'
import type { CapabilityDto } from '../types'

const mockCapabilities: CapabilityDto[] = [
  {
    id: '1',
    organizationId: '00000000-0000-0000-0000-000000000001',
    name: 'Mass Care Services',
    description: 'Provide shelter, food, and emergency services',
    category: 'Response',
    sortOrder: 1,
    isActive: true,
    sourceLibrary: 'FEMA',
    createdAt: '2025-01-01T09:00:00Z',
    updatedAt: '2025-01-01T09:00:00Z',
  },
  {
    id: '2',
    organizationId: '00000000-0000-0000-0000-000000000001',
    name: 'Planning',
    description: 'Conduct systematic planning',
    category: 'Prevention',
    sortOrder: 1,
    isActive: true,
    sourceLibrary: 'FEMA',
    createdAt: '2025-01-01T09:00:00Z',
    updatedAt: '2025-01-01T09:00:00Z',
  },
  {
    id: '3',
    organizationId: '00000000-0000-0000-0000-000000000001',
    name: 'Operational Communications',
    description: 'Establish communications',
    category: 'Response',
    sortOrder: 2,
    isActive: false,
    sourceLibrary: 'FEMA',
    createdAt: '2025-01-01T09:00:00Z',
    updatedAt: '2025-01-01T09:00:00Z',
  },
]

describe('CapabilityList', () => {
  describe('Empty state', () => {
    it('renders empty state when no capabilities provided', () => {
      render(
        <CapabilityList
          capabilities={[]}
          onEdit={vi.fn()}
          onDeactivate={vi.fn()}
        />,
      )

      expect(screen.getByText(/no capabilities defined/i)).toBeInTheDocument()
    })
  })

  describe('Capability display', () => {
    it('renders capabilities grouped by category', () => {
      render(
        <CapabilityList
          capabilities={mockCapabilities}
          onEdit={vi.fn()}
          onDeactivate={vi.fn()}
        />,
      )

      // Check category headers exist
      expect(screen.getByText('Response')).toBeInTheDocument()
      expect(screen.getByText('Prevention')).toBeInTheDocument()
    })

    it('displays capability names and descriptions', () => {
      render(
        <CapabilityList
          capabilities={mockCapabilities}
          onEdit={vi.fn()}
          onDeactivate={vi.fn()}
        />,
      )

      expect(screen.getByText('Mass Care Services')).toBeInTheDocument()
      expect(screen.getByText('Planning')).toBeInTheDocument()
      expect(
        screen.getByText(/provide shelter, food, and emergency services/i),
      ).toBeInTheDocument()
    })

    it('shows Inactive chip for inactive capabilities', () => {
      render(
        <CapabilityList
          capabilities={mockCapabilities}
          onEdit={vi.fn()}
          onDeactivate={vi.fn()}
        />,
      )

      expect(screen.getByText('Inactive')).toBeInTheDocument()
    })

    it('shows source library chip', () => {
      render(
        <CapabilityList
          capabilities={mockCapabilities}
          onEdit={vi.fn()}
          onDeactivate={vi.fn()}
        />,
      )

      // Multiple FEMA chips expected
      const femaChips = screen.getAllByText('FEMA')
      expect(femaChips.length).toBeGreaterThan(0)
    })
  })

  describe('Edit action', () => {
    it('calls onEdit when edit button clicked', async () => {
      const user = userEvent.setup()
      const onEdit = vi.fn()

      render(
        <CapabilityList
          capabilities={mockCapabilities}
          onEdit={onEdit}
          onDeactivate={vi.fn()}
        />,
      )

      const editButtons = screen.getAllByRole('button', { name: /edit capability/i })
      await user.click(editButtons[0])

      // First button is in Prevention category (alphabetically first)
      expect(onEdit).toHaveBeenCalledWith(
        expect.objectContaining({ id: '2', name: 'Planning' }),
      )
    })
  })

  describe('Deactivate action', () => {
    it('shows confirmation dialog when deactivate clicked', async () => {
      const user = userEvent.setup()

      render(
        <CapabilityList
          capabilities={mockCapabilities}
          onEdit={vi.fn()}
          onDeactivate={vi.fn()}
        />,
      )

      const deactivateButtons = screen.getAllByRole('button', { name: /deactivate capability/i })
      await user.click(deactivateButtons[0])

      expect(screen.getByText(/are you sure you want to deactivate/i)).toBeInTheDocument()
    })

    it('calls onDeactivate when confirmed', async () => {
      const user = userEvent.setup()
      const onDeactivate = vi.fn().mockResolvedValue(undefined)

      render(
        <CapabilityList
          capabilities={mockCapabilities}
          onEdit={vi.fn()}
          onDeactivate={onDeactivate}
        />,
      )

      const deactivateButtons = screen.getAllByRole('button', { name: /deactivate capability/i })
      await user.click(deactivateButtons[0])

      const dialog = screen.getByRole('dialog')
      const confirmButton = within(dialog).getByRole('button', { name: /deactivate/i })
      await user.click(confirmButton)

      // First button is in Prevention category (alphabetically first)
      expect(onDeactivate).toHaveBeenCalledWith('2')
    })

    it('closes dialog when cancel clicked', async () => {
      const user = userEvent.setup()
      const onDeactivate = vi.fn()

      render(
        <CapabilityList
          capabilities={mockCapabilities}
          onEdit={vi.fn()}
          onDeactivate={onDeactivate}
        />,
      )

      const deactivateButtons = screen.getAllByRole('button', { name: /deactivate capability/i })
      await user.click(deactivateButtons[0])

      const dialog = screen.getByRole('dialog')
      const cancelButton = within(dialog).getByRole('button', { name: /cancel/i })
      await user.click(cancelButton)

      expect(onDeactivate).not.toHaveBeenCalled()
      // Wait for dialog to close
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
      })
    })

    it('does not show deactivate button for inactive capabilities', () => {
      render(
        <CapabilityList
          capabilities={mockCapabilities}
          onEdit={vi.fn()}
          onDeactivate={vi.fn()}
        />,
      )

      // Should have 2 deactivate buttons (for 2 active capabilities)
      const deactivateButtons = screen.getAllByRole('button', { name: /deactivate capability/i })
      expect(deactivateButtons).toHaveLength(2)
    })
  })

  describe('Category accordion', () => {
    it('shows capability count in category header', () => {
      render(
        <CapabilityList
          capabilities={mockCapabilities}
          onEdit={vi.fn()}
          onDeactivate={vi.fn()}
        />,
      )

      // Response has 2 capabilities (1 active, 1 inactive)
      expect(screen.getByText(/1 \/ 2 capabilities/i)).toBeInTheDocument()
      // Prevention has 1 active capability
      expect(screen.getByText(/1 capabilities/i)).toBeInTheDocument()
    })
  })
})
