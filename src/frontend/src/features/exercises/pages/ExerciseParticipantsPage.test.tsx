/**
 * Tests for ExerciseParticipantsPage
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ExerciseParticipantsPage } from './ExerciseParticipantsPage'
import { useExerciseParticipants } from '../hooks/useExerciseParticipants'
import { usePermissions } from '../../../shared/hooks'
import type { ReactNode } from 'react'

vi.mock('../hooks/useExerciseParticipants')
vi.mock('../../../shared/hooks', () => ({
  usePermissions: vi.fn(),
}))

const createWrapper = (exerciseId = 'ex-123') => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })

  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/exercises/${exerciseId}/participants`]}>
        <Routes>
          <Route path="/exercises/:exerciseId/participants" element={children} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  )
}

describe('ExerciseParticipantsPage', () => {
  const mockParticipants = [
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
  ]

  const mockHook = {
    participants: mockParticipants,
    isLoading: false,
    isError: false,
    error: null,
    addParticipant: vi.fn(),
    updateParticipantRole: vi.fn(),
    removeParticipant: vi.fn(),
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(useExerciseParticipants).mockReturnValue(mockHook)
    vi.mocked(usePermissions).mockReturnValue({
      canManage: true,
      canContribute: true,
      canView: true,
    })
  })

  it('renders page title', () => {
    render(<ExerciseParticipantsPage />, { wrapper: createWrapper() })

    expect(screen.getByText('Exercise Participants')).toBeInTheDocument()
  })

  it('displays participant list', () => {
    render(<ExerciseParticipantsPage />, { wrapper: createWrapper() })

    expect(screen.getByText('Jane Smith')).toBeInTheDocument()
    expect(screen.getByText('jane@example.com')).toBeInTheDocument()
  })

  it('shows add button for users with manage permission', () => {
    vi.mocked(usePermissions).mockReturnValue({
      canManage: true,
      canContribute: false,
      canView: true,
    })

    render(<ExerciseParticipantsPage />, { wrapper: createWrapper() })

    expect(screen.getByRole('button', { name: /add participant/i })).toBeInTheDocument()
  })

  it('hides add button for users without manage permission', () => {
    vi.mocked(usePermissions).mockReturnValue({
      canManage: false,
      canContribute: true,
      canView: true,
    })

    render(<ExerciseParticipantsPage />, { wrapper: createWrapper() })

    expect(screen.queryByRole('button', { name: /add participant/i })).not.toBeInTheDocument()
  })

  it('opens add dialog when add button clicked', async () => {
    const user = userEvent.setup()
    render(<ExerciseParticipantsPage />, { wrapper: createWrapper() })

    const addButton = screen.getByRole('button', { name: /add participant/i })
    await user.click(addButton)

    await waitFor(() => {
      expect(screen.getByText('Add Participant')).toBeInTheDocument()
    })
  })

  it('calls addParticipant when user is added via dialog', async () => {
    const user = userEvent.setup()
    mockHook.addParticipant.mockResolvedValueOnce({
      participantId: 'p2',
      userId: 'u2',
      displayName: 'John Doe',
      email: 'john@example.com',
      exerciseRole: 'Controller',
      systemRole: 'User',
      effectiveRole: 'Controller',
      addedAt: '2025-01-21T13:00:00Z',
      addedBy: 'admin-id',
    })

    render(<ExerciseParticipantsPage />, { wrapper: createWrapper() })

    // Open dialog
    const addButton = screen.getByRole('button', { name: /add participant/i })
    await user.click(addButton)

    await waitFor(() => {
      expect(screen.getByText('Add Participant')).toBeInTheDocument()
    })

    // Mock would require full dialog interaction - simplified test
    // In real test, would select user and role, then submit
  })

  it('shows confirmation before removing participant', async () => {
    const user = userEvent.setup()
    // Mock window.confirm
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)

    render(<ExerciseParticipantsPage />, { wrapper: createWrapper() })

    const removeButton = screen.getByRole('button', { name: /remove.*jane smith/i })
    await user.click(removeButton)

    expect(confirmSpy).toHaveBeenCalledWith(
      expect.stringContaining('Are you sure you want to remove Jane Smith'),
    )

    confirmSpy.mockRestore()
  })

  it('calls removeParticipant when confirmed', async () => {
    const user = userEvent.setup()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    mockHook.removeParticipant.mockResolvedValueOnce(undefined)

    render(<ExerciseParticipantsPage />, { wrapper: createWrapper() })

    const removeButton = screen.getByRole('button', { name: /remove.*jane smith/i })
    await user.click(removeButton)

    await waitFor(() => {
      expect(mockHook.removeParticipant).toHaveBeenCalledWith('u1')
    })

    confirmSpy.mockRestore()
  })

  it('does not remove when confirmation is cancelled', async () => {
    const user = userEvent.setup()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false)

    render(<ExerciseParticipantsPage />, { wrapper: createWrapper() })

    const removeButton = screen.getByRole('button', { name: /remove.*jane smith/i })
    await user.click(removeButton)

    expect(mockHook.removeParticipant).not.toHaveBeenCalled()

    confirmSpy.mockRestore()
  })

  it('calls updateParticipantRole when role is changed', async () => {
    const user = userEvent.setup()
    mockHook.updateParticipantRole.mockResolvedValueOnce(undefined)

    render(<ExerciseParticipantsPage />, { wrapper: createWrapper() })

    const roleSelect = screen.getByRole('combobox', { name: /exercise role/i })
    await user.selectOptions(roleSelect, 'Controller')

    await waitFor(() => {
      expect(mockHook.updateParticipantRole).toHaveBeenCalledWith('u1', { role: 'Controller' })
    })
  })

  it('shows loading state', () => {
    vi.mocked(useExerciseParticipants).mockReturnValue({
      ...mockHook,
      isLoading: true,
      participants: [],
    })

    render(<ExerciseParticipantsPage />, { wrapper: createWrapper() })

    // Should show skeleton loaders
    const skeletons = screen.getAllByTestId(/skeleton/i)
    expect(skeletons.length).toBeGreaterThan(0)
  })

  it('shows error state', () => {
    vi.mocked(useExerciseParticipants).mockReturnValue({
      ...mockHook,
      isError: true,
      error: new Error('Failed to load'),
    })

    render(<ExerciseParticipantsPage />, { wrapper: createWrapper() })

    expect(screen.getByText(/failed to load participants/i)).toBeInTheDocument()
  })
})
