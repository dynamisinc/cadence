/**
 * PasswordRequirements Component Tests
 */
import { describe, it, expect } from 'vitest';
import { render, screen } from '../../../test/testUtils';
import { PasswordRequirements } from './PasswordRequirements';

describe('PasswordRequirements', () => {
  it('renders all requirements', () => {
    const requirements = {
      minLength: false,
      hasUppercase: false,
      hasNumber: false,
    };

    render(<PasswordRequirements requirements={requirements} />);

    expect(screen.getByText('At least 8 characters')).toBeInTheDocument();
    expect(screen.getByText('At least 1 uppercase letter')).toBeInTheDocument();
    expect(screen.getByText('At least 1 number')).toBeInTheDocument();
  });

  it('shows all requirements as unmet when password is empty', () => {
    const requirements = {
      minLength: false,
      hasUppercase: false,
      hasNumber: false,
    };

    const { container } = render(<PasswordRequirements requirements={requirements} />);

    // All requirements should show X marks (faXmark icons)
    const icons = container.querySelectorAll('svg');
    expect(icons.length).toBe(3);
  });

  it('shows requirements as met when password is valid', () => {
    const requirements = {
      minLength: true,
      hasUppercase: true,
      hasNumber: true,
    };

    const { container } = render(<PasswordRequirements requirements={requirements} />);

    // All requirements should show check marks (faCheck icons)
    const icons = container.querySelectorAll('svg');
    expect(icons.length).toBe(3);
  });

  it('shows mixed state when some requirements are met', () => {
    const requirements = {
      minLength: true,
      hasUppercase: false,
      hasNumber: true,
    };

    const { container } = render(<PasswordRequirements requirements={requirements} />);

    // Should show 3 icons (mix of checks and X marks)
    const icons = container.querySelectorAll('svg');
    expect(icons.length).toBe(3);
  });
});
