import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { devLog, devWarn } from './logger'

describe('devLog', () => {
  let consoleSpy: ReturnType<typeof vi.spyOn>
  let originalDev: boolean

  beforeEach(() => {
    consoleSpy = vi.spyOn(console, 'log').mockImplementation(() => undefined)
    originalDev = import.meta.env.DEV
  })

  afterEach(() => {
    consoleSpy.mockRestore()
    import.meta.env.DEV = originalDev
  })

  it('calls console.log in development mode', () => {
    import.meta.env.DEV = true

    devLog('test message')

    expect(consoleSpy).toHaveBeenCalledWith('test message')
  })

  it('passes multiple arguments to console.log', () => {
    import.meta.env.DEV = true

    devLog('arg1', 'arg2', 42, { key: 'value' })

    expect(consoleSpy).toHaveBeenCalledWith('arg1', 'arg2', 42, { key: 'value' })
  })

  it('does not call console.log in production mode', () => {
    import.meta.env.DEV = false

    devLog('test message')

    expect(consoleSpy).not.toHaveBeenCalled()
  })
})

describe('devWarn', () => {
  let consoleSpy: ReturnType<typeof vi.spyOn>
  let originalDev: boolean

  beforeEach(() => {
    consoleSpy = vi.spyOn(console, 'warn').mockImplementation(() => undefined)
    originalDev = import.meta.env.DEV
  })

  afterEach(() => {
    consoleSpy.mockRestore()
    import.meta.env.DEV = originalDev
  })

  it('calls console.warn in development mode', () => {
    import.meta.env.DEV = true

    devWarn('test warning')

    expect(consoleSpy).toHaveBeenCalledWith('test warning')
  })

  it('passes multiple arguments to console.warn', () => {
    import.meta.env.DEV = true

    devWarn('warn1', 'warn2', { detail: 'info' })

    expect(consoleSpy).toHaveBeenCalledWith('warn1', 'warn2', { detail: 'info' })
  })

  it('does not call console.warn in production mode', () => {
    import.meta.env.DEV = false

    devWarn('test warning')

    expect(consoleSpy).not.toHaveBeenCalled()
  })
})
