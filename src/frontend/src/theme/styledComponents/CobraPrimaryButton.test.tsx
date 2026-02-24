import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '../../test/testUtils'
import { CobraPrimaryButton } from './CobraPrimaryButton'

describe('CobraPrimaryButton', () => {
  it('renders with children text', () => {
    render(<CobraPrimaryButton>Save</CobraPrimaryButton>)
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument()
  })

  it('calls onClick when clicked', () => {
    const handleClick = vi.fn()
    render(<CobraPrimaryButton onClick={handleClick}>Save</CobraPrimaryButton>)

    fireEvent.click(screen.getByRole('button'))
    expect(handleClick).toHaveBeenCalledTimes(1)
  })

  it('can be disabled', () => {
    render(<CobraPrimaryButton disabled>Save</CobraPrimaryButton>)
    expect(screen.getByRole('button')).toBeDisabled()
  })

  it('applies navy blue background color', () => {
    render(<CobraPrimaryButton>Save</CobraPrimaryButton>)
    const button = screen.getByRole('button')
    expect(button).toHaveStyle({ textTransform: 'none' })
  })

  it('renders with start icon', () => {
    render(
      <CobraPrimaryButton startIcon={<span data-testid="icon">+</span>}>
        Add
      </CobraPrimaryButton>,
    )
    expect(screen.getByTestId('icon')).toBeInTheDocument()
  })

  it('applies rounded border radius', () => {
    render(<CobraPrimaryButton>Save</CobraPrimaryButton>)
    const button = screen.getByRole('button')
    expect(button).toHaveStyle({ borderRadius: '50px' })
  })
})
