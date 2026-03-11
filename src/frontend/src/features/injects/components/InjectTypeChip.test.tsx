import { describe, it, expect } from 'vitest'
import { screen } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { InjectTypeChip } from './InjectTypeChip'
import { InjectType } from '../../../types'

describe('InjectTypeChip', () => {
  it('renders null for Standard type (default, not shown)', () => {
    const { container } = render(<InjectTypeChip type={InjectType.Standard} />)
    expect(container.querySelector('.MuiChip-root')).not.toBeInTheDocument()
  })

  it('renders Contingency type with correct label', () => {
    render(<InjectTypeChip type={InjectType.Contingency} />)
    expect(screen.getByText('Contingency')).toBeInTheDocument()
  })

  it('renders Adaptive type with correct label', () => {
    render(<InjectTypeChip type={InjectType.Adaptive} />)
    expect(screen.getByText('Adaptive')).toBeInTheDocument()
  })

  it('renders Complexity type with correct label', () => {
    render(<InjectTypeChip type={InjectType.Complexity} />)
    expect(screen.getByText('Complexity')).toBeInTheDocument()
  })

  it('applies correct styling for Contingency type (blue)', () => {
    const { container } = render(<InjectTypeChip type={InjectType.Contingency} />)
    const chip = container.querySelector('.MuiChip-root')
    expect(chip).toBeInTheDocument()
    // Check for blue background - grid.main from cobraTheme
    expect(chip).toHaveStyle({ backgroundColor: 'rgb(219, 233, 250)' })
  })

  it('applies correct styling for Adaptive type (purple)', () => {
    const { container } = render(<InjectTypeChip type={InjectType.Adaptive} />)
    const chip = container.querySelector('.MuiChip-root')
    expect(chip).toBeInTheDocument()
    // Check for purple background - alpha(semantic.purple, 0.12) = rgba(156, 39, 176, 0.12)
    expect(chip).toHaveStyle({ backgroundColor: 'rgba(156, 39, 176, 0.12)' })
  })
})
