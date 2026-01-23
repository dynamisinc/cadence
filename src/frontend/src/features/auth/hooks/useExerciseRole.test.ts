/**
 * useExerciseRole Hook Tests
 *
 * @module features/auth
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { useExerciseRole } from './useExerciseRole';
import { roleResolutionService } from '../services/roleResolutionService';
import { useAuth } from '@/contexts/AuthContext';

vi.mock('../services/roleResolutionService');
vi.mock('@/contexts/AuthContext');

describe('useExerciseRole', () => {
  const mockUser = {
    id: 'user1',
    email: 'user@example.com',
    displayName: 'Test User',
    role: 'User', // System role
    status: 'Active',
  };

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useAuth).mockReturnValue({
      user: mockUser,
      isAuthenticated: true,
      isLoading: false,
      accessToken: 'token',
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      refreshAccessToken: vi.fn(),
    });
  });

  it('returns system role when user has no exercise role', async () => {
    const exerciseId = '123e4567-e89b-12d3-a456-426614174000';
    vi.mocked(roleResolutionService.getUserExerciseRole).mockResolvedValueOnce(null);

    const { result } = renderHook(() => useExerciseRole(exerciseId));

    // Initially loading
    expect(result.current.isLoading).toBe(true);

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    // Should use system role when no exercise role exists
    expect(result.current.effectiveRole).toBe('Observer'); // User system role defaults to Observer
    expect(result.current.systemRole).toBe('User');
    expect(result.current.exerciseRole).toBeNull();
  });

  it('returns exercise role when user is participant', async () => {
    const exerciseId = '123e4567-e89b-12d3-a456-426614174000';
    vi.mocked(roleResolutionService.getUserExerciseRole).mockResolvedValueOnce('Controller');

    const { result } = renderHook(() => useExerciseRole(exerciseId));

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.effectiveRole).toBe('Controller');
    expect(result.current.systemRole).toBe('User');
    expect(result.current.exerciseRole).toBe('Controller');
  });

  it('exercise role overrides system role', async () => {
    const exerciseId = '123e4567-e89b-12d3-a456-426614174000';
    const adminUser = {
      ...mockUser,
      role: 'Admin', // System admin
    };

    vi.mocked(useAuth).mockReturnValue({
      user: adminUser,
      isAuthenticated: true,
      isLoading: false,
      accessToken: 'token',
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      refreshAccessToken: vi.fn(),
    });

    // Admin assigned as Observer in this exercise
    vi.mocked(roleResolutionService.getUserExerciseRole).mockResolvedValueOnce('Observer');

    const { result } = renderHook(() => useExerciseRole(exerciseId));

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    // Exercise role should override system role
    expect(result.current.effectiveRole).toBe('Observer');
    expect(result.current.systemRole).toBe('Admin');
    expect(result.current.exerciseRole).toBe('Observer');
  });

  it('can check permissions via can function', async () => {
    const exerciseId = '123e4567-e89b-12d3-a456-426614174000';
    vi.mocked(roleResolutionService.getUserExerciseRole).mockResolvedValueOnce('Controller');

    const { result } = renderHook(() => useExerciseRole(exerciseId));

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    // Controller permissions
    expect(result.current.can('view_exercise')).toBe(true);
    expect(result.current.can('fire_inject')).toBe(true);
    expect(result.current.can('add_observation')).toBe(true);

    // No Director permissions
    expect(result.current.can('manage_participants')).toBe(false);
    expect(result.current.can('edit_exercise')).toBe(false);
  });

  it('handles null exercise ID gracefully', async () => {
    const { result } = renderHook(() => useExerciseRole(null));

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.effectiveRole).toBe('Observer');
    expect(result.current.exerciseRole).toBeNull();
  });

  it('handles unauthenticated user', async () => {
    vi.mocked(useAuth).mockReturnValue({
      user: null,
      isAuthenticated: false,
      isLoading: false,
      accessToken: null,
      login: vi.fn(),
      register: vi.fn(),
      logout: vi.fn(),
      refreshAccessToken: vi.fn(),
    });

    const exerciseId = '123e4567-e89b-12d3-a456-426614174000';

    const { result } = renderHook(() => useExerciseRole(exerciseId));

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.effectiveRole).toBe('Observer');
    expect(result.current.can('fire_inject')).toBe(false);
  });

  it('handles service error gracefully', async () => {
    const exerciseId = '123e4567-e89b-12d3-a456-426614174000';
    vi.mocked(roleResolutionService.getUserExerciseRole).mockRejectedValueOnce(
      new Error('Network error')
    );

    const { result } = renderHook(() => useExerciseRole(exerciseId));

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    // Should fall back to system role on error
    expect(result.current.effectiveRole).toBe('Observer');
    expect(result.current.exerciseRole).toBeNull();
  });
});
