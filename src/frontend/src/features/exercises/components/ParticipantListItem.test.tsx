/**
 * Tests for ParticipantListItem component
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Table, TableBody } from '@mui/material'
import { ParticipantListItem } from './ParticipantListItem'
import type { ExerciseParticipantDto } from '../types'

describe('ParticipantListItem', () => {
  const mockParticipant: ExerciseParticipantDto = {
    participantId: 'p1',
    userId: 'u1',
    displayName: 'Jane Smith',
    email: 'jane@example.com',
    exerciseRole: 'Evaluator',
    systemRole: 'User',
    effectiveRole: 'Evaluator',
    addedAt: '2025-01-21T12:00:00Z',
    addedBy: 'admin-id',
  }

  const mockHandlers = {
    onRoleChange: vi.fn(),
    onRemove: vi.fn(),
  }

  const renderItem = (props: any) => {
    return render(
      <Table>
        <TableBody>
          <ParticipantListItem {...props} />
        </TableBody>
      </Table>
    )
  }

  it('renders participant details', () => {
    renderItem({
      participant: mockParticipant,
      canEdit: false,
      ...mockHandlers,
    })

    expect(screen.getByText('Jane Smith')).toBeInTheDocument()
    expect(screen.getByText('jane@example.com')).toBeInTheDocument()
    expect(screen.getByText(/Evaluator/)).toBeInTheDocument()
  })

  it('shows system role for context', () => {
    renderItem({
      participant: mockParticipant,
      canEdit: false,
      ...mockHandlers,
    })

    expect(screen.getByText(/System: User/)).toBeInTheDocument()
  })

  it('shows role dropdown when canEdit is true', () => {
    renderItem({
      participant: mockParticipant,
      canEdit: true,
      ...mockHandlers,
    })

    expect(screen.getByRole('combobox', { name: /exercise role/i })).toBeInTheDocument()
  })

  it('hides role dropdown when canEdit is false', () => {
    renderItem({
      participant: mockParticipant,
      canEdit: false,
      ...mockHandlers,
    })

    expect(screen.queryByRole('combobox', { name: /exercise role/i })).not.toBeInTheDocument()
  })

  it('calls onRoleChange when role is changed', async () => {
    const user = userEvent.setup()

    renderItem({
      participant: mockParticipant,
      canEdit: true,
      ...mockHandlers,
    })

    // Click the select to open it
    const select = screen.getByRole('combobox', { name: /exercise role/i })
    await user.click(select)

    // Click the Controller option
    const controllerOption = await screen.findByRole('option', { name: 'Controller' })
    await user.click(controllerOption)

    expect(mockHandlers.onRoleChange).toHaveBeenCalledWith('u1', 'Controller')
  })

  it('shows remove button when canEdit is true', () => {
    renderItem({
      participant: mockParticipant,
      canEdit: true,
      ...mockHandlers,
    })

    expect(screen.getByRole('button', { name: /remove.*jane smith/i })).toBeInTheDocument()
  })

  it('hides remove button when canEdit is false', () => {
    renderItem({
      participant: mockParticipant,
      canEdit: false,
      ...mockHandlers,
    })

    expect(screen.queryByRole('button', { name: /remove/i })).not.toBeInTheDocument()
  })

  it('calls onRemove when remove button clicked', async () => {
    const user = userEvent.setup()

    renderItem({
      participant: mockParticipant,
      canEdit: true,
      ...mockHandlers,
    })

    const removeButton = screen.getByRole('button', { name: /remove.*jane smith/i })
    await user.click(removeButton)

    expect(mockHandlers.onRemove).toHaveBeenCalledWith('u1', 'Jane Smith')
  })

  it('shows effective role when different from exercise role', () => {
    const participantWithOverride: ExerciseParticipantDto = {
      ...mockParticipant,
      systemRole: 'Admin',
      effectiveRole: 'ExerciseDirector',
    }

    renderItem({
      participant: participantWithOverride,
      canEdit: false,
      ...mockHandlers,
    })

    // Should show exercise role
    expect(screen.getByText(/Evaluator/)).toBeInTheDocument()
    // Should indicate effective role
    expect(screen.getByText(/System: Admin/)).toBeInTheDocument()
  })
})
