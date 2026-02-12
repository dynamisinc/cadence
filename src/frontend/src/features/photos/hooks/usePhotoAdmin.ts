/**
 * usePhotoAdmin Hook
 *
 * React Query hook for managing soft-deleted photos (trash view).
 * Provides restore and permanent delete operations.
 *
 * @module features/photos/hooks
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { notify } from '@/shared/utils/notify'
import { photoService } from '../services/photoService'
import { photosQueryKey } from './usePhotos'
import type { DeletedPhotoDto } from '../types'

/** Query key for deleted photos list by exercise */
export const deletedPhotosQueryKey = (exerciseId: string) =>
  ['photos', 'deleted', exerciseId] as const

/**
 * Hook for managing soft-deleted photos (trash/admin view)
 */
export const usePhotoAdmin = (exerciseId: string) => {
  const queryClient = useQueryClient()

  // Fetch deleted photos
  const {
    data: deletedPhotos = [],
    isLoading,
    error,
  } = useQuery<DeletedPhotoDto[]>({
    queryKey: deletedPhotosQueryKey(exerciseId),
    queryFn: () => photoService.getDeletedPhotos(exerciseId),
    enabled: !!exerciseId,
  })

  // Restore mutation
  const restoreMutation = useMutation({
    mutationFn: (photoId: string) => photoService.restorePhoto(exerciseId, photoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: deletedPhotosQueryKey(exerciseId) })
      queryClient.invalidateQueries({ queryKey: photosQueryKey(exerciseId) })
      notify.success('Photo restored')
    },
    onError: (err: Error) => {
      notify.error(err.message || 'Failed to restore photo')
    },
  })

  // Permanent delete mutation
  const permanentDeleteMutation = useMutation({
    mutationFn: (photoId: string) => photoService.permanentDeletePhoto(exerciseId, photoId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: deletedPhotosQueryKey(exerciseId) })
      notify.success('Photo permanently deleted')
    },
    onError: (err: Error) => {
      notify.error(err.message || 'Failed to permanently delete photo')
    },
  })

  return {
    deletedPhotos,
    isLoading,
    error: error ? (error as Error).message : null,
    restorePhoto: (photoId: string) => restoreMutation.mutateAsync(photoId),
    isRestoring: restoreMutation.isPending,
    permanentDeletePhoto: (photoId: string) => permanentDeleteMutation.mutateAsync(photoId),
    isPermanentDeleting: permanentDeleteMutation.isPending,
  }
}
