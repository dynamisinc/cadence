/**
 * ObservationForm Component Tests
 *
 * Tests for the observation form with inject selector dropdown.
 */

import { describe, it, expect, vi } from 'vitest'
import { screen, fireEvent, within } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { ObservationForm } from './ObservationForm'
import { InjectStatus, InjectType, TriggerType } from '../../../types'
import type { InjectDto } from '../../injects/types'

// Mock the photo hooks used by PhotoAttachmentSection
vi.mock('../../photos/hooks/useCamera', () => ({
  useCamera: vi.fn(() => ({
    fileInputRef: { current: null },
    isCapturing: false,
    openCamera: vi.fn(),
    openGallery: vi.fn(),
    handleFileChange: vi.fn(),
    resetCapture: vi.fn(),
  })),
}))

vi.mock('../../photos/hooks/useImageCompression', () => ({
  useImageCompression: vi.fn(() => ({
    compressImage: vi.fn(async (file: File) => ({
      compressed: new Blob(['compressed'], { type: 'image/jpeg' }),
      thumbnail: new Blob(['thumbnail'], { type: 'image/jpeg' }),
      originalFileName: file.name,
      fileSizeBytes: 1024,
    })),
  })),
}))

vi.mock('../../photos/hooks/usePhotos', () => ({
  usePhotos: vi.fn(() => ({
    uploadPhoto: vi.fn(),
  })),
}))

vi.mock('../../../core/contexts', () => ({
  useConnectivity: vi.fn(() => ({
    connectivityState: 'online',
    incrementPendingCount: vi.fn(),
  })),
}))

// Helper to create mock inject
const createMockInject = (
  overrides: Partial<InjectDto> = {},
): InjectDto => ({
  id: 'inject-1',
  injectNumber: 1,
  title: 'Test Inject',
  description: 'Test description',
  scheduledTime: '09:00:00',
  deliveryTime: null,
  scenarioDay: null,
  scenarioTime: null,
  target: 'EOC Director',
  source: null,
  deliveryMethod: null,
  deliveryMethodId: null,
  deliveryMethodName: null,
  deliveryMethodOther: null,
  injectType: InjectType.Standard,
  status: InjectStatus.Draft,
  sequence: 1,
  parentInjectId: null,
  triggerCondition: null,
  expectedAction: null,
  controllerNotes: null,
  readyAt: null,
  firedAt: null,
  firedBy: null,
  firedByName: null,
  skippedAt: null,
  skippedBy: null,
  skippedByName: null,
  skipReason: null,
  mselId: 'msel-1',
  phaseId: null,
  phaseName: null,
  objectiveIds: [],
  createdAt: '2025-01-01T00:00:00Z',
  updatedAt: '2025-01-01T00:00:00Z',
  sourceReference: null,
  priority: null,
  triggerType: TriggerType.Manual,
  responsibleController: null,
  locationName: null,
  locationType: null,
  track: null,
  ...overrides,
})

describe('ObservationForm', () => {
  describe('Inject Dropdown Sorting', () => {
    it('sorts recently released injects first', async () => {
      const injects = [
        createMockInject({
          id: 'pending-1',
          injectNumber: 1,
          title: 'Draft Inject',
          status: InjectStatus.Draft,
          firedAt: null,
        }),
        createMockInject({
          id: 'fired-early',
          injectNumber: 2,
          title: 'Early Released',
          status: InjectStatus.Released,
          firedAt: '2025-01-01T09:00:00Z',
        }),
        createMockInject({
          id: 'fired-late',
          injectNumber: 3,
          title: 'Recent Released',
          status: InjectStatus.Released,
          firedAt: '2025-01-01T10:00:00Z', // Most recent
        }),
        createMockInject({
          id: 'pending-2',
          injectNumber: 4,
          title: 'Another Draft',
          status: InjectStatus.Draft,
          firedAt: null,
        }),
      ]

      render(
        <ObservationForm
          exerciseId="exercise-1"
          injects={injects}
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      // Open the dropdown
      const select = screen.getByLabelText(/Related Inject/i)
      fireEvent.mouseDown(select)

      // Get all menu items (except "None")
      const listbox = await screen.findByRole('listbox')
      const options = within(listbox).getAllByRole('option')

      // First option should be "None", followed by recently fired injects, then pending
      expect(options[0]).toHaveTextContent('None')
      // Most recently fired first
      expect(options[1]).toHaveTextContent('#3')
      expect(options[1]).toHaveTextContent('(Released)')
      // Earlier fired second
      expect(options[2]).toHaveTextContent('#2')
      expect(options[2]).toHaveTextContent('(Released)')
      // Pending injects by sequence
      expect(options[3]).toHaveTextContent('#1')
      expect(options[4]).toHaveTextContent('#4')
    })

    it('shows inject status in dropdown', async () => {
      const injects = [
        createMockInject({
          id: 'fired-1',
          injectNumber: 1,
          title: 'Fired Inject',
          status: InjectStatus.Released,
          firedAt: '2025-01-01T10:00:00Z',
        }),
        createMockInject({
          id: 'pending-1',
          injectNumber: 2,
          title: 'Pending Inject',
          status: InjectStatus.Draft,
        }),
        createMockInject({
          id: 'deferred-1',
          injectNumber: 3,
          title: 'Deferred Inject',
          status: InjectStatus.Deferred,
          skippedAt: '2025-01-01T10:05:00Z',
        }),
      ]

      render(
        <ObservationForm
          exerciseId="exercise-1"
          injects={injects}
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      // Open the dropdown
      const select = screen.getByLabelText(/Related Inject/i)
      fireEvent.mouseDown(select)

      const listbox = await screen.findByRole('listbox')

      // Check status indicators are shown
      expect(within(listbox).getByText(/\(Released\)/)).toBeInTheDocument()
      expect(within(listbox).getByText(/\(Deferred\)/)).toBeInTheDocument()
    })

    it('shows title instead of truncated description', async () => {
      const injects = [
        createMockInject({
          id: 'inject-1',
          injectNumber: 1,
          title: 'Media Inquiry',
          description: 'This is a very long description that would normally be truncated',
          status: InjectStatus.Released,
          firedAt: '2025-01-01T10:00:00Z',
        }),
      ]

      render(
        <ObservationForm
          exerciseId="exercise-1"
          injects={injects}
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      // Open the dropdown
      const select = screen.getByLabelText(/Related Inject/i)
      fireEvent.mouseDown(select)

      const listbox = await screen.findByRole('listbox')

      // Should show title, not description
      expect(within(listbox).getByText(/Media Inquiry/)).toBeInTheDocument()
    })
  })

  describe('Basic Form Behavior', () => {
    it('renders observation content field', () => {
      render(
        <ObservationForm
          exerciseId="exercise-1"
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      expect(screen.getByLabelText(/Observation/i)).toBeInTheDocument()
    })

    it('renders rating dropdown', () => {
      render(
        <ObservationForm
          exerciseId="exercise-1"
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      expect(screen.getByLabelText(/Rating/i)).toBeInTheDocument()
    })

    it('disables save button when content is empty', () => {
      render(
        <ObservationForm
          exerciseId="exercise-1"
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      expect(screen.getByRole('button', { name: /Save/i })).toBeDisabled()
    })

    it('enables save button when content has text and rating is selected', () => {
      render(
        <ObservationForm
          exerciseId="exercise-1"
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      // Fill in observation content
      const textarea = screen.getByLabelText(/Observation/i)
      fireEvent.change(textarea, { target: { value: 'Test observation' } })

      // Select a rating (required for form to be valid)
      const ratingSelect = screen.getByLabelText(/Rating/i)
      fireEvent.mouseDown(ratingSelect)
      const listbox = within(screen.getByRole('listbox'))
      fireEvent.click(listbox.getByText('P - Performed'))

      expect(screen.getByRole('button', { name: /Save/i })).not.toBeDisabled()
    })

    it('calls onCancel when cancel button clicked', () => {
      const onCancel = vi.fn()

      render(
        <ObservationForm
          exerciseId="exercise-1"
          onSubmit={vi.fn()}
          onCancel={onCancel}
        />,
      )

      fireEvent.click(screen.getByRole('button', { name: /Cancel/i }))

      expect(onCancel).toHaveBeenCalled()
    })

    it('does not show inject dropdown when no injects provided', () => {
      render(
        <ObservationForm
          exerciseId="exercise-1"
          injects={[]}
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      expect(screen.queryByLabelText(/Related Inject/i)).not.toBeInTheDocument()
    })
  })

  describe('Capability Tagging (S05)', () => {
    const mockCapabilities = [
      {
        id: 'cap-1',
        organizationId: 'org-1',
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
        id: 'cap-2',
        organizationId: 'org-1',
        name: 'Public Health',
        description: 'Provide health services',
        category: 'Response',
        sortOrder: 2,
        isActive: true,
        sourceLibrary: 'FEMA',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z',
      },
    ]

    it('shows capability selector when capabilities are provided', () => {
      render(
        <ObservationForm
          exerciseId="exercise-1"
          capabilities={mockCapabilities}
          targetCapabilityIds={['cap-1']}
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      expect(screen.getByText(/Capability Tags/i)).toBeInTheDocument()
      expect(screen.getByText('Mass Care Services')).toBeInTheDocument()
    })

    it('does not show capability selector when no capabilities provided', () => {
      render(
        <ObservationForm
          exerciseId="exercise-1"
          capabilities={[]}
          onSubmit={vi.fn()}
          onCancel={vi.fn()}
        />,
      )

      expect(screen.queryByText(/Capability Tags/i)).not.toBeInTheDocument()
    })

    it('includes selected capability IDs in submit data', () => {
      const onSubmit = vi.fn()

      render(
        <ObservationForm
          exerciseId="exercise-1"
          capabilities={mockCapabilities}
          targetCapabilityIds={['cap-1']}
          onSubmit={onSubmit}
          onCancel={vi.fn()}
        />,
      )

      // Fill in required fields
      const textarea = screen.getByLabelText(/Observation/i)
      fireEvent.change(textarea, { target: { value: 'Test observation' } })

      const ratingSelect = screen.getByLabelText(/Rating/i)
      fireEvent.mouseDown(ratingSelect)
      const listbox = within(screen.getByRole('listbox'))
      fireEvent.click(listbox.getByText('P - Performed'))

      // Select a capability
      const massCareChip = screen.getByText('Mass Care Services')
      fireEvent.click(massCareChip)

      // Submit
      fireEvent.click(screen.getByRole('button', { name: /Save/i }))

      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          capabilityIds: ['cap-1'],
        }),
        undefined,
      )
    })

    it('omits capabilityIds from submit data when none selected', () => {
      const onSubmit = vi.fn()

      render(
        <ObservationForm
          exerciseId="exercise-1"
          capabilities={mockCapabilities}
          targetCapabilityIds={['cap-1']}
          onSubmit={onSubmit}
          onCancel={vi.fn()}
        />,
      )

      // Fill in required fields
      const textarea = screen.getByLabelText(/Observation/i)
      fireEvent.change(textarea, { target: { value: 'Test observation' } })

      const ratingSelect = screen.getByLabelText(/Rating/i)
      fireEvent.mouseDown(ratingSelect)
      const listbox = within(screen.getByRole('listbox'))
      fireEvent.click(listbox.getByText('P - Performed'))

      // Submit without selecting capabilities
      fireEvent.click(screen.getByRole('button', { name: /Save/i }))

      expect(onSubmit).toHaveBeenCalledWith(
        expect.not.objectContaining({
          capabilityIds: expect.anything(),
        }),
        undefined,
      )
    })
  })
})
