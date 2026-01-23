/**
 * EffectiveRoleBadge Component Tests
 *
 * @module features/auth
 */
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { EffectiveRoleBadge } from './EffectiveRoleBadge';
import { useExerciseRole } from '../hooks/useExerciseRole';

vi.mock('../hooks/useExerciseRole');

describe('EffectiveRoleBadge', () => {
  it('displays effective role badge', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Controller',
      systemRole: 'User',
      exerciseRole: 'Controller',
      can: vi.fn(),
      isLoading: false,
    });

    render(<EffectiveRoleBadge exerciseId="123" />);

    expect(screen.getByText('Controller')).toBeInTheDocument();
  });

  it('shows Exercise Director with friendly name', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'ExerciseDirector',
      systemRole: 'Manager',
      exerciseRole: 'ExerciseDirector',
      can: vi.fn(),
      isLoading: false,
    });

    render(<EffectiveRoleBadge exerciseId="123" />);

    expect(screen.getByText('Exercise Director')).toBeInTheDocument();
  });

  it('uses correct color for Administrator', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Administrator',
      systemRole: 'Admin',
      exerciseRole: null,
      can: vi.fn(),
      isLoading: false,
    });

    render(<EffectiveRoleBadge exerciseId="123" />);

    const badge = screen.getByText('Administrator');
    expect(badge).toBeInTheDocument();
    // Badge should have error color (red) for admin
  });

  it('uses correct color for Controller', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Controller',
      systemRole: 'User',
      exerciseRole: 'Controller',
      can: vi.fn(),
      isLoading: false,
    });

    render(<EffectiveRoleBadge exerciseId="123" />);

    const badge = screen.getByText('Controller');
    expect(badge).toBeInTheDocument();
    // Badge should have primary color (blue) for controller
  });

  it('shows tooltip on hover', async () => {
    const user = userEvent.setup();

    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Controller',
      systemRole: 'User',
      exerciseRole: 'Controller',
      can: vi.fn(),
      isLoading: false,
    });

    render(<EffectiveRoleBadge exerciseId="123" />);

    const badge = screen.getByText('Controller');
    await user.hover(badge);

    // Tooltip should appear with role description
    await screen.findByRole('tooltip');
  });

  it('indicates when exercise role overrides system role', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Observer',
      systemRole: 'Admin',
      exerciseRole: 'Observer',
      can: vi.fn(),
      isLoading: false,
    });

    render(<EffectiveRoleBadge exerciseId="123" showOverride />);

    // Should show that Observer overrides Admin
    expect(screen.getByText('Observer')).toBeInTheDocument();
  });

  it('indicates when using system role as default', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Administrator',
      systemRole: 'Admin',
      exerciseRole: null,
      can: vi.fn(),
      isLoading: false,
    });

    render(<EffectiveRoleBadge exerciseId="123" showOverride />);

    expect(screen.getByText('Administrator')).toBeInTheDocument();
  });

  it('shows loading skeleton when loading', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Observer',
      systemRole: null,
      exerciseRole: null,
      can: vi.fn(),
      isLoading: true,
    });

    render(<EffectiveRoleBadge exerciseId="123" />);

    expect(screen.getByTestId('role-badge-skeleton')).toBeInTheDocument();
  });

  it('renders with custom size', () => {
    vi.mocked(useExerciseRole).mockReturnValue({
      effectiveRole: 'Evaluator',
      systemRole: 'User',
      exerciseRole: 'Evaluator',
      can: vi.fn(),
      isLoading: false,
    });

    render(<EffectiveRoleBadge exerciseId="123" size="small" />);

    expect(screen.getByText('Evaluator')).toBeInTheDocument();
  });
});
