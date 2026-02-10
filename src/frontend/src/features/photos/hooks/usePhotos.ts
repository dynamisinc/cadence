/**
 * usePhotos Hook
 *
 * React Query hook for managing photo state and operations.
 * Supports offline-first mutations with optimistic updates and queue sync.
 *
 * @module features/photos/hooks
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'react-toastify'
import { photoService } from '../services/photoService'
import { useConnectivity } from '../../../core/contexts'
import { addPendingAction } from '../../../core/offline'
import type {
  PhotoDto,
  PhotoListQuery,
  PhotoListResponse,
  UpdatePhotoRequest,
  QuickPhotoResponse,
} from '../types'

/** Query key for photos list by exercise */
export const photosQueryKey = (exerciseId: string) =>
  ['photos', 'exercise', exerciseId] as const

/**
 * Hook for managing photos for an exercise
 *
 * Provides CRUD operations with offline support and optimistic updates.
 * Automatically invalidates related queries (observations for quick photos).
 *
 * @param exerciseId - The exercise to manage photos for
 * @param query - Optional filter/pagination parameters
 *
 * @example
 * ```tsx
 * const { photos, uploadPhoto, isUploading } = usePhotos(exerciseId);
 *
 * const handleUpload = async (formData: FormData) => {
 *   await uploadPhoto(formData);
 * };
 * ```
 */
export const usePhotos = (exerciseId: string, query?: PhotoListQuery) => {
  const queryClient = useQueryClient()
  const { connectivityState, incrementPendingCount } = useConnectivity()

  // Consider offline if not fully connected
  const isEffectivelyOnline = connectivityState === 'online'

  // Query for fetching photos
  const {
    data,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: [...photosQueryKey(exerciseId), query],
    queryFn: () => photoService.getPhotos(exerciseId, query),
    enabled: !!exerciseId,
  })

  // Mutation for uploading photos
  const uploadMutation = useMutation({
    mutationFn: (formData: FormData) => photoService.uploadPhoto(exerciseId, formData),
    onSuccess: (newPhoto) => {
      queryClient.invalidateQueries({ queryKey: photosQueryKey(exerciseId) })
      toast.success('Photo uploaded')
    },
    onError: (err) => {
      toast.error(err instanceof Error ? err.message : 'Failed to upload photo')
    },
  })

  // Mutation for quick photo (upload + auto-create observation)
  const quickPhotoMutation = useMutation({
    mutationFn: (formData: FormData) => photoService.quickPhoto(exerciseId, formData),
    onSuccess: (result) => {
      // Invalidate both photos and observations queries
      queryClient.invalidateQueries({ queryKey: photosQueryKey(exerciseId) })
      queryClient.invalidateQueries({ queryKey: ['observations', 'exercise', exerciseId] })
      toast.success('Quick photo captured')
    },
    onError: (err) => {
      toast.error(err instanceof Error ? err.message : 'Failed to save quick photo')
    },
  })

  // Mutation for deleting photos with optimistic updates
  const deleteMutation = useMutation({
    mutationFn: (photoId: string) => photoService.deletePhoto(exerciseId, photoId),
    onMutate: async (deletedId) => {
      // Cancel pending queries to avoid race conditions
      await queryClient.cancelQueries({ queryKey: [...photosQueryKey(exerciseId), query] })

      // Snapshot for rollback
      const previousData = queryClient.getQueryData<PhotoListResponse>(
        [...photosQueryKey(exerciseId), query],
      )

      // Apply optimistic update - immediately remove from list
      queryClient.setQueryData<PhotoListResponse>(
        [...photosQueryKey(exerciseId), query],
        (old) => {
          if (!old) return old
          return {
            ...old,
            photos: old.photos.filter(photo => photo.id !== deletedId),
            totalCount: old.totalCount - 1,
          }
        },
      )

      return { previousData }
    },
    onSuccess: () => {
      toast.success('Photo deleted')
    },
    onError: (err, _deletedId, context) => {
      // Rollback to previous state
      if (context?.previousData) {
        queryClient.setQueryData(
          [...photosQueryKey(exerciseId), query],
          context.previousData,
        )
      }
      const message =
        err instanceof Error ? err.message : 'Failed to delete photo'
      toast.error(message)
    },
  })

  // Mutation for updating photo metadata with optimistic updates
  const updateMutation = useMutation({
    mutationFn: ({ photoId, request }: { photoId: string; request: UpdatePhotoRequest }) =>
      photoService.updatePhoto(exerciseId, photoId, request),
    onMutate: async ({ photoId, request }) => {
      // Cancel pending queries to avoid race conditions
      await queryClient.cancelQueries({ queryKey: [...photosQueryKey(exerciseId), query] })

      // Snapshot for rollback
      const previousData = queryClient.getQueryData<PhotoListResponse>(
        [...photosQueryKey(exerciseId), query],
      )

      // Apply optimistic update
      queryClient.setQueryData<PhotoListResponse>(
        [...photosQueryKey(exerciseId), query],
        (old) => {
          if (!old) return old
          return {
            ...old,
            photos: old.photos.map(photo =>
              photo.id === photoId
                ? {
                  ...photo,
                  observationId: request.observationId ?? photo.observationId,
                  displayOrder: request.displayOrder ?? photo.displayOrder,
                  updatedAt: new Date().toISOString(),
                }
                : photo,
            ),
          }
        },
      )

      return { previousData }
    },
    onSuccess: () => {
      toast.success('Photo updated')
    },
    onError: (err, _variables, context) => {
      // Rollback to previous state
      if (context?.previousData) {
        queryClient.setQueryData(
          [...photosQueryKey(exerciseId), query],
          context.previousData,
        )
      }
      const message =
        err instanceof Error ? err.message : 'Failed to update photo'
      toast.error(message)
    },
  })

  /**
   * Upload photo with offline support
   * When offline: queues action and stores photo in IndexedDB
   * When online: sends directly to API
   */
  const uploadPhoto = async (formData: FormData): Promise<PhotoDto> => {
    if (isEffectivelyOnline) {
      // Online: send directly to API
      return uploadMutation.mutateAsync(formData)
    }

    // Offline: queue action for later sync
    // Extract metadata from FormData
    const metadata = {
      capturedAt: formData.get('capturedAt') as string,
      scenarioTime: formData.get('scenarioTime') as string | null,
      latitude: formData.get('latitude') ? parseFloat(formData.get('latitude') as string) : null,
      longitude: formData.get('longitude') ? parseFloat(formData.get('longitude') as string) : null,
      locationAccuracy: formData.get('locationAccuracy') ? parseFloat(formData.get('locationAccuracy') as string) : null,
      observationId: formData.get('observationId') as string | null,
    }

    // Store photo blob in IndexedDB (converting to base64 for storage)
    const file = formData.get('photo') as Blob
    const reader = new FileReader()
    const photoData = await new Promise<string>((resolve) => {
      reader.onloadend = () => resolve(reader.result as string)
      reader.readAsDataURL(file)
    })

    const tempId = `temp-${Date.now()}-${Math.random().toString(36).slice(2)}`

    await addPendingAction({
      type: 'UPLOAD_PHOTO',
      exerciseId,
      payload: {
        photoData,
        metadata,
        tempId,
      },
    })

    incrementPendingCount()
    toast.info('Photo saved offline. Will sync when connection restores.')

    // Return optimistic photo
    return {
      id: tempId,
      exerciseId,
      observationId: metadata.observationId,
      capturedById: 'offline-user',
      capturedByName: 'You (offline)',
      fileName: 'offline-photo.jpg',
      blobUri: photoData,
      thumbnailUri: photoData,
      fileSizeBytes: file.size,
      capturedAt: metadata.capturedAt,
      scenarioTime: metadata.scenarioTime,
      latitude: metadata.latitude,
      longitude: metadata.longitude,
      locationAccuracy: metadata.locationAccuracy,
      displayOrder: 0,
      status: 'Draft',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
  }

  /**
   * Quick photo capture with offline support
   * When offline: queues action and stores photo in IndexedDB
   * When online: sends directly to API
   */
  const quickPhoto = async (formData: FormData): Promise<QuickPhotoResponse> => {
    if (isEffectivelyOnline) {
      // Online: send directly to API
      return quickPhotoMutation.mutateAsync(formData)
    }

    // Offline: queue action for later sync
    const metadata = {
      capturedAt: formData.get('capturedAt') as string,
      scenarioTime: formData.get('scenarioTime') as string | null,
      latitude: formData.get('latitude') ? parseFloat(formData.get('latitude') as string) : null,
      longitude: formData.get('longitude') ? parseFloat(formData.get('longitude') as string) : null,
      locationAccuracy: formData.get('locationAccuracy') ? parseFloat(formData.get('locationAccuracy') as string) : null,
    }

    const file = formData.get('photo') as Blob
    const reader = new FileReader()
    const photoData = await new Promise<string>((resolve) => {
      reader.onloadend = () => resolve(reader.result as string)
      reader.readAsDataURL(file)
    })

    const tempPhotoId = `temp-photo-${Date.now()}-${Math.random().toString(36).slice(2)}`
    const tempObsId = `temp-obs-${Date.now()}-${Math.random().toString(36).slice(2)}`

    await addPendingAction({
      type: 'QUICK_PHOTO',
      exerciseId,
      payload: {
        photoData,
        metadata,
        tempPhotoId,
        tempObsId,
      },
    })

    incrementPendingCount()
    toast.info('Quick photo saved offline. Will sync when connection restores.')

    // Return optimistic response
    return {
      photo: {
        id: tempPhotoId,
        exerciseId,
        observationId: tempObsId,
        capturedById: 'offline-user',
        capturedByName: 'You (offline)',
        fileName: 'offline-photo.jpg',
        blobUri: photoData,
        thumbnailUri: photoData,
        fileSizeBytes: file.size,
        capturedAt: metadata.capturedAt,
        scenarioTime: metadata.scenarioTime,
        latitude: metadata.latitude,
        longitude: metadata.longitude,
        locationAccuracy: metadata.locationAccuracy,
        displayOrder: 0,
        status: 'Draft',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
      observationId: tempObsId,
    }
  }

  /**
   * Delete photo with offline support
   * When offline: queues action and applies optimistic update
   * When online: sends directly to API
   */
  const deletePhoto = async (photoId: string): Promise<void> => {
    if (isEffectivelyOnline) {
      // Online: send directly to API
      await deleteMutation.mutateAsync(photoId)
      return
    }

    // Offline: queue action for later sync
    await addPendingAction({
      type: 'DELETE_PHOTO',
      exerciseId,
      payload: {
        photoId,
      },
    })

    // Apply optimistic update (remove from list)
    queryClient.setQueryData<PhotoListResponse>(
      [...photosQueryKey(exerciseId), query],
      (old) => {
        if (!old) return old
        return {
          ...old,
          photos: old.photos.filter(photo => photo.id !== photoId),
          totalCount: old.totalCount - 1,
        }
      },
    )

    incrementPendingCount()
    toast.info('Deletion queued. Will sync when connection restores.')
  }

  /**
   * Update photo metadata with offline support
   * When offline: queues action and applies optimistic update
   * When online: sends directly to API
   */
  const updatePhoto = async (
    photoId: string,
    request: UpdatePhotoRequest,
  ): Promise<PhotoDto> => {
    if (isEffectivelyOnline) {
      // Online: send directly to API
      return updateMutation.mutateAsync({ photoId, request })
    }

    // Offline: queue action and apply optimistic update
    const currentData = queryClient.getQueryData<PhotoListResponse>(
      [...photosQueryKey(exerciseId), query],
    )
    const existingPhoto = currentData?.photos.find(p => p.id === photoId)

    if (!existingPhoto) {
      throw new Error('Photo not found')
    }

    const optimisticPhoto: PhotoDto = {
      ...existingPhoto,
      observationId: request.observationId ?? existingPhoto.observationId,
      displayOrder: request.displayOrder ?? existingPhoto.displayOrder,
      updatedAt: new Date().toISOString(),
    }

    // Queue the action for later sync
    await addPendingAction({
      type: 'UPDATE_PHOTO',
      exerciseId,
      payload: {
        photoId,
        changes: request,
      },
    })

    // Apply optimistic update
    queryClient.setQueryData<PhotoListResponse>(
      [...photosQueryKey(exerciseId), query],
      (old) => {
        if (!old) return old
        return {
          ...old,
          photos: old.photos.map(photo =>
            photo.id === photoId ? optimisticPhoto : photo,
          ),
        }
      },
    )

    incrementPendingCount()
    toast.info('Changes saved offline. Will sync when connection restores.')

    return optimisticPhoto
  }

  return {
    photos: data?.photos ?? [],
    totalCount: data?.totalCount ?? 0,
    page: data?.page ?? 1,
    pageSize: data?.pageSize ?? 20,
    isLoading,
    error: error ? (error instanceof Error ? error.message : 'Failed to load photos') : null,
    refetch,
    uploadPhoto,
    quickPhoto,
    deletePhoto,
    updatePhoto,
    isUploading: uploadMutation.isPending,
    isDeleting: deleteMutation.isPending,
  }
}

export default usePhotos
