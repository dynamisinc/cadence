/**
 * RoleSelect Component Tests
 *
 * Tests for the system role selector dropdown.
 * Note: This component displays SYSTEM roles (Admin, Manager, User),
 * not HSEEP exercise roles (Controller, Evaluator, etc.)
 */
import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '../../../test/test-utils'
import userEvent from '@testing-library/user-event'
import { RoleSelect } from './RoleSelect'

describe('RoleSelect', () => {
  it('renders with current role selected', () => {
    render(<RoleSelect value="Admin" onChange={vi.fn()} />)

    // MUI Select displays the selected value as text content
    expect(screen.getByText('Admin')).toBeInTheDocument()
  })

  it('shows all available system roles', async () => {
    const user = userEvent.setup()
    render(<RoleSelect value="Admin" onChange={vi.fn()} />)

    const select = screen.getByRole('combobox')
    await user.click(select)

    // System roles: Admin, Manager, User
    expect(screen.getByRole('option', { name: 'Admin' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Manager' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'User' })).toBeInTheDocument()
  })

  it('calls onChange when role is selected', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()
    render(<RoleSelect value="Admin" onChange={onChange} />)

    const select = screen.getByRole('combobox')
    await user.click(select)
    await user.click(screen.getByRole('option', { name: 'Manager' }))

    expect(onChange).toHaveBeenCalledWith('Manager')
  })

  it('can be disabled', () => {
    render(<RoleSelect value="Admin" onChange={vi.fn()} disabled />)

    const select = screen.getByRole('combobox')
    // MUI disabled state is shown via aria-disabled attribute
    expect(select).toHaveAttribute('aria-disabled', 'true')
  })
})
