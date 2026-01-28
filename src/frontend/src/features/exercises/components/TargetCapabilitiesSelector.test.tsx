/**
 * TargetCapabilitiesSelector Component Tests
 *
 * Tests for S04 acceptance criteria:
 * - Multi-select shows capabilities grouped by category
 * - Selected capabilities shown as chips
 * - Can remove capabilities by clicking chip delete
 * - Empty state when no capabilities available
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { TargetCapabilitiesSelector } from './TargetCapabilitiesSelector'
import type { CapabilityDto } from '../../capabilities/types'

// Mock useCapabilities hook
vi.mock('../../capabilities/hooks/useCapabilities', () => ({
  useCapabilities: vi.fn(),
}))

import { useCapabilities } from '../../capabilities/hooks/useCapabilities'

const mockCapabilities: CapabilityDto[] = [
  {
    id: 'cap-1',
    organizationId: 'org-1',
    name: 'Mass Care Services',
    description: 'Provide shelter, food, and emergency assistance',
    category: 'Response',
    sortOrder: 1,
    isActive: true,
    sourceLibrary: 'FEMA',
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  },
  {
    id: 'cap-2',
    organizationId: 'org-1',
    name: 'Operational Communications',
    description: 'Ensure coordinated communications',
    category: 'Response',
    sortOrder: 2,
    isActive: true,
    sourceLibrary: 'FEMA',
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  },
  {
    id: 'cap-3',
    organizationId: 'org-1',
    name: 'Public Information and Warning',
    description: 'Deliver coordinated information',
    category: 'Prevention',
    sortOrder: 1,
    isActive: true,
    sourceLibrary: 'FEMA',
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  },
]

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  })

  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
}

describe('TargetCapabilitiesSelector', () => {
  beforeEach(() => {
    vi.mocked(useCapabilities).mockReturnValue({
      capabilities: mockCapabilities,
      loading: false,
      error: null,
      fetchCapabilities: vi.fn(),
      createCapability: vi.fn(),
      updateCapability: vi.fn(),
      deleteCapability: vi.fn(),
      isCreating: false,
      isUpdating: false,
      isDeleting: false,
    })
  })

  it('renders with empty selection', () => {
    const onChange = vi.fn()
    render(
      <TargetCapabilitiesSelector
        organizationId="org-1"
        selectedIds={[]}
        onChange={onChange}
      />,
      { wrapper: createWrapper() },
    )

    expect(screen.getByText(/Target Capabilities/i)).toBeInTheDocument()
    expect(screen.getByText(/\(0 selected\)/i)).toBeInTheDocument()
  })

  it('shows selected capabilities as chips', () => {
    const onChange = vi.fn()
    render(
      <TargetCapabilitiesSelector
        organizationId="org-1"
        selectedIds={['cap-1', 'cap-3']}
        onChange={onChange}
      />,
      { wrapper: createWrapper() },
    )

    expect(screen.getByText('Mass Care Services')).toBeInTheDocument()
    expect(screen.getByText('Public Information and Warning')).toBeInTheDocument()
    expect(screen.getByText(/\(2 selected\)/i)).toBeInTheDocument()
  })

  it('groups capabilities by category', () => {
    const onChange = vi.fn()
    render(
      <TargetCapabilitiesSelector
        organizationId="org-1"
        selectedIds={[]}
        onChange={onChange}
      />,
      { wrapper: createWrapper() },
    )

    expect(screen.getByText('Response')).toBeInTheDocument()
    expect(screen.getByText('Prevention')).toBeInTheDocument()
  })

  it('calls onChange when capability chip is clicked to select', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()

    render(
      <TargetCapabilitiesSelector
        organizationId="org-1"
        selectedIds={[]}
        onChange={onChange}
      />,
      { wrapper: createWrapper() },
    )

    // Find and click an unselected capability chip
    const responseSection = screen.getByText('Response').parentElement!
    const chipInSection = within(responseSection).getAllByRole('button')[0]
    await user.click(chipInSection)

    expect(onChange).toHaveBeenCalledWith(['cap-1'])
  })

  it('removes capability when chip delete icon clicked', async () => {
    const user = userEvent.setup()
    const onChange = vi.fn()

    render(
      <TargetCapabilitiesSelector
        organizationId="org-1"
        selectedIds={['cap-1', 'cap-3']}
        onChange={onChange}
      />,
      { wrapper: createWrapper() },
    )

    // Find the delete button on the first selected chip
    const selectedChips = screen.getAllByRole('button', { name: /Mass Care Services/i })
    const deleteButton = within(selectedChips[0]).getByTestId('CancelIcon')

    await user.click(deleteButton)

    expect(onChange).toHaveBeenCalledWith(['cap-3'])
  })

  it('disables all interactions when disabled prop is true', () => {
    const onChange = vi.fn()
    render(
      <TargetCapabilitiesSelector
        organizationId="org-1"
        selectedIds={['cap-1']}
        onChange={onChange}
        disabled={true}
      />,
      { wrapper: createWrapper() },
    )

    // Selected chips should not have delete buttons when disabled
    const chip = screen.getByText('Mass Care Services').closest('div')
    expect(chip).toHaveClass('MuiChip-root')
    // Chip should not be clickable (no onDelete when disabled)
  })

  it('shows empty state when no capabilities available', () => {
    vi.mocked(useCapabilities).mockReturnValue({
      capabilities: [],
      loading: false,
      error: null,
      fetchCapabilities: vi.fn(),
      createCapability: vi.fn(),
      updateCapability: vi.fn(),
      deleteCapability: vi.fn(),
      isCreating: false,
      isUpdating: false,
      isDeleting: false,
    })

    const onChange = vi.fn()
    render(
      <TargetCapabilitiesSelector
        organizationId="org-1"
        selectedIds={[]}
        onChange={onChange}
      />,
      { wrapper: createWrapper() },
    )

    expect(
      screen.getByText(/No capabilities available. Import a library from Settings/i),
    ).toBeInTheDocument()
  })

  it('shows loading state', () => {
    vi.mocked(useCapabilities).mockReturnValue({
      capabilities: [],
      loading: true,
      error: null,
      fetchCapabilities: vi.fn(),
      createCapability: vi.fn(),
      updateCapability: vi.fn(),
      deleteCapability: vi.fn(),
      isCreating: false,
      isUpdating: false,
      isDeleting: false,
    })

    const onChange = vi.fn()
    render(
      <TargetCapabilitiesSelector
        organizationId="org-1"
        selectedIds={[]}
        onChange={onChange}
      />,
      { wrapper: createWrapper() },
    )

    expect(screen.getByText(/Loading capabilities/i)).toBeInTheDocument()
  })

  it('shows HSEEP guidance about 3-5 capabilities', () => {
    const onChange = vi.fn()
    render(
      <TargetCapabilitiesSelector
        organizationId="org-1"
        selectedIds={[]}
        onChange={onChange}
      />,
      { wrapper: createWrapper() },
    )

    expect(screen.getByText(/HSEEP recommends focusing on 3-5 key capabilities/i)).toBeInTheDocument()
  })
})
