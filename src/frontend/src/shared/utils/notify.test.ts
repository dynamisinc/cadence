import { describe, it, expect, vi, beforeEach } from 'vitest'

const mockToast = {
  success: vi.fn(() => 'toast-id-1'),
  error: vi.fn(() => 'toast-id-2'),
  warning: vi.fn(() => 'toast-id-3'),
  info: vi.fn(() => 'toast-id-4'),
  dismiss: vi.fn(),
}

vi.mock('react-toastify', () => ({
  toast: mockToast,
}))

// Import after mock is set up
const { notify, _resetNotifyForTesting } = await import('./notify')

describe('notify', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    _resetNotifyForTesting()
  })

  describe('delegates to toast methods', () => {
    it('calls toast.success with message and options', () => {
      notify.success('Created', { autoClose: 2000 })
      expect(mockToast.success).toHaveBeenCalledWith('Created', { autoClose: 2000 })
    })

    it('calls toast.error with message and options', () => {
      notify.error('Failed', { toastId: 'err' })
      expect(mockToast.error).toHaveBeenCalledWith('Failed', { toastId: 'err' })
    })

    it('calls toast.warning with message and options', () => {
      notify.warning('Watch out')
      expect(mockToast.warning).toHaveBeenCalledWith('Watch out', undefined)
    })

    it('calls toast.info with message and options', () => {
      notify.info('FYI')
      expect(mockToast.info).toHaveBeenCalledWith('FYI', undefined)
    })

    it('exposes toast.dismiss directly', () => {
      notify.dismiss('some-id')
      expect(mockToast.dismiss).toHaveBeenCalledWith('some-id')
    })
  })

  describe('deduplication by message text', () => {
    it('suppresses identical message within dedup window', () => {
      vi.useFakeTimers()

      notify.success('Item saved')
      notify.success('Item saved')
      notify.success('Item saved')

      expect(mockToast.success).toHaveBeenCalledTimes(1)

      vi.useRealTimers()
    })

    it('allows same message after dedup window expires', () => {
      vi.useFakeTimers()

      notify.success('Item saved')
      expect(mockToast.success).toHaveBeenCalledTimes(1)

      vi.advanceTimersByTime(3001)

      notify.success('Item saved')
      expect(mockToast.success).toHaveBeenCalledTimes(2)

      vi.useRealTimers()
    })

    it('allows different messages within dedup window', () => {
      vi.useFakeTimers()

      notify.success('Item created')
      notify.success('Item updated')

      expect(mockToast.success).toHaveBeenCalledTimes(2)

      vi.useRealTimers()
    })
  })

  describe('deduplication by toastId', () => {
    it('uses toastId as dedup key when provided', () => {
      vi.useFakeTimers()

      notify.error('Error A', { toastId: 'save-error' })
      notify.error('Error B', { toastId: 'save-error' })

      expect(mockToast.error).toHaveBeenCalledTimes(1)
      expect(mockToast.error).toHaveBeenCalledWith('Error A', { toastId: 'save-error' })

      vi.useRealTimers()
    })

    it('allows same message with different toastIds', () => {
      vi.useFakeTimers()

      notify.error('Something failed', { toastId: 'error-1' })
      notify.error('Something failed', { toastId: 'error-2' })

      expect(mockToast.error).toHaveBeenCalledTimes(2)

      vi.useRealTimers()
    })
  })

  describe('cross-method deduplication', () => {
    it('deduplicates across different toast methods with same message', () => {
      vi.useFakeTimers()

      notify.success('Connection restored')
      notify.info('Connection restored')

      // Same message key, so second call is suppressed
      expect(mockToast.success).toHaveBeenCalledTimes(1)
      expect(mockToast.info).toHaveBeenCalledTimes(0)

      vi.useRealTimers()
    })
  })

  describe('return values', () => {
    it('returns toast id on first call', () => {
      vi.useFakeTimers()
      const id = notify.success('New toast')
      expect(id).toBe('toast-id-1')
      vi.useRealTimers()
    })

    it('returns undefined for suppressed duplicate', () => {
      vi.useFakeTimers()
      notify.success('Duplicate test')
      const id = notify.success('Duplicate test')
      expect(id).toBeUndefined()
      vi.useRealTimers()
    })
  })
})
