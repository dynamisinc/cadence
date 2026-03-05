import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { eulaService } from '../services/eulaService'

export const eulaKeys = {
  all: ['eula'] as const,
  status: () => [...eulaKeys.all, 'status'] as const,
}

export function useEulaStatus(enabled: boolean) {
  return useQuery({
    queryKey: eulaKeys.status(),
    queryFn: () => eulaService.getStatus(),
    enabled,
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 1,
  })
}

export function useAcceptEula() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (version: string) => eulaService.accept({ version }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: eulaKeys.all })
    },
  })
}
