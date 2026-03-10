import { describe, it, expect } from 'vitest'
import { isNetworkError } from './networkErrors'

/**
 * Helper to create an Axios-style error object with a code property.
 */
function axiosError(code: string, message = ''): object {
  return { code, message }
}

/**
 * Helper to create a plain Error-like object with only a message.
 */
function messageError(message: string): object {
  return { message }
}

describe('isNetworkError', () => {
  // True cases - Axios error codes

  it('returns true for ERR_NETWORK code', () => {
    expect(isNetworkError(axiosError('ERR_NETWORK'))).toBe(true)
  })

  it('returns true for ECONNABORTED code', () => {
    expect(isNetworkError(axiosError('ECONNABORTED'))).toBe(true)
  })

  it('returns true for ECONNREFUSED code', () => {
    expect(isNetworkError(axiosError('ECONNREFUSED'))).toBe(true)
  })

  it('returns true for ETIMEDOUT code', () => {
    expect(isNetworkError(axiosError('ETIMEDOUT'))).toBe(true)
  })

  // True cases - browser error messages

  it('returns true for "Network Error" message', () => {
    expect(isNetworkError(messageError('Network Error'))).toBe(true)
  })

  it('returns true for "Failed to fetch" message', () => {
    expect(isNetworkError(messageError('Failed to fetch'))).toBe(true)
  })

  it('returns true for "Load failed" message', () => {
    expect(isNetworkError(messageError('Load failed'))).toBe(true)
  })

  it('returns true for message containing "net::"', () => {
    expect(isNetworkError(messageError('net::ERR_CONNECTION_REFUSED'))).toBe(true)
  })

  it('returns true for "timeout" in message', () => {
    expect(isNetworkError(messageError('Request timeout exceeded'))).toBe(true)
  })

  it('returns true for "NetworkError" in message', () => {
    expect(isNetworkError(messageError('A NetworkError occurred'))).toBe(true)
  })

  // False cases

  it('returns false for 401 response error', () => {
    const error = {
      response: { status: 401 },
      message: 'Request failed with status code 401',
    }
    expect(isNetworkError(error)).toBe(false)
  })

  it('returns false for 404 response error', () => {
    const error = {
      response: { status: 404 },
      message: 'Request failed with status code 404',
    }
    expect(isNetworkError(error)).toBe(false)
  })

  it('returns false for generic Error', () => {
    expect(isNetworkError(new Error('Something went wrong'))).toBe(false)
  })

  // Edge cases

  it('returns false for null input', () => {
    expect(isNetworkError(null)).toBe(false)
  })

  it('returns false for undefined input', () => {
    expect(isNetworkError(undefined)).toBe(false)
  })

  it('returns false for empty object', () => {
    expect(isNetworkError({})).toBe(false)
  })

  it('returns false for string input', () => {
    expect(isNetworkError('Network Error')).toBe(false)
  })

  it('returns false for number input', () => {
    expect(isNetworkError(42)).toBe(false)
  })
})
