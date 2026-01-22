/**
 * RoleSelect Component Tests
 */
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '../../../test/test-utils';
import userEvent from '@testing-library/user-event';
import { RoleSelect } from './RoleSelect';

describe('RoleSelect', () => {
  it('renders with current role selected', () => {
    render(<RoleSelect value="Controller" onChange={vi.fn()} />);

    // MUI Select displays the selected value as text content
    expect(screen.getByText('Controller')).toBeInTheDocument();
  });

  it('shows all available roles', async () => {
    const user = userEvent.setup();
    render(<RoleSelect value="Controller" onChange={vi.fn()} />);

    const select = screen.getByRole('combobox');
    await user.click(select);

    expect(screen.getByRole('option', { name: 'Administrator' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'ExerciseDirector' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Controller' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Evaluator' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Observer' })).toBeInTheDocument();
  });

  it('calls onChange when role is selected', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<RoleSelect value="Controller" onChange={onChange} />);

    const select = screen.getByRole('combobox');
    await user.click(select);
    await user.click(screen.getByRole('option', { name: 'Evaluator' }));

    expect(onChange).toHaveBeenCalledWith('Evaluator');
  });

  it('can be disabled', () => {
    render(<RoleSelect value="Administrator" onChange={vi.fn()} disabled />);

    const select = screen.getByRole('combobox');
    // MUI disabled state is shown via aria-disabled attribute
    expect(select).toHaveAttribute('aria-disabled', 'true');
  });
});
