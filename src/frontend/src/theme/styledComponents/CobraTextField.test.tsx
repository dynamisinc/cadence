import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '../../test/testUtils'
import { CobraTextField } from './CobraTextField'

describe('CobraTextField', () => {
  it('renders with label', () => {
    render(<CobraTextField label="Title" />)
    expect(screen.getByLabelText('Title')).toBeInTheDocument()
  })

  it('displays value', () => {
    render(<CobraTextField label="Title" value="Test Value" onChange={() => {}} />)
    expect(screen.getByDisplayValue('Test Value')).toBeInTheDocument()
  })

  it('calls onChange when text is entered', () => {
    const handleChange = vi.fn()
    render(<CobraTextField label="Title" onChange={handleChange} />)

    const input = screen.getByRole('textbox')
    fireEvent.change(input, { target: { value: 'New Value' } })

    expect(handleChange).toHaveBeenCalled()
  })

  it('can be required', () => {
    render(<CobraTextField label="Title" required />)
    // MUI adds * to required field labels
    expect(screen.getByLabelText(/Title/)).toBeRequired()
  })

  it('can be disabled', () => {
    render(<CobraTextField label="Title" disabled />)
    expect(screen.getByRole('textbox')).toBeDisabled()
  })

  it('supports multiline mode', () => {
    const { container } = render(<CobraTextField label="Content" multiline rows={4} />)
    // MUI creates a textarea for multiline
    const textarea = container.querySelector('textarea')
    expect(textarea).toBeInTheDocument()
  })

  it('supports fullWidth prop', () => {
    const { container } = render(<CobraTextField label="Title" fullWidth />)
    const textField = container.querySelector('.MuiFormControl-root')
    expect(textField).toHaveClass('MuiFormControl-fullWidth')
  })

  it('displays helper text', () => {
    render(<CobraTextField label="Title" helperText="Enter a title" />)
    expect(screen.getByText('Enter a title')).toBeInTheDocument()
  })

  it('displays error state', () => {
    render(
      <CobraTextField
        label="Title"
        error
        helperText="Title is required"
      />,
    )
    expect(screen.getByText('Title is required')).toBeInTheDocument()
  })

  it('supports placeholder', () => {
    render(<CobraTextField label="Title" placeholder="Enter title here" />)
    expect(screen.getByPlaceholderText('Enter title here')).toBeInTheDocument()
  })
})
