import { describe, it, expect, vi, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import { render } from '../../test/testUtils'
import userEvent from '@testing-library/user-event'
import { SuggestionTextField } from './SuggestionTextField'

const mockSuggestions = [
  'County Emergency Manager',
  'County Executive Office',
  'City Manager',
  'Fire Department',
]

describe('SuggestionTextField', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('renders with label and placeholder', () => {
    render(
      <SuggestionTextField
        label="From (Source)"
        value=""
        onChange={vi.fn()}
        suggestions={[]}
        placeholder="e.g., County Emergency Manager"
      />,
    )

    expect(screen.getByLabelText(/from \(source\)/i)).toBeInTheDocument()
  })

  it('shows suggestions dropdown when focused', async () => {
    render(
      <SuggestionTextField
        label="Source"
        value=""
        onChange={vi.fn()}
        suggestions={mockSuggestions}
      />,
    )

    const input = screen.getByLabelText(/source/i)
    await userEvent.click(input)

    await waitFor(() => {
      expect(screen.getByText('County Emergency Manager')).toBeInTheDocument()
      expect(screen.getByText('Fire Department')).toBeInTheDocument()
    })
  })

  it('filters suggestions as user types', async () => {
    render(
      <SuggestionTextField
        label="Source"
        value=""
        onChange={vi.fn()}
        suggestions={['County Emergency Manager', 'County Executive Office']}
      />,
    )

    const input = screen.getByLabelText(/source/i)
    await userEvent.type(input, 'County')

    await waitFor(() => {
      expect(screen.getByText('County Emergency Manager')).toBeInTheDocument()
      expect(screen.getByText('County Executive Office')).toBeInTheDocument()
    })
  })

  it('calls onChange when a suggestion is selected', async () => {
    const handleChange = vi.fn()

    render(
      <SuggestionTextField
        label="Source"
        value=""
        onChange={handleChange}
        suggestions={mockSuggestions}
      />,
    )

    const input = screen.getByLabelText(/source/i)
    await userEvent.click(input)

    await waitFor(() => {
      expect(screen.getByText('Fire Department')).toBeInTheDocument()
    })

    await userEvent.click(screen.getByText('Fire Department'))

    expect(handleChange).toHaveBeenCalledWith('Fire Department')
  })

  it('allows free-text entry not in suggestions', async () => {
    const handleChange = vi.fn()

    render(
      <SuggestionTextField
        label="Source"
        value=""
        onChange={handleChange}
        suggestions={mockSuggestions}
      />,
    )

    const input = screen.getByLabelText(/source/i)
    await userEvent.type(input, 'New Custom Value')

    expect(handleChange).toHaveBeenCalledWith('New Custom Value')
  })

  it('shows loading spinner when isLoading is true', async () => {
    render(
      <SuggestionTextField
        label="Source"
        value=""
        onChange={vi.fn()}
        suggestions={[]}
        isLoading
      />,
    )

    const input = screen.getByLabelText(/source/i)
    await userEvent.click(input)

    expect(screen.getByRole('progressbar')).toBeInTheDocument()
  })

  it('works as plain text input when suggestions are empty', async () => {
    const handleChange = vi.fn()

    render(
      <SuggestionTextField
        label="Source"
        value=""
        onChange={handleChange}
        suggestions={[]}
      />,
    )

    const input = screen.getByLabelText(/source/i)
    await userEvent.type(input, 'Manual entry')

    expect(handleChange).toHaveBeenCalledWith('Manual entry')
  })

  it('shows required asterisk when required', () => {
    render(
      <SuggestionTextField
        label="Target"
        value=""
        onChange={vi.fn()}
        suggestions={[]}
        required
      />,
    )

    const input = screen.getByLabelText(/target/i)
    expect(input).toBeRequired()
  })

  it('displays helper text', () => {
    render(
      <SuggestionTextField
        label="Source"
        value=""
        onChange={vi.fn()}
        suggestions={[]}
        helperText="Simulated sender"
      />,
    )

    expect(screen.getByText('Simulated sender')).toBeInTheDocument()
  })

  it('shows error state', () => {
    render(
      <SuggestionTextField
        label="Target"
        value=""
        onChange={vi.fn()}
        suggestions={[]}
        error
        helperText="Target is required"
      />,
    )

    expect(screen.getByText('Target is required')).toBeInTheDocument()
  })

  it('calls onFilterChange after debounce when typing', async () => {
    const handleFilterChange = vi.fn()

    render(
      <SuggestionTextField
        label="Source"
        value=""
        onChange={vi.fn()}
        suggestions={[]}
        onFilterChange={handleFilterChange}
      />,
    )

    const input = screen.getByLabelText(/source/i)
    await userEvent.type(input, 'County')

    // After debounce settles, onFilterChange should have been called
    await waitFor(() => {
      expect(handleFilterChange).toHaveBeenCalled()
    })

    // The last call should include the typed text
    const lastCall = handleFilterChange.mock.calls[handleFilterChange.mock.calls.length - 1]
    expect(lastCall[0]).toContain('County')
  })

  it('displays the controlled value', () => {
    render(
      <SuggestionTextField
        label="Source"
        value="County Emergency Manager"
        onChange={vi.fn()}
        suggestions={mockSuggestions}
      />,
    )

    const input = screen.getByLabelText(/source/i) as HTMLInputElement
    expect(input.value).toBe('County Emergency Manager')
  })
})
