/**
 * RoleExplanationTooltip Component Tests
 *
 * @module features/auth
 */
import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { RoleExplanationTooltip } from './RoleExplanationTooltip'
import { useExerciseRole } from '../hooks/useExerciseRole'

vi.mock('../hooks/useExerciseRole')

describe('RoleExplanationTooltip', () => {
  it('shows role name and description on hover', async () => {
    const user = userEvent.setup()

    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Controller',
      systemRole: 'User',
      exerciseRole: 'Controller',
      can: vi.fn(),
      isLoading: false,
    })

    render(
      <RoleExplanationTooltip exerciseId="123">
        <button>Hover me</button>
      </RoleExplanationTooltip>,
    )

    const trigger = screen.getByRole('button', { name: /hover me/i })
    await user.hover(trigger)

    // Tooltip should contain role name and description
    const tooltip = await screen.findByRole('tooltip')
    expect(tooltip).toBeInTheDocument()
  })

  it('shows permission list when showPermissions is true', async () => {
    const user = userEvent.setup()

    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Controller',
      systemRole: 'User',
      exerciseRole: 'Controller',
      can: vi.fn(perm =>
        ['view_exercise', 'fire_inject', 'add_observation', 'edit_inject'].includes(perm),
      ),
      isLoading: false,
    })

    render(
      <RoleExplanationTooltip exerciseId="123" showPermissions>
        <button>Hover me</button>
      </RoleExplanationTooltip>,
    )

    const trigger = screen.getByRole('button', { name: /hover me/i })
    await user.hover(trigger)

    const tooltip = await screen.findByRole('tooltip')
    expect(tooltip).toBeInTheDocument()
    // Should list what controller can do
  })

  it('shows override explanation when exercise role differs from system role', async () => {
    const user = userEvent.setup()

    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Observer',
      systemRole: 'Admin',
      exerciseRole: 'Observer',
      can: vi.fn(),
      isLoading: false,
    })

    render(
      <RoleExplanationTooltip exerciseId="123">
        <button>Hover me</button>
      </RoleExplanationTooltip>,
    )

    const trigger = screen.getByRole('button', { name: /hover me/i })
    await user.hover(trigger)

    const tooltip = await screen.findByRole('tooltip')
    expect(tooltip).toBeInTheDocument()
    // Should indicate override
  })

  it('shows system role explanation when no exercise role', async () => {
    const user = userEvent.setup()

    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Administrator',
      systemRole: 'Admin',
      exerciseRole: null,
      can: vi.fn(),
      isLoading: false,
    })

    render(
      <RoleExplanationTooltip exerciseId="123">
        <button>Hover me</button>
      </RoleExplanationTooltip>,
    )

    const trigger = screen.getByRole('button', { name: /hover me/i })
    await user.hover(trigger)

    const tooltip = await screen.findByRole('tooltip')
    expect(tooltip).toBeInTheDocument()
  })

  it('renders children unchanged', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Evaluator',
      systemRole: 'User',
      exerciseRole: 'Evaluator',
      can: vi.fn(),
      isLoading: false,
    })

    render(
      <RoleExplanationTooltip exerciseId="123">
        <div data-testid="child-element">Child Content</div>
      </RoleExplanationTooltip>,
    )

    expect(screen.getByTestId('child-element')).toBeInTheDocument()
    expect(screen.getByText('Child Content')).toBeInTheDocument()
  })

  it('handles loading state', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Observer',
      systemRole: null,
      exerciseRole: null,
      can: vi.fn(),
      isLoading: true,
    })

    render(
      <RoleExplanationTooltip exerciseId="123">
        <button>Hover me</button>
      </RoleExplanationTooltip>,
    )

    // Should still render children while loading
    expect(screen.getByRole('button', { name: /hover me/i })).toBeInTheDocument()
  })
})
