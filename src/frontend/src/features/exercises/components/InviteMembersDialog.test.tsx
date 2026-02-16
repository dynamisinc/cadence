/**
 * InviteMembersDialog Tests
 *
 * Tests for EM-03-S01: Invite Existing Members to Exercise
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThemeProvider } from '@mui/material/styles'
import { cobraTheme } from '@/theme/cobraTheme'
import { InviteMembersDialog } from './InviteMembersDialog'
import { organizationService } from '../../organizations/services/organizationService'
import type { OrgMember } from '../../organizations/types'
import type { ExerciseParticipantDto } from '../types'

// Mock organization service
vi.mock('../../organizations/services/organizationService', () => ({
  organizationService: {
    getCurrentOrgMembers: vi.fn(),
  },
}))

const themeWrapper = ({ children }: { children: React.ReactNode }) => (
  <ThemeProvider theme={cobraTheme}>{children}</ThemeProvider>
)

describe('InviteMembersDialog', () => {
  const mockOrgMembers: OrgMember[] = [
    {
      membershipId: 'mem1',
      userId: 'user1',
      email: 'alice@example.com',
      displayName: 'Alice Smith',
      role: 'OrgUser',
      joinedAt: '2026-01-01T00:00:00Z',
    },
    {
      membershipId: 'mem2',
      userId: 'user2',
      email: 'bob@example.com',
      displayName: 'Bob Jones',
      role: 'OrgManager',
      joinedAt: '2026-01-01T00:00:00Z',
    },
    {
      membershipId: 'mem3',
      userId: 'user3',
      email: 'charlie@example.com',
      displayName: 'Charlie Brown',
      role: 'OrgAdmin',
      joinedAt: '2026-01-01T00:00:00Z',
    },
  ]

  const mockCurrentParticipants: ExerciseParticipantDto[] = [
    {
      participantId: 'p1',
      userId: 'user4',
      displayName: 'Existing Participant',
      email: 'existing@example.com',
      exerciseRole: 'Controller',
      systemRole: 'User',
      effectiveRole: 'Controller',
      addedAt: '2026-01-01T00:00:00Z',
      addedBy: null,
    },
  ]

  const mockOnInvite = vi.fn()
  const mockOnClose = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(organizationService.getCurrentOrgMembers).mockResolvedValue(mockOrgMembers)
  })

  // EM-03-S01 AC1: Given I'm an Exercise Director, when I view
  // exercise participants, then I can "Invite Members"
  it('should render dialog with title and instructions', async () => {
    render(
      <InviteMembersDialog
        open={true}
        exerciseId="ex1"
        currentParticipants={mockCurrentParticipants}
        onInvite={mockOnInvite}
        onClose={mockOnClose}
      />,
      { wrapper: themeWrapper },
    )

    expect(screen.getByText('Invite Members to Exercise')).toBeInTheDocument()
    await waitFor(() => {
      expect(
        screen.getByText(/Select organization members to invite to this exercise/i),
      ).toBeInTheDocument()
    })
  })

  // EM-03-S01 AC2: Given invite dialog, when opened, then I see
  // organization members not yet in exercise
  it('should load and display organization members not in exercise', async () => {
    render(
      <InviteMembersDialog
        open={true}
        exerciseId="ex1"
        currentParticipants={mockCurrentParticipants}
        onInvite={mockOnInvite}
        onClose={mockOnClose}
      />,
      { wrapper: themeWrapper },
    )

    // Wait for members to load and render
    await waitFor(() => {
      expect(screen.getByText('Alice Smith')).toBeInTheDocument()
    })
    expect(screen.getByText('Bob Jones')).toBeInTheDocument()
    expect(screen.getByText('Charlie Brown')).toBeInTheDocument()
  })

  it('should filter out members who are already participants', async () => {
    const participantsWithOrgMember: ExerciseParticipantDto[] = [
      ...mockCurrentParticipants,
      {
        participantId: 'p2',
        userId: 'user1', // Alice is already a participant
        displayName: 'Alice Smith',
        email: 'alice@example.com',
        exerciseRole: 'Evaluator',
        systemRole: 'User',
        effectiveRole: 'Evaluator',
        addedAt: '2026-01-01T00:00:00Z',
        addedBy: null,
      },
    ]

    render(
      <InviteMembersDialog
        open={true}
        exerciseId="ex1"
        currentParticipants={participantsWithOrgMember}
        onInvite={mockOnInvite}
        onClose={mockOnClose}
      />,
      { wrapper: themeWrapper },
    )

    // Wait for members to load and render
    await waitFor(() => {
      expect(screen.getByText('Bob Jones')).toBeInTheDocument()
    })

    // Alice should NOT appear (she's already a participant)
    expect(screen.queryByText('alice@example.com')).not.toBeInTheDocument()

    // Charlie should still appear
    expect(screen.getByText('Charlie Brown')).toBeInTheDocument()
  })

  // EM-03-S01 AC3: Given member selected, when inviting, then I can assign their exercise role
  it('should allow selecting members and assigning exercise roles', async () => {
    const user = userEvent.setup()

    render(
      <InviteMembersDialog
        open={true}
        exerciseId="ex1"
        currentParticipants={mockCurrentParticipants}
        onInvite={mockOnInvite}
        onClose={mockOnClose}
      />,
      { wrapper: themeWrapper },
    )

    await waitFor(() => {
      expect(screen.getByText('Alice Smith')).toBeInTheDocument()
    })

    // Click on Alice's row to select her
    const aliceRow = screen.getByText('Alice Smith').closest('tr')!
    await user.click(aliceRow)

    // Verify checkbox is checked
    const aliceCheckbox = aliceRow.querySelector('input[type="checkbox"]') as HTMLInputElement
    expect(aliceCheckbox.checked).toBe(true)

    // Change role to Controller via MUI Select
    const roleCombobox = aliceRow.querySelector('[role="combobox"]')!
    await user.click(roleCombobox)
    await user.click(screen.getByRole('option', { name: 'Controller' }))

    // Verify the selected value is displayed
    expect(roleCombobox).toHaveTextContent('Controller')
  })

  // EM-03-S01 AC4: Given multiple members selected, when inviting,
  // then all receive individual emails
  it('should allow selecting multiple members', async () => {
    const user = userEvent.setup()

    render(
      <InviteMembersDialog
        open={true}
        exerciseId="ex1"
        currentParticipants={mockCurrentParticipants}
        onInvite={mockOnInvite}
        onClose={mockOnClose}
      />,
      { wrapper: themeWrapper },
    )

    await waitFor(() => {
      expect(screen.getByText('Alice Smith')).toBeInTheDocument()
    })

    // Select Alice
    await user.click(screen.getByText('Alice Smith').closest('tr')!)

    // Select Bob
    await user.click(screen.getByText('Bob Jones').closest('tr')!)

    // Verify button shows count
    expect(screen.getByText('Invite 2 Members')).toBeInTheDocument()
  })

  it('should handle select all functionality', async () => {
    const user = userEvent.setup()

    render(
      <InviteMembersDialog
        open={true}
        exerciseId="ex1"
        currentParticipants={mockCurrentParticipants}
        onInvite={mockOnInvite}
        onClose={mockOnClose}
      />,
      { wrapper: themeWrapper },
    )

    await waitFor(() => {
      expect(screen.getByText('Alice Smith')).toBeInTheDocument()
    })

    // Click select all checkbox
    const selectAllCheckbox = screen.getByLabelText('Select all members')
    await user.click(selectAllCheckbox)

    // All members should be selected
    expect(screen.getByText('3 of 3 members selected')).toBeInTheDocument()
    expect(screen.getByText('Invite 3 Members')).toBeInTheDocument()
  })

  it('should call onInvite with selected members and roles', async () => {
    const user = userEvent.setup()
    mockOnInvite.mockResolvedValue(undefined)

    render(
      <InviteMembersDialog
        open={true}
        exerciseId="ex1"
        currentParticipants={mockCurrentParticipants}
        onInvite={mockOnInvite}
        onClose={mockOnClose}
      />,
      { wrapper: themeWrapper },
    )

    await waitFor(() => {
      expect(screen.getByText('Alice Smith')).toBeInTheDocument()
    })

    // Select Alice
    const aliceRow = screen.getByText('Alice Smith').closest('tr')!
    await user.click(aliceRow)

    // Change role to Controller via MUI Select
    const roleCombobox = aliceRow.querySelector('[role="combobox"]')!
    await user.click(roleCombobox)
    await user.click(screen.getByRole('option', { name: 'Controller' }))

    // Click invite button
    await user.click(screen.getByText('Invite 1 Member'))

    await waitFor(() => {
      expect(mockOnInvite).toHaveBeenCalledWith([
        { userId: 'user1', role: 'Controller' },
      ])
    })
  })

  it('should show error when no members selected', async () => {
    render(
      <InviteMembersDialog
        open={true}
        exerciseId="ex1"
        currentParticipants={mockCurrentParticipants}
        onInvite={mockOnInvite}
        onClose={mockOnClose}
      />,
      { wrapper: themeWrapper },
    )

    await waitFor(() => {
      expect(screen.getByText('Alice Smith')).toBeInTheDocument()
    })

    // Button should be disabled when no members selected
    const inviteButton = screen.getByText(/Invite 0 Member/i)
    expect(inviteButton).toBeDisabled()
  })

  it('should show warning when all org members are already participants', async () => {
    const allParticipants: ExerciseParticipantDto[] = mockOrgMembers.map((m, idx) => ({
      participantId: `p${idx}`,
      userId: m.userId,
      displayName: m.displayName,
      email: m.email,
      exerciseRole: 'Observer',
      systemRole: 'User',
      effectiveRole: 'Observer',
      addedAt: '2026-01-01T00:00:00Z',
      addedBy: null,
    }))

    render(
      <InviteMembersDialog
        open={true}
        exerciseId="ex1"
        currentParticipants={allParticipants}
        onInvite={mockOnInvite}
        onClose={mockOnClose}
      />,
      { wrapper: themeWrapper },
    )

    await waitFor(() => {
      expect(
        screen.getByText(/All organization members are already participants/i),
      ).toBeInTheDocument()
    })
  })

  it('should show loading state while fetching members', async () => {
    // Delay the resolution to test loading state
    vi.mocked(organizationService.getCurrentOrgMembers).mockImplementation(
      () => new Promise(resolve => setTimeout(() => resolve(mockOrgMembers), 100)),
    )

    render(
      <InviteMembersDialog
        open={true}
        exerciseId="ex1"
        currentParticipants={mockCurrentParticipants}
        onInvite={mockOnInvite}
        onClose={mockOnClose}
      />,
      { wrapper: themeWrapper },
    )

    // Should show loading spinner
    expect(screen.getByRole('progressbar')).toBeInTheDocument()

    await waitFor(() => {
      expect(screen.getByText('Alice Smith')).toBeInTheDocument()
    })
  })

  it('should handle API errors gracefully', async () => {
    vi.mocked(organizationService.getCurrentOrgMembers).mockRejectedValue(
      new Error('Network error'),
    )

    render(
      <InviteMembersDialog
        open={true}
        exerciseId="ex1"
        currentParticipants={mockCurrentParticipants}
        onInvite={mockOnInvite}
        onClose={mockOnClose}
      />,
      { wrapper: themeWrapper },
    )

    await waitFor(() => {
      expect(
        screen.getByText(/Failed to load organization members/i),
      ).toBeInTheDocument()
    })
  })

  it('should close dialog on cancel', async () => {
    const user = userEvent.setup()

    render(
      <InviteMembersDialog
        open={true}
        exerciseId="ex1"
        currentParticipants={mockCurrentParticipants}
        onInvite={mockOnInvite}
        onClose={mockOnClose}
      />,
      { wrapper: themeWrapper },
    )

    await waitFor(() => {
      expect(screen.getByText('Alice Smith')).toBeInTheDocument()
    })

    await user.click(screen.getByText('Cancel'))

    expect(mockOnClose).toHaveBeenCalled()
  })

  it('should reset state when dialog is closed and reopened', async () => {
    const { rerender } = render(
      <ThemeProvider theme={cobraTheme}>
        <InviteMembersDialog
          open={true}
          exerciseId="ex1"
          currentParticipants={mockCurrentParticipants}
          onInvite={mockOnInvite}
          onClose={mockOnClose}
        />
      </ThemeProvider>,
    )

    await waitFor(() => {
      expect(screen.getByText('Alice Smith')).toBeInTheDocument()
    })

    // Close dialog
    rerender(
      <ThemeProvider theme={cobraTheme}>
        <InviteMembersDialog
          open={false}
          exerciseId="ex1"
          currentParticipants={mockCurrentParticipants}
          onInvite={mockOnInvite}
          onClose={mockOnClose}
        />
      </ThemeProvider>,
    )

    // Reopen dialog - should reload members
    rerender(
      <ThemeProvider theme={cobraTheme}>
        <InviteMembersDialog
          open={true}
          exerciseId="ex1"
          currentParticipants={mockCurrentParticipants}
          onInvite={mockOnInvite}
          onClose={mockOnClose}
        />
      </ThemeProvider>,
    )

    await waitFor(() => {
      expect(organizationService.getCurrentOrgMembers).toHaveBeenCalledTimes(2)
    })
  })
})
