import { useState, useEffect } from 'react';
import { apiClient } from '@/core/services/api';
import type { ApiVersionInfo } from '../types';

interface UseApiVersionResult {
  apiVersion: ApiVersionInfo | null;
  isConnected: boolean;
  isLoading: boolean;
  error: Error | null;
}

/**
 * Hook to fetch API version information.
 * Caches result and indicates connection status.
 */
export function useApiVersion(): UseApiVersionResult {
  const [apiVersion, setApiVersion] = useState<ApiVersionInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    const fetchVersion = async () => {
      try {
        const response = await apiClient.get<ApiVersionInfo>('/version');
        setApiVersion(response.data);
        setError(null);
      } catch (err) {
        setError(err instanceof Error ? err : new Error('Failed to fetch API version'));
        setApiVersion(null);
      } finally {
        setIsLoading(false);
      }
    };

    fetchVersion();
  }, []);

  return {
    apiVersion,
    isConnected: apiVersion !== null && error === null,
    isLoading,
    error,
  };
}
