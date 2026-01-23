import '@testing-library/jest-dom/vitest'
import 'fake-indexeddb/auto'
import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'

// Cleanup after each test
afterEach(() => {
  cleanup()
})

// Mock window.matchMedia for MUI components
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
})

// Mock ResizeObserver for MUI - use a class
class ResizeObserverMock {
  observe = vi.fn()
  unobserve = vi.fn()
  disconnect = vi.fn()
}
globalThis.ResizeObserver = ResizeObserverMock

// Mock crypto.randomUUID for API client
Object.defineProperty(globalThis, 'crypto', {
  value: {
    randomUUID: vi.fn().mockReturnValue('test-uuid-123'),
  },
})

// Mock authService to prevent actual API calls during tests
vi.mock('../features/auth/services/authService', () => ({
  authService: {
    login: vi.fn().mockResolvedValue({
      isSuccess: true,
      accessToken: 'mock-token',
      expiresIn: 900,
      userId: 'test-user-id',
      email: 'test@example.com',
      displayName: 'Test User',
      role: 'Observer',
    }),
    register: vi.fn().mockResolvedValue({
      isSuccess: true,
      accessToken: 'mock-token',
      expiresIn: 900,
      userId: 'test-user-id',
      email: 'test@example.com',
      displayName: 'Test User',
      role: 'Administrator',
      isFirstUser: false,
      isNewAccount: true,
    }),
    logout: vi.fn().mockResolvedValue(undefined),
    refresh: vi.fn().mockResolvedValue({
      isSuccess: true,
      accessToken: 'mock-refreshed-token',
      expiresIn: 900,
      userId: 'test-user-id',
      email: 'test@example.com',
      displayName: 'Test User',
      role: 'Observer',
    }),
    getAvailableMethods: vi.fn().mockResolvedValue([
      { provider: 'Identity', displayName: 'Email & Password', isEnabled: true, isExternal: false },
    ]),
    requestPasswordReset: vi.fn().mockResolvedValue(undefined),
    resetPassword: vi.fn().mockResolvedValue(undefined),
  },
}))
