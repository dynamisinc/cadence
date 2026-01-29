/**
 * ImportLibraryMenu Component Tests
 *
 * Tests for the import library menu and confirmation dialog.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@/test/testUtils'
import userEvent from '@testing-library/user-event'
import { ImportLibraryMenu } from './ImportLibraryMenu'
import * as importHooks from '../hooks/useImportLibrary'
import type { PredefinedLibraryInfo, ImportLibraryResult } from '../types'

// Mock the hooks
vi.mock('../hooks/useImportLibrary')

const mockLibraries: PredefinedLibraryInfo[] = [
  {
    id: 'FEMA',
    name: 'FEMA Core Capabilities',
    description: '32 core capabilities from the National Preparedness Goal',
    capabilityCount: 32,
  },
  {
    id: 'NATO',
    name: 'NATO Baseline Requirements',
    description: '7 baseline requirements for collective defense',
    capabilityCount: 7,
  },
]

const mockImportResult: ImportLibraryResult = {
  totalInLibrary: 32,
  imported: 32,
  skippedDuplicates: 0,
  importedNames: [],
}

describe('ImportLibraryMenu', () => {
  beforeEach(() => {
    vi.clearAllMocks()

    // Default mock implementations
    vi.mocked(importHooks.useAvailableLibraries).mockReturnValue({
      data: mockLibraries,
      isLoading: false,
      isError: false,
      error: null,
    } as unknown as ReturnType<typeof importHooks.useAvailableLibraries>)

    vi.mocked(importHooks.useImportLibrary).mockReturnValue({
      mutateAsync: vi.fn().mockResolvedValue(mockImportResult),
      isPending: false,
      isError: false,
      error: null,
    } as unknown as ReturnType<typeof importHooks.useImportLibrary>)
  })

  const renderComponent = () => {
    return render(<ImportLibraryMenu />)
  }

  describe('rendering', () => {
    it('renders import button', () => {
      renderComponent()
      expect(screen.getByRole('button', { name: /import library/i })).toBeInTheDocument()
    })

    it('disables button while loading libraries', () => {
      vi.mocked(importHooks.useAvailableLibraries).mockReturnValue({
        data: undefined,
        isLoading: true,
        isError: false,
        error: null,
      } as unknown as ReturnType<typeof importHooks.useAvailableLibraries>)

      renderComponent()
      expect(screen.getByRole('button', { name: /import library/i })).toBeDisabled()
    })
  })

  describe('menu interactions', () => {
    it('shows library options when button clicked', async () => {
      const user = userEvent.setup()
      renderComponent()

      const button = screen.getByRole('button', { name: /import library/i })
      await user.click(button)

      await waitFor(() => {
        expect(screen.getByText('FEMA Core Capabilities')).toBeInTheDocument()
        expect(screen.getByText('32 capabilities')).toBeInTheDocument()
        expect(screen.getByText('NATO Baseline Requirements')).toBeInTheDocument()
        expect(screen.getByText('7 capabilities')).toBeInTheDocument()
      })
    })

    it('shows "no libraries" message when list is empty', async () => {
      vi.mocked(importHooks.useAvailableLibraries).mockReturnValue({
        data: [],
        isLoading: false,
        isError: false,
        error: null,
      } as unknown as ReturnType<typeof importHooks.useAvailableLibraries>)

      const user = userEvent.setup()
      renderComponent()

      const button = screen.getByRole('button', { name: /import library/i })
      await user.click(button)

      await waitFor(() => {
        expect(screen.getByText('No libraries available')).toBeInTheDocument()
      })
    })
  })

  describe('confirmation dialog', () => {
    it('shows confirmation dialog when library selected', async () => {
      const user = userEvent.setup()
      renderComponent()

      // Open menu
      const button = screen.getByRole('button', { name: /import library/i })
      await user.click(button)

      // Select FEMA library
      await waitFor(() => {
        expect(screen.getByText('FEMA Core Capabilities')).toBeInTheDocument()
      })
      await user.click(screen.getByText('FEMA Core Capabilities'))

      // Verify dialog
      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
        expect(screen.getByText(/import fema core capabilities\?/i)).toBeInTheDocument()
        expect(
          screen.getByText(/32 core capabilities from the national preparedness goal/i),
        ).toBeInTheDocument()
        expect(
          screen.getByText(/this will add 32 capabilities to your library/i),
        ).toBeInTheDocument()
      })
    })

    it('closes dialog when cancel clicked', async () => {
      const user = userEvent.setup()
      renderComponent()

      // Open menu and select library
      await user.click(screen.getByRole('button', { name: /import library/i }))
      await waitFor(() => {
        expect(screen.getByText('FEMA Core Capabilities')).toBeInTheDocument()
      })
      await user.click(screen.getByText('FEMA Core Capabilities'))

      // Wait for dialog
      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      // Click cancel
      const cancelButton = screen.getByRole('button', { name: /cancel/i })
      await user.click(cancelButton)

      // Dialog should close
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
      })
    })
  })

  describe('import functionality', () => {
    it('calls import mutation when confirmed', async () => {
      const mockMutateAsync = vi.fn().mockResolvedValue(mockImportResult)
      vi.mocked(importHooks.useImportLibrary).mockReturnValue({
        mutateAsync: mockMutateAsync,
        isPending: false,
        isError: false,
        error: null,
      } as unknown as ReturnType<typeof importHooks.useImportLibrary>)

      const user = userEvent.setup()
      renderComponent()

      // Open menu and select library
      await user.click(screen.getByRole('button', { name: /import library/i }))
      await waitFor(() => {
        expect(screen.getByText('FEMA Core Capabilities')).toBeInTheDocument()
      })
      await user.click(screen.getByText('FEMA Core Capabilities'))

      // Wait for dialog and click import
      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })
      const importButton = screen.getByRole('button', { name: /^import$/i })
      await user.click(importButton)

      // Verify mutation was called
      await waitFor(() => {
        expect(mockMutateAsync).toHaveBeenCalledWith('FEMA')
      })
    })

    it('shows loading state during import', async () => {
      vi.mocked(importHooks.useImportLibrary).mockReturnValue({
        mutateAsync: vi.fn().mockImplementation(
          () =>
            new Promise(resolve => {
              setTimeout(() => resolve(mockImportResult), 1000)
            }),
        ),
        isPending: true,
        isError: false,
        error: null,
      } as unknown as ReturnType<typeof importHooks.useImportLibrary>)

      const user = userEvent.setup()
      renderComponent()

      // Open menu and select library
      await user.click(screen.getByRole('button', { name: /import library/i }))
      await waitFor(() => {
        expect(screen.getByText('FEMA Core Capabilities')).toBeInTheDocument()
      })
      await user.click(screen.getByText('FEMA Core Capabilities'))

      // Wait for dialog
      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      // Verify loading state
      expect(screen.getByRole('button', { name: /importing\.\.\./i })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: /cancel/i })).toBeDisabled()
    })

    it('shows error message when import fails', async () => {
      const mockError = new Error('Network error')
      vi.mocked(importHooks.useImportLibrary).mockReturnValue({
        mutateAsync: vi.fn().mockRejectedValue(mockError),
        isPending: false,
        isError: true,
        error: mockError,
      } as unknown as ReturnType<typeof importHooks.useImportLibrary>)

      const user = userEvent.setup()
      renderComponent()

      // Open menu and select library
      await user.click(screen.getByRole('button', { name: /import library/i }))
      await waitFor(() => {
        expect(screen.getByText('FEMA Core Capabilities')).toBeInTheDocument()
      })
      await user.click(screen.getByText('FEMA Core Capabilities'))

      // Wait for dialog
      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      // Verify error is shown
      expect(screen.getByText(/network error/i)).toBeInTheDocument()
    })

    it('closes dialog after successful import', async () => {
      const mockMutateAsync = vi.fn().mockResolvedValue(mockImportResult)
      vi.mocked(importHooks.useImportLibrary).mockReturnValue({
        mutateAsync: mockMutateAsync,
        isPending: false,
        isError: false,
        error: null,
      } as unknown as ReturnType<typeof importHooks.useImportLibrary>)

      const user = userEvent.setup()
      renderComponent()

      // Open menu and select library
      await user.click(screen.getByRole('button', { name: /import library/i }))
      await waitFor(() => {
        expect(screen.getByText('NATO Baseline Requirements')).toBeInTheDocument()
      })
      await user.click(screen.getByText('NATO Baseline Requirements'))

      // Wait for dialog and click import
      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })
      const importButton = screen.getByRole('button', { name: /^import$/i })
      await user.click(importButton)

      // Dialog should close after successful import
      await waitFor(() => {
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
      })
    })
  })
})
