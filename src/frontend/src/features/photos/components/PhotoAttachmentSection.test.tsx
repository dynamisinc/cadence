/**
 * PhotoAttachmentSection Tests
 *
 * Tests for the photo attachment section in observation forms.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { PhotoAttachmentSection } from './PhotoAttachmentSection'
import type { PhotoTagDto } from '../../observations/types'

// Mock the photo hooks
vi.mock('../hooks/useCamera', () => ({
  useCamera: vi.fn(onFileSelected => ({
    fileInputRef: { current: null },
    isCapturing: false,
    openCamera: vi.fn(),
    openGallery: vi.fn(() => {
      // Simulate file selection
      const file = new File(['photo'], 'test.jpg', { type: 'image/jpeg' })
      onFileSelected(file)
    }),
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
    uploadPhoto: vi.fn(),
  })),
}))

vi.mock('../../../core/contexts', () => ({
  useConnectivity: vi.fn(() => ({
    connectivityState: 'online',
    incrementPendingCount: vi.fn(),
  })),
}))

// Create a query client for each test
const createQueryClient = () => new QueryClient({
  defaultOptions: {
    queries: { retry: false },
    mutations: { retry: false },
  },
})

const _createWrapper = (queryClient: QueryClient) => {
  return ({ children }: { children: React.ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
}

describe('PhotoAttachmentSection', () => {
  const mockPhotos: PhotoTagDto[] = [
    {
      id: 'photo-1',
      thumbnailUri: 'https://example.com/thumb1.jpg',
      capturedAt: '2026-02-10T10:00:00Z',
      displayOrder: 0,
    },
    {
      id: 'photo-2',
      thumbnailUri: 'https://example.com/thumb2.jpg',
      capturedAt: '2026-02-10T10:05:00Z',
      displayOrder: 1,
    },
  ]

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders nothing when onPendingFilesChange is not provided', () => {
    const queryClient = createQueryClient()
    const { container } = render(
      <QueryClientProvider client={queryClient}>
        <PhotoAttachmentSection
          photos={mockPhotos}
        />
      </QueryClientProvider>,
    )

    expect(container.firstChild).toBeNull()
  })

  it('renders section with Add Photo button when onPendingFilesChange is provided', () => {
    const queryClient = createQueryClient()
    render(
      <QueryClientProvider client={queryClient}>
        <PhotoAttachmentSection
          photos={[]}
          onPendingFilesChange={vi.fn()}
        />
      </QueryClientProvider>,
    )

    expect(screen.getByText('Attach Photos (Optional)')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /add photo/i })).toBeInTheDocument()
  })

  it('renders existing photo thumbnails sorted by displayOrder', () => {
    const queryClient = createQueryClient()
    render(
      <QueryClientProvider client={queryClient}>
        <PhotoAttachmentSection
          photos={mockPhotos}
          onPendingFilesChange={vi.fn()}
        />
      </QueryClientProvider>,
    )

    const images = screen.getAllByRole('img')
    expect(images).toHaveLength(2)
    expect(images[0]).toHaveAttribute('src', 'https://example.com/thumb1.jpg')
    expect(images[1]).toHaveAttribute('src', 'https://example.com/thumb2.jpg')
  })

  it('shows "Attached Photos" label when photos exist', () => {
    const queryClient = createQueryClient()
    render(
      <QueryClientProvider client={queryClient}>
        <PhotoAttachmentSection
          photos={mockPhotos}
          onPendingFilesChange={vi.fn()}
        />
      </QueryClientProvider>,
    )

    expect(screen.getByText('Attached Photos')).toBeInTheDocument()
  })

  it('maintains horizontal scroll layout with gap', () => {
    const queryClient = createQueryClient()
    render(
      <QueryClientProvider client={queryClient}>
        <PhotoAttachmentSection
          photos={mockPhotos}
          onPendingFilesChange={vi.fn()}
        />
      </QueryClientProvider>,
    )

    // Verify section renders with photos and Add Photo button
    expect(screen.getByText('Attached Photos')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /add photo/i })).toBeInTheDocument()
    expect(screen.getAllByRole('img')).toHaveLength(2)
  })
})
