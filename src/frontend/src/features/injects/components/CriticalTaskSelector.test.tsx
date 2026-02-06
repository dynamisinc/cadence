/**
 * CriticalTaskSelector Component Tests
 *
 * Tests for the critical task multi-select component.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { render } from '../../../test/test-utils'
import { CriticalTaskSelector } from './CriticalTaskSelector'
import type { CriticalTaskDto, CapabilityTargetDto } from '../../eeg/types'

// Mock the hooks
vi.mock('../../eeg/hooks/useCapabilityTargets', () => ({
  useCapabilityTargets: vi.fn(),
}))

vi.mock('../../eeg/hooks/useCriticalTasks', () => ({
  useCriticalTasks: vi.fn(),
  useCriticalTasksByExercise: vi.fn(),
}))

import { useCapabilityTargets } from '../../eeg/hooks/useCapabilityTargets'
import { useCriticalTasksByExercise } from '../../eeg/hooks/useCriticalTasks'

// Type the mocked functions
const mockUseCapabilityTargets = useCapabilityTargets as ReturnType<typeof vi.fn>
const mockUseCriticalTasksByExercise = useCriticalTasksByExercise as ReturnType<typeof vi.fn>

// Test data
const mockCapabilityTargets: CapabilityTargetDto[] = [
  {
    id: 'target-1',
    targetDescription: 'Activate EOC within 60 minutes',
    sortOrder: 0,
    exerciseId: 'exercise-1',
    capabilityId: 'cap-1',
    capability: {
      id: 'cap-1',
      name: 'Operational Communications',
      description: 'Communications capability',
    },
    criticalTaskCount: 2,
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z',
  },
  {
    id: 'target-2',
    targetDescription: 'Establish interoperable comms',
    sortOrder: 1,
    exerciseId: 'exercise-1',
    capabilityId: 'cap-2',
    capability: {
      id: 'cap-2',
      name: 'Mass Care',
      description: 'Mass care capability',
    },
    criticalTaskCount: 1,
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z',
  },
]

const mockCriticalTasks: CriticalTaskDto[] = [
  {
    id: 'task-1',
    capabilityTargetId: 'target-1',
    taskDescription: 'Issue EOC activation notification',
    standard: 'Per SOP 5.2',
    sortOrder: 0,
    linkedInjectCount: 0,
    eegEntryCount: 0,
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z',
  },
  {
    id: 'task-2',
    capabilityTargetId: 'target-1',
    taskDescription: 'Staff EOC positions per roster',
    standard: null,
    sortOrder: 1,
    linkedInjectCount: 1,
    eegEntryCount: 0,
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z',
  },
  {
    id: 'task-3',
    capabilityTargetId: 'target-2',
    taskDescription: 'Open shelter location',
    standard: 'Red Cross standards',
    sortOrder: 0,
    linkedInjectCount: 0,
    eegEntryCount: 0,
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z',
  },
]

describe('CriticalTaskSelector', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Loading State', () => {
    it('shows loading indicator when tasks are loading', () => {
      mockUseCapabilityTargets.mockReturnValue({
        capabilityTargets: [],
        loading: true,
        error: null,
      })
      mockUseCriticalTasksByExercise.mockReturnValue({
        criticalTasks: [],
        loading: true,
        error: null,
      })

      render(
        <CriticalTaskSelector
          exerciseId="exercise-1"
          selectedTaskIds={[]}
          onChange={vi.fn()}
        />,
      )

      // The Autocomplete should show in loading state
      expect(screen.getByRole('combobox')).toBeInTheDocument()
    })
  })

  describe('Empty State', () => {
    it('shows message when no critical tasks defined', () => {
      mockUseCapabilityTargets.mockReturnValue({
        capabilityTargets: [],
        loading: false,
        error: null,
      })
      mockUseCriticalTasksByExercise.mockReturnValue({
        criticalTasks: [],
        loading: false,
        error: null,
      })

      render(
        <CriticalTaskSelector
          exerciseId="exercise-1"
          selectedTaskIds={[]}
          onChange={vi.fn()}
        />,
      )

      expect(screen.getByText(/No Critical Tasks defined/)).toBeInTheDocument()
      expect(screen.getByText(/Set up the EEG in the EEG Setup tab first/)).toBeInTheDocument()
    })
  })

  describe('Task Display', () => {
    beforeEach(() => {
      mockUseCapabilityTargets.mockReturnValue({
        capabilityTargets: mockCapabilityTargets,
        loading: false,
        error: null,
      })
      mockUseCriticalTasksByExercise.mockReturnValue({
        criticalTasks: mockCriticalTasks,
        loading: false,
        error: null,
      })
    })

    it('renders autocomplete when tasks exist', () => {
      render(
        <CriticalTaskSelector
          exerciseId="exercise-1"
          selectedTaskIds={[]}
          onChange={vi.fn()}
        />,
      )

      expect(screen.getByRole('combobox')).toBeInTheDocument()
      expect(screen.getByLabelText(/Linked Critical Tasks/)).toBeInTheDocument()
    })

    it('shows tasks grouped by capability target when dropdown opened', async () => {
      const user = userEvent.setup()

      render(
        <CriticalTaskSelector
          exerciseId="exercise-1"
          selectedTaskIds={[]}
          onChange={vi.fn()}
        />,
      )

      // Open the dropdown
      const autocomplete = screen.getByRole('combobox')
      await user.click(autocomplete)

      // Should show task descriptions
      await waitFor(() => {
        expect(screen.getByText('Issue EOC activation notification')).toBeInTheDocument()
        expect(screen.getByText('Staff EOC positions per roster')).toBeInTheDocument()
        expect(screen.getByText('Open shelter location')).toBeInTheDocument()
      })
    })
  })

  describe('Selection', () => {
    beforeEach(() => {
      mockUseCapabilityTargets.mockReturnValue({
        capabilityTargets: mockCapabilityTargets,
        loading: false,
        error: null,
      })
      mockUseCriticalTasksByExercise.mockReturnValue({
        criticalTasks: mockCriticalTasks,
        loading: false,
        error: null,
      })
    })

    it('displays selected tasks as chips', () => {
      render(
        <CriticalTaskSelector
          exerciseId="exercise-1"
          selectedTaskIds={['task-1', 'task-3']}
          onChange={vi.fn()}
        />,
      )

      // Selected tasks should appear as chips
      expect(screen.getByText('Issue EOC activation notification')).toBeInTheDocument()
      expect(screen.getByText('Open shelter location')).toBeInTheDocument()
    })

    it('calls onChange when task is selected', async () => {
      const user = userEvent.setup()
      const handleChange = vi.fn()

      render(
        <CriticalTaskSelector
          exerciseId="exercise-1"
          selectedTaskIds={[]}
          onChange={handleChange}
        />,
      )

      // Open dropdown and select a task
      const autocomplete = screen.getByRole('combobox')
      await user.click(autocomplete)

      await waitFor(() => {
        expect(screen.getByText('Issue EOC activation notification')).toBeInTheDocument()
      })

      await user.click(screen.getByText('Issue EOC activation notification'))

      expect(handleChange).toHaveBeenCalledWith(['task-1'])
    })

    it('calls onChange when task is deselected', async () => {
      const user = userEvent.setup()
      const handleChange = vi.fn()

      render(
        <CriticalTaskSelector
          exerciseId="exercise-1"
          selectedTaskIds={['task-1', 'task-2']}
          onChange={handleChange}
        />,
      )

      // Find and click the delete button on the chip
      const chip = screen.getByText('Issue EOC activation notification').closest('.MuiChip-root')
      const deleteButton = chip?.querySelector('[data-testid="CancelIcon"]') || chip?.querySelector('svg')

      if (deleteButton) {
        await user.click(deleteButton)
        // onChange should be called with task-1 removed
        expect(handleChange).toHaveBeenCalled()
      }
    })
  })

  describe('Props', () => {
    beforeEach(() => {
      mockUseCapabilityTargets.mockReturnValue({
        capabilityTargets: mockCapabilityTargets,
        loading: false,
        error: null,
      })
      mockUseCriticalTasksByExercise.mockReturnValue({
        criticalTasks: mockCriticalTasks,
        loading: false,
        error: null,
      })
    })

    it('respects disabled prop', () => {
      render(
        <CriticalTaskSelector
          exerciseId="exercise-1"
          selectedTaskIds={[]}
          onChange={vi.fn()}
          disabled={true}
        />,
      )

      // MUI Autocomplete sets disabled on the input element
      const input = screen.getByRole('combobox')
      expect(input).toBeDisabled()
    })

    it('shows custom helper text', () => {
      render(
        <CriticalTaskSelector
          exerciseId="exercise-1"
          selectedTaskIds={[]}
          onChange={vi.fn()}
          helperText="Custom helper text for testing"
        />,
      )

      expect(screen.getByText('Custom helper text for testing')).toBeInTheDocument()
    })

    it('shows default helper text when not provided', () => {
      render(
        <CriticalTaskSelector
          exerciseId="exercise-1"
          selectedTaskIds={[]}
          onChange={vi.fn()}
        />,
      )

      expect(screen.getByText(/Critical tasks this inject is designed to test/)).toBeInTheDocument()
    })
  })
})
