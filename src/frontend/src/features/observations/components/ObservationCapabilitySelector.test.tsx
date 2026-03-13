/**
 * ObservationCapabilitySelector Tests
 *
 * Tests for the capability tagging component used in observation forms.
 */

import { describe, it, expect, vi } from 'vitest'
import { screen } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import userEvent from '@testing-library/user-event'
import { ObservationCapabilitySelector } from './ObservationCapabilitySelector'
import type { CapabilityDto } from '@/features/capabilities/types'

describe('ObservationCapabilitySelector', () => {
  const mockCapabilities: CapabilityDto[] = [
    {
      id: '1',
      organizationId: 'org1',
      name: 'Mass Care Services',
      description: 'Provide life-sustaining services',
      category: 'Response',
      sortOrder: 1,
      isActive: true,
      sourceLibrary: 'FEMA',
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    },
    {
      id: '2',
      organizationId: 'org1',
      name: 'Public Health',
      description: 'Provide health services',
      category: 'Response',
      sortOrder: 2,
      isActive: true,
      sourceLibrary: 'FEMA',
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    },
    {
      id: '3',
      organizationId: 'org1',
      name: 'Critical Infrastructure',
      description: 'Protect critical assets',
      category: 'Protection',
      sortOrder: 3,
      isActive: true,
      sourceLibrary: 'FEMA',
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    },
  ]

  it('renders capability tags section', () => {
    render(
      <ObservationCapabilitySelector
        capabilities={mockCapabilities}
        targetCapabilityIds={['1']}
        selectedIds={[]}
        onChange={vi.fn()}
      />,
    )

    expect(screen.getByText(/Capability Tags/i)).toBeInTheDocument()
  })

  it('shows target capabilities in priority section', () => {
    render(
      <ObservationCapabilitySelector
        capabilities={mockCapabilities}
        targetCapabilityIds={['1', '2']}
        selectedIds={[]}
        onChange={vi.fn()}
      />,
    )

    expect(screen.getByText('Target Capabilities')).toBeInTheDocument()
    expect(screen.getByText('Mass Care Services')).toBeInTheDocument()
    expect(screen.getByText('Public Health')).toBeInTheDocument()
  })

  it('shows other capabilities in secondary section', () => {
    render(
      <ObservationCapabilitySelector
        capabilities={mockCapabilities}
        targetCapabilityIds={['1']}
        selectedIds={[]}
        onChange={vi.fn()}
      />,
    )

    expect(screen.getByText('Other Capabilities')).toBeInTheDocument()
    expect(screen.getByText('Critical Infrastructure')).toBeInTheDocument()
  })

  it('highlights selected capabilities', () => {
    render(
      <ObservationCapabilitySelector
        capabilities={mockCapabilities}
        targetCapabilityIds={['1']}
        selectedIds={['1', '3']}
        onChange={vi.fn()}
      />,
    )

    const massCareChip = screen.getByText('Mass Care Services').closest('.MuiChip-root')
    const criticalInfraChip = screen.getByText('Critical Infrastructure').closest(
      '.MuiChip-root',
    )

    expect(massCareChip).toHaveClass('MuiChip-filled')
    expect(criticalInfraChip).toHaveClass('MuiChip-filled')
  })

  it('calls onChange when capability is selected', async () => {
    const user = userEvent.setup()
    const handleChange = vi.fn()

    render(
      <ObservationCapabilitySelector
        capabilities={mockCapabilities}
        targetCapabilityIds={['1']}
        selectedIds={[]}
        onChange={handleChange}
      />,
    )

    const massCarechip = screen.getByText('Mass Care Services')
    await user.click(massCarechip)

    expect(handleChange).toHaveBeenCalledWith(['1'])
  })

  it('calls onChange when capability is deselected', async () => {
    const user = userEvent.setup()
    const handleChange = vi.fn()

    render(
      <ObservationCapabilitySelector
        capabilities={mockCapabilities}
        targetCapabilityIds={['1']}
        selectedIds={['1', '3']}
        onChange={handleChange}
      />,
    )

    const massCareChip = screen.getByText('Mass Care Services')
    await user.click(massCareChip)

    expect(handleChange).toHaveBeenCalledWith(['3'])
  })

  it('does not call onChange when disabled', () => {
    const handleChange = vi.fn()

    render(
      <ObservationCapabilitySelector
        capabilities={mockCapabilities}
        targetCapabilityIds={['1']}
        selectedIds={[]}
        onChange={handleChange}
        disabled
      />,
    )

    const massCareChip = screen.getByText('Mass Care Services').closest('.MuiChip-root')

    // Check that chips are disabled
    expect(massCareChip).toHaveClass('Mui-disabled')
  })

  it('shows empty state when no capabilities available', () => {
    render(
      <ObservationCapabilitySelector
        capabilities={[]}
        targetCapabilityIds={[]}
        selectedIds={[]}
        onChange={vi.fn()}
      />,
    )

    expect(
      screen.getByText(/No capabilities available for tagging/i),
    ).toBeInTheDocument()
  })

  it('handles multiple selections', async () => {
    const user = userEvent.setup()
    const handleChange = vi.fn()

    render(
      <ObservationCapabilitySelector
        capabilities={mockCapabilities}
        targetCapabilityIds={['1']}
        selectedIds={['1']}
        onChange={handleChange}
      />,
    )

    const publicHealthChip = screen.getByText('Public Health')
    await user.click(publicHealthChip)

    expect(handleChange).toHaveBeenCalledWith(['1', '2'])
  })

  it('does not show other capabilities section when all capabilities are targets', () => {
    render(
      <ObservationCapabilitySelector
        capabilities={mockCapabilities}
        targetCapabilityIds={['1', '2', '3']}
        selectedIds={[]}
        onChange={vi.fn()}
      />,
    )

    expect(screen.queryByText('Other Capabilities')).not.toBeInTheDocument()
  })

  it('does not show target capabilities section when no targets', () => {
    render(
      <ObservationCapabilitySelector
        capabilities={mockCapabilities}
        targetCapabilityIds={[]}
        selectedIds={[]}
        onChange={vi.fn()}
      />,
    )

    expect(screen.queryByText('Target Capabilities')).not.toBeInTheDocument()
    expect(screen.getByText('Other Capabilities')).toBeInTheDocument()
  })
})
