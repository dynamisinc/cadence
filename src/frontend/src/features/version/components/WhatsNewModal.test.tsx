import { screen, fireEvent } from '@testing-library/react'
import { render } from '../../../test/test-utils'
import { describe, it, expect, vi } from 'vitest'
import { WhatsNewModal } from './WhatsNewModal'

// Mock the version module
vi.mock('@/config/version', () => ({
  appVersion: {
    version: '1.0.0',
    buildDate: '2026-01-30T00:00:00.000Z',
    commitSha: 'abc1234',
  },
}))

const renderModal = (props = {}) => {
  const defaultProps = {
    open: true,
    onDismiss: vi.fn(),
  }

  return render(<WhatsNewModal {...defaultProps} {...props} />)
}

describe('WhatsNewModal', () => {
  it('renders version number', () => {
    renderModal()

    expect(screen.getByText(/Version 1.0.0/)).toBeInTheDocument()
  })

  it('renders What\'s New title', () => {
    renderModal()

    expect(screen.getByText(/What's New in Cadence/)).toBeInTheDocument()
  })

  it('calls onDismiss when Got it button is clicked', () => {
    const onDismiss = vi.fn()
    renderModal({ onDismiss })

    fireEvent.click(screen.getByText('Got it'))

    expect(onDismiss).toHaveBeenCalledTimes(1)
  })

  it('calls onViewAllNotes when View all release notes is clicked', () => {
    const onDismiss = vi.fn()
    const onViewAllNotes = vi.fn()
    renderModal({ onDismiss, onViewAllNotes })

    fireEvent.click(screen.getByText('View all release notes'))

    expect(onDismiss).toHaveBeenCalled()
    expect(onViewAllNotes).toHaveBeenCalled()
  })

  it('does not render when open is false', () => {
    renderModal({ open: false })

    expect(screen.queryByText(/What's New in Cadence/)).not.toBeInTheDocument()
  })
})
