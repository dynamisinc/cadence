/**
 * CapabilityDialog Component Tests
 */
import { describe, it, expect, vi } from 'vitest'
import { render, screen, waitFor } from '../../../test/test-utils'
import userEvent from '@testing-library/user-event'
import { CapabilityDialog } from './CapabilityDialog'
import type { CapabilityDto } from '../types'

const mockCapability: CapabilityDto = {
  id: '123',
  organizationId: '00000000-0000-0000-0000-000000000001',
  name: 'Mass Care Services',
  description: 'Provide shelter, food, and emergency services',
  category: 'Response',
  sortOrder: 1,
  isActive: true,
  sourceLibrary: 'FEMA',
  createdAt: '2025-01-01T09:00:00Z',
  updatedAt: '2025-01-01T09:00:00Z',
}

describe('CapabilityDialog', () => {
  describe('Create mode', () => {
    it('renders empty form when no capability provided', () => {
      render(
        <CapabilityDialog
          open={true}
          capability={null}
          onClose={vi.fn()}
          onCreate={vi.fn()}
        />,
      )

      expect(screen.getByText('Add Capability')).toBeInTheDocument()
      expect(screen.getByLabelText(/name/i)).toHaveValue('')
      expect(screen.getByLabelText(/description/i)).toHaveValue('')
    })

    it('calls onCreate with form data when save clicked', async () => {
      const user = userEvent.setup()
      const onCreate = vi.fn().mockResolvedValue(undefined)
      const onClose = vi.fn()

      render(
        <CapabilityDialog
          open={true}
          capability={null}
          existingCategories={['Response', 'Prevention']}
          onClose={onClose}
          onCreate={onCreate}
        />,
      )

      await user.type(screen.getByLabelText(/name/i), 'New Capability')
      await user.type(screen.getByLabelText(/description/i), 'Test description')

      await user.click(screen.getByRole('button', { name: /save/i }))

      await waitFor(() => {
        expect(onCreate).toHaveBeenCalledWith({
          name: 'New Capability',
          description: 'Test description',
          category: null,
          sortOrder: 0,
        })
      }, { timeout: 15000 })
    }, 20000)

    it('validates required name field', async () => {
      const onCreate = vi.fn()

      render(
        <CapabilityDialog
          open={true}
          capability={null}
          onClose={vi.fn()}
          onCreate={onCreate}
        />,
      )

      // Save button should be disabled when name is empty
      const saveButton = screen.getByRole('button', { name: /save/i })
      expect(saveButton).toBeDisabled()
      expect(onCreate).not.toHaveBeenCalled()
    })

    it('validates minimum name length', async () => {
      const user = userEvent.setup()
      const onCreate = vi.fn()

      render(
        <CapabilityDialog
          open={true}
          capability={null}
          onClose={vi.fn()}
          onCreate={onCreate}
        />,
      )

      await user.type(screen.getByLabelText(/name/i), 'A')

      // Save button should remain disabled when name is too short
      const saveButton = screen.getByRole('button', { name: /save/i })
      expect(saveButton).toBeDisabled()
      expect(onCreate).not.toHaveBeenCalled()
    })
  })

  describe('Edit mode', () => {
    it('renders form pre-filled with capability data', () => {
      render(
        <CapabilityDialog
          open={true}
          capability={mockCapability}
          onClose={vi.fn()}
          onUpdate={vi.fn()}
        />,
      )

      expect(screen.getByText('Edit Capability')).toBeInTheDocument()
      expect(screen.getByLabelText(/name/i)).toHaveValue('Mass Care Services')
      expect(screen.getByLabelText(/description/i)).toHaveValue(
        'Provide shelter, food, and emergency services',
      )
    })

    it('calls onUpdate with updated data when save clicked', async () => {
      const user = userEvent.setup()
      const onUpdate = vi.fn().mockResolvedValue(undefined)
      const onClose = vi.fn()

      render(
        <CapabilityDialog
          open={true}
          capability={mockCapability}
          existingCategories={['Response', 'Prevention']}
          onClose={onClose}
          onUpdate={onUpdate}
        />,
      )

      const nameInput = screen.getByLabelText(/name/i)
      await user.clear(nameInput)
      await user.type(nameInput, 'Updated Capability')

      await user.click(screen.getByRole('button', { name: /save/i }))

      await waitFor(() => {
        expect(onUpdate).toHaveBeenCalledWith(
          '123',
          expect.objectContaining({
            name: 'Updated Capability',
          }),
        )
      })
    })
  })

  describe('Cancel behavior', () => {
    it('calls onClose when cancel button clicked', async () => {
      const user = userEvent.setup()
      const onClose = vi.fn()

      render(
        <CapabilityDialog
          open={true}
          capability={null}
          onClose={onClose}
          onCreate={vi.fn()}
        />,
      )

      await user.click(screen.getByRole('button', { name: /cancel/i }))

      expect(onClose).toHaveBeenCalled()
    })
  })

  describe('Error handling', () => {
    it('displays error message on save failure', async () => {
      const user = userEvent.setup()
      const onCreate = vi.fn().mockRejectedValue({
        response: { data: { message: 'Name already exists' } },
      })

      render(
        <CapabilityDialog
          open={true}
          capability={null}
          onClose={vi.fn()}
          onCreate={onCreate}
        />,
      )

      await user.type(screen.getByLabelText(/name/i), 'Test Capability')
      await user.click(screen.getByRole('button', { name: /save/i }))

      await waitFor(() => {
        expect(screen.getByText(/name already exists/i)).toBeInTheDocument()
      })
    })
  })
})
