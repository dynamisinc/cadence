import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { InjectStatusChip } from './InjectStatusChip'
import { InjectStatus } from '../../../types'

describe('InjectStatusChip', () => {
  it('renders Pending status with correct label', () => {
    render(<InjectStatusChip status={InjectStatus.Pending} />)
    expect(screen.getByText('Pending')).toBeInTheDocument()
  })

  it('renders Fired status with correct label', () => {
    render(<InjectStatusChip status={InjectStatus.Fired} />)
    expect(screen.getByText('Fired')).toBeInTheDocument()
  })

  it('renders Skipped status with correct label', () => {
    render(<InjectStatusChip status={InjectStatus.Skipped} />)
    expect(screen.getByText('Skipped')).toBeInTheDocument()
  })

  it('applies correct styling for Pending status (gray)', () => {
    const { container } = render(<InjectStatusChip status={InjectStatus.Pending} />)
    const chip = container.querySelector('.MuiChip-root')
    expect(chip).toBeInTheDocument()
    // Check for gray background - statusChart.grey from cobraTheme
    expect(chip).toHaveStyle({ backgroundColor: 'rgb(192, 192, 192)' })
  })

  it('applies correct styling for Fired status (green)', () => {
    const { container } = render(<InjectStatusChip status={InjectStatus.Fired} />)
    const chip = container.querySelector('.MuiChip-root')
    expect(chip).toBeInTheDocument()
    // Check for green background - notifications.success from cobraTheme
    expect(chip).toHaveStyle({ backgroundColor: 'rgb(174, 251, 184)' })
  })

  it('applies correct styling for Skipped status (warning/orange)', () => {
    const { container } = render(<InjectStatusChip status={InjectStatus.Skipped} />)
    const chip = container.querySelector('.MuiChip-root')
    expect(chip).toBeInTheDocument()
    // Check for warning background - notifications.warning from cobraTheme
    expect(chip).toHaveStyle({ backgroundColor: 'rgb(249, 249, 190)' })
  })
})
