import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor, within } from '../../../test/testUtils'
import { NotesPage } from './NotesPage'
import { useNotes } from '../hooks/useNotes'
import type { NoteDto } from '../types'

// Mock the useNotes hook
vi.mock('../hooks/useNotes', () => ({
  useNotes: vi.fn(),
}))

// Mock window.confirm
vi.stubGlobal('confirm', vi.fn())

describe('NotesPage', () => {
  const mockNotes: NoteDto[] = [
    {
      id: '1',
      title: 'First Note',
      content: 'First note content',
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    },
    {
      id: '2',
      title: 'Second Note',
      content: null,
      createdAt: '2024-01-02T00:00:00Z',
      updatedAt: '2024-01-02T00:00:00Z',
    },
  ]

  const mockUseNotes = {
    notes: mockNotes,
    loading: false,
    error: null,
    fetchNotes: vi.fn(),
    createNote: vi.fn(),
    updateNote: vi.fn(),
    deleteNote: vi.fn(),
  }

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(useNotes).mockReturnValue(mockUseNotes)
    vi.mocked(window.confirm).mockReturnValue(true)
  })

  describe('rendering', () => {
    it('renders the page title', () => {
      render(<NotesPage />)
      expect(screen.getByText('Notes')).toBeInTheDocument()
    })

    it('renders the New Note button', () => {
      render(<NotesPage />)
      expect(screen.getByRole('button', { name: /New Note/i })).toBeInTheDocument()
    })

    it('renders all notes', () => {
      render(<NotesPage />)
      expect(screen.getByText('First Note')).toBeInTheDocument()
      expect(screen.getByText('Second Note')).toBeInTheDocument()
    })

    it('renders note content when available', () => {
      render(<NotesPage />)
      expect(screen.getByText('First note content')).toBeInTheDocument()
    })

    it('shows loading spinner when loading and no notes', () => {
      vi.mocked(useNotes).mockReturnValue({
        ...mockUseNotes,
        notes: [],
        loading: true,
      })

      render(<NotesPage />)
      expect(screen.getByRole('progressbar')).toBeInTheDocument()
    })

    it('shows empty state message when no notes', () => {
      vi.mocked(useNotes).mockReturnValue({
        ...mockUseNotes,
        notes: [],
      })

      render(<NotesPage />)
      expect(
        screen.getByText(/No notes yet. Create your first note!/i),
      ).toBeInTheDocument()
    })

    it('shows error message when there is an error', () => {
      vi.mocked(useNotes).mockReturnValue({
        ...mockUseNotes,
        notes: [],
        error: 'Failed to load notes',
      })

      render(<NotesPage />)
      expect(screen.getByText('Failed to load notes')).toBeInTheDocument()
    })
  })

  describe('create note dialog', () => {
    it('opens create dialog when New Note button is clicked', async () => {
      render(<NotesPage />)

      fireEvent.click(screen.getByRole('button', { name: /New Note/i }))

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const dialog = screen.getByRole('dialog')
      expect(within(dialog).getByText('New Note')).toBeInTheDocument()
    })

    it('closes dialog when Cancel is clicked', async () => {
      render(<NotesPage />)

      fireEvent.click(screen.getByRole('button', { name: /New Note/i }))

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      fireEvent.click(screen.getByRole('button', { name: /Cancel/i }))

      await waitFor(() => {
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
      })
    })

    it('creates note when form is submitted', async () => {
      const mockCreateNote = vi.fn().mockResolvedValue({
        id: '3',
        title: 'New Note Title',
        content: 'New content',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      })

      vi.mocked(useNotes).mockReturnValue({
        ...mockUseNotes,
        createNote: mockCreateNote,
      })

      render(<NotesPage />)

      // Open dialog
      fireEvent.click(screen.getByRole('button', { name: /New Note/i }))

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      // Fill in form - get inputs from dialog
      const dialog = screen.getByRole('dialog')
      const titleInput = within(dialog).getByLabelText(/Title/i)
      const contentInput = within(dialog).getByLabelText(/Content/i)

      fireEvent.change(titleInput, { target: { value: 'New Note Title' } })
      fireEvent.change(contentInput, { target: { value: 'New content' } })

      // Submit
      fireEvent.click(screen.getByRole('button', { name: /Create Note/i }))

      await waitFor(() => {
        expect(mockCreateNote).toHaveBeenCalledWith({
          title: 'New Note Title',
          content: 'New content',
        })
      })
    })

    it('disables create button when title is empty', async () => {
      render(<NotesPage />)

      fireEvent.click(screen.getByRole('button', { name: /New Note/i }))

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      expect(screen.getByRole('button', { name: /Create Note/i })).toBeDisabled()
    })

    it('enables create button when title is entered', async () => {
      render(<NotesPage />)

      fireEvent.click(screen.getByRole('button', { name: /New Note/i }))

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const dialog = screen.getByRole('dialog')
      const titleInput = within(dialog).getByLabelText(/Title/i)

      fireEvent.change(titleInput, { target: { value: 'Test Title' } })

      expect(screen.getByRole('button', { name: /Create Note/i })).not.toBeDisabled()
    })
  })

  describe('edit note dialog', () => {
    it('opens edit dialog when edit icon is clicked', async () => {
      render(<NotesPage />)

      // Click the first edit button (FontAwesome icons have data-icon attribute)
      const editIcons = document.querySelectorAll('[data-icon="pen"]')
      fireEvent.click(editIcons[0].closest('button')!)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      const dialog = screen.getByRole('dialog')
      expect(within(dialog).getByText('Edit Note')).toBeInTheDocument()
    })

    it('populates form with existing note data', async () => {
      render(<NotesPage />)

      const editIcons = document.querySelectorAll('[data-icon="pen"]')
      fireEvent.click(editIcons[0].closest('button')!)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      expect(screen.getByDisplayValue('First Note')).toBeInTheDocument()
      expect(screen.getByDisplayValue('First note content')).toBeInTheDocument()
    })

    it('updates note when Save Changes is clicked', async () => {
      const mockUpdateNote = vi.fn().mockResolvedValue({
        ...mockNotes[0],
        title: 'Updated Title',
      })

      vi.mocked(useNotes).mockReturnValue({
        ...mockUseNotes,
        updateNote: mockUpdateNote,
      })

      render(<NotesPage />)

      // Open edit dialog
      const editIcons = document.querySelectorAll('[data-icon="pen"]')
      fireEvent.click(editIcons[0].closest('button')!)

      await waitFor(() => {
        expect(screen.getByRole('dialog')).toBeInTheDocument()
      })

      // Update title
      const dialog = screen.getByRole('dialog')
      const titleInput = within(dialog).getByLabelText(/Title/i)

      fireEvent.change(titleInput, { target: { value: 'Updated Title' } })

      // Save
      fireEvent.click(screen.getByRole('button', { name: /Save Changes/i }))

      await waitFor(() => {
        expect(mockUpdateNote).toHaveBeenCalledWith('1', {
          title: 'Updated Title',
          content: 'First note content',
        })
      })
    })
  })

  describe('delete note', () => {
    it('shows confirmation when delete icon is clicked', () => {
      render(<NotesPage />)

      // FontAwesome icons have data-icon attribute
      const deleteIcons = document.querySelectorAll('[data-icon="trash"]')
      fireEvent.click(deleteIcons[0].closest('button')!)

      expect(window.confirm).toHaveBeenCalledWith(
        'Are you sure you want to delete this note?',
      )
    })

    it('deletes note when confirmed', async () => {
      const mockDeleteNote = vi.fn().mockResolvedValue(undefined)

      vi.mocked(useNotes).mockReturnValue({
        ...mockUseNotes,
        deleteNote: mockDeleteNote,
      })

      render(<NotesPage />)

      const deleteIcons = document.querySelectorAll('[data-icon="trash"]')
      fireEvent.click(deleteIcons[0].closest('button')!)

      await waitFor(() => {
        expect(mockDeleteNote).toHaveBeenCalledWith('1')
      })
    })

    it('does not delete note when cancelled', () => {
      vi.mocked(window.confirm).mockReturnValue(false)
      const mockDeleteNote = vi.fn()

      vi.mocked(useNotes).mockReturnValue({
        ...mockUseNotes,
        deleteNote: mockDeleteNote,
      })

      render(<NotesPage />)

      const deleteIcons = document.querySelectorAll('[data-icon="trash"]')
      fireEvent.click(deleteIcons[0].closest('button')!)

      expect(mockDeleteNote).not.toHaveBeenCalled()
    })
  })

  describe('accessibility', () => {
    it('has proper heading hierarchy', () => {
      render(<NotesPage />)

      const heading = screen.getByRole('heading', { name: 'Notes' })
      expect(heading.tagName).toBe('H5')
    })

    it('edit and delete buttons are accessible', () => {
      render(<NotesPage />)

      // FontAwesome icons have data-icon attribute
      const editButtons = screen.getAllByRole('button').filter(btn =>
        btn.querySelector('[data-icon="pen"]'),
      )
      const deleteButtons = screen.getAllByRole('button').filter(btn =>
        btn.querySelector('[data-icon="trash"]'),
      )

      expect(editButtons.length).toBeGreaterThan(0)
      expect(deleteButtons.length).toBeGreaterThan(0)
    })
  })
})
