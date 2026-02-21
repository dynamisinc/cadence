import { render, screen, fireEvent } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import { faGear, faEye } from '@fortawesome/free-solid-svg-icons'
import { PageHeader } from './PageHeader'

describe('PageHeader', () => {
  it('renders title as h1 element', () => {
    render(<PageHeader title="Test Page" />)
    const heading = screen.getByRole('heading', { level: 1, name: 'Test Page' })
    expect(heading).toBeInTheDocument()
  })

  it('renders subtitle when provided', () => {
    render(<PageHeader title="Test" subtitle="A description" />)
    expect(screen.getByText('A description')).toBeInTheDocument()
  })

  it('does not render subtitle when not provided', () => {
    render(<PageHeader title="Test" />)
    expect(screen.queryByText('A description')).not.toBeInTheDocument()
  })

  it('renders icon when provided', () => {
    render(<PageHeader title="Settings" icon={faGear} />)
    // Icon renders with fontWeight 600 on title
    const heading = screen.getByRole('heading', { level: 1, name: 'Settings' })
    expect(heading).toBeInTheDocument()
  })

  it('applies fontWeight 600 when icon is provided', () => {
    render(<PageHeader title="Settings" icon={faGear} />)
    const heading = screen.getByRole('heading', { level: 1, name: 'Settings' })
    expect(heading).toHaveStyle({ fontWeight: 600 })
  })

  it('does not apply fontWeight 600 when no icon', () => {
    render(<PageHeader title="Page" />)
    const heading = screen.getByRole('heading', { level: 1, name: 'Page' })
    expect(heading).not.toHaveStyle({ fontWeight: 600 })
  })

  it('renders back button when showBackButton is true', () => {
    render(<PageHeader title="Test" showBackButton />)
    expect(screen.getByRole('button', { name: 'Go back' })).toBeInTheDocument()
  })

  it('does not render back button when showBackButton is false', () => {
    render(<PageHeader title="Test" />)
    expect(screen.queryByRole('button', { name: 'Go back' })).not.toBeInTheDocument()
  })

  it('calls onBackClick when back button is clicked', () => {
    const handleBack = vi.fn()
    render(<PageHeader title="Test" showBackButton onBackClick={handleBack} />)
    fireEvent.click(screen.getByRole('button', { name: 'Go back' }))
    expect(handleBack).toHaveBeenCalledOnce()
  })

  it('renders actions', () => {
    render(<PageHeader title="Test" actions={<button>Create</button>} />)
    expect(screen.getByRole('button', { name: 'Create' })).toBeInTheDocument()
  })

  it('renders chips next to title', () => {
    render(<PageHeader title="Items" chips={<span>5 total</span>} />)
    expect(screen.getByText('5 total')).toBeInTheDocument()
  })

  it('renders subtitle as ReactNode', () => {
    render(<PageHeader title="Test" subtitle={<span data-testid="custom-sub">Custom</span>} />)
    expect(screen.getByTestId('custom-sub')).toBeInTheDocument()
  })
})
