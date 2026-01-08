import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { validateEnvironment, getEnv } from './validateEnv'

describe('validateEnv', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_API_URL', 'http://localhost:7071')
    vi.stubEnv('VITE_SIGNALR_URL', 'http://localhost:7071/api')
  })

  afterEach(() => {
    vi.unstubAllEnvs()
  })

  describe('validateEnvironment', () => {
    it('returns valid when all required vars are set', () => {
      const result = validateEnvironment()

      expect(result.isValid).toBe(true)
      expect(result.errors).toHaveLength(0)
    })

    it('returns error when VITE_API_URL is missing', () => {
      vi.stubEnv('VITE_API_URL', '')

      const result = validateEnvironment()

      expect(result.isValid).toBe(false)
      expect(result.errors.length).toBeGreaterThan(0)
      expect(result.errors[0]).toContain('VITE_API_URL')
    })

    it('returns warning but valid when optional vars are missing', () => {
      vi.stubEnv('VITE_SIGNALR_URL', '')

      const result = validateEnvironment()

      expect(result.isValid).toBe(true)
      expect(result.warnings.length).toBeGreaterThan(0)
    })
  })

  describe('getEnv', () => {
    it('returns environment variables', () => {
      const env = getEnv()

      expect(env.apiUrl).toBe('http://localhost:7071')
      expect(env.signalRUrl).toBe('http://localhost:7071/api')
    })

    it('returns empty string for missing signalRUrl', () => {
      vi.stubEnv('VITE_SIGNALR_URL', '')

      const env = getEnv()

      expect(env.signalRUrl).toBe('')
    })
  })
})
