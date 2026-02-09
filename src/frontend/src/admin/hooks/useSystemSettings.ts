import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { systemSettingsService } from '../services/systemSettingsService'
import type { UpdateSystemSettingsRequest } from '../types/systemSettings'

export const systemSettingsKeys = {
  all: ['system-settings'] as const,
  settings: () => [...systemSettingsKeys.all, 'current'] as const,
}

export function useSystemSettings() {
  return useQuery({
    queryKey: systemSettingsKeys.settings(),
    queryFn: () => systemSettingsService.getSettings(),
  })
}

export function useUpdateSystemSettings() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: UpdateSystemSettingsRequest) =>
      systemSettingsService.updateSettings(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: systemSettingsKeys.all })
    },
  })
}
