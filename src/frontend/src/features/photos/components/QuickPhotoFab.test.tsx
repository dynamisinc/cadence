/**
 * QuickPhotoFab Tests
 *
 * Tests for the floating action button for quick photo capture during exercise conduct.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { QuickPhotoFab } from './QuickPhotoFab'
import { ExerciseStatus } from '@/types'

// Mock the photo hooks
vi.mock('../hooks/useCamera', () => ({
  useCamera: vi.fn((onFileSelected) => ({
    fileInputRef: { current: null },
    isCapturing: false,
    openCamera: vi.fn(),
    openGallery: vi.fn(),
    handleFileChange: vi.fn(),
    resetCapture: vi.fn(),
  })),
}))

vi.mock('../hooks/useImageCompression', () => ({
  useImageCompression: vi.fn(() => ({
    compressImage: vi.fn(async (file: File) => ({
      compressed: new Blob(['compressed'], { type: 'image/jpeg' }),
      thumbnail: new Blob(['thumbnail'], { type: 'image/jpeg' }),
      originalFileName: file.name,
      fileSizeBytes: 1024,
    })),
  })),
}))

vi.mock('../hooks/usePhotos', () => ({
  usePhotos: vi.fn(() => ({
    quickPhoto: vi.fn(),
  })),
}))

// Mock ExerciseNavigationContext
const mockUseExerciseNavigation = vi.fn()
vi.mock('@/shared/contexts', () => ({
  useExerciseNavigation: () => mockUseExerciseNavigation(),
}))

const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
}

describe('QuickPhotoFab', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders FAB when exercise is Active', () => {
    mockUseExerciseNavigation.mockReturnValue({
      currentExercise: {
        id: 'ex-1',
        name: 'Test Exercise',
        status: ExerciseStatus.Active,
        userRole: 'Controller',
      },
    })

    render(<QuickPhotoFab exerciseId="ex-1" scenarioTime="T+00:15:00" />, {
      wrapper: createWrapper(),
    })

    const fab = screen.getByRole('button', { name: /quick photo capture/i })
    expect(fab).toBeInTheDocument()
  })

  it('does not render when exercise is not Active', () => {
    mockUseExerciseNavigation.mockReturnValue({
      currentExercise: {
        id: 'ex-1',
        name: 'Test Exercise',
        status: ExerciseStatus.Draft,
        userRole: 'Controller',
      },
    })

    const { container } = render(<QuickPhotoFab exerciseId="ex-1" scenarioTime="T+00:15:00" />, {
      wrapper: createWrapper(),
    })

    expect(container.firstChild).toBeNull()
  })

  it('does not render when no exercise context', () => {
    mockUseExerciseNavigation.mockReturnValue({
      currentExercise: null,
    })

    const { container } = render(<QuickPhotoFab exerciseId="ex-1" scenarioTime="T+00:15:00" />, {
      wrapper: createWrapper(),
    })

    expect(container.firstChild).toBeNull()
  })

  it('shows camera icon when not processing', () => {
    mockUseExerciseNavigation.mockReturnValue({
      currentExercise: {
        id: 'ex-1',
        name: 'Test Exercise',
        status: ExerciseStatus.Active,
        userRole: 'Controller',
      },
    })

    render(<QuickPhotoFab exerciseId="ex-1" scenarioTime="T+00:15:00" />, {
      wrapper: createWrapper(),
    })

    const fab = screen.getByRole('button', { name: /quick photo capture/i })
    expect(fab).toBeInTheDocument()
    // Camera icon is present (FontAwesome renders as SVG)
    expect(fab.querySelector('svg')).toBeInTheDocument()
  })

  it('has hidden file input with camera capture attribute', () => {
    mockUseExerciseNavigation.mockReturnValue({
      currentExercise: {
        id: 'ex-1',
        name: 'Test Exercise',
        status: ExerciseStatus.Active,
        userRole: 'Controller',
      },
    })

    render(<QuickPhotoFab exerciseId="ex-1" scenarioTime="T+00:15:00" />, {
      wrapper: createWrapper(),
    })

    const fileInput = screen.getByLabelText(/camera input/i)
    expect(fileInput).toHaveAttribute('type', 'file')
    expect(fileInput).toHaveAttribute('accept', 'image/*')
    expect(fileInput).toHaveAttribute('capture', 'environment')
    expect(fileInput).toHaveAttribute('hidden')
  })

  it('has correct responsive sizing styles', () => {
    mockUseExerciseNavigation.mockReturnValue({
      currentExercise: {
        id: 'ex-1',
        name: 'Test Exercise',
        status: ExerciseStatus.Active,
        userRole: 'Controller',
      },
    })

    render(<QuickPhotoFab exerciseId="ex-1" scenarioTime="T+00:15:00" />, {
      wrapper: createWrapper(),
    })

    const fab = screen.getByRole('button', { name: /quick photo capture/i })
    // FAB is fixed positioned in bottom-right
    expect(fab).toHaveStyle({
      position: 'fixed',
      bottom: '24px',
      right: '24px',
    })
  })
})
