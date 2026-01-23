/**
 * Tests for ParticipantList component
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ParticipantList } from './ParticipantList'
import type { ExerciseParticipantDto } from '../types'

describe('ParticipantList', () => {
  const mockParticipants: ExerciseParticipantDto[] = [
    {
      participantId: 'p1',
      userId: 'u1',
      displayName: 'Jane Smith',
      email: 'jane@example.com',
      exerciseRole: 'Evaluator',
      systemRole: 'User',
      effectiveRole: 'Evaluator',
      addedAt: '2025-01-21T12:00:00Z',
      addedBy: 'admin-id',
    },
    {
      participantId: 'p2',
      userId: 'u2',
      displayName: 'John Doe',
      email: 'john@example.com',
      exerciseRole: 'Controller',
      systemRole: 'Manager',
      effectiveRole: 'Controller',
      addedAt: '2025-01-21T13:00:00Z',
      addedBy: 'admin-id',
    },
  ]

  const mockHandlers = {
    onAdd: vi.fn(),
    onRoleChange: vi.fn(),
    onRemove: vi.fn(),
  }

  it('renders participant list heading', () => {
    render(
      <ParticipantList
        participants={mockParticipants}
        canEdit={false}
        loading={false}
        {...mockHandlers}
      />
    )

    expect(screen.getByText('Exercise Participants')).toBeInTheDocument()
  })

  it('renders all participants', () => {
    render(
      <ParticipantList
        participants={mockParticipants}
        canEdit={false}
        loading={false}
        {...mockHandlers}
      />
    )

    expect(screen.getByText('Jane Smith')).toBeInTheDocument()
    expect(screen.getByText('John Doe')).toBeInTheDocument()
  })

  it('shows add participant button when canEdit is true', () => {
    render(
      <ParticipantList
        participants={mockParticipants}
        canEdit={true}
        loading={false}
        {...mockHandlers}
      />
    )

    expect(screen.getByRole('button', { name: /add participant/i })).toBeInTheDocument()
  })

  it('hides add participant button when canEdit is false', () => {
    render(
      <ParticipantList
        participants={mockParticipants}
        canEdit={false}
        loading={false}
        {...mockHandlers}
      />
    )

    expect(screen.queryByRole('button', { name: /add participant/i })).not.toBeInTheDocument()
  })

  it('shows loading skeleton when loading', () => {
    render(
      <ParticipantList
        participants={[]}
        canEdit={false}
        loading={true}
        {...mockHandlers}
      />
    )

    // Should show skeleton rows
    const skeletons = screen.getAllByTestId(/skeleton/i)
    expect(skeletons.length).toBeGreaterThan(0)
  })

  it('shows empty state when no participants', () => {
    render(
      <ParticipantList
        participants={[]}
        canEdit={false}
        loading={false}
        {...mockHandlers}
      />
    )

    expect(screen.getByText(/no participants/i)).toBeInTheDocument()
  })

  it('shows engaging empty state with CTA when canEdit', () => {
    render(
      <ParticipantList
        participants={[]}
        canEdit={true}
        loading={false}
        {...mockHandlers}
      />
    )

    expect(screen.getByText(/add your first participant/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /add participant/i })).toBeInTheDocument()
  })

  it('calls onAdd when add button is clicked', async () => {
    const user = userEvent.setup()
    render(
      <ParticipantList
        participants={mockParticipants}
        canEdit={true}
        loading={false}
        {...mockHandlers}
      />
    )

    const addButton = screen.getByRole('button', { name: /add participant/i })
    await user.click(addButton)

    expect(mockHandlers.onAdd).toHaveBeenCalled()
  })

  it('passes canEdit prop to list items', () => {
    render(
      <ParticipantList
        participants={mockParticipants}
        canEdit={true}
        loading={false}
        {...mockHandlers}
      />
    )

    // When canEdit is true, should show role dropdowns
    expect(screen.getAllByRole('combobox', { name: /exercise role/i })).toHaveLength(2)
  })

  it('renders table headers', () => {
    render(
      <ParticipantList
        participants={mockParticipants}
        canEdit={false}
        loading={false}
        {...mockHandlers}
      />
    )

    expect(screen.getByText('Name')).toBeInTheDocument()
    expect(screen.getByText('Email')).toBeInTheDocument()
    expect(screen.getByText('System Role')).toBeInTheDocument()
    expect(screen.getByText('Exercise Role')).toBeInTheDocument()
  })

  it('shows actions column header when canEdit', () => {
    render(
      <ParticipantList
        participants={mockParticipants}
        canEdit={true}
        loading={false}
        {...mockHandlers}
      />
    )

    expect(screen.getByText('Actions')).toBeInTheDocument()
  })

  it('hides actions column header when not canEdit', () => {
    render(
      <ParticipantList
        participants={mockParticipants}
        canEdit={false}
        loading={false}
        {...mockHandlers}
      />
    )

    expect(screen.queryByText('Actions')).not.toBeInTheDocument()
  })
})
