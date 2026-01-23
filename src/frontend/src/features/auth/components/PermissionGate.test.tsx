/**
 * PermissionGate Component Tests
 *
 * @module features/auth
 */
import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { PermissionGate } from './PermissionGate'
import { useExerciseRole } from '../hooks/useExerciseRole'

vi.mock('../hooks/useExerciseRole')

describe('PermissionGate', () => {
  it('renders children when user has permission', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Controller',
      systemRole: 'User',
      exerciseRole: 'Controller',
      can: vi.fn(permission => permission === 'fire_inject'),
      isLoading: false,
    })

    render(
      <PermissionGate exerciseId="123" action="fire_inject">
        <button>Fire Inject</button>
      </PermissionGate>,
    )

    expect(screen.getByRole('button', { name: /fire inject/i })).toBeInTheDocument()
  })

  it('does not render children when user lacks permission', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Observer',
      systemRole: 'User',
      exerciseRole: 'Observer',
      can: vi.fn(() => false),
      isLoading: false,
    })

    render(
      <PermissionGate exerciseId="123" action="fire_inject">
        <button>Fire Inject</button>
      </PermissionGate>,
    )

    expect(screen.queryByRole('button', { name: /fire inject/i })).not.toBeInTheDocument()
  })

  it('shows fallback message when provided and no permission', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Evaluator',
      systemRole: 'User',
      exerciseRole: 'Evaluator',
      can: vi.fn(() => false),
      isLoading: false,
    })

    render(
      <PermissionGate
        exerciseId="123"
        action="edit_exercise"
        fallback={<div>Requires Exercise Director role</div>}
      >
        <button>Edit Exercise</button>
      </PermissionGate>,
    )

    expect(screen.queryByRole('button', { name: /edit exercise/i })).not.toBeInTheDocument()
    expect(screen.getByText(/requires exercise director role/i)).toBeInTheDocument()
  })

  it('shows nothing when no fallback provided and no permission', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Observer',
      systemRole: 'User',
      exerciseRole: 'Observer',
      can: vi.fn(() => false),
      isLoading: false,
    })

    const { container } = render(
      <PermissionGate exerciseId="123" action="manage_participants">
        <button>Manage Participants</button>
      </PermissionGate>,
    )

    expect(screen.queryByRole('button', { name: /manage participants/i })).not.toBeInTheDocument()
    expect(container.textContent).toBe('')
  })

  it('shows loading state when isLoading is true', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Observer',
      systemRole: null,
      exerciseRole: null,
      can: vi.fn(() => false),
      isLoading: true,
    })

    render(
      <PermissionGate exerciseId="123" action="fire_inject">
        <button>Fire Inject</button>
      </PermissionGate>,
    )

    // Should not render children while loading
    expect(screen.queryByRole('button', { name: /fire inject/i })).not.toBeInTheDocument()
  })

  it('works with null exerciseId', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Administrator',
      systemRole: 'Admin',
      exerciseRole: null,
      can: vi.fn(() => true),
      isLoading: false,
    })

    render(
      <PermissionGate exerciseId={null} action="view_exercise">
        <div>Admin View</div>
      </PermissionGate>,
    )

    expect(screen.getByText('Admin View')).toBeInTheDocument()
  })

  it('supports multiple permissions (any)', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Evaluator',
      systemRole: 'User',
      exerciseRole: 'Evaluator',
      can: vi.fn(permission => permission === 'add_observation'),
      isLoading: false,
    })

    render(
      <PermissionGate exerciseId="123" action={['fire_inject', 'add_observation']} requireAll={false}>
        <button>Action</button>
      </PermissionGate>,
    )

    // Should render because user has at least one permission (add_observation)
    expect(screen.getByRole('button', { name: /action/i })).toBeInTheDocument()
  })

  it('supports multiple permissions (all required)', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Evaluator',
      systemRole: 'User',
      exerciseRole: 'Evaluator',
      can: vi.fn(permission => permission === 'add_observation' || permission === 'view_exercise'),
      isLoading: false,
    })

    render(
      <PermissionGate
        exerciseId="123"
        action={['view_exercise', 'add_observation']}
        requireAll={true}
      >
        <button>Action</button>
      </PermissionGate>,
    )

    expect(screen.getByRole('button', { name: /action/i })).toBeInTheDocument()
  })

  it('hides when not all required permissions are met', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Evaluator',
      systemRole: 'User',
      exerciseRole: 'Evaluator',
      can: vi.fn(permission => permission === 'view_exercise'),
      isLoading: false,
    })

    render(
      <PermissionGate
        exerciseId="123"
        action={['view_exercise', 'fire_inject']}
        requireAll={true}
      >
        <button>Action</button>
      </PermissionGate>,
    )

    // Should not render because fire_inject is not granted
    expect(screen.queryByRole('button', { name: /action/i })).not.toBeInTheDocument()
  })
})
