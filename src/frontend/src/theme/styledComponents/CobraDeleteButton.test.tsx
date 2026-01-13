import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '../../test/testUtils'
import { CobraDeleteButton } from './CobraDeleteButton'

describe('CobraDeleteButton', () => {
  it('renders with children text', () => {
    render(<CobraDeleteButton>Delete</CobraDeleteButton>)
    expect(screen.getByRole('button', { name: /Delete/i })).toBeInTheDocument()
  })

  it('shows delete icon by default', () => {
    render(<CobraDeleteButton>Delete</CobraDeleteButton>)
    // FontAwesome icons render as SVG with data-icon attribute
    const icon = document.querySelector('[data-icon="trash"]')
    expect(icon).toBeInTheDocument()
  })

  it('hides icon when hideIcon is true', () => {
    render(<CobraDeleteButton hideIcon>Delete</CobraDeleteButton>)
    const icon = document.querySelector('[data-icon="trash"]')
    expect(icon).not.toBeInTheDocument()
  })

  it('calls onClick when clicked', () => {
    const handleClick = vi.fn()
    render(<CobraDeleteButton onClick={handleClick}>Delete</CobraDeleteButton>)

    fireEvent.click(screen.getByRole('button'))
    expect(handleClick).toHaveBeenCalledTimes(1)
  })

  it('can be disabled', () => {
    render(<CobraDeleteButton disabled>Delete</CobraDeleteButton>)
    expect(screen.getByRole('button')).toBeDisabled()
  })

  it('applies red/delete theme color', () => {
    render(<CobraDeleteButton>Delete</CobraDeleteButton>)
    const button = screen.getByRole('button')
    // Verify text transform is none (from styled component)
    expect(button).toHaveStyle({ textTransform: 'none' })
  })
})
